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
    /// A simple builder which utilizes a line as its core feature
    /// Creates dungeon layouts with rooms arranged in a linear path
    /// </summary>
    public class LineBuilder : RegularBuilder
    {
        /// <summary>
        /// Builds a dungeon with a linear layout
        /// </summary>
        public override List<Room> Build(List<Room> rooms)
        {
            SetupRooms(rooms);

            if (entrance == null)
            {
                return null;
            }

            float direction = DungeonRandom.Float(0, 360);
            List<Room> branchable = new List<Room>();

            entrance.SetSize();
            entrance.SetPos(0, 0);
            branchable.Add(entrance);

            // Place shop behind entrance
            if (shop != null)
            {
                PlaceRoom(rooms, entrance, shop, direction + 180f);
            }

            int roomsOnPath = (int)(multiConnections.Count * pathLength) + DungeonRandom.Chances(pathLenJitterChances);
            roomsOnPath = Mathf.Min(roomsOnPath, multiConnections.Count);

            Room curr = entrance;

            float[] pathTunnels = (float[])pathTunnelChances.Clone();
            for (int i = 0; i <= roomsOnPath; i++)
            {
                if (i == roomsOnPath && exit == null)
                    continue;

                int tunnels = DungeonRandom.Chances(pathTunnels);
                if (tunnels == -1)
                {
                    pathTunnels = (float[])pathTunnelChances.Clone();
                    tunnels = DungeonRandom.Chances(pathTunnels);
                }
                pathTunnels[tunnels]--;

                for (int j = 0; j < tunnels; j++)
                {
                    Room t = CreateConnectionRoom();
                    PlaceRoom(rooms, curr, t, direction + DungeonRandom.Float(-pathVariance, pathVariance));
                    branchable.Add(t);
                    rooms.Add(t);
                    curr = t;
                }

                Room r = (i == roomsOnPath ? exit : multiConnections[i]);
                PlaceRoom(rooms, curr, r, direction + DungeonRandom.Float(-pathVariance, pathVariance));
                branchable.Add(r);
                curr = r;
            }

            // Collect rooms to branch
            List<Room> roomsToBranch = new List<Room>();
            for (int i = roomsOnPath; i < multiConnections.Count; i++)
            {
                roomsToBranch.Add(multiConnections[i]);
            }
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
    }
}
