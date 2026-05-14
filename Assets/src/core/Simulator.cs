using System;
using System.Text.Json.Nodes;
using UnityEngine;

namespace core {
    public sealed class Simulator : MonoBehaviour {
        public int tickNumber { get; private set; } = 0;
        public float partialTick = 1f;

        public readonly BeamImageData.BeamImageDataManager beamImageDataManager = new();
        
        public SimSpace rootSpace;

        private void Awake() {
            rootSpace = new SimSpace(this);
        }

        public void tick() {
            rootSpace.tick();
        }

        public void reset() {
            tickNumber = 0;
            partialTick = 1f;
            rootSpace.reset();
        }

        public JsonObject save() {
            var data = new JsonObject();
            data["tickNumber"] = tickNumber;
            data["partialTick"] = partialTick;
            data["rootSpace"] = rootSpace.save();
            return data;
        }
        
        public void load(JsonObject data) {
            tickNumber = data["tickNumber"].GetValue<int>();
            partialTick = data["partialTick"].GetValue<float>();
            rootSpace = new SimSpace(this);
            rootSpace.load(data["rootSpace"].AsObject());
        }
    }
}