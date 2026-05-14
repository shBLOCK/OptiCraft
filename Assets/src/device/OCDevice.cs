using System.Collections.Generic;
using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using utils;

namespace device {
    public abstract class OCDevice {
        public SimSpace space { get; private set; }

        public virtual void onAdded(SimSpace simSpace) {
            Assert.IsNull(space, "Device already added to space");
            space = simSpace;
        }

        public virtual void onRemoved() {
            Assert.IsNotNull(space, "Device not added to space");
            space = null;
        }

        protected void occupy(int3 gridPos) => space._deviceOccupy(this, gridPos);
        protected void unoccupy(int3 gridPos) => space._deviceUnoccupy(this, gridPos);

        public virtual void reset() { }
        public virtual void tick() { }

        public virtual void onBeamHit(ref Beam beam) { }
        public virtual void onBeamHitEdge(ref Beam beam) { }
        public virtual void onBeamEnd(ref Beam beam) { }
        public virtual void onBeamEndEdge(ref Beam beam) { }

        public virtual void render(CommandBuffer cmds) { }

        public abstract Bounds getVisualBox();

        public virtual void userActionRotate(AxisDirection axis) {
            Assert.IsTrue(space == null);
        }

        protected virtual JsonObject saveData() => new();
        protected virtual void loadData(JsonObject data) { }

        public abstract OCDeviceType TYPE { get; }

        public abstract class OCDeviceType {
            public readonly string id;

            protected OCDeviceType(string id) {
                this.id = id;
            }

            public abstract OCDevice construct();
        }

        public class OCDeviceType<T> : OCDeviceType where T : OCDevice, new() {
            public OCDeviceType(string id) : base(id) { }

            public override OCDevice construct() => new T();
        }

        private static Dictionary<string, OCDeviceType> REGISTRY = new();

        public static void register(OCDeviceType type) {
            Assert.IsFalse(REGISTRY.ContainsKey(type.id));
            REGISTRY[type.id] = type;
        }

        public JsonObject save() {
            var data = new JsonObject();
            data["type"] = TYPE.id;
            data["data"] = saveData();
            return data;
        }

        public static OCDevice load(JsonObject data) {
            var device = REGISTRY[data["type"].GetValue<string>()].construct();
            device.loadData(data["data"].AsObject());
            return device;
        }
    }
}