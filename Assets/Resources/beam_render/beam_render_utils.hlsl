#pragma once

struct RayRectIntersection {
    float enterDist;
    float exitDist;
};

RayRectIntersection rayRectIntersect(float2 origin, float2 dir, float2 rectHalfSize) {
    RayRectIntersection result;
    result.enterDist = 1.#INF;
    result.exitDist = 1.#INF;

    // parallel
    if ((dir.x == 0.0 && abs(origin.x) > rectHalfSize.x) ||
        (dir.y == 0.0 && abs(origin.y) > rectHalfSize.y)) {
        return result;
    }

    float2 invDir = 1.0 / dir;

    float2 t0 = (-rectHalfSize - origin) * invDir;
    float2 t1 = (rectHalfSize - origin) * invDir;

    float2 tMin = min(t0, t1);
    float2 tMax = max(t0, t1);

    float enter = max(tMin.x, tMin.y);
    float exit = min(tMax.x, tMax.y);

    bool inside = all(abs(origin) <= rectHalfSize);

    bool hit = exit >= 0.0 && (inside || enter <= exit);

    if (!hit) return result;

    result.enterDist = inside ? 0.0 : max(enter, 0.0);
    result.exitDist = exit;

    return result;
}


struct PixelGridWalker {
    int2 pixel; // current pixel

    float2 tMax; // distance until next X/Y boundary
    float2 tDelta; // distance to cross one full cell in X/Y

    int2 step; // ±1 direction
};

PixelGridWalker initPixelGridWalker(
    float2 origin,
    float2 dir, // normalized
    float2 pixelSize
) {
    PixelGridWalker w;

    float2 cell = origin / pixelSize;

    w.pixel = int2(floor(cell));

    // Boundary belongs to next cell along ray
    if (dir.x < 0 && frac(cell.x) == 0)
        w.pixel.x--;

    if (dir.y < 0 && frac(cell.y) == 0)
        w.pixel.y--;

    w.step = int2(sign(dir));

    w.tDelta = pixelSize / abs(dir);

    if (dir.x == 0) w.tDelta.x = 1e30;
    if (dir.y == 0) w.tDelta.y = 1e30;

    float2 nextBoundary;

    nextBoundary.x =
        (dir.x >= 0)
            ? (w.pixel.x + 1) * pixelSize.x
            : w.pixel.x * pixelSize.x;

    nextBoundary.y =
        (dir.y >= 0)
            ? (w.pixel.y + 1) * pixelSize.y
            : w.pixel.y * pixelSize.y;

    w.tMax.x =
        dir.x == 0
            ? 1e30
            : abs((nextBoundary.x - origin.x) / dir.x);

    w.tMax.y =
        dir.y == 0
            ? 1e30
            : abs((nextBoundary.y - origin.y) / dir.y);

    return w;
}


// Returns distance travelled this step
float stepPixelGridWalker(inout PixelGridWalker w) {
    float stepLen;

    if (w.tMax.x < w.tMax.y) {
        stepLen = w.tMax.x;

        w.pixel.x += w.step.x;

        w.tMax.y -= stepLen;
        w.tMax.x = w.tDelta.x;
    } else {
        stepLen = w.tMax.y;

        w.pixel.y += w.step.y;

        w.tMax.x -= stepLen;
        w.tMax.y = w.tDelta.y;
    }

    return stepLen;
}
