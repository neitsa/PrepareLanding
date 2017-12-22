using System;
using UnityEngine;

namespace PrepareLanding.Core.Extensions
{
    public static class Vector3Extensions
    {
        public static Vector3 Round(this Vector3 vec, int digits = 1)
        {
            var vecX = (float)Math.Round(vec.x, digits);
            var vecY = (float)Math.Round(vec.y, digits);
            var vecZ = (float)Math.Round(vec.z, digits);
            return new Vector3(vecX, vecY, vecZ);
        }
    }
}
