using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;
using Vertx.Debugging;

namespace device {
    public class ModulatorDevice : SimpleGridDevice {
        private struct ModulatingState {
            public ushort input;
            public ushort output;
            public bool dirty;

            public ModulatingState(byte dummy) {
                input = output = Beam.INVALID_ID;
                dirty = false;
            }
        }

        public Axis modulatingAxis = Axis.X; // TODO: make this flippable (when implementing beam orientation)
        public Axis modulatorAxis = Axis.Z;
        private ByteEnumMap<Sign, ModulatingState> modulatingStates = new(2, new ModulatingState(0));
        private ByteEnumMap<Sign, ushort> modulatorBeams = new(2, Beam.INVALID_ID);
        private bool modulatorDirty = false;

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            handleInputBeam(ref beam, beam.id);
        }

        public override void onBeamEnd(ref Beam beam) {
            handleInputBeam(ref beam, Beam.INVALID_ID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void handleInputBeam(ref Beam beam, ushort id) {
            if (beam.direction.axis() == modulatingAxis) {
                ref var state = ref modulatingStates[beam.direction.sign()];
                state.input = id;
                state.dirty = true;
            } else if (beam.direction.axis() == modulatorAxis) {
                modulatorBeams[beam.direction.sign()] = id;
                modulatorDirty = true;
            }
        }

        private static ComputeShader CS;
        private static int CSK;
        private static LocalKeyword MODULATOR_B;
        private static BeamImageShaderUniform uModulatingImage;
        private static BeamImageShaderUniform uModulatorAImage;
        private static BeamImageShaderUniform uModulatorBImage;
        private static int uOutputImage;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_CS() {
            CS = Resources.Load<ComputeShader>("compute/device/modulator");
            CSK = CS.FindKernel("ModulatorMain");
            MODULATOR_B = CS.keywordSpace.FindKeyword("MODULATOR_B");
            CS.EnableKeyword(MODULATOR_B);
            uModulatingImage = new BeamImageShaderUniform("uModulatingImage");
            uModulatorAImage = new BeamImageShaderUniform("uModulatorAImage");
            uModulatorBImage = new BeamImageShaderUniform("uModulatorBImage");
            uOutputImage = Shader.PropertyToID("uOutputImage");
        }

        public override void tick() {
            if (modulatorDirty || modulatingStates[Sign.Neg].dirty) {
                updateOutput(Sign.Neg);
                modulatingStates[Sign.Neg].dirty = false;
            }

            if (modulatorDirty || modulatingStates[Sign.Pos].dirty) {
                updateOutput(Sign.Pos);
                modulatingStates[Sign.Pos].dirty = false;
            }

            modulatorDirty = false;
        }

        private void updateOutput(Sign sign) {
            ref var modulatingState = ref modulatingStates[sign];
            if (modulatingState.output != Beam.INVALID_ID) {
                space.stopEmitBeam(modulatingState.output);
                modulatingState.output = Beam.INVALID_ID;
            }

            if (modulatingState.input == Beam.INVALID_ID) return;

            ref var modulatingBeam = ref space.getBeam(modulatingState.input);
            var modulatorABeam = modulatorBeams[Sign.Neg];
            var modulatorBBeam = modulatorBeams[Sign.Pos];
            if (modulatorABeam == Beam.INVALID_ID && modulatorBBeam == Beam.INVALID_ID) {
                modulatingState.output = space.emitBeam(
                    new Beam(
                        gridPos, modulatingAxis.withSign(sign),
                        modulatingBeam.image
                    )
                ).id;
                return;
            }

            if (modulatorBBeam != Beam.INVALID_ID) {
                (modulatorABeam, modulatorBBeam) = (modulatorBBeam, modulatorABeam);
            }

            BeamImage outputImage;
            if (modulatingBeam.image.isSinglePixel) {
                var a = space.getBeam(modulatorABeam).image.modulation;
                var b = modulatorBBeam != Beam.INVALID_ID ? space.getBeam(modulatorBBeam).image.modulation : 0f;
                //TODO: deal with the case where modulator beams aren't single pixel
                outputImage = BeamImage.singlePixel(modulatingBeam.image.modulation * (a + b));
            } else {
                var bidm = space.simulator.beamImageDataManager;
                var cmds = space.simulator.cmds;
                cmds.SetKeyword(CS, MODULATOR_B, modulatorBBeam != Beam.INVALID_ID);
                modulatingBeam.image.setToShader(bidm, cmds, CS, CSK, uModulatingImage);
                space.getBeam(modulatorABeam).image.setToShader(bidm, cmds, CS, CSK, uModulatorAImage);
                if (modulatorBBeam != Beam.INVALID_ID)
                    space.getBeam(modulatorBBeam).image.setToShader(bidm, cmds, CS, CSK, uModulatorBImage);
                var size = modulatingBeam.image.size;
                var imageData = bidm.addNew(size);
                outputImage = new BeamImage(imageData.id, size, BeamImage.Orientation.PosXPosY, 0, 1f, 0f);
                cmds.SetComputeTextureParam(CS, CSK, uOutputImage, imageData._tmp_getRT());
                cmds.dispatchCompute2D(CS, CSK, size);
            }

            modulatingState.output = space.emitBeam(
                new Beam(gridPos, modulatingAxis.withSign(sign), outputImage)
            ).id;
        }

        public override void render() {
            base.render();
            D.raw(new Bounds(new float3(gridPos), modulatingAxis.float3(1.5f) + 0.1f), Color.green);
            D.raw(new Bounds(new float3(gridPos), modulatorAxis.float3(1.5f) + 0.1f), Color.red);
        }

        public override void reset() {
            base.reset();
            modulatingStates.fill(new ModulatingState(0));
            modulatorBeams.fill(Beam.INVALID_ID);
            modulatorDirty = false;
        }

        public override void onRemoved() {
            for (byte i = 0; i < 2; i++) {
                var beam = modulatorBeams[(Sign)i];
                if (beam != Beam.INVALID_ID) space.stopConsumeBeam(beam);
            }

            for (byte i = 0; i < 2; i++) {
                ref var state = ref modulatingStates[(Sign)i];
                if (state.input != Beam.INVALID_ID) space.stopConsumeBeam(state.input);
                if (state.output != Beam.INVALID_ID) space.stopEmitBeam(state.output);
            }

            reset();
            base.onRemoved();
        }

        public override void userActionRotate(AxisDirection axis) {
            base.userActionRotate(axis);
            modulatingAxis = modulatingAxis.rotate(axis.axis());
            modulatorAxis = modulatorAxis.rotate(axis.axis());
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["modulatingAxis"] = modulatingAxis.ToString();
            data["modulatorAxis"] = modulatorAxis.ToString();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            modulatingAxis = Enum.Parse<Axis>(data["modulatingAxis"].ToString());
            modulatorAxis = Enum.Parse<Axis>(data["modulatorAxis"].ToString());
        }

        private static readonly OCDeviceType<ModulatorDevice> _TYPE = new("modulator");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}