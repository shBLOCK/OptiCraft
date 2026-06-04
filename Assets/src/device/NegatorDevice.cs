using System;
using System.Text.Json.Nodes;
using core.beam;
using Unity.Mathematics;
using UnityEngine;
using utils;

namespace device {
    public class NegatorDevice : SimpleGridDevice {
        private Axis axis;
        private ByteEnumMap<Sign, BeamIOPair> beams = new(2, BeamIOPair.INVALID_IDS);

        public override void onBeamHit(ref Beam beam) {
            space.consumeBeam(ref beam);
            if (beam.direction.axis() == axis) {
                beams[beam.direction.sign()] = new BeamIOPair(
                    beam.id,
                    space.emitBeam(new Beam(gridPos, beam.direction, beam.image.modulated(-1f))).id
                );
            }
        }

        public override void onBeamEnd(ref Beam beam) {
            space.stopEmitBeam(beams[beam.direction.sign()].output);
        }

        public override void onBeamIdChanged(ref Beam beam, Beam.End beamEnd, ushort newId) {
            for (byte i = 0; i < 2; i++) {
                ref var pair = ref beams[(Sign)i];
                if (beamEnd == Beam.End.Head) {
                    pair.input.replaceThis(beam.id, newId);
                } else {
                    pair.output.replaceThis(beam.id, newId);
                }
            }
        }

        public override void onRemoved() {
            for (byte i = 0; i < 2; i++) {
                var pair = beams[(Sign)i];
                if (pair != BeamIOPair.INVALID_IDS) {
                    beams[(Sign)i] = BeamIOPair.INVALID_IDS;
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
            data["axis"] = axis.ToString();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            axis = Enum.Parse<Axis>(data["axis"].GetValue<string>());
        }

        private AxisDirection anim_rotAxis;
        private Axis anim_lastAxis;
        private float anim_rotStartTime = float.NegativeInfinity;

        public override void userActionRotate(AxisDirection axis) {
            anim_lastAxis = this.axis;
            anim_rotAxis = axis;
            anim_rotStartTime = Time.time;
            
            this.axis = this.axis.rotate(axis.axis());
        }
        
        private static Mesh MESH_FRAME;
        private static Mesh MESH_FILM;
        private static Material MAT_FRAME;
        private static Material MAT_FILM;

        [RuntimeInitializeOnLoadMethod]
        private static void LOAD_MODEL() {
            var obj = Resources.Load<GameObject>("model/device/mirror");
            MESH_FRAME = obj.getMesh("90deg/frame");
            MESH_FILM = obj.getMesh("90deg/mirror");
            MAT_FRAME = Resources.Load<Material>("material/device/negator/frame");
            MAT_FILM = Resources.Load<Material>("material/device/negator/film");
        }

        public override void render() {
            var quat = AnimationUtils.deviceRotationAnimation(
                (Time.time - anim_rotStartTime) / 0.2f,
                anim_lastAxis.modelRotation(), axis.modelRotation(),
                anim_rotAxis.float3(), math.PIHALF,
                out _
            );
            var modelMat = new float4x4(quat, getRenderPosWithAnimation());
            Graphics.RenderMesh(new RenderParams(MAT_FRAME), MESH_FRAME, 0, modelMat);
            Graphics.RenderMesh(new RenderParams(MAT_FILM), MESH_FILM, 0, modelMat);
        }

        private static readonly OCDeviceType<NegatorDevice> _TYPE = new("negator");
        public override OCDeviceType TYPE => _TYPE;

        [RuntimeInitializeOnLoadMethod]
        private static void REGISTER() => register(_TYPE);
    }
}