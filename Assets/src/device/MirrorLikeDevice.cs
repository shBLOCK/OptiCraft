using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using core.beam;
using Unity.Mathematics;
using UnityEngine;
using utils;

namespace device {
    public abstract class MirrorLikeDevice : SimpleGridDevice {
        protected MirrorDirection mirrorDir = MirrorDirection.PosXNegZ;

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

        public override void beamRendering_configureBeamEnd(
            in Beam beam, Beam.End beamEnd, AxisDirection enterDir, float3 endPos,
            List<Vector4> clipPlanesData, out float boundsOffset
        ) {
            var axis = enterDir.axis();
            if (axis == mirrorDir.dirA().axis() || axis == mirrorDir.dirB().axis()) {
                if (mirrorDir.dirA() == mirrorDir.dirB()) {
                    base.beamRendering_configureBeamEnd(beam, beamEnd, enterDir, endPos, clipPlanesData, out boundsOffset);
                    return;
                }
                
                var normal = mirrorDir.normal();
                if (normal.dot(enterDir.float3()) < 0f) {
                    normal = -normal;
                }
                clipPlanesData.Add(new float3(gridPos).f4());
                clipPlanesData.Add(normal.f4());
                
                if (beamEnd == Beam.End.Tail) {
                    boundsOffset = beam.beingEmitted ? 1f : 0f;
                    if (beam.wasBeingEmitted && !beam.beingEmitted) {
                        boundsOffset = 1f - space.simulator.partialTick;
                    }
                } else {
                    boundsOffset = 0f;
                    if (beam.wasBeingConsumed) {
                        boundsOffset = beam.wasWasBeingConsumed ? 1f : space.simulator.partialTick;
                    }
                }
            
                clipPlanesData.Add((endPos + enterDir.float3(boundsOffset)).f4());
                clipPlanesData.Add(enterDir.float3().f4());
            } else {
                //TODO
                clipPlanesData.Add(endPos.f4());
                clipPlanesData.Add(enterDir.float3().f4());
                boundsOffset = 0f;
            }
        }

        protected void _tmpDrawIO() {
            DebugUtils.drawBoundsWireframe(new Bounds(gridPos + mirrorDir.dirA().float3(), new float3(0.2f)),
                Color.red);
            DebugUtils.drawBoundsWireframe(new Bounds(gridPos + mirrorDir.dirB().float3(), new float3(0.2f)),
                Color.green);
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