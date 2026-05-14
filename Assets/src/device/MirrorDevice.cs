using core;
using UnityEngine;
using utils;

namespace device {
    public class MirrorDevice : MirrorLikeDevice {
        private bool doubleSided = true;
        private readonly AxisDirectionMap<BeamIOPair> beams = new(BeamIOPair.INVALID);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(beam.id);
            if (mirrorDir.reflect(beam.direction.opposite(), out var reflectDir, out var isFrontSide)) {
                if (isFrontSide || doubleSided) {
                    beams[beam.direction.opposite()] = new BeamIOPair(
                        beam.id,
                        space.emitBeam(new Beam(reflectDir, gridPos, beam.image)).id
                    );
                }
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            space.stopEmitBeam(beams[beam.direction.opposite()].output);
        }
        
        public override void onRemoved() {
            for (byte i = 0; i < 6; i++) {
                var pair = beams[(AxisDirection)i];
                if (pair != BeamIOPair.INVALID) {
                    beams[(AxisDirection)i] = BeamIOPair.INVALID;
                    space.stopConsumeBeam(pair.input);
                    space.stopEmitBeam(pair.output);
                }
            }
            base.onRemoved();
        }

        public override void reset() {
            base.reset();
            beams.fill(BeamIOPair.INVALID);
        }

        private static readonly OCDeviceType<MirrorDevice> _TYPE = new("mirror");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}