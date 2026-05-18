using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using core;
using Unity.Mathematics;
using UnityEngine;

namespace level {
    public class TmpSimManager : MonoBehaviour {
        public float speed = 10f;
        private Simulator simulator;

        private void Awake() {
            simulator = GetComponent<Simulator>();
        }

        private bool running = false;

        private void Update() {
            simulator.partialTick += math.min(Time.deltaTime, 1f / 30f) * speed;
            if (running) {
                while (simulator.partialTick >= 1f) {
                    simulator.partialTick -= 1f;
                    simulator.tick();
                    // todoTicks--;
                }
            }

            simulator.partialTick = math.min(simulator.partialTick, 1f);
        }

        private string saveFileName = DateTime.Now.ToString("MM-dd_HH-mm-ss");
        
        public void _OnGUI() {
            GUILayout.Label($"Tick: {simulator.tickNumber - 1 + simulator.partialTick:F2}");
            GUILayout.BeginHorizontal();
            running = GUILayout.RepeatButton("Run");
            if (GUILayout.Button("Reset")) {
                simulator.reset();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            saveFileName = GUILayout.TextField(saveFileName);

            if (GUILayout.Button("Save")) {
                var data = simulator.save();
                Directory.CreateDirectory("saves");
                using var file = File.Create($"saves/{saveFileName}.json");
                JsonSerializer.Serialize(file, data,
                    new JsonSerializerOptions() { WriteIndented = true, IndentSize = 4 });
            }

            if (GUILayout.Button("Load")) {
                using var file = File.OpenRead($"saves/{saveFileName}.json");
                var data = JsonNode.Parse(file)!.AsObject();
                simulator.load(data);
            }

            GUILayout.EndHorizontal();
        }
    }
}