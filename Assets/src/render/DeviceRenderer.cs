using core;
using UnityEngine;

namespace render {
    [RequireComponent(typeof(Simulator))]
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