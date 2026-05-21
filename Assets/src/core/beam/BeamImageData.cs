using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace core.beam {
    public sealed class BeamImageData {
        public const ushort INVALID_ID = ushort.MaxValue;

        public readonly Manager manager;
        public readonly ushort id;

        public readonly uint2 size;

        [CanBeNull] private RenderTexture renderTexture = null;
        // //TODO: cpu side for small stuff
        // private float4[,] data;
        // private NativeArray<float4> nativeData = default;

        private BeamImageData(Manager manager, ushort id, uint2 size) {
            this.manager = manager;
            this.id = id;
            this.size = size;
        }

        private void createRenderTexture() {
            if (renderTexture) return;
            renderTexture = new RenderTexture(
                (int)size.x, (int)size.y,
                GraphicsFormat.R32G32B32A32_SFloat,
                GraphicsFormat.None,
                0
            ) {
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Point
            };
            Assert.IsTrue(renderTexture.Create());
        }

        public Texture getTexture() {
            if (!renderTexture) {
                return Texture2D.redTexture; // TODO
            }

            return renderTexture;
        }

        public void blitFromTexture(Texture texture) {
            createRenderTexture();
            Graphics.Blit(texture, renderTexture);
        }

        public RenderTexture _tmp_getRT() {
            if (!renderTexture) createRenderTexture();
            return renderTexture;
        }

        private ushort refCount = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void incRef() => refCount++;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void decRef() {
            refCount--;
            if (refCount == 0) {
                manager._free(this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void free() {
            if (renderTexture) renderTexture.Release();
        }

        public sealed class Manager {
            private List<BeamImageData> instances = new();
            private Stack<ushort> freeSlots = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BeamImageData get(ushort id) => instances[id];

            public BeamImageData addNew(uint2 size) {
                BeamImageData data;
                if (freeSlots.TryPop(out var slot)) {
                    instances[slot] = data = new BeamImageData(this, slot, size);
                } else {
                    data = new BeamImageData(this, (ushort)instances.Count, size);
                    instances.Add(data);
                }

                return data;
            }

            internal void _free(BeamImageData instance) {
                instance.free();
                instances[instance.id] = null;
                freeSlots.Push(instance.id);
            }

            internal void reset() {
                instances.ForEach(it => {
                    if (it != null) it.free();
                });
                instances.Clear();
                freeSlots.Clear();
            }
        }
    }
}