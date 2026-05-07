using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace core {
    [DisallowMultipleComponent]
    public class GridPosition : MonoBehaviour {
        public int3 gridPos;

        private void OnValidate() {
            transform.localPosition = new float3(gridPos);
        }
    }
}