using core.beam;

namespace device {
    public class ImageSensorDevice : SimpleGridDevice {
        public class ImageChangedEvent { }

        public override void onBeamHit(ref Beam beam) {
            base.onBeamHit(ref beam);
        }

        public override void tick() {
            base.tick();
        }

        private static readonly OCDeviceType<ImageSensorDevice> _TYPE = new("image_sensor");
        public override OCDeviceType TYPE => _TYPE;
    }
}