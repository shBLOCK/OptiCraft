using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;

namespace device {
    public sealed class BeamSourceDevice : SimpleGridDevice {
        public Texture2D image = null;
        public float4 color = 1f;
        private AxisDirection direction = AxisDirection.PosZ;

        private ushort beam = Beam.INVALID_ID;

        public override void reset() {
            beam = Beam.INVALID_ID;
        }

        public override void tick() {
            if (beam == Beam.INVALID_ID) {
                BeamImage beamImage;
                if (!image) {
                    beamImage = BeamImage.singlePixel(color);
                } else {
                    var data = space.simulator.beamImageDataManager.addNew((uint2)image.size());
                    data.blitFromTexture(image);
                    beamImage = new BeamImage(data.id, data.size, BeamImage.Orientation.PosXPosY, 0, color, 0f);
                }

                beam = space.emitBeam(new Beam(gridPos.offset(direction), direction, beamImage)).id;
            }
        }

        public override void onRemoved() {
            if (beam != Beam.INVALID_ID) {
                space.stopEmitBeam(beam);
                beam = Beam.INVALID_ID;
            }

            base.onRemoved();
        }

        private static Mesh MESH;
        private static Material MATERIAL;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_MESH() {
            MESH = Resources.Load<Mesh>("BeamSourceDevice");
            MATERIAL = Resources.Load<Material>("Green");
        }

        public override void render(CommandBuffer cmds) {
            var matrix = Matrix4x4.TRS(
                new float3(gridPos) - new float3(0f, 0.5f, 0f),
                Quaternion.FromToRotation(Vector3.forward, direction.float3()),
                Vector3.one
            );
            Graphics.RenderMesh(new RenderParams(MATERIAL), MESH, 0, matrix);
        }

        public override void userActionRotate(AxisDirection axis) {
            base.userActionRotate(axis);
            direction = direction.rotate(axis);
        }

        private static readonly OCDeviceType<BeamSourceDevice> _TYPE = new("beam_source");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}