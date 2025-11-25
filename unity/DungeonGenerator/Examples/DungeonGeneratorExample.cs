/*
 * Unity Dungeon Room Generator - Example Script
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

namespace DungeonGenerator.Examples
{
    /// <summary>
    /// Example script showing how to use the dungeon generator
    /// This creates a simple visual representation of the generated dungeon
    /// </summary>
    public class DungeonGeneratorExample : MonoBehaviour
    {
        [Header("Dungeon Settings")]
        public DungeonLayoutType layoutType = DungeonLayoutType.Loop;
        public int standardRoomCount = 10;
        public int seed = 0;

        [Header("Visualization")]
        public Material floorMaterial;
        public Material wallMaterial;
        public float roomScale = 1f;
        public float roomHeight = 0.1f;

        private List<Room> generatedRooms;
        private List<GameObject> createdObjects = new List<GameObject>();

        /// <summary>
        /// Generate the dungeon on Start
        /// </summary>
        void Start()
        {
            GenerateAndVisualize();
        }

        /// <summary>
        /// Regenerate dungeon when space key is pressed
        /// </summary>
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GenerateAndVisualize();
            }
        }

        /// <summary>
        /// Generates a new dungeon and creates visual representation
        /// </summary>
        [ContextMenu("Generate Dungeon")]
        public void GenerateAndVisualize()
        {
            // Clear previous dungeon
            ClearDungeon();

            // Set random seed
            if (seed != 0)
            {
                DungeonRandom.SetSeed(seed);
            }
            else
            {
                DungeonRandom.SetSeed((int)System.DateTime.Now.Ticks);
            }

            // Create rooms
            List<Room> rooms = CreateRoomList();

            // Create builder
            RegularBuilder builder = CreateBuilder();

            // Generate dungeon
            generatedRooms = builder.Build(rooms);

            if (generatedRooms == null)
            {
                Debug.LogWarning("Failed to generate dungeon, retrying...");
                GenerateAndVisualize();
                return;
            }

            // Create visual representation
            CreateVisualDungeon();

            Debug.Log($"Generated dungeon with {generatedRooms.Count} rooms");
        }

        /// <summary>
        /// Creates the list of rooms to be placed
        /// </summary>
        private List<Room> CreateRoomList()
        {
            List<Room> rooms = new List<Room>();

            // Entrance
            rooms.Add(new Room(RoomType.Entrance));

            // Exit
            rooms.Add(new Room(RoomType.Exit));

            // Standard rooms
            for (int i = 0; i < standardRoomCount; i++)
            {
                Room room = new Room(RoomType.Standard);
                if (DungeonRandom.Float() < 0.2f)
                {
                    room.sizeCategory = RoomSizeCategory.Large;
                }
                rooms.Add(room);
            }

            // Shop
            rooms.Add(new Room(RoomType.Shop));

            return rooms;
        }

        /// <summary>
        /// Creates the appropriate builder
        /// </summary>
        private RegularBuilder CreateBuilder()
        {
            RegularBuilder builder;

            switch (layoutType)
            {
                case DungeonLayoutType.Loop:
                    builder = new LoopBuilder();
                    break;
                case DungeonLayoutType.Line:
                    builder = new LineBuilder();
                    break;
                case DungeonLayoutType.Branches:
                    builder = new BranchesBuilder();
                    break;
                case DungeonLayoutType.FigureEight:
                    builder = new FigureEightBuilder();
                    break;
                default:
                    builder = new LoopBuilder();
                    break;
            }

            builder.SetPathVariance(45f);
            builder.SetPathLength(0.5f, new float[] { 0, 1, 0 });
            builder.SetExtraConnectionChance(0.2f);

            return builder;
        }

        /// <summary>
        /// Creates visual GameObjects for each room
        /// </summary>
        private void CreateVisualDungeon()
        {
            GameObject dungeonParent = new GameObject("GeneratedDungeon");
            createdObjects.Add(dungeonParent);

            foreach (Room room in generatedRooms)
            {
                CreateRoomObject(room, dungeonParent.transform);
            }

            // Create connections
            CreateConnectionLines(dungeonParent.transform);
        }

        /// <summary>
        /// Creates a visual object for a room
        /// </summary>
        private void CreateRoomObject(Room room, Transform parent)
        {
            GameObject roomObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roomObj.name = $"Room_{room.roomType}_{room.left}_{room.top}";
            roomObj.transform.parent = parent;

            // Position
            roomObj.transform.position = new Vector3(
                (room.left + room.right) / 2f * roomScale,
                0,
                (room.top + room.bottom) / 2f * roomScale
            );

            // Scale
            roomObj.transform.localScale = new Vector3(
                room.Width() * roomScale,
                roomHeight,
                room.Height() * roomScale
            );

            // Color based on room type
            Renderer renderer = roomObj.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = GetRoomColor(room);

            createdObjects.Add(roomObj);
        }

        /// <summary>
        /// Creates line renderers for connections
        /// </summary>
        private void CreateConnectionLines(Transform parent)
        {
            HashSet<(Room, Room)> drawnConnections = new HashSet<(Room, Room)>();

            foreach (Room room in generatedRooms)
            {
                foreach (Room connected in room.connected.Keys)
                {
                    // Use object reference identity for comparison
                    var key = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(room) < 
                              System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(connected)
                        ? (room, connected)
                        : (connected, room);

                    if (drawnConnections.Contains(key))
                        continue;

                    drawnConnections.Add(key);

                    GameObject lineObj = new GameObject($"Connection_{room.left}_{room.top}_to_{connected.left}_{connected.top}");
                    lineObj.transform.parent = parent;

                    LineRenderer line = lineObj.AddComponent<LineRenderer>();
                    line.positionCount = 2;
                    line.startWidth = 0.1f * roomScale;
                    line.endWidth = 0.1f * roomScale;
                    line.material = new Material(Shader.Find("Sprites/Default"));
                    line.material.color = Color.blue;

                    Vector3 pos1 = new Vector3(
                        (room.left + room.right) / 2f * roomScale,
                        roomHeight,
                        (room.top + room.bottom) / 2f * roomScale
                    );

                    Vector3 pos2 = new Vector3(
                        (connected.left + connected.right) / 2f * roomScale,
                        roomHeight,
                        (connected.top + connected.bottom) / 2f * roomScale
                    );

                    line.SetPosition(0, pos1);
                    line.SetPosition(1, pos2);

                    createdObjects.Add(lineObj);
                }
            }
        }

        /// <summary>
        /// Gets color for room type
        /// </summary>
        private Color GetRoomColor(Room room)
        {
            switch (room.roomType)
            {
                case RoomType.Entrance:
                    return Color.green;
                case RoomType.Exit:
                    return Color.red;
                case RoomType.Connection:
                    return Color.gray;
                case RoomType.Shop:
                    return Color.yellow;
                case RoomType.Secret:
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// Clears all generated objects
        /// </summary>
        private void ClearDungeon()
        {
            foreach (GameObject obj in createdObjects)
            {
                if (obj != null)
                {
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                }
            }
            createdObjects.Clear();
            generatedRooms = null;
        }

        /// <summary>
        /// Draw help text
        /// </summary>
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label("Press SPACE to regenerate dungeon");
            GUILayout.Label($"Layout: {layoutType}");
            if (generatedRooms != null)
            {
                GUILayout.Label($"Rooms: {generatedRooms.Count}");
            }
            GUILayout.EndArea();
        }
    }
}
