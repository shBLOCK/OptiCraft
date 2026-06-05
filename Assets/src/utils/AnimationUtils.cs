using Unity.Mathematics;

namespace utils {
    public static class AnimationUtils {
        public static quaternion deviceRotationAnimation(
            float progress,
            quaternion lastRot, quaternion currentRot,
            float3 axis, float angle,
            out bool renderNew
        ) {
            if (progress >= 1f) {
                renderNew = true;
                return currentRot;
            } else {
                progress = (1f - math.cos(progress * math.PI)) * 0.5f;
                renderNew = progress > 0.5f;
                if (!renderNew) {
                    return quaternion.AxisAngle(axis, math.lerp(0f, angle, progress)).mul(lastRot);
                } else {
                    return quaternion.AxisAngle(axis, math.lerp(-angle, 0f, progress)).mul(currentRot);
                }
            }
        }
    }
}