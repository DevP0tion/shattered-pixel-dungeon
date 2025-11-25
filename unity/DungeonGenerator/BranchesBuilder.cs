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
    /// A builder that creates only branches, very simple and very random
    /// Creates dungeon layouts with rooms branching from a central entrance
    /// </summary>
    public class BranchesBuilder : RegularBuilder
    {
        /// <summary>
        /// Builds a dungeon with a branching layout
        /// </summary>
        public override List<Room> Build(List<Room> rooms)
        {
            SetupRooms(rooms);

            if (entrance == null)
            {
                return null;
            }

            List<Room> branchable = new List<Room>();

            entrance.SetSize();
            entrance.SetPos(0, 0);
            branchable.Add(entrance);

            // Place shop near entrance
            if (shop != null)
            {
                PlaceRoom(branchable, entrance, shop, DungeonRandom.Float(360f));
            }

            // Collect all rooms to branch
            List<Room> roomsToBranch = new List<Room>();
            roomsToBranch.AddRange(multiConnections);
            if (exit != null) roomsToBranch.Add(exit);
            roomsToBranch.AddRange(singleConnections);
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
