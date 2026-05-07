using Unity.Mathematics;
using UnityEngine;

namespace core {
    [RequireComponent(typeof(GridPosition))]
    public sealed class LaserSource : Device, ITickingDevice {
        private AxisDirection direction = AxisDirection.PosZ;
        
        private Beam beam = null;

        public override void reset() {
            beam = null;
        }

        public void tick() {
            if (beam == null) {
                beam = new Beam.Laser(space, direction, gridPos.offset(direction), 10000).emit();
                print("Emit beam");
            }
        }
    }
}