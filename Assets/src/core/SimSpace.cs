using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using device;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using utils;

namespace core {
    public sealed class SimSpace {
        public readonly Simulator simulator;

        public SimSpace(Simulator simulator) {
            this.simulator = simulator;
        }

        private HashSet<OCDevice> devices = new(); // TODO: optimize
        private Dictionary<int3, OCDevice> posDeviceMap = new(); // TODO: optimize

        private NativeList<Beam> beams = new(Allocator.Persistent);
        private Stack<ushort> beamsFreeSlots = new();
        private List<Beam> beamsStaging = new();
        public event Action<Beam> onBeamAdded;
        public event Action<Beam> onBeamRemoved;

        [CanBeNull]
        public OCDevice getDeviceAt(int3 pos) => posDeviceMap.GetValueOrDefault(pos);

        public IEnumerable<OCDevice> enumerateDevices() {
            foreach (var device in devices) {
                yield return device;
            }
        }

        public IEnumerable<Beam> enumerateBeams() {
            foreach (var beam in beams) {
                if (beam.isValid) yield return beam;
            }
        }

        public void tick() {
            // beam collision
            for (int i = 0; i < beams.Length; i++) {
                ref var beam = ref beams.ElementAt(i);
                if (!beam.isValid) continue;
                if (!beam.beingConsumed) {
                    getDeviceAt(beam.headPos)?.onBeamHit(ref beam);
                    if (GridUtils.isGridEdge(beam.headPos)) {
                        getDeviceAt(beam.headPos.offset(beam.direction))?.onBeamHitEdge(ref beam);
                    }
                }
            }

            // tick devices
            foreach (var device in devices) {
                device.tick();
            }

            // emit staged beams
            foreach (var _beam in beamsStaging) {
                var beam = _beam;
                if (beamsFreeSlots.TryPop(out var slot)) {
                    beam.id = slot;
                    beams[slot] = beam;
                } else {
                    beam.id = (ushort)beams.Length;
                    beams.Add(beam);
                }
            }

            beamsStaging.Clear();

            // tick beams
            for (int i = 0; i < beams.Length; i++) {
                ref var beam = ref beams.ElementAt(i);
                if (!beam.isValid) continue;
                if (!beam.tick()) {
                    getDeviceAt(beam.headPos)?.onBeamEnd(ref beam);
                    if (GridUtils.isGridEdge(beam.headPos)) {
                        getDeviceAt(beam.headPos.offset(beam.direction))?.onBeamEndEdge(ref beam);
                    }

                    beamsFreeSlots.Push((ushort)i);
                    beam._invalidate();
                    onBeamRemoved?.Invoke(beam);
                }
            }
        }

        public void addDevice(OCDevice device) {
            devices.Add(device);
            device.onAdded(this);
        }

        public void removeDevice(OCDevice device) {
            devices.Remove(device);
            device.onRemoved();
        }

        internal void _deviceOccupy(OCDevice device, int3 gridPos) {
            Assert.IsFalse(posDeviceMap.ContainsKey(gridPos));
            posDeviceMap[gridPos] = device;
        }

        internal void _deviceUnoccupy(OCDevice device, int3 gridPos) {
            Assert.IsTrue(posDeviceMap.GetValueOrDefault(gridPos) == device);
            posDeviceMap.Remove(gridPos);
        }

        public Beam emitBeam(Beam beam) {
            if (beamsFreeSlots.TryPop(out var slot)) {
                beam.id = slot;
                beams[slot] = beam;
            } else {
                beam.id = (ushort)(beams.Length + beamsStaging.Count);
                beamsStaging.Add(beam);
            }

            onBeamAdded?.Invoke(beam);
            return beam;
        }

        public void stopEmitBeam(ushort id) {
            beams[id]._stopEmit();
        }

        public void consumeBeam(ushort id) {
            beams[id]._consume();
            Assert.IsTrue(beams[id].beingConsumed);
        }

        public void stopConsumeBeam(ushort id) {
            beams[id]._stopConsume();
        }

        public void reset() {
            foreach (var device in devices) {
                device.reset();
            }

            clearBeams();
        }

        public void clear() {
            devices.Clear();
            posDeviceMap.Clear();
            clearBeams();
        }

        private void clearBeams() {
            foreach (var beam in beams) {
                onBeamRemoved?.Invoke(beam);
            }

            beams.Clear();
            beamsFreeSlots.Clear();
            beamsStaging.Clear();
        }

        public JsonObject save() {
            var data = new JsonObject();
            var devicesData = new JsonArray();
            foreach (var device in devices) {
                devicesData.Add(device.save());
            }

            data["devices"] = devicesData;
            //TODO: beams
            return data;
        }

        public void load(JsonObject data) {
            clear();
            foreach (var device in data["devices"].AsArray()) {
                addDevice(OCDevice.load(device.AsObject()));
            }
            //TODO: beams
        }
    }
}