using System;
using core;
using Unity.Mathematics;
using UnityEngine;

namespace tmp {
    public class TmpSimManager : MonoBehaviour {
        public float speed = 10f;
        private Simulator simulator;
        
        private void Awake() {
            simulator = transform.parent.GetComponent<Simulator>();
            simulator.rootSpace = transform.parent.GetComponentInChildren<SimSpace>();
        }

        private int todoTicks = 0;
        
        public void tick() {
            todoTicks++;
        }
        
        public void reset() {
            simulator.reset();
            todoTicks = 0;
        }

        private void Update() {
            simulator.partialTick += math.min(Time.deltaTime * speed, 1f);
            if (todoTicks > 0) {
                if (simulator.partialTick >= 1f) {
                    simulator.partialTick -= 1f;
                    simulator.tick();
                    todoTicks--;
                }
            }

            simulator.partialTick = math.min(simulator.partialTick, 1f);
        }
    }
}