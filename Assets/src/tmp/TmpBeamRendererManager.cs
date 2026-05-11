using System;
using System.Collections.Generic;
using core;
using Unity.Mathematics;
using UnityEngine;

namespace tmp {
    public partial class TmpBeamRendererManager : MonoBehaviour {
        private SimSpace space;

        public GameObject rendererPrefab;
        private Dictionary<Beam, GameObject> renderers = new();

        private InputSystem_Actions inputActions;

        private void Awake() {
            inputActions = new();
            inputActions.Enable();
            space = transform.parent.GetComponent<SimSpace>();
            space.onBeamAdded += beam => {
                var obj = Instantiate(rendererPrefab, transform);
                obj.GetComponent<TmpBeamRenderer>().beam = beam;
                renderers.Add(beam, obj);
            };
            space.onBeamRemoved += beam => {
                renderers.Remove(beam, out var obj);
                Destroy(obj);
            };
        }

        private Beam hoveringBeam = null;

        private void OnGUI() {
            if (Event.current.type == EventType.Repaint) {
                if (hoveringBeam != null) {
                    // GUILayout.Label("Bea");
                    GUI.DrawTexture(
                        new Rect(Screen.width - 10 - 256, 10, 256, 256),
                        hoveringBeam.image.getTexture(),
                        ScaleMode.ScaleToFit,
                        false,
                        0,
                        (Vector4)hoveringBeam.image.tint,
                        Vector4.zero,
                        0
                    );
                }
            }
        }

        private void LateUpdate() {
            hoveringBeam = null;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(inputActions.UI.Point.ReadValue<Vector2>()),
                    out var hit)) {
                if (hit.collider.transform.TryGetComponent<TmpBeamRenderer>(out var beam)) {
                    hoveringBeam = beam.beam;
                }
            }
        }
    }
}