using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using utils;

namespace core.beam {
    public struct Beam : IEquatable<Beam> {
        public const ushort INVALID_ID = ushort.MaxValue;

        public int3 tailPos { get; private set; }
        public ushort id { get; internal set; }
        public readonly AxisDirection direction;
        public ushort length;
        public int3 headPos => tailPos.offset(direction, length);

        [Flags]
        private enum BeamFlags : ushort {
            BeingEmitted = 1 << 0,
            BeingConsumed = 1 << 1,
            RedChannelEmpty = 1 << 2, // TODO
            GreenChannelEmpty = 1 << 3,
            BlueChannelEmpty = 1 << 4,
            UVChannelEmpty = 1 << 5,
        }

        private BeamFlags flags;
        public bool beingEmitted => (flags & BeamFlags.BeingEmitted) == BeamFlags.BeingEmitted;
        public bool beingConsumed => (flags & BeamFlags.BeingConsumed) == BeamFlags.BeingConsumed;

        public readonly BeamImage image;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Beam(int3 startPos, AxisDirection direction, in BeamImage image) {
            this.tailPos = startPos;
            this.direction = direction;
            this.image = image;
            length = 0;
            flags = 0;
            id = INVALID_ID;
        }

        public bool isValid => id != INVALID_ID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _emit(BeamImageData.Manager beamImageDataManager) {
            Assert.IsFalse(beingEmitted, "Beam already being emitted");
            flags |= BeamFlags.BeingEmitted;
            image.incRef(beamImageDataManager);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _stopEmit() {
            Assert.IsTrue(beingEmitted, "Beam not being emitted");
            flags &= ~BeamFlags.BeingEmitted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _consume() {
            Assert.IsFalse(beingConsumed, "Beam already being consumed");
            flags |= BeamFlags.BeingConsumed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _stopConsume() {
            Assert.IsTrue(beingConsumed, "Beam not being consumed");
            flags &= ~BeamFlags.BeingConsumed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void _end(BeamImageData.Manager beamImageDataManager) {
            id = INVALID_ID;
            image.decRef(beamImageDataManager);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void tick() {
            if (!beingEmitted) {
                tailPos += direction.int3();
            }

            if (beingEmitted) length++;
            if (beingConsumed) length--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Beam other) => id == other.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is Beam other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => id;

        // [RuntimeInitializeOnLoadMethod]
        // private static void PRINT_LAYOUT() {
        //     DebugUtils.printStructLayout<Beam>();
        //     DebugUtils.printStructLayout<BeamImage>();
        // }
    }
}