using System;
using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;

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

        public override void render(CommandBuffer cmds) {
            base.render(cmds);
            _tmpDrawIO();
        }

        protected void _tmpDrawIO() {
            new Bounds(gridPos + mirrorDir.dirA().float3(), new float3(0.2f)).debugDraw(Color.red);
            new Bounds(gridPos + mirrorDir.dirB().float3(), new float3(0.2f)).debugDraw(Color.green);
            new Bounds(gridPos - mirrorDir.dirA().float3(), new float3(0.2f)).debugDraw(Color.darkRed);
            new Bounds(gridPos - mirrorDir.dirB().float3(), new float3(0.2f)).debugDraw(Color.darkGreen);
        }

        public override void userActionRotate(AxisDirection axis) {
            base.userActionRotate(axis);
            mirrorDir = mirrorDir.rotateStep(axis);
        }
    }
}