using System;
using core;
using Unity.Mathematics;
using UnityEngine;
using utils;

namespace device {
    public class BeamSplitterDevice : MirrorLikeDevice {
        private struct State {
            public ushort frontInput;
            public BeamImage oldFrontInputImage;
            public ushort backInput;
            public BeamImage oldBackInputImage;
            public ushort frontOutput;
            public ushort backOutput;
            public bool frontDirty;
            public bool backDirty;

            public State(ushort value) {
                frontInput = value;
                oldFrontInputImage = BeamImage.DUMMY;
                backInput = value;
                oldBackInputImage = BeamImage.DUMMY;
                frontOutput = value;
                backOutput = value;
                frontDirty = backDirty = false;
            }
        }

        private readonly State[] stateOnFrontInputAxis = CollectionUtils.newFilledArray(3, new State(Beam.INVALID_ID));

        private bool dirty = false;

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            updateInputBeam(beam.direction.opposite(), beam.id, beam.image);
        }

        public override void onBeamEnd(ref Beam beam) {
            updateInputBeam(beam.direction.opposite(), Beam.INVALID_ID, BeamImage.DUMMY);
        }

        private void updateInputBeam(AxisDirection dir, ushort beamId, in BeamImage beamImage) {
            if (mirrorDir.reflect(dir, out var reflectDir, out var isFrontSide)) {
                var frontDir = isFrontSide ? dir : reflectDir.opposite();
                ref var state = ref stateOnFrontInputAxis[(byte)frontDir.axis()];
                dirty = true;
                if (isFrontSide) {
                    state.frontDirty = !state.oldFrontInputImage.isEqualConservative(beamImage);
                    state.frontInput = beamId;
                } else {
                    state.backDirty = !state.oldBackInputImage.isEqualConservative(beamImage);
                    state.backInput = beamId;
                }
            }
        }

        private static ComputeShader CS;
        private static int CSK;
        private static readonly BeamImageShaderUniform uInputAImage = new("uInputAImage");
        private static readonly BeamImageShaderUniform uInputBImage = new("uInputBImage");
        private static readonly int uOutputImage = Shader.PropertyToID("uOutputImage");

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_CS() {
            CS = Resources.Load<ComputeShader>("device/beam_splitter");
            CSK = CS.FindKernel("BeamSplitterMain");
        }

        public override void tick() {
            if (dirty) {
                dirty = false;
                for (int i = 0; i < 3; i++) {
                    var axis = (Axis)i;
                    ref var state = ref stateOnFrontInputAxis[(byte)axis];
                    if (state.frontDirty || state.backDirty) {
                        state.frontDirty = state.backDirty = false;

                        if (state.frontOutput != Beam.INVALID_ID) space.stopEmitBeam(state.frontOutput);
                        if (state.backOutput != Beam.INVALID_ID) space.stopEmitBeam(state.backOutput);

                        var bidm = space.simulator.beamImageDataManager;
                        BeamImage outputImage = default;
                        var hasOutput = false;
                        if (state.frontInput != Beam.INVALID_ID && state.backInput != Beam.INVALID_ID) {
                            hasOutput = true;
                            ref var frontBeam = ref space.getBeam(state.frontInput);
                            ref var backBeam = ref space.getBeam(state.backInput);
                            if (frontBeam.image.isSinglePixel && backBeam.image.isSinglePixel) {
                                outputImage = BeamImage.singlePixel(frontBeam.image.modulation + backBeam.image.modulation);
                            } else {
                                var size = math.max(frontBeam.image.size, backBeam.image.size);
                                outputImage = new BeamImage(
                                    bidm.addNew(size).id,
                                    size,
                                    BeamImage.Orientation.PosXPosY,
                                    0,
                                    1f, 0f
                                );
                                var cmds = space.simulator.cmds;
                                frontBeam.image.setToShader(bidm, cmds, CS, CSK, uInputAImage);
                                backBeam.image.setToShader(bidm, cmds, CS, CSK, uInputBImage);
                                cmds.SetComputeTextureParam(CS, CSK, uOutputImage,
                                    outputImage.getData(bidm)._tmp_getRT());
                                cmds.dispatchCompute2D(CS, CSK, size);
                            }
                        } else {
                            if (state.frontInput != Beam.INVALID_ID) {
                                outputImage = space.getBeam(state.frontInput).image;
                                hasOutput = true;
                            } else if (state.backInput != Beam.INVALID_ID) {
                                outputImage = space.getBeam(state.backInput).image;
                                hasOutput = true;
                            }
                        }

                        state.oldFrontInputImage.decRef(bidm);
                        state.oldBackInputImage.decRef(bidm);
                        if (hasOutput) {
                            var (inDir, reflectDir) = mirrorDir.getDirOnAxisAndOtherDir(axis);
                            var frontOutput = space.emitBeam(new Beam(gridPos, reflectDir, outputImage));
                            var backOutput = space.emitBeam(new Beam(gridPos, inDir.opposite(), outputImage));
                            state.frontOutput = frontOutput.id;
                            frontOutput.image.incRef(bidm);
                            state.oldFrontInputImage = frontOutput.image;
                            state.backOutput = backOutput.id;
                            backOutput.image.incRef(bidm);
                            state.oldBackInputImage = backOutput.image;
                        } else {
                            state.frontOutput = Beam.INVALID_ID;
                            state.oldFrontInputImage = BeamImage.DUMMY;
                            state.backOutput = Beam.INVALID_ID;
                            state.oldBackInputImage = BeamImage.DUMMY;
                        }
                    }
                }
            }
        }

        public override void reset() {
            Array.Fill(stateOnFrontInputAxis, new State(Beam.INVALID_ID));
        }

        public override void onRemoved() {
            for (int i = 0; i < 3; i++) {
                ref var state = ref stateOnFrontInputAxis[i];
                if (state.frontInput != Beam.INVALID_ID) space.stopConsumeBeam(state.frontInput);
                if (state.backInput != Beam.INVALID_ID) space.stopConsumeBeam(state.backInput);
                if (state.frontOutput != Beam.INVALID_ID) space.stopEmitBeam(state.frontOutput);
                if (state.backOutput != Beam.INVALID_ID) space.stopEmitBeam(state.backOutput);
                state.oldFrontInputImage.decRef(space.simulator.beamImageDataManager);
                state.oldBackInputImage.decRef(space.simulator.beamImageDataManager);
            }
            Array.Fill(stateOnFrontInputAxis, new State(Beam.INVALID_ID));

            base.onRemoved();
        }

        private static readonly OCDeviceType<BeamSplitterDevice> _TYPE = new("beam_splitter");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}