using System;
using core;
using UnityEngine;
using utils;

namespace device {
    public class NegatorDevice : SimpleGridDevice {
        private Axis axis;
        private readonly BeamIOPair[] beams = CollectionUtils.newFilledArray(6, BeamIOPair.INVALID);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (beam.direction.axis() == axis) {
                beams[(byte)beam.direction.opposite()] = new BeamIOPair(
                    beam.id,
                    space.emitBeam(new Beam(gridPos, beam.direction, beam.image.modulated(-1f))).id
                );
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            space.stopEmitBeam(beams[(byte)beam.direction.opposite()].output);
        }

        public override void onBeamHitEdge(ref Beam beam) {
            if (beam.direction.axis() != axis) {
                space.consumeBeam(ref beam);
            }
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

        private static readonly OCDeviceType<NegatorDevice> _TYPE = new("negator");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}