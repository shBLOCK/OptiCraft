using System;
using System.Text.Json.Nodes;
using core.beam;
using Unity.Mathematics;
using UnityEngine;
using utils;

namespace device {
    public sealed class BeamSourceDevice : BlockerDevice {
        private AxisDirection direction = AxisDirection.PosZ;
        public string imagePath = null;
        public Texture2D image = null;
        public float4 color = 1f;

        private ushort emittingBeam = Beam.INVALID_ID;

        public override void reset() {
            base.reset();
            emittingBeam = Beam.INVALID_ID;
        }

        public override void tick() {
            if (emittingBeam == Beam.INVALID_ID) {
                BeamImage beamImage;
                if (imagePath != null && !image) {
                    image = Resources.Load<Texture2D>(imagePath); //TODO: tmp
                }
                if (!image) {
                    beamImage = BeamImage.singlePixel(color);
                } else {
                    var data = space.simulator.beamImageDataManager.addNew((uint2)image.size());
                    data.blitFromTexture(image);
                    beamImage = new BeamImage(data.id, data.size, BeamImageOrientation.PosXPosY, 0, color, 0f);
                }

                emittingBeam = space.emitBeam(new Beam(gridPos.offset(direction), direction, beamImage)).id;
            }
        }
        
        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
        }

        public override void onBeamIdChanged(ref Beam beam, Beam.End beamEnd, ushort newId) {
            if (beamEnd == Beam.End.Tail) {
                emittingBeam.replaceThis(beam.id, newId);
            }
        }

        public override void onRemoved() {
            if (emittingBeam != Beam.INVALID_ID) {
                space.stopEmitBeam(emittingBeam);
                emittingBeam = Beam.INVALID_ID;
            }

            base.onRemoved();
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["direction"] = direction.ToString();
            data["color"] = color.toJsonArray();
            data["imagePath"] = imagePath;
            return data;
        }
        protected override void loadData(JsonObject data) {
            base.loadData(data);
            direction = Enum.Parse<AxisDirection>(data["direction"].ToString());
            color = data["color"].AsArray().toFloat4();
            imagePath = data["imagePath"]?.GetValue<string>();
        }
        
        private AxisDirection anim_rotAxis;
        private AxisDirection anim_lastDirection;
        private float anim_rotStartTime = float.NegativeInfinity;

        public override void userActionRotate(AxisDirection axis) {
            base.userActionRotate(axis);

            anim_lastDirection = direction;
            anim_rotAxis = axis;
            anim_rotStartTime = Time.time;
            
            direction = direction.rotate(axis);
        }
        
        private static Mesh MESH;
        private static Material MATERIAL;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_MESH() {
            MESH = Resources.Load<Mesh>("BeamSourceDevice");
            MATERIAL = Resources.Load<Material>("Gray");
        }

        public override void render() {
            var quat = AnimationUtils.deviceRotationAnimation(
                (Time.time - anim_rotStartTime) / 0.2f,
                anim_lastDirection.modelRotation(), direction.modelRotation(),
                anim_rotAxis.float3(), math.PIHALF,
                out _
            );
            var modelMat = new float4x4(quat, getRenderPosWithAnimation());
            modelMat = math.mul(modelMat, float4x4.Translate(new float3(0f, -0.5f, 0f)));
            Graphics.RenderMesh(new RenderParams(MATERIAL), MESH, 0, modelMat);
        }

        private static readonly OCDeviceType<BeamSourceDevice> _TYPE = new("beam_source");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}