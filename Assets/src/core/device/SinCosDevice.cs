using Unity.Mathematics;
using UnityEngine;

namespace core {
    [RequireComponent(typeof(GridPosition))]
    public class SinCosDevice : Device {
        [SerializeField] private ComputeShader cs;
        
        [SerializeField] private AxisDirection thetaDirection;
        [SerializeField] private AxisDirection sinDirection;

        private bool dirty = false;
        private Beam thetaBeam;
        private Beam sinBeam;
        private Beam cosBeam;

        public override void reset() {
            dirty = false;
            thetaBeam = null;
            sinBeam = null;
            cosBeam = null;
        }

        public override void onBeamHit(Beam beam) {
            dirty = true;
            if (beam.direction == thetaDirection.opposite()) {
                beam.consume();
                thetaBeam = beam;
            }
            //TODO: inverse
        }

        public override void postTick() {
            if (dirty) {
                if (sinBeam != null) sinBeam.stopEmit();
                if (cosBeam != null) cosBeam.stopEmit();
            }
            if (thetaBeam == null) return;
            int2 size = thetaBeam.image.size;
            BeamImageData sinData = new BeamImageData(size);
            BeamImageData cosData = new BeamImageData(size);
            thetaBeam.image._tmp_setToCS(cs, 0, "uInput");
            cs.SetTexture(0, "uSinOutput", sinData._tmp_getRT());
            cs.SetTexture(0, "uCosOutput", cosData._tmp_getRT());
            cs.Dispatch(0, size.x / 16, size.y / 16, 1);
            new Beam(space, sinDirection, gridPos, new BeamImage(sinData, 1f)).emit();
            new Beam(space, sinDirection.opposite(), gridPos, new BeamImage(cosData, 1f)).emit();
        }
    }
}