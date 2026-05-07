using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace core {
    [DisallowMultipleComponent]
    public abstract class Device : MonoBehaviour {
        public SimSpace space { get; private set; }
        [CanBeNull] private GridPosition _gridPosComponent;

        public int3 gridPos {
            get {
                Assert.IsNotNull(_gridPosComponent, "This device does not have a GridPosition component");
                return _gridPosComponent.gridPos;
            }
        }

        private void Awake() {
            space = GetComponentInParent<SimSpace>(includeInactive: true);
            _gridPosComponent = GetComponent<GridPosition>();
            space.addDevice(this);
        }
        
        public virtual void reset() {}
        // public virtual void postTick() {}
        
        public virtual void onBeamHit(Beam beam) {}
    }

    public interface ITickingDevice {
        void tick();
    }
}