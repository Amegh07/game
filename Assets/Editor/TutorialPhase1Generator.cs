using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

public static class TutorialPhase1Generator
{
    const float ROOM_H = 3.5f;
    const float WALL_T = 0.2f;
    const float FLOOR_T = 0.1f;
    const float DOOR_W = 1.8f;

    static Dictionary<string, Material> _materials;
    static Dictionary<string, Object> _configs;

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
        _materials = new Dictionary<string, Material>();
        _configs = new Dictionary<string, Object>();
        LoadConfigs();

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

        Transform managersRoot = root.transform.Find("Managers");
        Transform uiRoot = root.transform.Find("UI");

        SpawnManagers(managersRoot, uiRoot);
        PopulateRoom1(envRoot);
        PopulateRoom2(envRoot);
        PopulateRoom3(envRoot);
        PopulateRoom4(envRoot);
        PopulateRoom5(envRoot);
        SetupMission(managersRoot);

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
            MakeDoorwayWall(room.transform, "BackWall", cx, cz - hd, w, wallColor);
        if (hasFront)
            MakeDoorwayWall(room.transform, "FrontWall", cx, cz + hd, w, wallColor);
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

    static void MakeDoorwayWall(Transform parent, string name, float cx, float cz,
        float roomW, Color color)
    {
        float hh = ROOM_H / 2f;

        float segW = (roomW - DOOR_W) / 2f;
        if (segW <= 0f) return;

        float leftCx = cx - (roomW + DOOR_W) / 4f;
        float rightCx = cx + (roomW + DOOR_W) / 4f;

        MakeWall(parent, name + "_Left", leftCx, hh, cz, segW, ROOM_H, WALL_T, color);
        MakeWall(parent, name + "_Right", rightCx, hh, cz, segW, ROOM_H, WALL_T, color);
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

    // ---------------------------------------------------------------
    //  Config
    // ---------------------------------------------------------------

    static void LoadConfigs()
    {
        string baseDir = "Assets/Config/Tutorial";
        _configs["door"] = AssetDatabase.LoadAssetAtPath<DoorConfig>($"{baseDir}/Doors/TutorialDoor.asset");
        _configs["camera"] = AssetDatabase.LoadAssetAtPath<CameraConfig>($"{baseDir}/Cameras/TutorialCamera.asset");
        _configs["terminal"] = AssetDatabase.LoadAssetAtPath<TerminalConfig>($"{baseDir}/Terminals/TutorialTerminal.asset");
        _configs["roles"] = AssetDatabase.LoadAssetAtPath<RolePermissionsConfig>($"{baseDir}/Roles/TutorialRoleConfig.asset");
    }

    // ---------------------------------------------------------------
    //  Managers
    // ---------------------------------------------------------------

    static void SpawnManagers(Transform managersRoot, Transform uiRoot)
    {
        GameObject smObj = new GameObject("SecurityManager");
        smObj.transform.SetParent(managersRoot);
        smObj.AddComponent<SecurityManager>();

        GameObject cmObj = new GameObject("CredentialManager");
        cmObj.transform.SetParent(managersRoot);
        cmObj.AddComponent<CredentialManager>();

        GameObject amObj = new GameObject("AuthenticationService");
        amObj.transform.SetParent(managersRoot);
        amObj.AddComponent<AuthenticationService>();

        GameObject azObj = new GameObject("AuthorizationService");
        azObj.transform.SetParent(managersRoot);
        var az = azObj.AddComponent<AuthorizationService>();
        if (_configs["roles"] is RolePermissionsConfig roleCfg)
            az.SetRoleConfig(roleCfg);

        GameObject imObj = new GameObject("InventoryManager");
        imObj.transform.SetParent(managersRoot);
        imObj.AddComponent<InventoryManager>();

        GameObject timerObj = new GameObject("CameraDisableTimer");
        timerObj.transform.SetParent(managersRoot);
        var timer = timerObj.AddComponent<CameraDisableTimer>();
        timer.SetPrivateField("eastCameraID", "camera_training");

        GameObject laptopObj = new GameObject("LaptopController");
        laptopObj.transform.SetParent(managersRoot);
        laptopObj.AddComponent<LaptopController>();

        GameObject checkpointObj = new GameObject("CheckpointManager");
        checkpointObj.transform.SetParent(managersRoot);
        checkpointObj.AddComponent<CheckpointManager>();

        GameObject doorFeedbackObj = new GameObject("DoorUIFeedback");
        doorFeedbackObj.transform.SetParent(uiRoot);
        doorFeedbackObj.AddComponent<DoorUIFeedback>();

        GameObject hudObj = new GameObject("MissionHUD");
        hudObj.transform.SetParent(uiRoot);
        hudObj.AddComponent<MissionHUD>();

        GameObject debugObj = new GameObject("DebugOverlay");
        debugObj.transform.SetParent(uiRoot);
        debugObj.AddComponent<DebugOverlay>();
    }

    // ---------------------------------------------------------------
    //  Room 1 — TrainingHall: Keycard + ReachKeycard trigger
    // ---------------------------------------------------------------

    static void PopulateRoom1(Transform envRoot)
    {
        Transform room = envRoot.Find("Room1_TrainingHall");
        if (room == null) return;

        GameObject keycardObj = CreatePrimitive("TrainingKeycard", PrimitiveType.Cube,
            new Vector3(0f, 1f, -9f), new Vector3(0.4f, 0.1f, 0.3f), room, Color.yellow);
        Object.DestroyImmediate(keycardObj.GetComponent<BoxCollider>());
        var keycard = keycardObj.AddComponent<KeycardItem>();
        keycardObj.AddComponent<SphereCollider>().isTrigger = true;
        keycard.SetPrivateField("keycardType", KeycardType.Public);

        CreateMissionZone(room, "Zone_ReachKeycard",
            ObjectiveID.Tutorial_ReachKeycard, new Vector3(0f, 1.5f, -7.5f), new Vector3(4f, 3f, 3f));
    }

    // ---------------------------------------------------------------
    //  Room 2 — KeycardDoor: DoorController + DoorConfig
    // ---------------------------------------------------------------

    static void PopulateRoom2(Transform envRoot)
    {
        Transform room = envRoot.Find("Room2_KeycardDoor");
        if (room == null) return;

        CreateTutorialDoor(room, "door_training", 0f, -3f, KeycardType.Public, "Training Door");

        CreateMissionZone(room, "Zone_PickupKeycard",
            ObjectiveID.Tutorial_PickupKeycard, new Vector3(0f, 1.5f, -1.5f), new Vector3(2f, 3f, 2f));
    }

    // ---------------------------------------------------------------
    //  Room 3 — GuardGallery: GuardFSM + waypoints
    // ---------------------------------------------------------------

    static void PopulateRoom3(Transform envRoot)
    {
        Transform room = envRoot.Find("Room3_GuardGallery");
        if (room == null) return;

        GameObject wpRoot = CreateEmpty("GuardWaypoints", Vector3.zero, room);
        Vector3[] wpPositions =
        {
            new Vector3(-2.5f, 0f, 7f),
            new Vector3(2.5f, 0f, 7f),
            new Vector3(2.5f, 0f, 10f)
        };
        Transform[] waypoints = new Transform[wpPositions.Length];
        for (int i = 0; i < wpPositions.Length; i++)
            waypoints[i] = CreateEmpty("WP" + (i + 1), wpPositions[i], wpRoot.transform).transform;

        GameObject guardObj = CreatePrimitive("TrainingGuard", PrimitiveType.Capsule,
            new Vector3(0f, 0.5f, 7f), new Vector3(0.5f, 1f, 0.5f), room, new Color(0.3f, 0.3f, 0.8f));
        var fsm = guardObj.AddComponent<GuardFSM>();
        fsm.waypoints = waypoints;
        fsm.patrolSpeed = 1.5f;
        fsm.visionRange = 8f;
        fsm.visionAngle = 75f;
        fsm.suspicionTime = 2f;
        fsm.searchDuration = 3f;
        fsm.player = null;
        Object.DestroyImmediate(guardObj.GetComponent<CapsuleCollider>());

        CreateMissionZone(room, "Zone_OpenDoor",
            ObjectiveID.Tutorial_OpenDoor, new Vector3(0f, 1.5f, 4f), new Vector3(2f, 3f, 2f));

        CreateMissionZone(room, "Zone_SneakPastGuard",
            ObjectiveID.Tutorial_SneakPastGuard, new Vector3(0f, 1.5f, 11f), new Vector3(4f, 3f, 2f));
    }

    // ---------------------------------------------------------------
    //  Room 4 — ServerRoom: SecurityCamera + TerminalController
    // ---------------------------------------------------------------

    static void PopulateRoom4(Transform envRoot)
    {
        Transform room = envRoot.Find("Room4_ServerRoom");
        if (room == null) return;

        GameObject camObj = CreatePrimitive("TrainingCamera", PrimitiveType.Sphere,
            new Vector3(0f, 3f, 15f), new Vector3(0.3f, 0.3f, 0.3f), room, Color.red);
        Object.DestroyImmediate(camObj.GetComponent<SphereCollider>());
        var cam = camObj.AddComponent<SecurityCamera>();
        cam.SetPrivateField("cameraID", "camera_training");
        cam.SetPrivateField("config", _configs["camera"] as CameraConfig);
        if (cam.config != null)
        {
            cam.SetPrivateField("rotationAngle", cam.config.patrolAngle);
            cam.SetPrivateField("rotationSpeed", cam.config.patrolSpeed);
            cam.SetPrivateField("detectionRange", cam.config.detectionRange);
            cam.SetPrivateField("fieldOfView", cam.config.fieldOfView);
        }

        GameObject termObj = CreatePrimitive("TrainingTerminal", PrimitiveType.Cube,
            new Vector3(-1.5f, 0.75f, 15f), new Vector3(0.8f, 0.1f, 0.6f), room,
            new Color(0.1f, 0.1f, 0.12f));

        GameObject screenObj = CreatePrimitive("Screen", PrimitiveType.Cube,
            new Vector3(-1.5f, 1.1f, 14.7f), new Vector3(0.6f, 0.4f, 0.05f), termObj.transform,
            new Color(0f, 0.6f, 0.2f));

        var term = termObj.AddComponent<TerminalController>();
        term.SetPrivateField("terminalID", "terminal_training");
        term.SetPrivateField("config", _configs["terminal"] as TerminalConfig);

        termObj.AddComponent<TerminalLog>();
        termObj.AddComponent<TerminalConnectionPoint>();

        var consoleUI = Object.FindFirstObjectByType<SecurityConsoleUI>();
        if (consoleUI == null)
        {
            GameObject uiObj = new GameObject("SecurityConsoleUI");
            uiObj.transform.SetParent(room);
            consoleUI = uiObj.AddComponent<SecurityConsoleUI>();
        }
        term.SetPrivateField("consoleUI", consoleUI);

        CreateMissionZone(room, "Zone_DisableCamera",
            ObjectiveID.Tutorial_DisableCamera, new Vector3(0f, 1.5f, 14f), new Vector3(4f, 3f, 3f));
    }

    // ---------------------------------------------------------------
    //  Room 5 — Vault: ArtifactCollectible + Exit trigger
    // ---------------------------------------------------------------

    static void PopulateRoom5(Transform envRoot)
    {
        Transform room = envRoot.Find("Room5_Vault");
        if (room == null) return;

        GameObject artObj = CreatePrimitive("TrainingArtifact", PrimitiveType.Cube,
            new Vector3(0f, 1f, 23f), new Vector3(0.5f, 0.5f, 0.5f), room,
            new Color(1f, 0.8f, 0.2f));
        Object.DestroyImmediate(artObj.GetComponent<BoxCollider>());
        var artifact = artObj.AddComponent<ArtifactCollectible>();
        artifact.completeObjective = ObjectiveID.Tutorial_StealArtifact;
        artifact.triggerEscapePhase = true;
        artObj.AddComponent<SphereCollider>().isTrigger = true;

        CreateMissionZone(room, "Zone_Escape",
            ObjectiveID.Tutorial_Escape, new Vector3(0f, 1.5f, 25f), new Vector3(5f, 3f, 3f));
    }

    // ---------------------------------------------------------------
    //  Mission
    // ---------------------------------------------------------------

    static void SetupMission(Transform managersRoot)
    {
        GameObject mmObj = new GameObject("MissionManager");
        mmObj.transform.SetParent(managersRoot);
        var mm = mmObj.AddComponent<MissionManager>();
        mm.autoStart = true;
        mm.objectiveOrder = new List<ObjectiveID>
        {
            ObjectiveID.Tutorial_ReachKeycard,
            ObjectiveID.Tutorial_PickupKeycard,
            ObjectiveID.Tutorial_OpenDoor,
            ObjectiveID.Tutorial_SneakPastGuard,
            ObjectiveID.Tutorial_DisableCamera,
            ObjectiveID.Tutorial_StealArtifact,
            ObjectiveID.Tutorial_Escape
        };

        GameObject escapeObj = new GameObject("EscapePhaseController");
        escapeObj.transform.SetParent(managersRoot);
        var escape = escapeObj.AddComponent<EscapePhaseController>();
        escape.SetPrivateField("exitDoorID", "door_training_exit");
    }

    // ---------------------------------------------------------------
    //  Door helper
    // ---------------------------------------------------------------

    static void CreateTutorialDoor(Transform parent, string id, float x, float z,
        KeycardType keycard, string doorName)
    {
        GameObject doorObj = CreatePrimitive("Door_" + doorName, PrimitiveType.Cube,
            new Vector3(x, 1.5f, z), new Vector3(2.5f, 2.8f, 0.12f), parent, Color.red);
        BoxCollider bc = doorObj.GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = false;

        var door = doorObj.AddComponent<DoorController>();
        DoorConfig cfg = _configs["door"] as DoorConfig;
        if (cfg != null)
        {
            door.AssignConfig(cfg);
        }
        else
        {
            var fallback = ScriptableObject.CreateInstance<DoorConfig>();
            fallback.doorName = doorName;
            fallback.requiredKeycard = keycard;
            fallback.startsLocked = true;
            fallback.openSpeed = 120f;
            fallback.closeSpeed = 120f;
            fallback.openAngle = 90f;
            fallback.canAutoClose = false;
            fallback.lockOnAlert = false;
            fallback.lockDuringLockdown = false;
            door.AssignConfig(fallback);
        }
        door.SetPrivateField("doorID", id);

        CreatePrimitive("LockIndicator", PrimitiveType.Cube,
            new Vector3(x + 1.5f, 1.5f, z), new Vector3(0.15f, 0.15f, 0.05f), doorObj.transform,
            Color.red);
    }

    // ---------------------------------------------------------------
    //  MissionZone helper
    // ---------------------------------------------------------------

    static void CreateMissionZone(Transform parent, string name, ObjectiveID id,
        Vector3 position, Vector3 size)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = position;

        var bc = zone.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = size;

        var mz = zone.AddComponent<MissionZone>();
        mz.objectiveID = id;
        mz.oneShot = true;
    }

    // ---------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------

    static GameObject CreatePrimitive(string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Transform parent, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            string colorKey = "mat_" + color.ToString();
            if (!_materials.TryGetValue(colorKey, out Material mat))
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                _materials[colorKey] = mat;
            }
            renderer.sharedMaterial = mat;
        }

        return go;
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
