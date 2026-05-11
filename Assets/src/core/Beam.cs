using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace core {
    public sealed class Beam {
        public readonly SimSpace space;
        public readonly AxisDirection direction;
        public int3 tailPos { get; private set; }
        public int length = 0;
        public int3 headPos => tailPos.offset(direction, length);
        public bool beingEmitted { get; private set; } = false;
        public bool beingConsumed { get; private set; } = false;
        public bool wasBeingEmitted { get; private set; } = false;
        public bool wasBeingConsumed { get; private set; } = false;

        public readonly BeamImage image;

        /// Used by devices that consume the beam
        public Beam userData_Beam = null;

        public Beam(SimSpace space, AxisDirection direction, int3 startPos, BeamImage image) {
            this.space = space;
            this.direction = direction;
            this.tailPos = startPos;
            this.image = image;
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
    }

    public sealed class BeamImageData {
        public readonly int2 size;

        [CanBeNull] private RenderTexture renderTexture = null;
        // //TODO: cpu side for small stuff
        // private float4[,] data;
        // private NativeArray<float4> nativeData = default;

        public BeamImageData(int2 size) {
            this.size = size;
        }

        private void createRenderTexture() {
            renderTexture = new RenderTexture(
                size.x, size.y,
                GraphicsFormat.R32G32B32A32_SFloat,
                GraphicsFormat.None,
                0
            );
            renderTexture.enableRandomWrite = true;
            renderTexture.useMipMap = false;
            renderTexture.autoGenerateMips = false;
            renderTexture.filterMode = FilterMode.Point;
            Assert.IsTrue(renderTexture.Create());
        }

        public Texture getTexture() {
            if (!renderTexture) {
                return Texture2D.redTexture; // TODO
            }

            return renderTexture;
        }

        public static BeamImageData fromTexture2D(Texture2D texture) {
            BeamImageData obj = new(new int2(texture.width, texture.height));
            obj.createRenderTexture();
            Graphics.Blit(texture, obj.renderTexture);
            return obj;
        }

        public RenderTexture _tmp_getRT() {
            if (renderTexture == null) createRenderTexture();
            return renderTexture;
        }
    }

    public readonly struct BeamImage {
        private static Texture SINGLE_PIXEL_TEXTURE = null;

        [RuntimeInitializeOnLoadMethod]
        private static void INIT() {
            var texture = new Texture2D(1, 1, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);
            texture.SetPixel(0, 0, new Color(1, 1, 1, 1));
            texture.Apply();
            SINGLE_PIXEL_TEXTURE = texture;
        }

        public readonly BeamImageData data;

        public int2 size => data?.size ?? new int2(1, 1);

        public readonly float4 tint;
        //TODO
        // public readonly Orientation orientation;
        // public readonly byte2 scale;
        // public readonly int2 offset;

        public BeamImage(BeamImageData data, float4 tint) {
            this.data = data;
            this.tint = tint;
        }

        public static BeamImage singlePixel(float4 color) => new(null, color);

        public BeamImage withTint(float4 newTint) => new(data, newTint);

        public bool isEqualConservative(BeamImage other) =>
            data == other.data && math.all(tint == other.tint);

        public Texture getTexture() {
            if (data == null) {
                return SINGLE_PIXEL_TEXTURE;
            }

            return data.getTexture();
        }

        public void _tmp_setToCS(ComputeShader cs, int kernel, string name) {
            if (data != null) {
                cs.SetTexture(kernel, name, data._tmp_getRT());
            } else {
                cs.SetTexture(kernel, name, SINGLE_PIXEL_TEXTURE);
            }

            cs.SetVector(name + "Tint", tint);
        }
    }
}