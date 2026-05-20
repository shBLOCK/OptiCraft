using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using utils;
using Vertx.Debugging;

namespace device {
    public abstract class SimpleGridDevice : OCDevice {
        protected int3 gridPos { get; private set; }

        public override void onAdded(SimSpace simSpace) {
            base.onAdded(simSpace);
            occupy(gridPos);
        }

        public override void onRemoved() {
            unoccupy(gridPos);
            base.onRemoved();
        }

        public override Bounds getVisualBox() => new(new float3(gridPos), new float3(1.5f));

        protected override JsonObject saveData() {
            var data = base.saveData();
            data["gridPos"] = gridPos.toJsonArray();
            return data;
        }

        protected override void loadData(JsonObject data) {
            base.loadData(data);
            gridPos = data["gridPos"].AsArray().toInt3();
        }

        public override void render() {
            _tmpDrawBox(Color.darkMagenta);
        }

        protected void _tmpDrawBox(Color color) {
            var pos = getRenderPosWithAnimation();
            D.raw(new Bounds(pos, new float3(1.5f)), color);
            D.raw(new Shape.Text(pos, TYPE.id, Camera.main), Color.gray);
        }

        private float3 anim_gridPos;
        private float anim_startTime = float.NegativeInfinity;
        
        public void _tmp_setGridPos(int3 pos) {
            assertNotInSpace();
            
            anim_gridPos = getRenderPosWithAnimation();
            anim_startTime = Time.time;
            
            gridPos = pos;
        }

        protected float3 getRenderPosWithAnimation() {
            var animProgress = (Time.time - anim_startTime) / 0.1f;
            if (animProgress >= 1f) return new float3(gridPos);
            animProgress = mathx.smoothstop(animProgress, 2);
            return math.lerp(anim_gridPos, new float3(gridPos), animProgress);
        }
    }
}