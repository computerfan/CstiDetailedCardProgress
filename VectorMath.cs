using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CstiDetailedCardProgress
{
    public static class VectorMath
    {
        public static Vector2 RangeIntersect(Vector2 a, Vector2 b)
        {
            var left = Math.Max(a.x, b.x);
            var right = Math.Min(a.y, b.y);
            if (left <= right) return new(left, right);
            else return Vector2.zero;
        }
        public static float RangeLength(this Vector2 vec) => Mathf.Abs(vec.y - vec.x);
        public static float RangeMidValue(this Vector2 vec) => Mathf.Lerp(vec.x, vec.y, 0.5f);
        public static Vector2Int ToInt(this Vector2 vec) => new((int)vec.x, (int)vec.y);
    }
}
