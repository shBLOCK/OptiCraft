using System.Collections.Generic;
using UnityEngine;

namespace core {
    public abstract class MirrorLikeDevice : Device {
        [SerializeField] protected AxisDirection directionA;
        [SerializeField] protected AxisDirection directionB;

        protected abstract void onBeamHitMirror(Beam beam, AxisDirection reflectDir, bool isFrontSide);

        public override void onBeamHit(Beam beam) {
            if (beam.direction == directionA.opposite()) {
                onBeamHitMirror(beam, directionB, true);
            } else if (beam.direction == directionB.opposite()) {
                onBeamHitMirror(beam, directionA, true);
            } else if (beam.direction == directionA) {
                onBeamHitMirror(beam, directionB.opposite(), false);
            } else if (beam.direction == directionB) {
                onBeamHitMirror(beam, directionA.opposite(), false);
            }
        }

        public override void onBeamHitEdge(Beam beam) {
            if (beam.direction.axis() != directionA.axis() && beam.direction.axis() != directionB.axis()) {
                beam.consume();
            }
        }

        private void Update() {
            //TODO: tmp
            updateRendering();
        }

        private void OnValidate() {
            updateRendering();
        }

        private void updateRendering() {
            transform.localRotation =
                Quaternion.FromToRotation(Vector3.forward, directionA.float3() + directionB.float3());
        }
    }
}