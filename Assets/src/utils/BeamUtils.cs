using System;

namespace utils {
    public readonly struct BeamIOPair : IEquatable<BeamIOPair> {
        public static BeamIOPair INVALID = new(ushort.MaxValue, ushort.MaxValue);

        public readonly ushort input;
        public readonly ushort output;

        public BeamIOPair(ushort input, ushort output) {
            this.input = input;
            this.output = output;
        }

        public bool Equals(BeamIOPair other) {
            return input == other.input && output == other.output;
        }

        public override bool Equals(object obj) {
            return obj is BeamIOPair other && Equals(other);
        }

        public override int GetHashCode() {
            return HashCode.Combine(input, output);
        }

        public static bool operator ==(BeamIOPair a, BeamIOPair b) => a.Equals(b);
        public static bool operator !=(BeamIOPair a, BeamIOPair b) => !a.Equals(b);
    }
}