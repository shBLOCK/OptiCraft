using core;
using UnityEngine;
using UnityEngine.Rendering;

namespace render {
    public class DeviceRenderer : MonoBehaviour {
        private Simulator simulator;

        private void Awake() {
            simulator = GetComponent<Simulator>();
        }

        private void LateUpdate() {
            foreach (var device in simulator.rootSpace.enumerateDevices()) {
                device.render();
            }
        }
    }
}