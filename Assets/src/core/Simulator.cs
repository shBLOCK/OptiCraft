using System.Text.Json.Nodes;
using core.beam;
using UnityEngine;
using UnityEngine.Rendering;

namespace core {
    public sealed class Simulator : MonoBehaviour {
        public int tickNumber { get; private set; } = 0;
        public float partialTick = 1f;

        public readonly BeamImageData.Manager beamImageDataManager = new();
        public CommandBuffer cmds;

        public SimSpace rootSpace;

        private void Awake() {
            cmds = new CommandBuffer();
            rootSpace = new SimSpace(this);
        }

        public void tick() {
            Debug.Log($"tick {tickNumber}");
            
            cmds.Clear();
            rootSpace.tick();
            
            //TODO: maybe manage this somewhere else
            Graphics.ExecuteCommandBuffer(cmds);
            cmds.Clear();
            
            tickNumber++;
        }

        public void reset() {
            tickNumber = 0;
            partialTick = 1f;
            rootSpace.reset();
            beamImageDataManager.reset();
        }

        public JsonObject save() {
            var data = new JsonObject();
            data["tickNumber"] = tickNumber;
            data["partialTick"] = partialTick;
            data["rootSpace"] = rootSpace.save();
            return data;
        }

        public void load(JsonObject data) {
            reset();
            tickNumber = data["tickNumber"].GetValue<int>();
            partialTick = data["partialTick"].GetValue<float>();
            rootSpace = new SimSpace(this);
            rootSpace.load(data["rootSpace"].AsObject());
        }
    }
}