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
    /// Abstract base class for dungeon builders
    /// Builders take a list of rooms and return them as a connected map
    /// </summary>
    public abstract class Builder
    {
        private const double A = 180.0 / System.Math.PI;

        /// <summary>
        /// Builds a connected dungeon from the given rooms
        /// Returns null on failure
        /// </summary>
        public abstract List<Room> Build(List<Room> rooms);

        /// <summary>
        /// Finds all neighbouring rooms in the room list
        /// </summary>
        protected static void FindNeighbours(List<Room> rooms)
        {
            Room[] ra = rooms.ToArray();
            for (int i = 0; i < ra.Length - 1; i++)
            {
                for (int j = i + 1; j < ra.Length; j++)
                {
                    ra[i].AddNeighbour(ra[j]);
                }
            }
        }

        /// <summary>
        /// Returns a rectangle representing the maximum amount of free space from a specific start point
        /// </summary>
        protected static DungeonRect FindFreeSpace(Vector2Int start, List<Room> collision, int maxSize)
        {
            DungeonRect space = new DungeonRect(
                start.x - maxSize, start.y - maxSize,
                start.x + maxSize, start.y + maxSize
            );

            // Shallow copy
            List<Room> colliding = new List<Room>(collision);

            do
            {
                // Remove empty rooms and any rooms we aren't currently overlapping
                for (int i = colliding.Count - 1; i >= 0; i--)
                {
                    Room room = colliding[i];
                    // If not colliding
                    if (room.IsEmpty()
                        || Mathf.Max(space.left, room.left) >= Mathf.Min(space.right, room.right)
                        || Mathf.Max(space.top, room.top) >= Mathf.Min(space.bottom, room.bottom))
                    {
                        colliding.RemoveAt(i);
                    }
                }

                // Iterate through all rooms we are overlapping, and find the closest one
                Room closestRoom = null;
                int closestDiff = int.MaxValue;
                bool inside = true;
                int curDiff = 0;

                foreach (Room curRoom in colliding)
                {
                    curDiff = 0;
                    inside = true;

                    if (start.x <= curRoom.left)
                    {
                        inside = false;
                        curDiff += curRoom.left - start.x;
                    }
                    else if (start.x >= curRoom.right)
                    {
                        inside = false;
                        curDiff += start.x - curRoom.right;
                    }

                    if (start.y <= curRoom.top)
                    {
                        inside = false;
                        curDiff += curRoom.top - start.y;
                    }
                    else if (start.y >= curRoom.bottom)
                    {
                        inside = false;
                        curDiff += start.y - curRoom.bottom;
                    }

                    if (inside)
                    {
                        space.Set(start.x, start.y, start.x, start.y);
                        return space;
                    }

                    if (curDiff < closestDiff)
                    {
                        closestDiff = curDiff;
                        closestRoom = curRoom;
                    }
                }

                int wDiff, hDiff;
                if (closestRoom != null)
                {
                    wDiff = int.MaxValue;
                    if (closestRoom.left >= start.x)
                    {
                        wDiff = (space.right - closestRoom.left) * (space.Height() + 1);
                    }
                    else if (closestRoom.right <= start.x)
                    {
                        wDiff = (closestRoom.right - space.left) * (space.Height() + 1);
                    }

                    hDiff = int.MaxValue;
                    if (closestRoom.top >= start.y)
                    {
                        hDiff = (space.bottom - closestRoom.top) * (space.Width() + 1);
                    }
                    else if (closestRoom.bottom <= start.y)
                    {
                        hDiff = (closestRoom.bottom - space.top) * (space.Width() + 1);
                    }

                    // Reduce by as little as possible to resolve the collision
                    if (wDiff < hDiff || (wDiff == hDiff && DungeonRandom.Int(2) == 0))
                    {
                        if (closestRoom.left >= start.x && closestRoom.left < space.right) space.right = closestRoom.left;
                        if (closestRoom.right <= start.x && closestRoom.right > space.left) space.left = closestRoom.right;
                    }
                    else
                    {
                        if (closestRoom.top >= start.y && closestRoom.top < space.bottom) space.bottom = closestRoom.top;
                        if (closestRoom.bottom <= start.y && closestRoom.bottom > space.top) space.top = closestRoom.bottom;
                    }
                    colliding.Remove(closestRoom);
                }
                else
                {
                    colliding.Clear();
                }

                // Loop until we are no longer colliding with any rooms
            } while (colliding.Count > 0);

            return space;
        }

        /// <summary>
        /// Returns the angle in degrees made by the centerpoints of 2 rooms, with 0 being straight up
        /// </summary>
        protected static float AngleBetweenRooms(Room from, Room to)
        {
            Vector2 fromCenter = new Vector2((from.left + from.right) / 2f, (from.top + from.bottom) / 2f);
            Vector2 toCenter = new Vector2((to.left + to.right) / 2f, (to.top + to.bottom) / 2f);
            return AngleBetweenPoints(fromCenter, toCenter);
        }

        /// <summary>
        /// Returns the angle between two points
        /// </summary>
        protected static float AngleBetweenPoints(Vector2 from, Vector2 to)
        {
            double m = (to.y - from.y) / (to.x - from.x);

            float angle = (float)(A * (System.Math.Atan(m) + System.Math.PI / 2.0));
            if (from.x > to.x) angle -= 180f;
            return angle;
        }

        /// <summary>
        /// Clamps value between min and max (inclusive)
        /// </summary>
        protected static float Gate(float min, float value, float max)
        {
            return Mathf.Max(min, Mathf.Min(max, value));
        }

        /// <summary>
        /// Attempts to place a room such that the angle between the center of the previous room
        /// and it matches the given angle ([0-360), where 0 is straight up) as closely as possible.
        /// Returns the exact angle between the centerpoints of the two rooms, or -1 if placement fails.
        /// </summary>
        protected static float PlaceRoom(List<Room> collision, Room prev, Room next, float angle)
        {
            // Wrap angle around to always be [0-360)
            angle %= 360f;
            if (angle < 0)
            {
                angle += 360f;
            }

            Vector2 prevCenter = new Vector2((prev.left + prev.right) / 2f, (prev.top + prev.bottom) / 2f);

            // Calculating using y = mx+b, straight line formula
            double m = System.Math.Tan(angle / A + System.Math.PI / 2.0);
            double b = prevCenter.y - m * prevCenter.x;

            // Using the line equation, we find the point along the prev room where the line exists
            Vector2Int start;
            int direction;

            if (System.Math.Abs(m) >= 1)
            {
                if (angle < 90 || angle > 270)
                {
                    direction = Room.TOP;
                    start = new Vector2Int((int)System.Math.Round((prev.top - b) / m), prev.top);
                }
                else
                {
                    direction = Room.BOTTOM;
                    start = new Vector2Int((int)System.Math.Round((prev.bottom - b) / m), prev.bottom);
                }
            }
            else
            {
                if (angle < 180)
                {
                    direction = Room.RIGHT;
                    start = new Vector2Int(prev.right, (int)System.Math.Round(m * prev.right + b));
                }
                else
                {
                    direction = Room.LEFT;
                    start = new Vector2Int(prev.left, (int)System.Math.Round(m * prev.left + b));
                }
            }

            // Cap it to a valid connection point for most rooms
            if (direction == Room.TOP || direction == Room.BOTTOM)
            {
                start.x = (int)Gate(prev.left + 1, start.x, prev.right - 1);
            }
            else
            {
                start.y = (int)Gate(prev.top + 1, start.y, prev.bottom - 1);
            }

            // Space checking
            DungeonRect space = FindFreeSpace(start, collision, Mathf.Max(next.MaxWidth(), next.MaxHeight()));
            if (!next.SetSizeWithLimit(space.Width() + 1, space.Height() + 1))
            {
                return -1;
            }

            // Find the ideal center for this new room using the line equation and known dimensions
            Vector2 targetCenter = new Vector2();

            if (direction == Room.TOP)
            {
                targetCenter.y = prev.top - (next.Height() - 1) / 2f;
                targetCenter.x = (float)((targetCenter.y - b) / m);
                next.SetPos(Mathf.RoundToInt(targetCenter.x - (next.Width() - 1) / 2f), prev.top - (next.Height() - 1));
            }
            else if (direction == Room.BOTTOM)
            {
                targetCenter.y = prev.bottom + (next.Height() - 1) / 2f;
                targetCenter.x = (float)((targetCenter.y - b) / m);
                next.SetPos(Mathf.RoundToInt(targetCenter.x - (next.Width() - 1) / 2f), prev.bottom);
            }
            else if (direction == Room.RIGHT)
            {
                targetCenter.x = prev.right + (next.Width() - 1) / 2f;
                targetCenter.y = (float)(m * targetCenter.x + b);
                next.SetPos(prev.right, Mathf.RoundToInt(targetCenter.y - (next.Height() - 1) / 2f));
            }
            else if (direction == Room.LEFT)
            {
                targetCenter.x = prev.left - (next.Width() - 1) / 2f;
                targetCenter.y = (float)(m * targetCenter.x + b);
                next.SetPos(prev.left - (next.Width() - 1), Mathf.RoundToInt(targetCenter.y - (next.Height() - 1) / 2f));
            }

            // Perform connection bounds and target checking, move the room if necessary
            if (direction == Room.TOP || direction == Room.BOTTOM)
            {
                if (next.right < prev.left + 2) next.Shift(prev.left + 2 - next.right, 0);
                else if (next.left > prev.right - 2) next.Shift(prev.right - 2 - next.left, 0);

                if (next.right > space.right) next.Shift(space.right - next.right, 0);
                else if (next.left < space.left) next.Shift(space.left - next.left, 0);
            }
            else
            {
                if (next.bottom < prev.top + 2) next.Shift(0, prev.top + 2 - next.bottom);
                else if (next.top > prev.bottom - 2) next.Shift(0, prev.bottom - 2 - next.top);

                if (next.bottom > space.bottom) next.Shift(0, space.bottom - next.bottom);
                else if (next.top < space.top) next.Shift(0, space.top - next.top);
            }

            // Attempt to connect, return the result angle if successful
            if (next.Connect(prev))
            {
                return AngleBetweenRooms(prev, next);
            }
            else
            {
                return -1;
            }
        }
    }
}
