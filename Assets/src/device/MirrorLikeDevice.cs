using System;
using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;
using Vertx.Debugging;

namespace device {
    public abstract class MirrorLikeDevice : SimpleGridDevice {
        protected MirrorDirection mirrorDir = MirrorDirection.PosXNegZ;

        public override void onBeamHitEdge(ref Beam beam) {
            if (beam.direction.axis() != mirrorDir.dirA().axis() && beam.direction.axis() != mirrorDir.dirB().axis()) {
                space.consumeBeam(ref beam);
            }
        }

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["mirrorDir"] = mirrorDir.ToString();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            mirrorDir = Enum.Parse<MirrorDirection>(data["mirrorDir"].GetValue<string>());
        }

        public override void render() {
            base.render();
            _tmpDrawIO();
        }

        protected void _tmpDrawIO() {
            D.raw(new Bounds(gridPos + mirrorDir.dirA().float3(), new float3(0.2f)), Color.red);
            D.raw(new Bounds(gridPos + mirrorDir.dirB().float3(), new float3(0.2f)), Color.green);
        }

        private AxisDirection anim_rotAxis;
        private MirrorDirection anim_lastMirrorDir;
        private MirrorDirectionExtensions.RotateStepType anim_rotateStepType;
        private float anim_rotStartTime = float.NegativeInfinity;

        public override void userActionRotate(AxisDirection axis) {
            anim_rotAxis = axis;
            anim_lastMirrorDir = mirrorDir;
            anim_rotStartTime = Time.time;

            base.userActionRotate(axis);
            mirrorDir = mirrorDir.rotateStep(axis, out anim_rotateStepType);
        }

        protected void getRenderParamsWithAnimation(out MirrorDirection visualMirrorDir, out float4x4 modelMat) {
            var rotProgress = (Time.time - anim_rotStartTime) / 0.2f;
            var rotAngle = anim_rotateStepType switch {
                MirrorDirectionExtensions.RotateStepType.None => math.PIHALF,
                MirrorDirectionExtensions.RotateStepType.Deg45 => math.PIHALF * 0.5f,
                MirrorDirectionExtensions.RotateStepType.Deg90 => math.PIHALF,
                _ => throw new ArgumentOutOfRangeException()
            };
            var quat = AnimationUtils.deviceRotationAnimation(
                rotProgress,
                anim_lastMirrorDir.modelRotation(), mirrorDir.modelRotation(),
                anim_rotAxis.float3(), rotAngle,
                out var renderNew
            );
            visualMirrorDir = renderNew ? mirrorDir : anim_lastMirrorDir;
            modelMat = new float4x4(quat, getRenderPosWithAnimation());
        }
    }
}