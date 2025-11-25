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
    /// Rectangle utility class for room boundaries
    /// </summary>
    [System.Serializable]
    public class DungeonRect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public DungeonRect()
        {
            left = top = right = bottom = 0;
        }

        public DungeonRect(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        public DungeonRect(DungeonRect other)
        {
            Set(other);
        }

        public DungeonRect Set(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            return this;
        }

        public DungeonRect Set(DungeonRect other)
        {
            this.left = other.left;
            this.top = other.top;
            this.right = other.right;
            this.bottom = other.bottom;
            return this;
        }

        public DungeonRect SetPos(int x, int y)
        {
            int w = Width();
            int h = Height();
            left = x;
            top = y;
            right = x + w - 1;
            bottom = y + h - 1;
            return this;
        }

        public DungeonRect Shift(int dx, int dy)
        {
            left += dx;
            right += dx;
            top += dy;
            bottom += dy;
            return this;
        }

        public DungeonRect Resize(int w, int h)
        {
            right = left + w;
            bottom = top + h;
            return this;
        }

        public bool IsEmpty()
        {
            return right < left || bottom < top;
        }

        public DungeonRect SetEmpty()
        {
            left = right = top = bottom = 0;
            return this;
        }

        public DungeonRect Intersect(DungeonRect other)
        {
            DungeonRect result = new DungeonRect();
            result.left = Mathf.Max(left, other.left);
            result.right = Mathf.Min(right, other.right);
            result.top = Mathf.Max(top, other.top);
            result.bottom = Mathf.Min(bottom, other.bottom);
            return result;
        }

        public DungeonRect Union(DungeonRect other)
        {
            if (IsEmpty()) return new DungeonRect(other);
            if (other.IsEmpty()) return new DungeonRect(this);

            return new DungeonRect(
                Mathf.Min(left, other.left),
                Mathf.Min(top, other.top),
                Mathf.Max(right, other.right),
                Mathf.Max(bottom, other.bottom)
            );
        }

        public virtual int Width()
        {
            return right - left + 1;
        }

        public virtual int Height()
        {
            return bottom - top + 1;
        }

        public int Square()
        {
            return Width() * Height();
        }

        public List<Vector2Int> GetPoints()
        {
            List<Vector2Int> points = new List<Vector2Int>();
            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    points.Add(new Vector2Int(x, y));
                }
            }
            return points;
        }

        public override string ToString()
        {
            return $"Rect({left}, {top}, {right}, {bottom})";
        }
    }
}
