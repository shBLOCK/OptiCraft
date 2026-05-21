using System.Text.Json.Nodes;
using core;
using core.beam;
using UnityEngine;
using UnityEngine.Rendering;
using utils;

namespace device {
    public class MirrorDevice : MirrorLikeDevice {
        private bool doubleSided = true;
        private ByteEnumMap<AxisDirection, BeamIOPair> beams = new(6, BeamIOPair.INVALID_IDS);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (mirrorDir.reflect(beam.direction.opposite(), out var reflectDir, out var isFrontSide)) {
                if (isFrontSide || doubleSided) {
                    var image = beam.image;
                    var orientation = image.orientation.reflect(beam.direction.opposite(), reflectDir);
                    beams[beam.direction.opposite()] = new BeamIOPair(
                        beam.id,
                        space.emitBeam(new Beam(gridPos, reflectDir, image.withOrientation(orientation))).id
                    );
                }
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            var io = beams[beam.direction.opposite()];
            if (io.output != Beam.INVALID_ID) space.stopEmitBeam(io.output);
            beams[beam.direction.opposite()] = BeamIOPair.INVALID_IDS;
        }

        public override void onRemoved() {
            for (byte i = 0; i < 6; i++) {
                var direction = (AxisDirection)i;
                var pair = beams[direction];
                if (pair != BeamIOPair.INVALID_IDS) {
                    beams[direction] = BeamIOPair.INVALID_IDS;
                    space.stopConsumeBeam(pair.input);
                    space.stopEmitBeam(pair.output);
                }
            }

            base.onRemoved();
        }

        public override void reset() {
            base.reset();
            beams.fill(BeamIOPair.INVALID_IDS);
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["doubleSided"] = doubleSided;
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            doubleSided = data["doubleSided"].GetValue<bool>();
        }

        private static Mesh MESH_FRAME_90DEG;
        private static Mesh MESH_MIRROR_90DEG;
        private static Mesh MESH_FRAME_45DEG;
        private static Mesh MESH_MIRROR_45DEG;
        private static Material MAT_FRAME;
        private static Material MAT_MIRROR;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_MODEL() {
            var obj = Resources.Load<GameObject>("model/device/mirror");
            MESH_FRAME_90DEG = obj.getMesh("90deg/frame");
            MESH_MIRROR_90DEG = obj.getMesh("90deg/mirror");
            MESH_FRAME_45DEG = obj.getMesh("45deg/frame");
            MESH_MIRROR_45DEG = obj.getMesh("45deg/mirror");
            MAT_FRAME = Resources.Load<Material>("material/device/mirror/frame");
            MAT_MIRROR = Resources.Load<Material>("material/device/mirror/mirror");
        }

        public override void render() {
            getRenderParamsWithAnimation(out var visualMirrorDir, out var modelMat);

            var (frame, mirror) = visualMirrorDir.dirA() == visualMirrorDir.dirB()
                ? (MESH_FRAME_90DEG, MESH_MIRROR_90DEG)
                : (MESH_FRAME_45DEG, MESH_MIRROR_45DEG);

            Graphics.RenderMesh(new RenderParams(MAT_FRAME), frame, 0, modelMat);
            Graphics.RenderMesh(new RenderParams(MAT_MIRROR), mirror, 0, modelMat);
        }

        private static readonly OCDeviceType<MirrorDevice> _TYPE = new("mirror");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}