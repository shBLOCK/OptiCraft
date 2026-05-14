using core;
using UnityEngine;
using UnityEngine.Rendering;

namespace render {
    public class DeviceRenderer : MonoBehaviour {
        private Simulator simulator;
        private CommandBuffer cmds;

        private void Awake() {
            simulator = GetComponent<Simulator>();
            cmds = new CommandBuffer();
        }

        private void LateUpdate() {
            foreach (var device in simulator.rootSpace.enumerateDevices()) {
                device.render(cmds);
            }
            Graphics.ExecuteCommandBuffer(cmds);
            cmds.Clear();
        }
    }
}