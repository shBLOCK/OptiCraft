using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace tmp {
    public class GUIManager : MonoBehaviour {
        public UnityEvent methods;
        
        private void OnGUI() {
            var orgMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(float3.zero, quaternion.identity, new float3(1.5f));
            methods?.Invoke();
            GUI.matrix = orgMatrix;
        }
    }
}