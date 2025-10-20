// Copyright (c) 2025 Maged Farid
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Linq;
using UnityEngine;

namespace RecyclableScrollRect
{
    public static class Helpers
    {
        public static Vector2 Abs(this Vector2 vec2)
        {
            vec2.x = Mathf.Abs(vec2.x);
            vec2.y = Mathf.Abs(vec2.y);
            return vec2;
        }

        public static Vector3 Abs(this Vector3 vec3)
        {
            vec3.x = Mathf.Abs(vec3.x);
            vec3.y = Mathf.Abs(vec3.y);
            vec3.z = Mathf.Abs(vec3.z);
            return vec3;
        }
        
        private static System.Random random = new System.Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}