using core;
using core.beam;
using Unity.Mathematics;
using UnityEngine;
using utils;

namespace device {
    public sealed class ImageProjectorDevice : SimpleGridDevice {
        public sealed record UpdateImageEvent(Texture2D image) : Simulator.Event;

        public BeamImage? image;
        private bool dirty = false;

        private void onEvent(Simulator.Event @event) {
            if (@event is UpdateImageEvent updateImageEvent) {
                image?.decRef(space.simulator.beamImageDataManager);
                dirty = true;
                var imageData = space.simulator.beamImageDataManager.addNew(updateImageEvent.image.size().asuint());
                imageData.blitFromTexture(updateImageEvent.image);
                //TODO: custom orientation
                image = new BeamImage(imageData.id, imageData.size, Orientation2D.PosXPosY, 0, 1f, 0f);
                image.Value.incRef(space.simulator.beamImageDataManager);
            }
        }

        public override void onAdded(SimSpace simSpace) {
            base.onAdded(simSpace);
            space.simulator.onEvent += onEvent;
        }

        public override void onRemoved() {
            space.simulator.onEvent -= onEvent;
            base.onRemoved();
        }

        private static readonly OCDeviceType<ImageProjectorDevice> _TYPE = new("image_projector");
        public override OCDeviceType TYPE => _TYPE;
    }
}