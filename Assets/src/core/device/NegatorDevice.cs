using System;
using System.Collections.Generic;
using UnityEngine;

namespace core {
    [RequireComponent(typeof(GridPosition))]
    public class NegatorDevice : Device {
        [SerializeField] private Axis axis;
        
        public override void onBeamHit(Beam beam) {
            beam.consume();
            if (beam.direction.axis() == axis) {
                beam.userData_Beam =
                    new Beam(space, beam.direction, gridPos, beam.image.withTint(-beam.image.tint)).emit();
            }
        }

        public override void onBeamEnd(Beam beam) {
            if (beam.userData_Beam != null) {
                beam.userData_Beam.stopEmit();
            }
        }

        public override void onBeamHitEdge(Beam beam) {
            if (beam.direction.axis() != axis) {
                beam.consume();
            }
        }

        private void OnValidate() {
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, axis.float3());
        }
    }
}