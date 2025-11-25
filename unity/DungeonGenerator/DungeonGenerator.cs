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
    /// The type of dungeon layout to generate
    /// </summary>
    public enum DungeonLayoutType
    {
        Loop,
        Line,
        Branches,
        FigureEight
    }

    /// <summary>
    /// Main component for generating dungeons in Unity
    /// Attach this to a GameObject to generate dungeon layouts
    /// </summary>
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        [Tooltip("The type of dungeon layout to generate")]
        public DungeonLayoutType layoutType = DungeonLayoutType.Loop;

        [Tooltip("Random seed for reproducible generation (0 = random)")]
        public int randomSeed = 0;

        [Header("Room Configuration")]
        [Tooltip("Number of standard rooms to generate")]
        [Range(5, 30)]
        public int standardRoomCount = 10;

        [Tooltip("Number of secret rooms to add")]
        [Range(0, 5)]
        public int secretRoomCount = 1;

        [Tooltip("Include a shop room")]
        public bool includeShop = true;

        [Header("Path Configuration")]
        [Tooltip("Percentage of rooms on the main path (0-1)")]
        [Range(0.3f, 0.8f)]
        public float pathLength = 0.5f;

        [Tooltip("Variance in path direction (degrees)")]
        [Range(0f, 90f)]
        public float pathVariance = 45f;

        [Tooltip("Chance for extra connections between rooms")]
        [Range(0f, 0.5f)]
        public float extraConnectionChance = 0.2f;

        [Header("Loop Settings (for Loop/FigureEight)")]
        [Tooltip("Curvature of the loop (0 = circle, higher = more oval)")]
        [Range(0, 3)]
        public int curveExponent = 0;

        [Tooltip("Intensity of the curve (0 = circle, 1 = full curve)")]
        [Range(0f, 1f)]
        public float curveIntensity = 1f;

        [Header("Visualization")]
        [Tooltip("Scale factor for room positions")]
        public float positionScale = 1f;

        [Tooltip("Color for entrance room")]
        public Color entranceColor = Color.green;

        [Tooltip("Color for exit room")]
        public Color exitColor = Color.red;

        [Tooltip("Color for standard rooms")]
        public Color standardRoomColor = Color.white;

        [Tooltip("Color for connection rooms")]
        public Color connectionRoomColor = Color.gray;

        [Tooltip("Color for shop room")]
        public Color shopColor = Color.yellow;

        [Tooltip("Color for secret rooms")]
        public Color secretRoomColor = Color.magenta;

        [Tooltip("Color for connections between rooms")]
        public Color connectionLineColor = Color.blue;

        // Generated data
        private List<Room> generatedRooms = new List<Room>();

        /// <summary>
        /// Gets the list of generated rooms
        /// </summary>
        public List<Room> GeneratedRooms => generatedRooms;

        /// <summary>
        /// Generates a new dungeon layout
        /// </summary>
        [ContextMenu("Generate Dungeon")]
        public List<Room> GenerateDungeon()
        {
            // Set random seed
            if (randomSeed != 0)
            {
                DungeonRandom.SetSeed(randomSeed);
            }
            else
            {
                DungeonRandom.SetSeed((int)System.DateTime.Now.Ticks);
            }

            // Create room list
            List<Room> rooms = CreateRoomList();

            // Create builder based on type
            RegularBuilder builder = CreateBuilder();

            // Generate dungeon
            generatedRooms = builder.Build(rooms);

            if (generatedRooms == null)
            {
                Debug.LogWarning("DungeonGenerator: Failed to generate dungeon, retrying...");
                return GenerateDungeon();
            }

            Debug.Log($"DungeonGenerator: Successfully generated {generatedRooms.Count} rooms");
            return generatedRooms;
        }

        /// <summary>
        /// Creates the list of rooms to be placed
        /// </summary>
        private List<Room> CreateRoomList()
        {
            List<Room> rooms = new List<Room>();

            // Entrance room
            rooms.Add(new Room(RoomType.Entrance));

            // Exit room
            rooms.Add(new Room(RoomType.Exit));

            // Standard rooms
            for (int i = 0; i < standardRoomCount; i++)
            {
                Room room = new Room(RoomType.Standard);
                // Randomly assign size categories
                float roll = DungeonRandom.Float();
                if (roll < 0.1f)
                    room.sizeCategory = RoomSizeCategory.Giant;
                else if (roll < 0.3f)
                    room.sizeCategory = RoomSizeCategory.Large;
                rooms.Add(room);
            }

            // Secret rooms
            for (int i = 0; i < secretRoomCount; i++)
            {
                rooms.Add(new Room(RoomType.Secret));
            }

            // Shop room
            if (includeShop)
            {
                rooms.Add(new Room(RoomType.Shop));
            }

            return rooms;
        }

        /// <summary>
        /// Creates the appropriate builder based on layout type
        /// </summary>
        private RegularBuilder CreateBuilder()
        {
            RegularBuilder builder;

            switch (layoutType)
            {
                case DungeonLayoutType.Loop:
                    LoopBuilder loopBuilder = new LoopBuilder();
                    loopBuilder.SetLoopShape(curveExponent, curveIntensity, 0);
                    builder = loopBuilder;
                    break;

                case DungeonLayoutType.Line:
                    builder = new LineBuilder();
                    break;

                case DungeonLayoutType.Branches:
                    builder = new BranchesBuilder();
                    break;

                case DungeonLayoutType.FigureEight:
                    FigureEightBuilder figureEightBuilder = new FigureEightBuilder();
                    figureEightBuilder.SetLoopShape(curveExponent, curveIntensity, 0);
                    builder = figureEightBuilder;
                    break;

                default:
                    builder = new LoopBuilder();
                    break;
            }

            builder.SetPathVariance(pathVariance);
            builder.SetPathLength(pathLength, new float[] { 0, 1, 0 });
            builder.SetExtraConnectionChance(extraConnectionChance);

            return builder;
        }

        /// <summary>
        /// Draw the dungeon in the Scene view using Gizmos
        /// </summary>
        private void OnDrawGizmos()
        {
            if (generatedRooms == null || generatedRooms.Count == 0)
                return;

            // Draw rooms
            foreach (Room room in generatedRooms)
            {
                Gizmos.color = GetRoomColor(room);

                Vector3 center = new Vector3(
                    (room.left + room.right) / 2f * positionScale,
                    0,
                    (room.top + room.bottom) / 2f * positionScale
                );

                Vector3 size = new Vector3(
                    room.Width() * positionScale,
                    0.1f,
                    room.Height() * positionScale
                );

                Gizmos.DrawCube(center, size);
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(center, size);
            }

            // Draw connections
            Gizmos.color = connectionLineColor;
            HashSet<(Room, Room)> drawnConnections = new HashSet<(Room, Room)>();

            foreach (Room room in generatedRooms)
            {
                Vector3 fromPos = new Vector3(
                    (room.left + room.right) / 2f * positionScale,
                    0.1f,
                    (room.top + room.bottom) / 2f * positionScale
                );

                foreach (Room connected in room.connected.Keys)
                {
                    // Avoid drawing same connection twice by using consistent ordering
                    // Use object reference identity for comparison
                    var key = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(room) < 
                              System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(connected)
                        ? (room, connected)
                        : (connected, room);

                    if (drawnConnections.Contains(key))
                        continue;

                    drawnConnections.Add(key);

                    Vector3 toPos = new Vector3(
                        (connected.left + connected.right) / 2f * positionScale,
                        0.1f,
                        (connected.top + connected.bottom) / 2f * positionScale
                    );

                    Gizmos.DrawLine(fromPos, toPos);
                }
            }
        }

        /// <summary>
        /// Gets the color for a room based on its type
        /// </summary>
        private Color GetRoomColor(Room room)
        {
            switch (room.roomType)
            {
                case RoomType.Entrance:
                    return entranceColor;
                case RoomType.Exit:
                    return exitColor;
                case RoomType.Connection:
                    return connectionRoomColor;
                case RoomType.Shop:
                    return shopColor;
                case RoomType.Secret:
                    return secretRoomColor;
                default:
                    return standardRoomColor;
            }
        }

        /// <summary>
        /// Gets the bounds of the entire dungeon
        /// </summary>
        public Bounds GetDungeonBounds()
        {
            if (generatedRooms == null || generatedRooms.Count == 0)
                return new Bounds();

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (Room room in generatedRooms)
            {
                minX = Mathf.Min(minX, room.left);
                maxX = Mathf.Max(maxX, room.right);
                minY = Mathf.Min(minY, room.top);
                maxY = Mathf.Max(maxY, room.bottom);
            }

            Vector3 center = new Vector3(
                (minX + maxX) / 2f * positionScale,
                0,
                (minY + maxY) / 2f * positionScale
            );

            Vector3 size = new Vector3(
                (maxX - minX) * positionScale,
                1,
                (maxY - minY) * positionScale
            );

            return new Bounds(center, size);
        }

        /// <summary>
        /// Gets the entrance room
        /// </summary>
        public Room GetEntranceRoom()
        {
            return generatedRooms?.Find(r => r.roomType == RoomType.Entrance);
        }

        /// <summary>
        /// Gets the exit room
        /// </summary>
        public Room GetExitRoom()
        {
            return generatedRooms?.Find(r => r.roomType == RoomType.Exit);
        }
    }
}
