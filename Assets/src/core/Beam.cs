using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace core {
    public abstract class Beam {
        public readonly SimSpace space;
        public readonly AxisDirection direction;
        public int3 tailPos { get; private set; }
        public int length = 0;
        public int3 headPos => tailPos.offset(direction, length);
        public bool beingEmitted { get; private set; } = false;
        public bool beingConsumed { get; private set; } = false;
        public bool wasBeingEmitted { get; private set; } = false;
        public bool wasBeingConsumed { get; private set; } = false;

        public Beam(SimSpace space, AxisDirection direction, int3 startPos) {
            this.space = space;
            this.direction = direction;
            this.tailPos = startPos;
        }
        
        public Beam emit() {
            Assert.IsFalse(beingEmitted, "Beam already emitted");
            beingEmitted = true;
            space._emitBeam(this);
            return this;
        }

        public void stopEmit() {
            Assert.IsTrue(beingEmitted, "Beam not emitted yet");
            beingEmitted = false;
        }

        public void consume() {
            Assert.IsFalse(beingConsumed, "Beam already consumed");
            beingConsumed = true;
        }

        public bool tick() {
            if (!beingEmitted) {
                tailPos += direction.int3();
            }
            
            if (beingEmitted) length++;
            if (beingConsumed) length--;
            
            wasBeingEmitted = beingEmitted;
            wasBeingConsumed = beingConsumed;
            
            return length > 0;
        }

        public sealed class Laser : Beam {
            public readonly int3 color;

            public Laser(SimSpace space, AxisDirection direction, int3 startPos, int3 color)
                : base(space, direction, startPos) {
                this.color = color;
            }
        }
    }

    // public record Beam {
    //     void a() {
    //         var l = new Laser();
    //     }
    // }
}