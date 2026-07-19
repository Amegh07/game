using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only Phase 1 blockout for the Security Academy. It creates visual layout,
/// spawn and named Phase 2 attachment points, but intentionally owns no runtime systems.
/// </summary>
public static class SecurityAcademyPhase1Generator
{
    private const float RoomHeight = 3.5f;
    private const float WallThickness = 0.2f;
    private const float FloorThickness = 0.1f;
    private const float DoorWidth = 1.8f;
    private const float CorridorWidth = 3f;
    private const float ClusterGap = 1.5f;

    private struct RoomDefinition
    {
        public string Name;
        public float Width;
        public float Depth;

        public RoomDefinition(string name, float width, float depth)
        {
            Name = name;
            Width = width;
            Depth = depth;
        }
    }

    private struct PlacedRoom
    {
        public string Name;
        public float X;
        public float Z;
        public float Width;
        public float Depth;

        public PlacedRoom(string name, float x, float z, float width, float depth)
        {
            Name = name;
            X = x;
            Z = z;
            Width = width;
            Depth = depth;
        }
    }

    private static readonly RoomDefinition[] SpineRooms =
    {
        new("Reception", 15f, 12f),
        new("SecurityCheckin", 8f, 6f),
        new("MovementCourse", 30f, 20f),
        new("GuardAwarenessLab", 22f, 20f),
        new("StealthMaze", 35f, 30f),
        new("SecurityOperationsCentre", 20f, 15f),
        new("MiniMuseumSimulation", 40f, 35f),
        new("GraduationLobby", 15f, 12f)
    };

    private static readonly RoomDefinition[] EquipmentCluster =
    {
        new("EquipmentLab", 15f, 12f),
        new("BriefingRoom", 18f, 15f),
        new("BreakArea", 12f, 10f)
    };

    private static readonly RoomDefinition[] DetectionCluster =
    {
        new("AccessControlLab", 15f, 15f),
        new("CameraDetectionLab", 20f, 18f)
    };

    private static readonly List<PlacedRoom> PlacedRooms = new();
    private static readonly Dictionary<Color, Material> Materials = new();

    [MenuItem("Tools/Security Academy/Generate Phase 1 Blockout")]
    private static void Generate()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play Mode Active", "Exit Play Mode before generating the Security Academy blockout.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PlacedRooms.Clear();
        Materials.Clear();

        GameObject root = CreateEmpty("SecurityAcademy", Vector3.zero, null);
        Transform environment = CreateEmpty("Environment", Vector3.zero, root.transform).transform;
        Transform lighting = CreateEmpty("Lighting", Vector3.zero, root.transform).transform;
        Transform props = CreateEmpty("Props", Vector3.zero, root.transform).transform;
        Transform phase2Anchors = CreateEmpty("Phase2_TriggerLocations", Vector3.zero, root.transform).transform;
        Transform player = CreateEmpty("Player", Vector3.zero, root.transform).transform;

        float cursor = 0f;
        cursor = PlaceSpineRoom(environment, SpineRooms[0], cursor, false);
        cursor = PlaceCorridor(environment, cursor);
        cursor = PlaceSpineRoom(environment, SpineRooms[1], cursor, true);
        cursor = PlaceCorridor(environment, cursor);
        cursor = PlaceCluster(environment, "EquipmentCluster", EquipmentCluster, cursor);
        cursor = PlaceCorridor(environment, cursor);
        cursor = PlaceSpineRoom(environment, SpineRooms[2], cursor, true);
        cursor = PlaceCorridor(environment, cursor);
        cursor = PlaceCluster(environment, "DetectionCluster", DetectionCluster, cursor);
        cursor = PlaceCorridor(environment, cursor);

        for (int i = 3; i < SpineRooms.Length; i++)
        {
            bool hasFront = i < SpineRooms.Length - 1;
            cursor = PlaceSpineRoom(environment, SpineRooms[i], cursor, true, hasFront);
            if (hasFront)
                cursor = PlaceCorridor(environment, cursor);
        }

