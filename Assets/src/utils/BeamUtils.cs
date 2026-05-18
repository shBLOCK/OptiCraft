using System;
using System.Runtime.CompilerServices;

namespace utils {
    public readonly struct BeamIOPair : IEquatable<BeamIOPair> {
        public static BeamIOPair INVALID = new(ushort.MaxValue, ushort.MaxValue);

        public readonly ushort input;
        public readonly ushort output;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BeamIOPair(ushort input, ushort output) {
            this.input = input;
            this.output = output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BeamIOPair other) {
            return input == other.input && output == other.output;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is BeamIOPair other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() {
            return HashCode.Combine(input, output);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BeamIOPair a, BeamIOPair b) => a.Equals(b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BeamIOPair a, BeamIOPair b) => !a.Equals(b);
    }
}