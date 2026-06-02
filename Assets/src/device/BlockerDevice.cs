using core;
using core.beam;
using UnityEngine;
using utils;

namespace device {
    public class BlockerDevice : SimpleGridDevice {
        private ByteEnumMap<AxisDirection, ushort> consumingBeams = new(6, Beam.INVALID_ID);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            consumingBeams[beam.direction] = beam.id;
        }

        public override void onBeamEnd(ref Beam beam) {
            consumingBeams[beam.direction] = Beam.INVALID_ID;
        }

        public override void reset() {
            consumingBeams.fill(Beam.INVALID_ID);
        }

        public override void onRemoved() {
            for (byte i = 0; i < 6; i++) {
                var id = consumingBeams[(AxisDirection)i];
                if (id != Beam.INVALID_ID) space.stopConsumeBeam(id);
            }

            consumingBeams.fill(Beam.INVALID_ID);
            
            base.onRemoved();
        }

        private static Mesh MESH;
        private static Material MATERIAL;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_MESH() {
            MESH = Resources.Load<GameObject>("model/device/blocker").getMesh();
            MATERIAL = Resources.Load<Material>("material/device/blocker/blocker");
        }

        public override void render() {
            Graphics.RenderMesh(new RenderParams(MATERIAL), MESH, 0, Matrix4x4.Translate(getRenderPosWithAnimation()));
        }

        private static readonly OCDeviceType<BlockerDevice> _TYPE = new("blocker");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}