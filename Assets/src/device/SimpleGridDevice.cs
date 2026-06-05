using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using core;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using utils;
using Vertx.Debugging;
using Assert = UnityEngine.Assertions.Assert;

namespace device {
    public abstract class SimpleGridDevice : OCDevice {
        public int3 gridPos { get; private set; }

        public override void onAdded(SimSpace simSpace) {
            Assert.IsTrue(isCurrentGridPosValidInSpace(simSpace));
            base.onAdded(simSpace);
            occupy(gridPos);
        }

        public override void onRemoved() {
            unoccupy(gridPos);
            base.onRemoved();
        }

        public override Bounds getVisualBox() => new(new float3(gridPos), new float3(1.5f));

        public virtual bool isValidGridPos(int3 pos) => GridUtils.isValidGridPos(pos);

        public virtual int3 findGridPosForPlacement(float3 pos) {
            const int RANGE = 2;
            var center = pos.rint();
            var (min, max) = (center - RANGE, center + RANGE);
            var candidates = new List<int3>();
            for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            for (int z = min.z; z <= max.z; z++) {
                candidates.Add(new int3(x, y, z));
            }

            return candidates
                .OrderBy(p => new float3(p).distancesq(pos))
                .Select(p => (int3?)p)
                .FirstOrDefault(p => isValidGridPos(p.Value))
                .GetValueOrDefault(pos.rint());
        }

        public virtual bool isCurrentGridPosValidInSpace(SimSpace space) {
            if (!isValidGridPos(gridPos)) return false;
            var device = space.getDeviceAt(gridPos);
            return device == null;
        }

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
            DebugUtils.drawBoundsWireframe(new Bounds(pos, new float3(1.5f)), color);
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