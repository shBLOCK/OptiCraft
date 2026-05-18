using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace utils {
    public static class ShaderUtils {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint2 getKernelThreadGroupSizeUInt2(this ComputeShader cs, int kernelIndex) {
            cs.GetKernelThreadGroupSizes(kernelIndex, out var x, out var y, out _);
            return new uint2(x, y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void dispatchCompute2D(
            this CommandBuffer cmds,
            ComputeShader cs, int kernelIndex, uint2 totalSize
        ) {
            var kernelSize = cs.getKernelThreadGroupSizeUInt2(kernelIndex);
            var groupSize = totalSize.ceilDiv(kernelSize);
            cmds.DispatchCompute(cs, kernelIndex, (int)groupSize.x, (int)groupSize.y, 1);
        }
    }
}