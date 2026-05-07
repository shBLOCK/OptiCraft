using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace core {
    public class SimSpace : MonoBehaviour {
        public Simulator simulator { get; private set; }

        private void Awake() {
            simulator = GetComponentInParent<Simulator>();
            Assert.IsNotNull(simulator);
        }
        
        private HashSet<Device> devices = new();
        private Dictionary<int3, Device> posDeviceMap = new();
        
        private List<Beam> beams = new();
        public event Action<Beam> onBeamAdded; 
        public event Action<Beam> onBeamRemoved; 

        [CanBeNull]
        public Device getDeviceAt(int3 pos) => posDeviceMap.GetValueOrDefault(pos);
        
        public IEnumerable<Beam> getBeams() => beams;

        public void tick() {
            // tick beams
            for (int i = 0; i < beams.Count; i++) {
                var beam = beams[i];
                if (!beam.tick()) {
                    beams.RemoveAtSwapBack(i);
                    onBeamRemoved?.Invoke(beam);
                    i--;
                }
            }
            
            // beam collision
            foreach (var beam in beams) {
                if (!beam.beingConsumed) {
                    getDeviceAt(beam.headPos)?.onBeamHit(beam);
                    if (GridUtils.isGridEdge(beam.headPos)) {
                        getDeviceAt(beam.headPos.offset(beam.direction))?.onBeamHit(beam);
                    }
                }
            }
            
            // tick devices
            foreach (var device in devices) {
                if (device is ITickingDevice tickingDevice) {
                    tickingDevice.tick();
                }
            }
        }
        
        
        public void addDevice(Device device) {
            devices.Add(device);
            posDeviceMap[device.gridPos] = device; // TODO: tmp impl
        }

        internal void _emitBeam(Beam beam) {
            beams.Add(beam);
            onBeamAdded?.Invoke(beam);
        }

        public void reset() {
            foreach (var device in devices) {
                device.reset();
            }
            beams.ForEach(beam => onBeamRemoved?.Invoke(beam));
            beams.Clear();
        }
    }
}