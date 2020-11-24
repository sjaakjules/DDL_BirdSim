using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeepDesignLab.Base {
    public static class DeepDesignEctensions
    {
        public static float[] Add(this float[] a, float[] b)
        {
            if (a.Length == b.Length)
            {
                float[] output = new float[a.Length];
                for (int i = 0; i < a.Length; i++)
                {
                    output[i] = a[i] + b[i];
                }
                return output;
            }
            return null;
        }
        public static void Multiply(this float[] a, float b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = a[i] * b;
            }
        }
        /// <summary>
        /// Buckets the vector into middle averaged buckets of size _bucketSize. ie. if 0.1, -0.05 to 0.05 not inclusive will be a value of 0.
        /// </summary>
        /// <param name="_vector"></param> returns the vecter value with rounding buckets.
        /// <param name="_bucketSize"></param> Amount to bucket around. 0 is middle of bucket with +-(_bucketSize) returning value of 0.
        /// <returns></returns>
        public static Vector3 bucket(this Vector3 _vector, float _bucketSize)
        {

            // spatial bucket, such as 2^8 (64)
            _bucketSize = Mathf.Abs(_bucketSize);

            return new Vector3((float)Math.Round(Math.Abs(_vector.x / _bucketSize)) * _bucketSize * Math.Sign(_vector.x),
                                (float)Math.Round(Math.Abs(_vector.y / _bucketSize)) * _bucketSize * Math.Sign(_vector.y),
                                (float)Math.Round(Math.Abs(_vector.z / _bucketSize)) * _bucketSize * Math.Sign(_vector.z));

        }
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
    }
}
