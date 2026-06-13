using System.Text.Json;
using core;
using level.validation;
using render;
using UnityEngine;

namespace level {
    [RequireComponent(typeof(Simulator))]
    [RequireComponent(typeof(BeamRenderer))]
    [RequireComponent(typeof(DeviceRenderer))]
    public class GameLevel : MonoBehaviour {
        [SerializeField] private string levelId;
        [SerializeField] private string levelName;
        [SerializeField] private TextAsset description;
        [SerializeField] private TextAsset validatorDef;
        private LevelValidator validator = null;

        private void Awake() {
            if (validatorDef) {
                validator = JsonSerializer.Deserialize<LevelValidator>(validatorDef.bytes);
            }
        }
    }
}