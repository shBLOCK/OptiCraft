using System.Text.Json.Serialization;
using core;
using core.beam;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using utils;

namespace level.validation;

public class MSEImageValidator : LevelValidator {
    [JsonInclude, JsonConverter(typeof(JsonUtils.UnityResourcesConverter<Texture2D>))]
    private Texture2D reference;

    [JsonInclude] private float threshold;

    private static ComputeShader CS;
    private static int CSK;
    private static BeamImageShaderUniform uInputImage = new("uInputImage");
    private static int uReferenceImage = Shader.PropertyToID("uReferenceImage");
    private static int uOutputImage = Shader.PropertyToID("uOutputImage");

    [RuntimeInitializeOnLoadMethod]
    private static void INIT() {
        CS = Resources.Load<ComputeShader>("validation/MSE");
        CSK = CS.FindKernel("MSE");
    }

    private RenderTexture outputRT;
    private NativeArray<float> outputBuffer;

    private void OnEnable() {
        Assert.IsFalse(reference.isDataSRGB);
        Assert.IsTrue(
            reference.format == TextureFormat.RGBAFloat &&
            reference.graphicsFormat == GraphicsFormat.R32G32B32A32_SFloat
        );

        outputRT = new RenderTexture(new RenderTextureDescriptor(
            reference.width, reference.height, RenderTextureFormat.RFloat
        )) {
            enableRandomWrite = true
        };
        outputRT.Create();
    }

    private void OnDisable() {
        outputRT.Release();
    }

    private void compute(SimSpace space, BeamImage image, CommandBuffer cmds) {
        image.setToShader(space.simulator.beamImageDataManager, cmds, CS, CSK, uInputImage);
        cmds.SetComputeTextureParam(CS, CSK, uReferenceImage, reference, 0);
        cmds.SetComputeTextureParam(CS, CSK, uOutputImage, outputRT, 0);
        cmds.dispatchCompute2D(CS, CSK, reference.size().asuint());
        cmds.RequestAsyncReadbackIntoNativeArray(ref outputBuffer, outputRT, request => {
            //TODO
        });
        //TODO: readback
    }
}