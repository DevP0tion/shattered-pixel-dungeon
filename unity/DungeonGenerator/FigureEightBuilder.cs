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
    /// A builder that creates a figure-eight (two connected loops) layout
    /// Creates dungeon layouts with rooms arranged in two intersecting loops
    /// </summary>
    public class FigureEightBuilder : RegularBuilder
    {
        // Loop shape parameters
        private int curveExponent = 0;
        private float curveIntensity = 1;
        private float curveOffset = 0;

        private Room landmarkRoom;
        private List<Room> firstLoop = new List<Room>();
        private List<Room> secondLoop = new List<Room>();
        private Vector2 firstLoopCenter;
        private Vector2 secondLoopCenter;

        /// <summary>
        /// Adjusts the shape of both loops
        /// </summary>
        public FigureEightBuilder SetLoopShape(int exponent, float intensity, float offset)
        {
            this.curveExponent = Mathf.Abs(exponent);
            curveIntensity = intensity % 1f;
            curveOffset = offset % 0.5f;
            return this;
        }

        /// <summary>
        /// Sets the landmark room (intersection point of the two loops)
        /// </summary>
        public FigureEightBuilder SetLandmarkRoom(Room room)
        {
            landmarkRoom = room;
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
        /// Builds a dungeon with a figure-eight layout
        /// </summary>
        public override List<Room> Build(List<Room> rooms)
        {
            SetupRooms(rooms);

            // Select landmark room if not set
            if (landmarkRoom == null)
            {
                landmarkRoom = DungeonRandom.Element(multiConnections);
            }

            if (multiConnections.Contains(landmarkRoom))
            {
                multiConnections.Remove(landmarkRoom);
            }

            float startAngle = DungeonRandom.Float(0, 180);

            int roomsOnLoop = (int)(multiConnections.Count * pathLength) + DungeonRandom.Chances(pathLenJitterChances);
            roomsOnLoop = Mathf.Min(roomsOnLoop, multiConnections.Count);

            int roomsOnFirstLoop = roomsOnLoop / 2;
            if (roomsOnLoop % 2 == 1) roomsOnFirstLoop += DungeonRandom.Int(2);

            // Build first loop
            firstLoop = new List<Room>();
            float[] pathTunnels = (float[])pathTunnelChances.Clone();
            for (int i = 0; i <= roomsOnFirstLoop; i++)
            {
                if (i == 0)
                    firstLoop.Add(landmarkRoom);
                else
                    firstLoop.Add(multiConnections[0]);

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
                    firstLoop.Add(CreateConnectionRoom());
                }
            }
            if (entrance != null) firstLoop.Insert((firstLoop.Count + 1) / 2, entrance);

            // Build second loop
            int roomsOnSecondLoop = roomsOnLoop - roomsOnFirstLoop;
            secondLoop = new List<Room>();
            for (int i = 0; i <= roomsOnSecondLoop; i++)
            {
                if (i == 0)
                    secondLoop.Add(landmarkRoom);
                else
                    secondLoop.Add(multiConnections[0]);

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
                    secondLoop.Add(CreateConnectionRoom());
                }
            }
            if (exit != null) secondLoop.Insert((secondLoop.Count + 1) / 2, exit);

            // Position landmark room
            landmarkRoom.SetSize();
            landmarkRoom.SetPos(0, 0);

            // Place rooms on first loop
            Room prev = landmarkRoom;
            float targetAngle;
            for (int i = 1; i < firstLoop.Count; i++)
            {
                Room r = firstLoop[i];
                targetAngle = startAngle + TargetAngle(i / (float)firstLoop.Count);
                if (PlaceRoom(rooms, prev, r, targetAngle) != -1)
                {
                    prev = r;
                    if (!rooms.Contains(prev))
                        rooms.Add(prev);
                }
                else
                {
                    return null;
                }
            }

            // Close first loop
            while (!prev.Connect(landmarkRoom))
            {
                Room c = CreateConnectionRoom();
                if (PlaceRoom(rooms, prev, c, AngleBetweenRooms(prev, landmarkRoom)) == -1)
                {
                    return null;
                }
                firstLoop.Add(c);
                rooms.Add(c);
                prev = c;
            }

            // Place rooms on second loop
            prev = landmarkRoom;
            startAngle += 180f;
            for (int i = 1; i < secondLoop.Count; i++)
            {
                Room r = secondLoop[i];
                targetAngle = startAngle + TargetAngle(i / (float)secondLoop.Count);
                if (PlaceRoom(rooms, prev, r, targetAngle) != -1)
                {
                    prev = r;
                    if (!rooms.Contains(prev))
                        rooms.Add(prev);
                }
                else
                {
                    return null;
                }
            }

            // Close second loop
            while (!prev.Connect(landmarkRoom))
            {
                Room c = CreateConnectionRoom();
                if (PlaceRoom(rooms, prev, c, AngleBetweenRooms(prev, landmarkRoom)) == -1)
                {
                    return null;
                }
                secondLoop.Add(c);
                rooms.Add(c);
                prev = c;
            }

            // Place shop near entrance
            if (shop != null)
            {
                float angle;
                int tries = 10;
                do
                {
                    angle = PlaceRoom(firstLoop, entrance, shop, DungeonRandom.Float(360f));
                    tries--;
                } while (angle == -1 && tries >= 0);
                if (angle == -1) return null;
            }

            // Calculate loop centers
            firstLoopCenter = Vector2.zero;
            foreach (Room r in firstLoop)
            {
                firstLoopCenter.x += (r.left + r.right) / 2f;
                firstLoopCenter.y += (r.top + r.bottom) / 2f;
            }
            firstLoopCenter.x /= firstLoop.Count;
            firstLoopCenter.y /= firstLoop.Count;

            secondLoopCenter = Vector2.zero;
            foreach (Room r in secondLoop)
            {
                secondLoopCenter.x += (r.left + r.right) / 2f;
                secondLoopCenter.y += (r.top + r.bottom) / 2f;
            }
            secondLoopCenter.x /= secondLoop.Count;
            secondLoopCenter.y /= secondLoop.Count;

            // Create branches
            List<Room> branchable = new List<Room>(firstLoop);
            branchable.AddRange(secondLoop);
            branchable.Remove(landmarkRoom); // Remove once so it isn't present twice

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
        /// Override to generate angles that point towards the appropriate loop center
        /// </summary>
        protected override float RandomBranchAngle(Room r)
        {
            Vector2 center;
            if (firstLoop.Contains(r))
            {
                center = firstLoopCenter;
            }
            else
            {
                center = secondLoopCenter;
            }

            if (center == Vector2.zero)
                return base.RandomBranchAngle(r);
            else
            {
                // Generate four angles randomly and return the one which points closer to the center
                float toCenter = AngleBetweenPoints(
                    new Vector2((r.left + r.right) / 2f, (r.top + r.bottom) / 2f),
                    center);
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
