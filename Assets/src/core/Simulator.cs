using UnityEngine;

namespace core {
    public class Simulator : MonoBehaviour {
        public int tickNumber { get; private set; } = 0;
        public float partialTick = 1f;

        public SimSpace rootSpace;

        public void tick() {
            rootSpace.tick();
        }

        public void reset() {
            tickNumber = 0;
            partialTick = 1f;
            rootSpace.reset();
        }
    }
}