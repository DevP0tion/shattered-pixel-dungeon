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
    /// A builder with one core loop as its primary element
    /// Creates dungeon layouts with rooms arranged in a circular pattern
    /// </summary>
    public class LoopBuilder : RegularBuilder
    {
        // Loop shape parameters
        private int curveExponent = 0;
        private float curveIntensity = 1;
        private float curveOffset = 0;

        private Vector2 loopCenter;

        /// <summary>
        /// Adjusts the shape of the loop
        /// exponent: increasing makes the loop more oval shaped
        /// intensity: 0 = perfect circle, 1 = fully affected by curve exponent
        /// offset: adjusts starting point along the loop (0.25 = short fat oval)
        /// </summary>
        public LoopBuilder SetLoopShape(int exponent, float intensity, float offset)
        {
            this.curveExponent = Mathf.Abs(exponent);
            curveIntensity = intensity % 1f;
            curveOffset = offset % 0.5f;
            return this;
        }

        private float TargetAngle(float percentAlong)
        {
            percentAlong += curveOffset;
            return 360f * (float)(
                curveIntensity * CurveEquation(percentAlong)
                + (1 - curveIntensity) * percentAlong
                - curveOffset);
        }

        private double CurveEquation(double x)
        {
            return System.Math.Pow(4, 2 * curveExponent)
                * (System.Math.Pow((x % 0.5f) - 0.25, 2 * curveExponent + 1))
                + 0.25 + 0.5 * System.Math.Floor(2 * x);
        }

        /// <summary>
        /// Builds a dungeon with a loop layout
        /// </summary>
        public override List<Room> Build(List<Room> rooms)
        {
            SetupRooms(rooms);

            if (entrance == null)
            {
                return null;
            }

            entrance.SetSize();
            entrance.SetPos(0, 0);

            float startAngle = DungeonRandom.Float(0, 360);

            List<Room> loop = new List<Room>();
            int roomsOnLoop = (int)(multiConnections.Count * pathLength) + DungeonRandom.Chances(pathLenJitterChances);
            roomsOnLoop = Mathf.Min(roomsOnLoop, multiConnections.Count);

            float[] pathTunnels = (float[])pathTunnelChances.Clone();
            for (int i = 0; i <= roomsOnLoop; i++)
            {
                if (i == 0)
                    loop.Add(entrance);
                else
                    loop.Add(multiConnections[0]);

                if (i > 0) multiConnections.RemoveAt(0);

                int tunnels = DungeonRandom.Chances(pathTunnels);
                if (tunnels == -1)
                {
                    pathTunnels = (float[])pathTunnelChances.Clone();
                    tunnels = DungeonRandom.Chances(pathTunnels);
                }
                pathTunnels[tunnels]--;

                for (int j = 0; j < tunnels; j++)
                {
                    loop.Add(CreateConnectionRoom());
                }
            }

            if (exit != null) loop.Insert((loop.Count + 1) / 2, exit);

            Room prev = entrance;
            float targetAngle;
            for (int i = 1; i < loop.Count; i++)
            {
                Room r = loop[i];
                targetAngle = startAngle + TargetAngle(i / (float)loop.Count);
                if (PlaceRoom(rooms, prev, r, targetAngle) != -1)
                {
                    prev = r;
                    if (!rooms.Contains(prev))
                        rooms.Add(prev);
                }
                else
                {
                    // Placement failed
                    return null;
                }
            }

            // Close the loop
            while (!prev.Connect(entrance))
            {
                Room c = CreateConnectionRoom();
                if (PlaceRoom(loop, prev, c, AngleBetweenRooms(prev, entrance)) == -1)
                {
                    return null;
                }
                loop.Add(c);
                rooms.Add(c);
                prev = c;
            }

            // Place shop near entrance if present
            if (shop != null)
            {
                float angle;
                int tries = 10;
                do
                {
                    angle = PlaceRoom(loop, entrance, shop, DungeonRandom.Float(360f));
                    tries--;
                } while (angle == -1 && tries >= 0);
                if (angle == -1) return null;
            }

            // Calculate loop center for branch placement
            loopCenter = Vector2.zero;
            foreach (Room r in loop)
            {
                loopCenter.x += (r.left + r.right) / 2f;
                loopCenter.y += (r.top + r.bottom) / 2f;
            }
            loopCenter.x /= loop.Count;
            loopCenter.y /= loop.Count;

            // Create branches
            List<Room> branchable = new List<Room>(loop);
            List<Room> roomsToBranch = new List<Room>();
            roomsToBranch.AddRange(multiConnections);
            roomsToBranch.AddRange(singleConnections);
            WeightRooms(branchable);
            CreateBranches(rooms, branchable, roomsToBranch, branchTunnelChances);

            // Find all neighbours
            FindNeighbours(rooms);

            // Add extra connections
            foreach (Room r in rooms)
            {
                foreach (Room n in r.neighbours)
                {
                    if (!n.connected.ContainsKey(r)
                        && DungeonRandom.Float() < extraConnectionChance)
                    {
                        r.Connect(n);
                    }
                }
            }

            return rooms;
        }

        /// <summary>
        /// Override to generate angles that point towards the loop center
        /// </summary>
        protected override float RandomBranchAngle(Room r)
        {
            if (loopCenter == Vector2.zero)
                return base.RandomBranchAngle(r);
            else
            {
                // Generate four angles randomly and return the one which points closer to the center
                float toCenter = AngleBetweenPoints(
                    new Vector2((r.left + r.right) / 2f, (r.top + r.bottom) / 2f),
                    loopCenter);
                if (toCenter < 0) toCenter += 360f;

                float currAngle = DungeonRandom.Float(360f);
                for (int i = 0; i < 4; i++)
                {
                    float newAngle = DungeonRandom.Float(360f);
                    if (Mathf.Abs(toCenter - newAngle) < Mathf.Abs(toCenter - currAngle))
                    {
                        currAngle = newAngle;
                    }
                }
                return currAngle;
            }
        }
    }
}