        CreateLighting(lighting);
        CreateProps(props);
        CreatePhase2Anchors(phase2Anchors);
        CreateSpawn(player);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/SecurityAcademy.unity");
        Debug.Log($"[SecurityAcademyPhase1Generator] Generated visual blockout with {PlacedRooms.Count} rooms and corridors.");
    }

    private static float PlaceSpineRoom(Transform parent, RoomDefinition room, float cursor, bool hasBack, bool hasFront = true)
    {
        float z = cursor + room.Depth * 0.5f;
        CreateRoom(parent, room.Name, 0f, z, room.Width, room.Depth, hasBack, hasFront);
        PlacedRooms.Add(new PlacedRoom(room.Name, 0f, z, room.Width, room.Depth));
        return cursor + room.Depth;
    }

    private static float PlaceCorridor(Transform parent, float cursor)
    {
        float z = cursor + CorridorWidth * 0.5f;
        CreateRoom(parent, $"Corridor_{PlacedRooms.Count}", 0f, z, CorridorWidth, CorridorWidth, false, false);
        return cursor + CorridorWidth;
    }

    private static float PlaceCluster(Transform parent, string clusterName, RoomDefinition[] rooms, float cursor)
    {
        float maxDepth = 0f;
        float totalWidth = -ClusterGap;
        for (int i = 0; i < rooms.Length; i++)
        {
            maxDepth = Mathf.Max(maxDepth, rooms[i].Depth);
            totalWidth += rooms[i].Width + ClusterGap;
        }

        float z = cursor + maxDepth * 0.5f;
        Transform cluster = CreateEmpty(clusterName, Vector3.zero, parent).transform;

        // Open central crossing and lateral side corridors keep every branch physically reachable.
        CreateOpenCrossing(cluster, 0f, z, CorridorWidth, maxDepth);

        float xCursor = -totalWidth * 0.5f;
        for (int i = 0; i < rooms.Length; i++)
        {
            RoomDefinition room = rooms[i];
            float x = xCursor + room.Width * 0.5f;
            CreateRoom(cluster, room.Name, x, z, room.Width, room.Depth, true, true);
            CreateSideCorridor(cluster, room.Name, x, z - room.Depth * 0.5f);
            PlacedRooms.Add(new PlacedRoom(room.Name, x, z, room.Width, room.Depth));
            xCursor += room.Width + ClusterGap;
        }

        return cursor + maxDepth;
    }

    private static void CreateOpenCrossing(Transform parent, float x, float z, float width, float depth)
    {
        MakeBox(parent, "ClusterCrossing_Floor", x, FloorThickness * 0.5f, z, width, FloorThickness, depth, new Color(0.28f, 0.28f, 0.31f));
        MakeBox(parent, "ClusterCrossing_Ceiling", x, RoomHeight - FloorThickness * 0.5f, z, width, FloorThickness, depth, new Color(0.4f, 0.4f, 0.42f));
    }

    private static void CreateSideCorridor(Transform parent, string roomName, float roomX, float doorZ)
    {
        float width = Mathf.Abs(roomX);
        if (width < 0.01f)
            return;

        float centerX = roomX * 0.5f;
        MakeBox(parent, $"{roomName}_SideCorridor_Floor", centerX, FloorThickness * 0.5f, doorZ, width, FloorThickness, CorridorWidth, new Color(0.28f, 0.28f, 0.31f));
        MakeBox(parent, $"{roomName}_SideCorridor_Ceiling", centerX, RoomHeight - FloorThickness * 0.5f, doorZ, width, FloorThickness, CorridorWidth, new Color(0.4f, 0.4f, 0.42f));
        MakeBox(parent, $"{roomName}_SideCorridor_LeftWall", centerX, RoomHeight * 0.5f, doorZ - CorridorWidth * 0.5f, width, RoomHeight, WallThickness, new Color(0.55f, 0.53f, 0.50f));
        MakeBox(parent, $"{roomName}_SideCorridor_RightWall", centerX, RoomHeight * 0.5f, doorZ + CorridorWidth * 0.5f, width, RoomHeight, WallThickness, new Color(0.55f, 0.53f, 0.50f));
    }

    private static void CreateRoom(Transform parent, string name, float x, float z, float width, float depth, bool hasBack, bool hasFront)
    {
        Transform room = CreateEmpty(name, Vector3.zero, parent).transform;
        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;
        Color floor = new(0.35f, 0.35f, 0.38f);
        Color wall = new(0.55f, 0.53f, 0.50f);
        Color ceiling = new(0.40f, 0.40f, 0.42f);

        MakeBox(room, "Floor", x, FloorThickness * 0.5f, z, width, FloorThickness, depth, floor);
        MakeBox(room, "Ceiling", x, RoomHeight - FloorThickness * 0.5f, z, width, FloorThickness, depth, ceiling);
        MakeBox(room, "LeftWall", x - halfWidth, RoomHeight * 0.5f, z, WallThickness, RoomHeight, depth, wall);
        MakeBox(room, "RightWall", x + halfWidth, RoomHeight * 0.5f, z, WallThickness, RoomHeight, depth, wall);
        if (hasBack) CreateDoorwayWall(room, "BackWall", x, z - halfDepth, width, wall);
        if (hasFront) CreateDoorwayWall(room, "FrontWall", x, z + halfDepth, width, wall);
    }

    private static void CreateDoorwayWall(Transform parent, string name, float x, float z, float width, Color color)
    {
        float segmentWidth = (width - DoorWidth) * 0.5f;
        if (segmentWidth <= 0f)
            return;

        float offset = (width + DoorWidth) * 0.25f;
        MakeBox(parent, name + "_Left", x - offset, RoomHeight * 0.5f, z, segmentWidth, RoomHeight, WallThickness, color);
        MakeBox(parent, name + "_Right", x + offset, RoomHeight * 0.5f, z, segmentWidth, RoomHeight, WallThickness, color);
    }

    private static void CreateLighting(Transform parent)
    {
        GameObject ambient = new("AmbientLight");
        ambient.transform.SetParent(parent);
        Light ambientLight = ambient.AddComponent<Light>();
        ambientLight.type = LightType.Directional;
        ambientLight.intensity = 0.4f;
        ambientLight.color = new Color(0.7f, 0.7f, 0.8f);
        ambient.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        foreach (PlacedRoom room in PlacedRooms)
        {
            GameObject lightObject = new($"FillLight_{room.Name}");
            lightObject.transform.SetParent(parent);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = Mathf.Max(room.Width, room.Depth) * 0.7f;
            light.spotAngle = 60f;
            light.intensity = 0.7f;
            light.color = new Color(0.9f, 0.85f, 0.8f);
            lightObject.transform.position = new Vector3(room.X, RoomHeight - 0.3f, room.Z);
            lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private static void CreateProps(Transform parent)
    {
        foreach (PlacedRoom room in PlacedRooms)
        {
            if (room.Name.StartsWith("Corridor"))
                continue;

            GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prop.name = $"{room.Name}_BlockoutProp";
            prop.transform.SetParent(parent);
            prop.transform.position = new Vector3(room.X, 0.45f, room.Z);
            prop.transform.localScale = new Vector3(1.2f, 0.9f, 0.8f);
            ApplyMaterial(prop, new Color(0.22f, 0.25f, 0.28f));
        }
    }

    private static void CreatePhase2Anchors(Transform parent)
    {
        foreach (PlacedRoom room in PlacedRooms)
        {
            if (room.Name.StartsWith("Corridor"))
                continue;

            GameObject anchor = CreateEmpty($"{room.Name}_MissionZoneAnchor", new Vector3(room.X, 0.5f, room.Z), parent);
            // Phase 2: attach MissionZone or AcademyMissionFlow composition here.
            // Phase 2: attach existing GuardFSM, SecurityCamera, DoorController and terminal components as required.
        }
    }

    private static void CreateSpawn(Transform parent)
    {
        PlacedRoom reception = PlacedRooms[0];
        GameObject spawn = CreateEmpty("SpawnPoint", new Vector3(reception.X, 1f, reception.Z - reception.Depth * 0.5f + 2f), parent);
        GameObject camera = new("MainCamera");
        camera.tag = "MainCamera";
        camera.transform.SetParent(parent);
        camera.transform.position = spawn.transform.position + new Vector3(0f, 0.7f, 0f);
        camera.AddComponent<Camera>();
        camera.AddComponent<AudioListener>();

        // Phase 2: MissionBootstrap resolves existing persistent services and sets Academy objective order.
        // Phase 2: AcademyMissionFlow completes objectives through existing systems; this generator remains visual-only.
    }

    private static GameObject CreateEmpty(string name, Vector3 position, Transform parent)
    {
        GameObject gameObject = new(name);
        if (parent != null)
            gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        return gameObject;
    }

    private static void MakeBox(Transform parent, string name, float x, float y, float z, float width, float height, float depth, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = new Vector3(x, y, z);
        box.transform.localScale = new Vector3(width, height, depth);
        ApplyMaterial(box, color);
    }

    private static void ApplyMaterial(GameObject gameObject, Color color)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer == null)
            return;

        if (!Materials.TryGetValue(color, out Material material))
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                return;

            material = new Material(shader) { color = color };
            Materials.Add(color, material);
        }

        renderer.sharedMaterial = material;
    }
}