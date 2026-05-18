using System;
using core;
using UnityEngine;
using utils;

namespace device {
    public class MirrorDevice : MirrorLikeDevice {
        private bool doubleSided = true;
        private readonly BeamIOPair[] beams = CollectionUtils.newFilledArray(6, BeamIOPair.INVALID);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (mirrorDir.reflect(beam.direction.opposite(), out var reflectDir, out var isFrontSide)) {
                if (isFrontSide || doubleSided) {
                    beams[(byte)beam.direction.opposite()] = new BeamIOPair(
                        beam.id,
                        space.emitBeam(new Beam(gridPos, reflectDir, beam.image)).id
                    );
                }
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            var io = beams[(byte)beam.direction.opposite()];
            if (io.output != Beam.INVALID_ID) space.stopEmitBeam(io.output);
            beams[(byte)beam.direction.opposite()] = BeamIOPair.INVALID;
        }
        
        public override void onRemoved() {
            for (byte i = 0; i < 6; i++) {
                var pair = beams[i];
                if (pair != BeamIOPair.INVALID) {
                    beams[i] = BeamIOPair.INVALID;
                    space.stopConsumeBeam(pair.input);
                    space.stopEmitBeam(pair.output);
                }
            }
            base.onRemoved();
        }

        public override void reset() {
            base.reset();
            Array.Fill(beams, BeamIOPair.INVALID);
        }

        private static readonly OCDeviceType<MirrorDevice> _TYPE = new("mirror");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}