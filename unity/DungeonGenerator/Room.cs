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
    /// Door connection point between rooms
    /// </summary>
    [System.Serializable]
    public class Door
    {
        public enum DoorType
        {
            Empty,
            Tunnel,
            Regular,
            Unlocked,
            Hidden,
            Barricade,
            Locked
        }

        public Vector2Int position;
        public DoorType type = DoorType.Empty;

        public Door()
        {
            position = Vector2Int.zero;
        }

        public Door(Vector2Int pos)
        {
            position = pos;
        }

        public Door(int x, int y)
        {
            position = new Vector2Int(x, y);
        }

        public void Set(DoorType newType)
        {
            if ((int)newType > (int)type)
            {
                type = newType;
            }
        }
    }

    /// <summary>
    /// Enumeration for room types
    /// </summary>
    public enum RoomType
    {
        Standard,
        Entrance,
        Exit,
        Connection,
        Secret,
        Shop,
        Special
    }

    /// <summary>
    /// Room size categories with connection weights
    /// </summary>
    public enum RoomSizeCategory
    {
        Normal,
        Large,
        Giant
    }

    /// <summary>
    /// Base Room class representing a dungeon room
    /// </summary>
    [System.Serializable]
    public class Room : DungeonRect
    {
        // Direction constants
        public const int ALL = 0;
        public const int LEFT = 1;
        public const int TOP = 2;
        public const int RIGHT = 3;
        public const int BOTTOM = 4;

        public RoomType roomType = RoomType.Standard;
        public RoomSizeCategory sizeCategory = RoomSizeCategory.Normal;

        public List<Room> neighbours = new List<Room>();
        public Dictionary<Room, Door> connected = new Dictionary<Room, Door>();

        public int distance;
        public int price = 1;

        // Size constraints - can be overridden per room type
        protected int _minWidth = 4;
        protected int _maxWidth = 10;
        protected int _minHeight = 4;
        protected int _maxHeight = 10;

        public Room() : base() { }

        public Room(DungeonRect other) : base(other) { }

        public Room(RoomType type) : base()
        {
            roomType = type;
            SetSizeConstraintsForType();
        }

        protected void SetSizeConstraintsForType()
        {
            switch (roomType)
            {
                case RoomType.Connection:
                    _minWidth = 3;
                    _maxWidth = 8;
                    _minHeight = 3;
                    _maxHeight = 8;
                    break;
                case RoomType.Secret:
                    _minWidth = 4;
                    _maxWidth = 6;
                    _minHeight = 4;
                    _maxHeight = 6;
                    break;
                case RoomType.Shop:
                    _minWidth = 6;
                    _maxWidth = 9;
                    _minHeight = 6;
                    _maxHeight = 9;
                    break;
                case RoomType.Entrance:
                case RoomType.Exit:
                    _minWidth = 5;
                    _maxWidth = 7;
                    _minHeight = 5;
                    _maxHeight = 7;
                    break;
                default:
                    _minWidth = 4;
                    _maxWidth = 10;
                    _minHeight = 4;
                    _maxHeight = 10;
                    break;
            }
        }

        /// <summary>
        /// Sets room with specified type
        /// </summary>
        public Room SetType(RoomType type)
        {
            roomType = type;
            SetSizeConstraintsForType();
            return this;
        }

        /// <summary>
        /// Copies properties from another room
        /// </summary>
        public Room Set(Room other)
        {
            base.Set(other);
            foreach (Room r in other.neighbours)
            {
                neighbours.Add(r);
                r.neighbours.Remove(other);
                r.neighbours.Add(this);
            }
            foreach (Room r in new List<Room>(other.connected.Keys))
            {
                Door d = other.connected[r];
                r.connected.Remove(other);
                r.connected[this] = d;
                connected[r] = d;
            }
            return this;
        }

        // Size constraints getters
        public virtual int MinWidth() => _minWidth;
        public virtual int MaxWidth() => _maxWidth;
        public virtual int MinHeight() => _minHeight;
        public virtual int MaxHeight() => _maxHeight;

        // Override Width/Height to include +1 for inclusive bounds
        public override int Width()
        {
            return base.Width();
        }

        public override int Height()
        {
            return base.Height();
        }

        /// <summary>
        /// Sets the room size using default constraints
        /// </summary>
        public bool SetSize()
        {
            return SetSize(MinWidth(), MaxWidth(), MinHeight(), MaxHeight());
        }

        /// <summary>
        /// Forces a specific size for the room
        /// </summary>
        public bool ForceSize(int w, int h)
        {
            return SetSize(w, w, h, h);
        }

        /// <summary>
        /// Sets size with upper limits
        /// </summary>
        public bool SetSizeWithLimit(int w, int h)
        {
            if (w < MinWidth() || h < MinHeight())
            {
                return false;
            }
            else
            {
                SetSize();

                if (Width() > w || Height() > h)
                {
                    Resize(Mathf.Min(Width(), w) - 1, Mathf.Min(Height(), h) - 1);
                }

                return true;
            }
        }

        /// <summary>
        /// Sets size with constraints
        /// </summary>
        protected bool SetSize(int minW, int maxW, int minH, int maxH)
        {
            if (minW < MinWidth()
                || maxW > MaxWidth()
                || minH < MinHeight()
                || maxH > MaxHeight()
                || minW > maxW
                || minH > maxH)
            {
                return false;
            }
            else
            {
                // Subtract one because rooms are inclusive to their right and bottom sides
                Resize(DungeonRandom.NormalIntRange(minW, maxW) - 1,
                       DungeonRandom.NormalIntRange(minH, maxH) - 1);
                return true;
            }
        }

        /// <summary>
        /// Returns a random point inside the room
        /// </summary>
        public Vector2Int Random()
        {
            return Random(1);
        }

        /// <summary>
        /// Returns a random point inside the room with margin
        /// </summary>
        public Vector2Int Random(int m)
        {
            return new Vector2Int(
                DungeonRandom.IntRange(left + m, right - m),
                DungeonRandom.IntRange(top + m, bottom - m)
            );
        }

        /// <summary>
        /// Checks if a point is inside the room (within 1 tile perimeter)
        /// </summary>
        public bool Inside(Vector2Int p)
        {
            return p.x > left && p.y > top && p.x < right && p.y < bottom;
        }

        /// <summary>
        /// Returns the center point of the room
        /// </summary>
        public Vector2Int Center()
        {
            return new Vector2Int(
                (left + right) / 2 + (((right - left) % 2) == 1 ? DungeonRandom.Int(2) : 0),
                (top + bottom) / 2 + (((bottom - top) % 2) == 1 ? DungeonRandom.Int(2) : 0)
            );
        }

        // Connection logic

        public virtual int MinConnections(int direction)
        {
            if (direction == ALL) return 1;
            else return 0;
        }

        public int CurConnections(int direction)
        {
            if (direction == ALL)
            {
                return connected.Count;
            }
            else
            {
                int total = 0;
                foreach (Room r in connected.Keys)
                {
                    DungeonRect i = Intersect(r);
                    if (direction == LEFT && i.Width() == 0 && i.left == left) total++;
                    else if (direction == TOP && i.Height() == 0 && i.top == top) total++;
                    else if (direction == RIGHT && i.Width() == 0 && i.right == right) total++;
                    else if (direction == BOTTOM && i.Height() == 0 && i.bottom == bottom) total++;
                }
                return total;
            }
        }

        public int RemConnections(int direction)
        {
            if (CurConnections(ALL) >= MaxConnections(ALL)) return 0;
            else return MaxConnections(direction) - CurConnections(direction);
        }

        public virtual int MaxConnections(int direction)
        {
            if (direction == ALL) return 16;
            else return 4;
        }

        /// <summary>
        /// Checks if room can connect at a specific point
        /// </summary>
        public virtual bool CanConnect(Vector2Int p)
        {
            // Point must be along exactly one edge, no corners
            return (p.x == left || p.x == right) != (p.y == top || p.y == bottom);
        }

        /// <summary>
        /// Checks if room can connect in a direction
        /// </summary>
        public virtual bool CanConnect(int direction)
        {
            return RemConnections(direction) > 0;
        }

        /// <summary>
        /// Checks if this room can connect to another room
        /// </summary>
        public virtual bool CanConnect(Room r)
        {
            DungeonRect i = Intersect(r);

            bool foundPoint = false;
            foreach (Vector2Int p in i.GetPoints())
            {
                if (CanConnect(p) && r.CanConnect(p))
                {
                    foundPoint = true;
                    break;
                }
            }
            if (!foundPoint) return false;

            if (i.Width() == 0 && i.left == left)
                return CanConnect(LEFT) && r.CanConnect(LEFT);
            else if (i.Height() == 0 && i.top == top)
                return CanConnect(TOP) && r.CanConnect(TOP);
            else if (i.Width() == 0 && i.right == right)
                return CanConnect(RIGHT) && r.CanConnect(RIGHT);
            else if (i.Height() == 0 && i.bottom == bottom)
                return CanConnect(BOTTOM) && r.CanConnect(BOTTOM);
            else
                return false;
        }

        /// <summary>
        /// Adds a room as a neighbour if they share an edge
        /// </summary>
        public bool AddNeighbour(Room other)
        {
            if (neighbours.Contains(other))
                return true;

            DungeonRect i = Intersect(other);
            if ((i.Width() == 0 && i.Height() >= 2) ||
                (i.Height() == 0 && i.Width() >= 2))
            {
                neighbours.Add(other);
                other.neighbours.Add(this);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Connects this room to another room
        /// </summary>
        public bool Connect(Room room)
        {
            if ((neighbours.Contains(room) || AddNeighbour(room))
                && !connected.ContainsKey(room) && CanConnect(room))
            {
                connected[room] = null;
                room.connected[this] = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all connections and neighbours
        /// </summary>
        public void ClearConnections()
        {
            foreach (Room r in new List<Room>(neighbours))
            {
                r.neighbours.Remove(this);
            }
            neighbours.Clear();

            foreach (Room r in new List<Room>(connected.Keys))
            {
                r.connected.Remove(this);
            }
            connected.Clear();
        }

        /// <summary>
        /// Gets the connection weight based on size category
        /// </summary>
        public int ConnectionWeight()
        {
            switch (sizeCategory)
            {
                case RoomSizeCategory.Large: return 2;
                case RoomSizeCategory.Giant: return 3;
                default: return 1;
            }
        }
    }
}
