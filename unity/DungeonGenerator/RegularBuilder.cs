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
    /// RegularBuilder introduces the concept of a major path and branches
    /// with tunnels padding rooms placed in them
    /// </summary>
    public abstract class RegularBuilder : Builder
    {
        // Parameter values for level building logic
        protected float pathVariance = 45f;
        protected float pathLength = 0.5f;
        protected float[] pathLenJitterChances = new float[] { 0, 1, 0 };
        protected float[] pathTunnelChances = new float[] { 1, 3, 1 };
        protected float[] branchTunnelChances = new float[] { 2, 2, 1 };
        protected float extraConnectionChance = 0.2f;

        // Room references
        protected Room entrance = null;
        protected Room exit = null;
        protected Room shop = null;

        protected List<Room> multiConnections = new List<Room>();
        protected List<Room> singleConnections = new List<Room>();

        /// <summary>
        /// Sets the path variance angle
        /// </summary>
        public RegularBuilder SetPathVariance(float var)
        {
            pathVariance = var;
            return this;
        }

        /// <summary>
        /// Sets path length and jitter chances
        /// </summary>
        public RegularBuilder SetPathLength(float len, float[] jitter)
        {
            pathLength = len;
            pathLenJitterChances = jitter;
            return this;
        }

        /// <summary>
        /// Sets tunnel length chances for path and branches
        /// </summary>
        public RegularBuilder SetTunnelLength(float[] path, float[] branch)
        {
            pathTunnelChances = path;
            branchTunnelChances = branch;
            return this;
        }

        /// <summary>
        /// Sets extra connection chance between rooms
        /// </summary>
        public RegularBuilder SetExtraConnectionChance(float chance)
        {
            extraConnectionChance = chance;
            return this;
        }

        /// <summary>
        /// Sets up the rooms list categorizing by type
        /// </summary>
        protected void SetupRooms(List<Room> rooms)
        {
            foreach (Room r in rooms)
            {
                r.SetEmpty();
            }

            entrance = exit = shop = null;
            singleConnections.Clear();
            multiConnections.Clear();

            foreach (Room r in rooms)
            {
                if (r.roomType == RoomType.Entrance)
                {
                    entrance = r;
                }
                else if (r.roomType == RoomType.Exit)
                {
                    exit = r;
                }
                else if (r.roomType == RoomType.Shop && r.MaxConnections(Room.ALL) == 1)
                {
                    shop = r;
                }
                else if (r.MaxConnections(Room.ALL) > 1)
                {
                    multiConnections.Add(r);
                }
                else if (r.MaxConnections(Room.ALL) == 1)
                {
                    singleConnections.Add(r);
                }
            }

            // Weight larger rooms to be more likely to appear in the main loop
            WeightRooms(multiConnections);
            DungeonRandom.Shuffle(multiConnections);

            // Remove duplicates while preserving order
            List<Room> unique = new List<Room>();
            HashSet<Room> seen = new HashSet<Room>();
            foreach (Room r in multiConnections)
            {
                if (!seen.Contains(r))
                {
                    seen.Add(r);
                    unique.Add(r);
                }
            }
            multiConnections = unique;
        }

        /// <summary>
        /// Weights rooms by their size category
        /// </summary>
        protected void WeightRooms(List<Room> rooms)
        {
            Room[] roomArray = rooms.ToArray();
            foreach (Room r in roomArray)
            {
                for (int i = 1; i < r.ConnectionWeight(); i++)
                {
                    rooms.Add(r);
                }
            }
        }

        /// <summary>
        /// Creates branches from branchable rooms
        /// </summary>
        protected void CreateBranches(List<Room> rooms, List<Room> branchable,
            List<Room> roomsToBranch, float[] connChances)
        {
            int i = 0;
            float angle;
            int tries;
            Room curr;
            List<Room> connectingRoomsThisBranch = new List<Room>();

            float[] connectionChances = (float[])connChances.Clone();

            while (i < roomsToBranch.Count)
            {
                Room r = roomsToBranch[i];

                connectingRoomsThisBranch.Clear();

                do
                {
                    curr = DungeonRandom.Element(branchable);
                } while (r.roomType == RoomType.Secret && curr.roomType == RoomType.Connection);

                int connectingRooms = DungeonRandom.Chances(connectionChances);
                if (connectingRooms == -1)
                {
                    connectionChances = (float[])connChances.Clone();
                    connectingRooms = DungeonRandom.Chances(connectionChances);
                }
                connectionChances[connectingRooms]--;

                for (int j = 0; j < connectingRooms; j++)
                {
                    Room t = CreateConnectionRoom(r.roomType == RoomType.Secret);
                    tries = 3;

                    do
                    {
                        angle = PlaceRoom(rooms, curr, t, RandomBranchAngle(curr));
                        tries--;
                    } while (angle == -1 && tries > 0);

                    if (angle == -1)
                    {
                        t.ClearConnections();
                        foreach (Room c in connectingRoomsThisBranch)
                        {
                            c.ClearConnections();
                            rooms.Remove(c);
                        }
                        connectingRoomsThisBranch.Clear();
                        break;
                    }
                    else
                    {
                        connectingRoomsThisBranch.Add(t);
                        rooms.Add(t);
                    }

                    curr = t;
                }

                if (connectingRoomsThisBranch.Count != connectingRooms)
                {
                    continue;
                }

                tries = 10;

                do
                {
                    angle = PlaceRoom(rooms, curr, r, RandomBranchAngle(curr));
                    tries--;
                } while (angle == -1 && tries > 0);

                if (angle == -1)
                {
                    r.ClearConnections();
                    foreach (Room t in connectingRoomsThisBranch)
                    {
                        t.ClearConnections();
                        rooms.Remove(t);
                    }
                    connectingRoomsThisBranch.Clear();
                    continue;
                }

                foreach (Room conn in connectingRoomsThisBranch)
                {
                    if (DungeonRandom.Int(3) <= 1) branchable.Add(conn);
                }

                if (r.MaxConnections(Room.ALL) > 1 && DungeonRandom.Int(3) == 0)
                {
                    for (int j = 0; j < r.ConnectionWeight(); j++)
                    {
                        branchable.Add(r);
                    }
                }

                i++;
            }
        }

        /// <summary>
        /// Creates a connection room
        /// </summary>
        protected Room CreateConnectionRoom(bool secret = false)
        {
            Room room = new Room(RoomType.Connection);
            // Secret rooms might use a maze-like connection
            return room;
        }

        /// <summary>
        /// Returns a random branch angle
        /// </summary>
        protected virtual float RandomBranchAngle(Room r)
        {
            return DungeonRandom.Float(360f);
        }
    }
}
