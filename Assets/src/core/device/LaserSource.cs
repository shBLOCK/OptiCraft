using System;
using Unity.Mathematics;
using UnityEngine;

namespace core {
    [RequireComponent(typeof(GridPosition))]
    public sealed class LaserSource : Device, ITickingDevice {
        [SerializeField] private AxisDirection direction = AxisDirection.PosZ;

        private Beam beam = null;

        public override void reset() {
            beam = null;
        }

        public void tick() {
            if (beam == null) {
                beam = new Beam.Laser(space, direction, gridPos.offset(direction), 10000).emit();
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
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, direction.float3());
        }
    }
}