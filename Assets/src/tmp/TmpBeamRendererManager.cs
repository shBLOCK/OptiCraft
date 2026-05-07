using System;
using System.Collections.Generic;
using core;
using UnityEngine;

namespace tmp {
    public partial class TmpBeamRendererManager : MonoBehaviour {
        private SimSpace space;
        
        public GameObject rendererPrefab;
        private Dictionary<Beam, GameObject> renderers = new();
        
        private void Awake() {
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
    }
}