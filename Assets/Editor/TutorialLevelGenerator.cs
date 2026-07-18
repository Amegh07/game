using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

public static class TutorialLevelGenerator
{
    const float ROOM_W = 6f;
    const float ROOM_D = 5f;
    const float ROOM_H = 3.5f;
    const float CORR_W = 3f;
    const float CORR_D = 3f;
    const float WALL_T = 0.2f;
    const float FLOOR_T = 0.1f;

    static readonly float[] ROOM_Z = { -9f, -1f, 7f, 15f, 23f };
    static readonly string[] ROOM_NAMES = { "TrainingHall", "KeycardChamber", "GuardGallery", "ServerAlcove", "Vault" };
    static readonly float[] ROOM_WIDTHS = { 6f, 5f, 7f, 5f, 6f };
    static readonly float[] ROOM_DEPTHS = { 6f, 5f, 6f, 5f, 5f };
    static readonly float[] CORR_Z = { -5f, 3f, 11f, 19f };

    static Dictionary<string, Material> _materials;
    static Dictionary<string, Object> _configs;

    [MenuItem("Tools/Tutorial/Generate Tutorial Level")]
    static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _materials = new Dictionary<string, Material>();
        _configs = new Dictionary<string, Object>();

        LoadConfigs();
        if (_configs["door"] == null || _configs["camera"] == null || _configs["terminal"] == null)
        {
            Debug.LogError("Tutorial config assets not found. Run Tools -> Tutorial -> Generate Config Assets first.");
            return;
        }

        GameObject envRoot = CreateEmpty("_Environment", Vector3.zero);
        GameObject roomsRoot = CreateEmpty("Rooms", Vector3.zero, envRoot.transform);
        GameObject propsRoot = CreateEmpty("_Props", Vector3.zero);
        GameObject triggersRoot = CreateEmpty("_Triggers", Vector3.zero);
        GameObject securityRoot = CreateEmpty("_Security", Vector3.zero);
        GameObject playerRoot = CreateEmpty("_Player", Vector3.zero);
        GameObject uiRoot = CreateEmpty("_UI", Vector3.zero);
        GameObject lightsRoot = CreateEmpty("_Lights", Vector3.zero);

