using System;
using core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace tmp {
    public class TmpSimManager : MonoBehaviour {
        public float speed = 10f;
        private Simulator simulator;
        
        private void Awake() {
            simulator = transform.parent.GetComponent<Simulator>();
            simulator.rootSpace = transform.parent.GetComponentInChildren<SimSpace>();
        }
        
        private bool running = false;

        private void Update() {
            simulator.partialTick += math.min(Time.deltaTime * speed, 1f);
            if (running) {
                if (simulator.partialTick >= 1f) {
                    simulator.partialTick -= 1f;
                    simulator.tick();
                    // todoTicks--;
                }
            }

            simulator.partialTick = math.min(simulator.partialTick, 1f);
        }

        private void OnGUI() {
            running = GUILayout.RepeatButton("Run");
            if (GUILayout.Button("Reset")) {
                simulator.reset();
            }
        }
    }
}