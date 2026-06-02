using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using core.beam;
using device;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
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
        private List<Beam> beamsStagingExtra = new();
        public event Action<Beam> onBeamAdded;
        public event Action<Beam> onBeamRemoved;

        [CanBeNull]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private void tick_tickBeams() {
            for (int i = 0; i < beams.Length; i++) {
                ref var beam = ref beams.ElementAt(i);
                if (!beam.isValid) continue;
                beam.tick();
            }
        }

        private void tick_beamDeviceInteraction() {
            // beam-device interaction: remove ended beams
            for (int i = 0; i < beams.Length; i++) {
                ref var beam = ref beams.ElementAt(i);
                if (!beam.isValid) continue;
                if (!beam.beingEmitted) {
                    if (beam.length == 0) {
                        getDeviceAt(beam.headPos)?.onBeamEnd(ref beam);
                    } else if (beam.length <= -1) { // keep beam around for one more tick for rendering
                        beamsFreeSlots.Push(beam.id);
                        beam._end(simulator.beamImageDataManager);
                        onBeamRemoved?.Invoke(beam);
                    }
                }
            }

            // beam-device interaction: beam hit
            for (int i = 0; i < beams.Length; i++) {
                ref var beam = ref beams.ElementAt(i);
                if (!beam.isValid) continue;
                if (!beam.beingConsumed) {
                    getDeviceAt(beam.headPos)?.onBeamHit(ref beam);
                }
            }
        }

        private void tick_tickDevices() {
            foreach (var device in devices) {
                device.tick();
            }
        }

        private void tick_emitStagedBeams() {
            foreach (var beam in beamsStaging) {
                beams[beam.id] = beam;
            }

            foreach (var beam in beamsStagingExtra) {
                beams.Add(beam);
            }

            beamsStaging.Clear();
            beamsStagingExtra.Clear();
        }

        public void tick() {
            tick_tickBeams();
            tick_beamDeviceInteraction();
            tick_tickDevices();
            tick_emitStagedBeams();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref Beam getBeam(ushort id) => ref beams.ElementAt(id);

        /// <returns>The same reference as the parameter beam reference, for convenience.
        /// **NOT** a reference to the actual beam instance in the space.</returns>
        public Beam emitBeam(Beam beam) {
            beam._emit(simulator.beamImageDataManager);
            if (beamsFreeSlots.TryPop(out var slot)) {
                beam.id = slot;
                beamsStaging.Add(beam);
            } else {
                beam.id = (ushort)(beams.Length + beamsStagingExtra.Count);
                beamsStagingExtra.Add(beam);
            }

            onBeamAdded?.Invoke(beam);
            return beam;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void stopEmitBeam(ushort id) => beams.ElementAt(id)._stopEmit();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void stopEmitBeam(ref Beam beam) => beam._stopEmit();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void consumeBeam(ushort id) => beams.ElementAt(id)._consume();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void consumeBeam(ref Beam beam) => beam._consume();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void stopConsumeBeam(ushort id) => beams.ElementAt(id)._stopConsume();

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
            beamsStagingExtra.Clear();
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