        SpawnManagers(securityRoot, triggersRoot, uiRoot);
        SpawnRooms(roomsRoot);
        SpawnPickups(propsRoot);
        SpawnDoors(propsRoot);
        SpawnGuard(securityRoot, propsRoot);
        SpawnCamera(securityRoot);
        SpawnTerminal(propsRoot, securityRoot);
        SpawnArtifact(propsRoot);
        SpawnTriggers(triggersRoot);
        SpawnPlayer(playerRoot);
        SpawnLights(lightsRoot);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/Tutorial.unity");
        Debug.Log("Tutorial level generated! Tools -> Tutorial -> Generate Tutorial Level");
    }

    static void LoadConfigs()
    {
        string baseDir = "Assets/Config/Tutorial";
        _configs["door"] = AssetDatabase.LoadAssetAtPath<DoorConfig>($"{baseDir}/Doors/TutorialDoor.asset");
        _configs["camera"] = AssetDatabase.LoadAssetAtPath<CameraConfig>($"{baseDir}/Cameras/TutorialCamera.asset");
        _configs["terminal"] = AssetDatabase.LoadAssetAtPath<TerminalConfig>($"{baseDir}/Terminals/TutorialTerminal.asset");
        _configs["roles"] = AssetDatabase.LoadAssetAtPath<RolePermissionsConfig>($"{baseDir}/Roles/TutorialRoleConfig.asset");
    }

    static void SpawnManagers(GameObject securityRoot, GameObject triggersRoot, GameObject uiRoot)
    {
        GameObject smObj = new GameObject("SecurityManager");
        smObj.transform.SetParent(securityRoot.transform);
        var sm = smObj.AddComponent<SecurityManager>();

        GameObject cmObj = new GameObject("CredentialManager");
        cmObj.transform.SetParent(securityRoot.transform);
        cmObj.AddComponent<CredentialManager>();

        GameObject amObj = new GameObject("AuthenticationService");
        amObj.transform.SetParent(securityRoot.transform);
        amObj.AddComponent<AuthenticationService>();

        GameObject azObj = new GameObject("AuthorizationService");
        azObj.transform.SetParent(securityRoot.transform);
        var az = azObj.AddComponent<AuthorizationService>();
        if (_configs["roles"] is RolePermissionsConfig roleCfg)
            az.SetRoleConfig(roleCfg);

        GameObject imObj = new GameObject("InventoryManager");
        imObj.transform.SetParent(securityRoot.transform);
        imObj.AddComponent<InventoryManager>();

        GameObject mmObj = new GameObject("MissionManager");
        mmObj.transform.SetParent(triggersRoot.transform);
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

        GameObject timerObj = new GameObject("CameraDisableTimer");
        timerObj.transform.SetParent(securityRoot.transform);
        var timer = timerObj.AddComponent<CameraDisableTimer>();
        timer.SetPrivateField("eastCameraID", "camera_training");

        GameObject laptopObj = new GameObject("LaptopController");
        laptopObj.transform.SetParent(securityRoot.transform);
        laptopObj.AddComponent<LaptopController>();

        GameObject checkpointObj = new GameObject("CheckpointManager");
        checkpointObj.transform.SetParent(triggersRoot.transform);
        checkpointObj.AddComponent<CheckpointManager>();

        GameObject doorFeedbackObj = new GameObject("DoorUIFeedback");
        doorFeedbackObj.transform.SetParent(uiRoot.transform);
        doorFeedbackObj.AddComponent<DoorUIFeedback>();

        GameObject hudObj = new GameObject("MissionHUD");
        hudObj.transform.SetParent(uiRoot.transform);
        hudObj.AddComponent<MissionHUD>();

        GameObject debugObj = new GameObject("DebugOverlay");
        debugObj.transform.SetParent(uiRoot.transform);
        debugObj.AddComponent<DebugOverlay>();

        GameObject escapeObj = new GameObject("EscapePhaseController");
        escapeObj.transform.SetParent(triggersRoot.transform);
        var escape = escapeObj.AddComponent<EscapePhaseController>();
        escape.SetPrivateField("exitDoorID", "door_training_exit");
    }

    static void SpawnRooms(GameObject parent)
    {
        for (int i = 0; i < ROOM_NAMES.Length; i++)
        {
            bool hasBack = (i != 0);
            bool hasFront = (i != ROOM_NAMES.Length - 1);
            CreateRoom(parent.transform, ROOM_NAMES[i], ROOM_Z[i],
                hasBack, hasFront, ROOM_WIDTHS[i], ROOM_DEPTHS[i]);
        }

        for (int i = 0; i < CORR_Z.Length; i++)
        {
            CreateCorridor(parent.transform, "Corridor_" + (i + 1), CORR_Z[i]);
        }
    }

    static void SpawnPickups(GameObject parent)
    {
        GameObject keycardObj = CreatePrimitive("Keycard_Training", PrimitiveType.Cube,
            new Vector3(0f, 1f, -9f), new Vector3(0.4f, 0.1f, 0.3f), parent.transform, Color.yellow);
        UnityEngine.Object.DestroyImmediate(keycardObj.GetComponent<BoxCollider>());
        var keycard = keycardObj.AddComponent<KeycardItem>();
        keycardObj.AddComponent<SphereCollider>().isTrigger = true;
        keycard.SetPrivateField("keycardType", KeycardType.Public);

        GameObject credObj = CreatePrimitive("Credential_Training", PrimitiveType.Cube,
            new Vector3(0f, 1f, 15f), new Vector3(0.3f, 0.05f, 0.2f), parent.transform, new Color(0f, 1f, 0.4f));
        UnityEngine.Object.DestroyImmediate(credObj.GetComponent<BoxCollider>());
        var cred = credObj.AddComponent<CredentialItem>();
        credObj.AddComponent<SphereCollider>().isTrigger = true;
        cred.SetPrivateField("credentialID", "tutorial_admin");
        cred.SetPrivateField("credentialType", CredentialType.Keycard);
        cred.SetPrivateField("grantedRole", UserRole.Administrator);
        cred.SetPrivateField("displayName", "Training Credential");
    }

    static void SpawnDoors(GameObject parent)
    {
        CreateDoor(parent.transform, "door_training", 0f, -3f, KeycardType.Public, "Training Door");
        CreateDoor(parent.transform, "door_training_camera", 3.5f, 15f, KeycardType.Public, "Camera Gate");
        CreateDoor(parent.transform, "door_training_exit", 0f, 26f, KeycardType.Public, "Training Exit");
    }

    static void SpawnGuard(GameObject securityRoot, GameObject propsRoot)
    {
        GameObject wpRoot = CreateEmpty("GuardWaypoints", Vector3.zero, propsRoot.transform);
        Vector3[] wpPositions = { new Vector3(-2.5f, 0f, 7f), new Vector3(2.5f, 0f, 7f), new Vector3(2.5f, 0f, 10f) };
        Transform[] waypoints = new Transform[wpPositions.Length];
        for (int i = 0; i < wpPositions.Length; i++)
        {
            waypoints[i] = CreateEmpty("WP" + (i + 1), wpPositions[i], wpRoot.transform).transform;
        }

        GameObject alcove = CreatePrimitive("ShadowAlcove", PrimitiveType.Cube,
            new Vector3(-3f, 1.5f, 9f), new Vector3(0.1f, 3f, 2.5f), propsRoot.transform,
            new Color(0.15f, 0.15f, 0.15f));

        GameObject guardObj = CreatePrimitive("TrainingGuard", PrimitiveType.Capsule,
            new Vector3(0f, 0.5f, 7f), new Vector3(0.5f, 1f, 0.5f), securityRoot.transform,
            new Color(0.3f, 0.3f, 0.8f));
        var fsm = guardObj.AddComponent<GuardFSM>();
        fsm.waypoints = waypoints;
        fsm.patrolSpeed = 1.5f;
        fsm.visionRange = 8f;
        fsm.visionAngle = 75f;
        fsm.suspicionTime = 2f;
        fsm.searchDuration = 3f;
        fsm.player = null;
        UnityEngine.Object.DestroyImmediate(guardObj.GetComponent<CapsuleCollider>());
    }

    static void SpawnCamera(GameObject parent)
    {
        GameObject camObj = CreatePrimitive("TrainingCamera", PrimitiveType.Sphere,
            new Vector3(0f, 3f, 15f), new Vector3(0.3f, 0.3f, 0.3f), parent.transform, Color.red);
        UnityEngine.Object.DestroyImmediate(camObj.GetComponent<SphereCollider>());

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
    }

    static void SpawnTerminal(GameObject parent, GameObject securityRoot)
    {
        GameObject termObj = CreatePrimitive("TrainingTerminal", PrimitiveType.Cube,
            new Vector3(-1.5f, 0.75f, 15f), new Vector3(0.8f, 0.1f, 0.6f), parent.transform,
            new Color(0.1f, 0.1f, 0.12f));

        GameObject screenObj = CreatePrimitive("Screen", PrimitiveType.Cube,
            new Vector3(-1.5f, 1.1f, 14.7f), new Vector3(0.6f, 0.4f, 0.05f), termObj.transform,
            new Color(0f, 0.6f, 0.2f));

        var term = termObj.AddComponent<TerminalController>();
        term.SetPrivateField("terminalID", "terminal_training");
        term.SetPrivateField("config", _configs["terminal"] as TerminalConfig);

        var termLog = termObj.AddComponent<TerminalLog>();

        var connPoint = termObj.AddComponent<TerminalConnectionPoint>();

        var consoleUI = securityRoot.GetComponentInChildren<SecurityConsoleUI>();
        if (consoleUI == null)
        {
            GameObject uiObj = new GameObject("SecurityConsoleUI");
            uiObj.transform.SetParent(securityRoot.transform);
            consoleUI = uiObj.AddComponent<SecurityConsoleUI>();
        }
        term.SetPrivateField("consoleUI", consoleUI);
    }

    static void SpawnArtifact(GameObject parent)
    {
        GameObject artObj = CreatePrimitive("TrainingArtifact", PrimitiveType.Cube,
            new Vector3(0f, 1f, 23f), new Vector3(0.5f, 0.5f, 0.5f), parent.transform,
            new Color(1f, 0.8f, 0.2f));
        UnityEngine.Object.DestroyImmediate(artObj.GetComponent<BoxCollider>());
        var artifact = artObj.AddComponent<ArtifactCollectible>();
        artifact.completeObjective = ObjectiveID.Tutorial_StealArtifact;
        artifact.triggerEscapePhase = true;
        artObj.AddComponent<SphereCollider>().isTrigger = true;
    }

    static void SpawnTriggers(GameObject parent)
    {
        CreateMissionZone(parent.transform, "Zone_ReachKeycard",
            ObjectiveID.Tutorial_ReachKeycard, new Vector3(0f, 1.5f, -6f), new Vector3(4f, 3f, 2f));
        CreateMissionZone(parent.transform, "Zone_SneakPastGuard",
            ObjectiveID.Tutorial_SneakPastGuard, new Vector3(0f, 1.5f, 11f), new Vector3(4f, 3f, 2f));
        CreateMissionZone(parent.transform, "Zone_Escape",
            ObjectiveID.Tutorial_Escape, new Vector3(0f, 1.5f, 25f), new Vector3(5f, 3f, 3f));
    }

    static void SpawnPlayer(GameObject parent)
    {
        GameObject playerObj = new GameObject("PlayerController");
        playerObj.transform.SetParent(parent.transform);
        playerObj.transform.position = new Vector3(0f, 1f, -12f);

        var cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;

        GameObject camObj = new GameObject("PlayerCamera");
        camObj.transform.SetParent(playerObj.transform);
        camObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 90f;
        cam.nearClipPlane = 0.1f;

        playerObj.tag = "Player";

        var playerController = playerObj.AddComponent<PlayerController>();
        var interaction = playerObj.AddComponent<PlayerInteraction>();
    }

    static void SpawnLights(GameObject parent)
    {
        GameObject ambient = new GameObject("AmbientLight");
        var light = ambient.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.4f;
        light.color = new Color(0.7f, 0.7f, 0.8f);
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject keycardSpot = new GameObject("KeycardSpotlight");
        var spot = keycardSpot.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 8f;
        spot.spotAngle = 30f;
        spot.intensity = 1.5f;
        spot.color = Color.white;
        spot.transform.position = new Vector3(0f, 4f, -9f);
        spot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject credSpot = new GameObject("CredentialSpotlight");
        var spot2 = credSpot.AddComponent<Light>();
        spot2.type = LightType.Spot;
        spot2.range = 6f;
        spot2.spotAngle = 25f;
        spot2.intensity = 1.2f;
        spot2.color = new Color(0f, 1f, 0.4f);
        spot2.transform.position = new Vector3(0f, 4f, 15f);
        spot2.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    static void CreateRoom(Transform parent, string name, float zCenter,
        bool hasBack, bool hasFront, float w, float d)
    {
        GameObject room = CreateEmpty(name, Vector3.zero, parent);
        float hw = w / 2f, hd = d / 2f, hh = ROOM_H / 2f;
        Color c = Color.gray;

        CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0, FLOOR_T / 2f, zCenter),
            new Vector3(w, FLOOR_T, d), room.transform, c);
        CreatePrimitive("Ceiling", PrimitiveType.Cube, new Vector3(0, ROOM_H - FLOOR_T / 2f, zCenter),
            new Vector3(w, FLOOR_T, d), room.transform, c);
        CreatePrimitive("LeftWall", PrimitiveType.Cube, new Vector3(-hw, hh, zCenter),
            new Vector3(WALL_T, ROOM_H, d), room.transform, c);
        CreatePrimitive("RightWall", PrimitiveType.Cube, new Vector3(hw, hh, zCenter),
            new Vector3(WALL_T, ROOM_H, d), room.transform, c);
        if (hasBack)
            CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0, hh, zCenter - hd),
                new Vector3(w, ROOM_H, WALL_T), room.transform, c);
        if (hasFront)
            CreatePrimitive("FrontWall", PrimitiveType.Cube, new Vector3(0, hh, zCenter + hd),
                new Vector3(w, ROOM_H, WALL_T), room.transform, c);
    }

    static void CreateCorridor(Transform parent, string name, float zCenter)
    {
        CreateRoom(parent, name, zCenter, false, false, CORR_W, CORR_D);
    }

    static void CreateDoor(Transform parent, string id, float x, float z, KeycardType keycard, string name)
    {
        GameObject doorObj = CreatePrimitive("Door_" + name, PrimitiveType.Cube,
            new Vector3(x, 1.5f, z), new Vector3(2.5f, 2.8f, 0.12f), parent, Color.red);
        BoxCollider bc = doorObj.GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = false;

        var door = doorObj.AddComponent<DoorController>();
        door.AssignConfig(CreateDoorConfig(name, keycard));
        door.SetPrivateField("doorID", id);

        GameObject indicator = CreatePrimitive("LockIndicator", PrimitiveType.Cube,
            new Vector3(x + 1.5f, 1.5f, z), new Vector3(0.15f, 0.15f, 0.05f), doorObj.transform,
            Color.red);
    }

    static DoorConfig CreateDoorConfig(string doorName, KeycardType keycard)
    {
        string key = "doorcfg_" + doorName;
        if (_configs.TryGetValue(key, out var existing)) return (DoorConfig)existing;

        var cfg = ScriptableObject.CreateInstance<DoorConfig>();
        cfg.doorName = doorName;
        cfg.requiredKeycard = keycard;
        cfg.startsLocked = true;
        cfg.openSpeed = 120f;
        cfg.closeSpeed = 120f;
        cfg.openAngle = 90f;
        cfg.canAutoClose = false;
        cfg.lockOnAlert = false;
        cfg.lockDuringLockdown = false;

        string path = "Assets/Config/Tutorial/Doors/" + doorName.Replace(" ", "") + "DoorConfig.asset";
        var existingAsset = AssetDatabase.LoadAssetAtPath<DoorConfig>(path);
        if (existingAsset != null)
        {
            _configs[key] = existingAsset;
            return existingAsset;
        }

        AssetDatabase.CreateAsset(cfg, path);
        _configs[key] = cfg;
        return cfg;
    }

    static void CreateMissionZone(Transform parent, string name, ObjectiveID id, Vector3 position, Vector3 size)
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

    static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Transform parent, Color color)
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
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = position;
        return go;
    }
}
