using System;
using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;
using Vertx.Debugging;

namespace device {
    public class ChannelBroadcastDevice : SimpleGridDevice {
        private Axis axis;
        private ByteEnumMap<Sign, BeamIOPair> beams = new(2, BeamIOPair.INVALID_IDS);
        
        private static ComputeShader CS;
        private static int CSK;
        private static BeamImageShaderUniform uInputImage;
        private static int uOutputImage;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_CS() {
            CS = Resources.Load<ComputeShader>("device/channel_broadcast");
            CSK = CS.FindKernel("ChannelBroadcastMain");
            uInputImage = new BeamImageShaderUniform("uInputImage");
            uOutputImage = Shader.PropertyToID("uOutputImage");
        }

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (beam.direction.axis() == axis) {
                BeamImage outputImage;
                if (beam.image.isSinglePixel) {
                    outputImage = BeamImage.singlePixel(math.cmax(beam.image.modulation));
                } else {
                    var bidm = space.simulator.beamImageDataManager;
                    var size = beam.image.size;
                    var imageData = bidm.addNew(size);
                    outputImage = new BeamImage(imageData.id, size,
                        BeamImage.Orientation.PosXPosY, 0, 1f, 0f);
                    var cmds = space.simulator.cmds;
                    beam.image.setToShader(bidm, cmds, CS, CSK, uInputImage);
                    cmds.SetComputeTextureParam(CS, CSK, uOutputImage, imageData._tmp_getRT());
                    cmds.dispatchCompute2D(CS, CSK, size);
                }
                beams[beam.direction.sign()] = new BeamIOPair(
                    beam.id,
                    space.emitBeam(new Beam(gridPos, beam.direction, outputImage)).id
                );
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            space.stopEmitBeam(beams[beam.direction.sign()].output);
        }

        public override void onBeamHitEdge(ref Beam beam) {
            if (beam.direction.axis() != axis) {
                space.consumeBeam(ref beam);
            }
        }

        public override void onRemoved() {
            for (byte i = 0; i < 2; i++) {
                var pair = beams[(Sign)i];
                if (pair != BeamIOPair.INVALID_IDS) {
                    beams[(Sign)i] = BeamIOPair.INVALID_IDS;
                    space.stopConsumeBeam(pair.input);
                    space.stopEmitBeam(pair.output);
                }
            }

            base.onRemoved();
        }

        public override void reset() {
            base.reset();
            beams.fill(BeamIOPair.INVALID_IDS);
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["axis"] = axis.ToString();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            axis = Enum.Parse<Axis>(data["axis"].GetValue<string>());
        }

        public override void userActionRotate(AxisDirection axis) {
            this.axis = this.axis.rotate(axis.axis());
        }

        public override void render(CommandBuffer cmds) {
            base.render(cmds);
            D.raw(new Bounds(new float3(gridPos), axis.float3(1.5f) + 0.1f), Color.white);
        }

        private static readonly OCDeviceType<ChannelBroadcastDevice> _TYPE = new("channel_broadcast");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}