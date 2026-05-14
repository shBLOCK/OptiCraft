using System;
using UnityEngine;
using UnityEngine.Events;

namespace tmp {
    public class GUIManager : MonoBehaviour {
        public UnityEvent methods;
        
        private void OnGUI() {
            methods?.Invoke();
        }
    }
}