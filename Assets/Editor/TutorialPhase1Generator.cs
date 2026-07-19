using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class TutorialPhase1Generator
{
    const float ROOM_H = 3.5f;
    const float WALL_T = 0.2f;
    const float FLOOR_T = 0.1f;

    static readonly string[] ROOM_NAMES =
    {
        "Room1_TrainingHall",
        "Room2_KeycardDoor",
        "Room3_GuardGallery",
        "Room4_ServerRoom",
        "Room5_Vault"
    };

    static readonly float[] ROOM_Z = { -9f, -1f, 7f, 15f, 23f };
    static readonly float[] ROOM_WIDTHS = { 6f, 5f, 7f, 5f, 6f };
    static readonly float[] ROOM_DEPTHS = { 6f, 5f, 6f, 5f, 5f };
    static readonly float[] CORR_Z = { -5f, 3f, 11f, 19f };
    const float CORR_W = 3f;
    const float CORR_D = 3f;

    [MenuItem("Tools/Tutorial/Generate Phase 1 Environment")]
    static void Generate()
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Play Mode Active",
                "Exit Play Mode before generating the tutorial environment.", "OK");
            Debug.LogWarning("[TutorialPhase1Generator] Cannot generate in Play Mode. Exit Play Mode and try again.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root      = CreateEmpty("Tutorial", Vector3.zero);
        CreateEmpty("Environment", Vector3.zero, root.transform);
        CreateEmpty("Lighting", Vector3.zero, root.transform);
        CreateEmpty("Navigation", Vector3.zero, root.transform);
        CreateEmpty("Gameplay", Vector3.zero, root.transform);
        CreateEmpty("Managers", Vector3.zero, root.transform);
        CreateEmpty("Player", Vector3.zero, root.transform);
        CreateEmpty("UI", Vector3.zero, root.transform);

        Transform envRoot = root.transform.Find("Environment");
        Transform lightRoot = root.transform.Find("Lighting");
        Transform playerRoot = root.transform.Find("Player");

        for (int i = 0; i < ROOM_NAMES.Length; i++)
        {
            bool hasBack = (i != 0);
            bool hasFront = (i != ROOM_NAMES.Length - 1);
            CreateRoom(envRoot, ROOM_NAMES[i], 0f, ROOM_Z[i],
                ROOM_WIDTHS[i], ROOM_DEPTHS[i], hasBack, hasFront);
        }

        for (int i = 0; i < CORR_Z.Length; i++)
            CreateCorridor(envRoot, "Corridor_" + (i + 1), CORR_Z[i]);

        SpawnLights(lightRoot);

        GameObject spawn = new GameObject("SpawnPoint");
        spawn.transform.SetParent(playerRoot);
        spawn.transform.position = new Vector3(0f, 1f, -12f);

        GameObject camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(playerRoot);
        camGO.transform.position = new Vector3(0f, 1.7f, -11f);
        camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/Tutorial.unity");
        Debug.Log("Tutorial Phase 1 environment generated.");
    }

    static void CreateRoom(Transform parent, string name, float cx, float cz,
        float w, float d, bool hasBack, bool hasFront)
    {
        GameObject room = new GameObject(name);
        room.transform.SetParent(parent);

        float hw = w / 2f;
        float hd = d / 2f;
        float hh = ROOM_H / 2f;

        Color floorColor = new Color(0.35f, 0.35f, 0.38f);
        Color wallColor  = new Color(0.55f, 0.53f, 0.50f);
        Color ceilColor  = new Color(0.40f, 0.40f, 0.42f);

        MakeFloor(room.transform, "Floor", cx, FLOOR_T / 2f, cz, w, FLOOR_T, d, floorColor);
        MakeFloor(room.transform, "Ceiling", cx, ROOM_H - FLOOR_T / 2f, cz, w, FLOOR_T, d, ceilColor);
        MakeWall(room.transform, "LeftWall",  cx - hw, hh, cz, WALL_T, ROOM_H, d, wallColor);
        MakeWall(room.transform, "RightWall", cx + hw, hh, cz, WALL_T, ROOM_H, d, wallColor);
        if (hasBack)
            MakeWall(room.transform, "BackWall", cx, hh, cz - hd, w, ROOM_H, WALL_T, wallColor);
        if (hasFront)
            MakeWall(room.transform, "FrontWall", cx, hh, cz + hd, w, ROOM_H, WALL_T, wallColor);
    }

    static void CreateCorridor(Transform parent, string name, float cz)
    {
        CreateRoom(parent, name, 0f, cz, CORR_W, CORR_D, false, false);
    }

    static void MakeFloor(Transform parent, string name, float x, float y, float z,
        float sx, float sy, float sz, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(sx, sy, sz);
        ApplyMaterial(go, color);
    }

    static void MakeWall(Transform parent, string name, float x, float y, float z,
        float sx, float sy, float sz, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(sx, sy, sz);
        ApplyMaterial(go, color);
    }

    static void ApplyMaterial(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }

    static void SpawnLights(Transform parent)
    {
        GameObject ambient = new GameObject("AmbientLight");
        ambient.transform.SetParent(parent);
        var light = ambient.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.4f;
        light.color = new Color(0.7f, 0.7f, 0.8f);
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        for (int i = 0; i < ROOM_NAMES.Length; i++)
        {
            GameObject spot = new GameObject("FillLight_" + ROOM_NAMES[i]);
            spot.transform.SetParent(parent);
            var s = spot.AddComponent<Light>();
            s.type = LightType.Spot;
            s.range = 8f;
            s.spotAngle = 50f;
            s.intensity = 0.8f;
            s.color = new Color(0.9f, 0.85f, 0.8f);
            s.transform.position = new Vector3(0f, ROOM_H - 0.3f, ROOM_Z[i]);
            s.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        for (int i = 0; i < CORR_Z.Length; i++)
        {
            GameObject spot = new GameObject("FillLight_Corridor_" + (i + 1));
            spot.transform.SetParent(parent);
            var s = spot.AddComponent<Light>();
            s.type = LightType.Spot;
            s.range = 6f;
            s.spotAngle = 40f;
            s.intensity = 0.5f;
            s.color = new Color(0.85f, 0.8f, 0.75f);
            s.transform.position = new Vector3(0f, ROOM_H - 0.3f, CORR_Z[i]);
            s.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    static GameObject CreateEmpty(string name, Vector3 position, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent);
        go.transform.position = position;
        return go;
    }
}
