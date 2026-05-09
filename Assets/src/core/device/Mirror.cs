using System;
using System.Collections.Generic;
using UnityEngine;

namespace core {
    [RequireComponent(typeof(GridPosition))]
    public class Mirror : Device {
        [SerializeField] private AxisDirection directionA;
        [SerializeField] private AxisDirection directionB;
        [SerializeField] private bool doubleSided = false;

        private Dictionary<Beam, Beam> activeBeamIOs = new();

        private void Reset() {
            activeBeamIOs.Clear();
        }

        public override void onBeamHit(Beam beam) {
            AxisDirection? outDir = null;
            if (beam.direction == directionA.opposite()) {
                outDir = directionB;
            } else if (beam.direction == directionB.opposite()) {
                outDir = directionA;
            } else if (doubleSided) {
                if (beam.direction == directionA) {
                    outDir = directionB.opposite();
                } else if (beam.direction == directionB) {
                    outDir = directionA.opposite();
                }
            }

            if (outDir != null) {
                beam.consume();
                activeBeamIOs[beam] = beam.instantiate(space, outDir.Value, gridPos).emit();
            }
        }

        public override void onBeamHitEdge(Beam beam) {
            if (beam.direction.axis() != directionA.axis() && beam.direction.axis() != directionB.axis()) {
                beam.consume();
            }
        }

        public override void onBeamEnd(Beam beam) {
            if (activeBeamIOs.Remove(beam, out var outBeam)) {
                outBeam.stopEmit();
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