using core;
using Unity.Mathematics;
using UnityEngine;

namespace tmp {
    public class TmpBeamRenderer : MonoBehaviour {
        public Beam beam;
        
        private MeshRenderer meshRenderer;
        
        private void Awake() {
            var child = transform.GetChild(0);
            meshRenderer = child.GetComponent<MeshRenderer>();
        }

        private void LateUpdate() {
            var pt = beam.space.simulator.partialTick;
            
            float3 tailPos = beam.tailPos;
            if (!beam.wasBeingEmitted) {
                tailPos -= beam.direction.float3() * (1f - pt);
            }
            
            float length = beam.length;
            float lengthDelta = 0f;
            if (beam.wasBeingEmitted) lengthDelta += 1f;
            if (beam.wasBeingConsumed) lengthDelta -= 1f;
            length -= lengthDelta * (1f - pt);
            
            transform.localPosition = tailPos;
            transform.localScale = new float3(1f, 1f, length);
            transform.localRotation = Quaternion.FromToRotation(Vector3.forward, beam.direction.float3());
        }
    }
}