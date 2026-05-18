using System.Text.Json.Nodes;
using core;
using UnityEngine;
using utils;

namespace device {
    public class MirrorDevice : MirrorLikeDevice {
        private bool doubleSided = true;
        private ByteEnumMap<AxisDirection, BeamIOPair> beams = new(6, BeamIOPair.INVALID_IDS);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (mirrorDir.reflect(beam.direction.opposite(), out var reflectDir, out var isFrontSide)) {
                if (isFrontSide || doubleSided) {
                    beams[beam.direction.opposite()] = new BeamIOPair(
                        beam.id,
                        space.emitBeam(new Beam(gridPos, reflectDir, beam.image)).id
                    );
                }
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            var io = beams[beam.direction.opposite()];
            if (io.output != Beam.INVALID_ID) space.stopEmitBeam(io.output);
            beams[beam.direction.opposite()] = BeamIOPair.INVALID_IDS;
        }

        public override void onRemoved() {
            for (byte i = 0; i < 6; i++) {
                var direction = (AxisDirection)i;
                var pair = beams[direction];
                if (pair != BeamIOPair.INVALID_IDS) {
                    beams[direction] = BeamIOPair.INVALID_IDS;
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
            data["doubleSided"] = doubleSided;
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            doubleSided = data["doubleSided"].GetValue<bool>();
        }

        private static readonly OCDeviceType<MirrorDevice> _TYPE = new("mirror");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}