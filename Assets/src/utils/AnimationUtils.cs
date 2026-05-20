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
                var rot = quaternion.AxisAngle(axis, -angle);
                if (!renderNew) {
                    return math.slerp(quaternion.identity, rot, progress).mul(lastRot);
                } else {
                    return math.slerp(quaternion.identity, rot.inverse(), 1f - progress).mul(currentRot);
                }
            }
        }
    }
}