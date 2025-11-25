/*
 * Unity Dungeon Room Generator
 * Based on Shattered Pixel Dungeon room generation system
 * Original: Copyright (C) 2014-2021 Evan Debenham
 * Ported for Unity by: GitHub Copilot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 */

using UnityEngine;
using System.Collections.Generic;

namespace DungeonGenerator
{
    /// <summary>
    /// Random utility class providing various random number generation methods
    /// </summary>
    public static class DungeonRandom
    {
        private static System.Random _random = new System.Random();

        /// <summary>
        /// Sets the seed for random number generation
        /// </summary>
        public static void SetSeed(int seed)
        {
            _random = new System.Random(seed);
        }

        /// <summary>
        /// Returns a random integer from 0 to max-1
        /// </summary>
        public static int Int(int max)
        {
            return _random.Next(max);
        }

        /// <summary>
        /// Returns a random integer from min to max-1
        /// </summary>
        public static int Int(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Returns a random integer from min to max (inclusive)
        /// </summary>
        public static int IntRange(int min, int max)
        {
            return _random.Next(min, max + 1);
        }

        /// <summary>
        /// Returns a random integer with a normal distribution between min and max
        /// </summary>
        public static int NormalIntRange(int min, int max)
        {
            return (int)Mathf.Round((Float(min, max) + Float(min, max)) / 2f);
        }

        /// <summary>
        /// Returns a random float from 0 to max
        /// </summary>
        public static float Float(float max)
        {
            return (float)_random.NextDouble() * max;
        }

        /// <summary>
        /// Returns a random float from 0 to 1
        /// </summary>
        public static float Float()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>
        /// Returns a random float from min to max
        /// </summary>
        public static float Float(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Returns a random element from the list
        /// </summary>
        public static T Element<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[Int(list.Count)];
        }

        /// <summary>
        /// Returns a random element from the array
        /// </summary>
        public static T Element<T>(T[] array)
        {
            if (array == null || array.Length == 0) return default(T);
            return array[Int(array.Length)];
        }

        /// <summary>
        /// Shuffles the list in place
        /// </summary>
        public static void Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Int(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        /// <summary>
        /// Returns an index based on the probability weights in the chances array
        /// Returns -1 if all chances are 0
        /// </summary>
        public static int Chances(float[] chances)
        {
            float total = 0;
            foreach (float chance in chances)
            {
                total += chance;
            }

            if (total <= 0) return -1;

            float value = Float(total);
            for (int i = 0; i < chances.Length; i++)
            {
                if (value < chances[i])
                {
                    return i;
                }
                value -= chances[i];
            }

            return chances.Length - 1;
        }
    }
}
