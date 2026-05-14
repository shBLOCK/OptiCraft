using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using utils;

namespace device {
    public abstract class SimpleGridDevice : OCDevice {
        protected int3 gridPos;

        public override void onAdded(SimSpace simSpace) {
            base.onAdded(simSpace);
            occupy(gridPos);
        }

        public override void onRemoved() {
            base.onRemoved();
            unoccupy(gridPos);
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

        public override void render(CommandBuffer cmds) {
        }
    }
}