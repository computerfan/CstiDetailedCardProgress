using System;
using UnityEngine;

namespace CstiDetailedCardProgress;

public static class VectorMath
{
    public static Vector2 RangeIntersect(Vector2 a, Vector2 b)
    {
        float left = Math.Max(a.x, b.x);
        float right = Math.Min(a.y, b.y);
        return left <= right ? new Vector2(left, right) : Vector2.zero;
    }

    public static float RangeLength(this Vector2 vec)
    {
        return Mathf.Abs(vec.y - vec.x);
    }

    public static float RangeMidValue(this Vector2 vec)
    {
        return Mathf.Lerp(vec.x, vec.y, 0.5f);
    }

    public static Vector2Int ToInt(this Vector2 vec)
    {
        return new Vector2Int((int)vec.x, (int)vec.y);
    }
}