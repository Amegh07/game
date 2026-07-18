using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

public class MuseumLevelGenerator
{
    const float ROOM_W = 6f;
    const float ROOM_D = 5f;
    const float ROOM_H = 3.5f;
    const float CORR_W = 3f;
    const float CORR_D = 3f;
    const float WALL_T = 0.2f;
    const float FLOOR_T = 0.1f;

    // Room Z-center positions (linear layout, +Z = north / deeper into museum)
    static readonly float[] ROOM_Z = { -22, -14, -7, 1, 7, 13, 18, 22, 26, 30 };
    static readonly string[] ROOM_NAMES = {
        "Entrance", "Lobby", "GalleryA", "StaffOffice",
        "SecurityOffice", "ControlRoom", "RestrictedCorridor",
        "VaultAntechamber", "Vault", "EmergencyExit"
    };
    static readonly float[] ROOM_WIDTHS = { 5, 6, 7, 5, 5, 4, 3, 5, 6, 4 };
    static readonly float[] ROOM_DEPTHS = { 5, 5, 6, 4, 4, 4, 6, 4, 5, 3 };

    // Corridor Z positions (between rooms)
    static readonly float[] CORR_Z = { -18, -10.5f, -3, 4, 10, 15.5f, 20, 24, 28 };

    // Door positions (X, Z) with config
    struct DoorDef { public string id; public float x, z; public KeycardType keycard; public string name; }
    static readonly DoorDef[] DOORS = {
        new DoorDef { id = "door_staff", x = 0, z = -5, keycard = KeycardType.Staff, name = "Staff Office" },
        new DoorDef { id = "door_restricted", x = 0, z = 11, keycard = KeycardType.Security, name = "Restricted Wing" },
        new DoorDef { id = "door_vault", x = 0, z = 24, keycard = KeycardType.Vault, name = "Vault" },
        new DoorDef { id = "door_emergency", x = 0, z = 28, keycard = KeycardType.Emergency, name = "Emergency Exit" },
    };

    static Dictionary<string, Material> _materials;
    static Dictionary<string, Object> _assets;

    [MenuItem("Tools/Museum Heist/Generate Museum Level")]
    static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _materials = new Dictionary<string, Material>();
        _assets = new Dictionary<string, Object>();

        // ---- Hierarchy roots ----
        GameObject envRoot = CreateEmpty("_Environment", Vector3.zero);
        GameObject roomsRoot = CreateEmpty("Rooms", Vector3.zero, envRoot.transform);
        GameObject propsRoot = CreateEmpty("_Props", Vector3.zero);
        GameObject guardsRoot = CreateEmpty("_Guards", Vector3.zero);
        GameObject camerasRoot = CreateEmpty("_Cameras", Vector3.zero);
        GameObject terminalsRoot = CreateEmpty("_Terminals", Vector3.zero);
        GameObject doorsRoot = CreateEmpty("_Doors", Vector3.zero);
        GameObject lightsRoot = CreateEmpty("_Lights", Vector3.zero);
        GameObject missionRoot = CreateEmpty("_Mission", Vector3.zero);
        GameObject cyberRoot = CreateEmpty("_CyberSystems", Vector3.zero);

        // ---- Create rooms ----
        for (int i = 0; i < ROOM_NAMES.Length; i++)
        {
            bool hasBack = i == 0;
            bool hasFront = i == ROOM_NAMES.Length - 1;
            CreateRoom(roomsRoot.transform, ROOM_NAMES[i], ROOM_Z[i],
                hasBack, hasFront, ROOM_WIDTHS[i], ROOM_DEPTHS[i]);
        }

        // ---- Create corridors ----
        for (int i = 0; i < CORR_Z.Length; i++)
        {
            CreateCorridor(roomsRoot.transform, "Corridor_" + (i + 1), CORR_Z[i]);
        }

        // ---- Services (singletons) ----
        // SecurityManager
        GameObject smObj = new GameObject("SecurityManager");
        smObj.transform.SetParent(null);
        smObj.AddComponent<SecurityManager>();

        // Cyber services
        CreateCyberService(cyberRoot.transform, "AuthenticationService", typeof(AuthenticationService));
        CreateCyberService(cyberRoot.transform, "AuthorizationService", typeof(AuthorizationService));
        CreateCyberService(cyberRoot.transform, "CredentialManager", typeof(CredentialManager));

        // MissionManager
        SetupMissionManager(missionRoot.transform);

        // CheckpointManager
        GameObject cmObj = new GameObject("CheckpointManager");
        cmObj.transform.SetParent(missionRoot.transform);
        cmObj.AddComponent<CheckpointManager>();

        // DemoModeController
        GameObject dmObj = new GameObject("DemoModeController");
        dmObj.transform.SetParent(null);
        dmObj.AddComponent<DemoModeController>();

        // ---- Doors ----
        foreach (var d in DOORS)
        {
            CreateDoor(doorsRoot.transform, d.id, d.x, d.z, d.keycard, d.name);
        }

        // ---- Guards ----
        CreateGuard(guardsRoot.transform, "Guard_GalleryA", new Vector3(-2.5f, 0.5f, -7), new Vector3[] {
            new Vector3(-3, 0, -9), new Vector3(-3, 0, -5), new Vector3(3, 0, -5), new Vector3(3, 0, -9)
        }, Color.blue);

        CreateGuard(guardsRoot.transform, "Guard_Restricted", new Vector3(0, 0.5f, 18), new Vector3[] {
            new Vector3(-1, 0, 16), new Vector3(-1, 0, 20), new Vector3(1, 0, 20), new Vector3(1, 0, 16)
        }, Color.blue);

        CreateGuard(guardsRoot.transform, "Guard_Vault", new Vector3(0, 0.5f, 26), new Vector3[] {
            new Vector3(-2, 0, 24), new Vector3(-2, 0, 28), new Vector3(2, 0, 28), new Vector3(2, 0, 24)
        }, Color.blue);

        // ---- Cameras ----
        CreateCamera(camerasRoot.transform, "camera_east", new Vector3(0, 2.8f, -7), 45f, 30f, 8f, 60f, true);
        CreateCamera(camerasRoot.transform, "camera_security_room", new Vector3(0, 2.8f, 7), 45f, 30f, 8f, 60f, true);
        CreateCamera(camerasRoot.transform, "camera_west", new Vector3(0, 2.8f, 13), 45f, 30f, 8f, 60f, false);

        // ---- Credentials ----
        GameObject staffCredObj = CreatePrimitive("StaffCredential", PrimitiveType.Cube,
            new Vector3(2f, 0.3f, -7), new Vector3(0.3f, 0.05f, 0.2f), propsRoot.transform, Color.yellow);
        var staffCred = staffCredObj.AddComponent<CredentialItem>();
        staffCred.SetPrivateField("credentialID", "cred_staff");
        staffCred.SetPrivateField("credentialType", CredentialType.Keycard);
        staffCred.SetPrivateField("grantedRole", UserRole.Staff);
        staffCred.SetPrivateField("displayName", "Staff Keycard");

        GameObject secCredObj = CreatePrimitive("SecurityCredential", PrimitiveType.Cube,
            new Vector3(-2f, 0.3f, 7), new Vector3(0.3f, 0.05f, 0.2f), propsRoot.transform, new Color(1f, 0.5f, 0f));
        var secCred = secCredObj.AddComponent<CredentialItem>();
        secCred.SetPrivateField("credentialID", "cred_security");
        secCred.SetPrivateField("credentialType", CredentialType.Keycard);
        secCred.SetPrivateField("grantedRole", UserRole.SecurityOfficer);
        secCred.SetPrivateField("displayName", "Security Credential");

        // ---- Terminals ----
        CreateTerminal(terminalsRoot.transform, "StaffTerminal", "term_staff", "Staff Terminal",
            "STAFF ACCESS ONLY", new Color(0.2f, 0.6f, 0.2f),
            CredentialType.Keycard, UserRole.Staff, new Vector3(-1.5f, 0.8f, 1), null);

        CreateTerminal(terminalsRoot.transform, "SecurityTerminal", "term_security", "Security Terminal",
            "CLASSIFIED — SECURITY OPERATIONS", new Color(0.9f, 0.3f, 0.1f),
            CredentialType.Keycard, UserRole.SecurityOfficer, new Vector3(1.5f, 0.8f, 7),
            new List<(string type, string name, string desc, string perm, string target)> {
                ("DisableCamera", "Disable East Cameras", "Disable east wing cameras (30s).", Permissions.DisableCameras, "camera_east"),
                ("UnlockDoor", "Unlock Restricted Wing", "Unlock door to restricted area.", Permissions.OpenSecurityDoors, "door_restricted"),
                ("ResetAlarm", "Reset Alarm", "Reset alarm to normal state.", Permissions.ResetAlarm, "")
            });

        CreateTerminal(terminalsRoot.transform, "VaultTerminal", "term_vault", "Vault Terminal",
            "MAXIMUM SECURITY — VAULT CONTROL", new Color(1f, 0.6f, 0f),
            CredentialType.Keycard, UserRole.Administrator, new Vector3(0, 0.8f, 22),
            new List<(string type, string name, string desc, string perm, string target)> {
                ("OpenVault", "Open Vault", "Override vault door security.", Permissions.OpenVault, "door_vault"),
                ("DownloadLogs", "Download Audit Log", "Export vault audit trail.", Permissions.DownloadLogs, "")
            });

        // ---- Artifact ----
        GameObject artifactObj = CreatePrimitive("MainArtifact", PrimitiveType.Sphere,
            new Vector3(0, 1f, 26), Vector3.one * 0.5f, propsRoot.transform, new Color(1f, 0.8f, 0.2f));
        var artifact = artifactObj.AddComponent<ArtifactController>();
        BoxCollider artCol = artifactObj.GetComponent<BoxCollider>();
        if (artCol == null) artCol = artifactObj.AddComponent<BoxCollider>();
        artCol.isTrigger = true;
        artCol.size = Vector3.one * 1.5f;

        // ---- Artifact pedestal ----
        CreatePrimitive("ArtifactPedestal", PrimitiveType.Cube,
            new Vector3(0, 0.25f, 26), new Vector3(1f, 0.1f, 1f), propsRoot.transform, Color.gray);

        // ---- Player ----
        CreatePlayer(propsRoot.transform, new Vector3(0, 1f, -25));

        // ---- Lights ----
        CreateLight("DirectionalLight", LightType.Directional, new Vector3(0, 15, 0), Color.white, lightsRoot.transform)
            .GetComponent<Light>().intensity = 0.8f;
        CreateLight("DirectionalLight", LightType.Directional, new Vector3(0, 15, 0), Color.white, lightsRoot.transform)
            .transform.eulerAngles = new Vector3(50, -30, 0);

        string[] lightRooms = { "Entrance", "Lobby", "GalleryA", "StaffOffice", "SecurityOffice",
            "ControlRoom", "RestrictedCorridor", "VaultAntechamber", "Vault", "EmergencyExit" };
        for (int i = 0; i < lightRooms.Length; i++)
        {
            var light = CreateLight("PointLight_" + lightRooms[i], LightType.Point,
                new Vector3(0, 2.8f, ROOM_Z[i]), new Color(0.9f, 0.9f, 1f), lightsRoot.transform)
                .GetComponent<Light>();
            light.range = 6f;
            light.intensity = 0.6f;
        }

        // ---- Camera timer coroutine holder ----
        GameObject timerObj = new GameObject("CameraTimer");
        timerObj.transform.SetParent(null);
        timerObj.AddComponent<CameraDisableTimer>();

        // ---- Save ----
        string sceneDir = Application.dataPath + "/Scenes";
        Directory.CreateDirectory(sceneDir);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/MuseumHeist.unity");
        Debug.Log("Museum Heist level generated! Tools -> Museum Heist -> Generate Museum Level");
    }

    // ---------------------------------------------------------------
    //  Room & Corridor
    // ---------------------------------------------------------------

    static void CreateRoom(Transform parent, string name, float zCenter,
        bool hasBack, bool hasFront, float w, float d)
    {
        GameObject room = CreateEmpty(name, Vector3.zero, parent);
        float hw = w / 2f, hd = d / 2f, hh = ROOM_H / 2f;
        Color c = Color.gray;

        if (name == "Vault") c = new Color(0.3f, 0.3f, 0.35f);
        if (name == "EmergencyExit") c = Color.white;

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

    // ---------------------------------------------------------------
    //  Doors
    // ---------------------------------------------------------------

    static void CreateDoor(Transform parent, string id, float x, float z, KeycardType keycard, string name)
    {
        GameObject doorObj = CreatePrimitive("Door_" + name, PrimitiveType.Cube,
            new Vector3(x, 1.5f, z), new Vector3(2.5f, 2.8f, 0.12f), parent, Color.red);
        BoxCollider bc = doorObj.GetComponent<BoxCollider>();
        if (bc != null) bc.isTrigger = false;

        var door = doorObj.AddComponent<DoorController>();
        door.AssignConfig(CreateDoorConfig(name, keycard));
        door.SetPrivateField("doorID", id);
    }

    static DoorConfig CreateDoorConfig(string doorName, KeycardType keycard)
    {
        string key = "doorcfg_" + doorName;
        if (_assets.TryGetValue(key, out var existing)) return (DoorConfig)existing;

        var cfg = ScriptableObject.CreateInstance<DoorConfig>();
        cfg.doorName = doorName;
        cfg.requiredKeycard = keycard;
        cfg.startsLocked = true;
        cfg.openSpeed = 120f;
        cfg.closeSpeed = 120f;
        cfg.openAngle = 90f;
        cfg.canAutoClose = true;
        cfg.autoCloseDelay = 3f;
        cfg.lockDuringLockdown = true;
        cfg.isEmergencyExit = (doorName == "Emergency Exit");

        string path = "Assets/Materials/Generated/" + doorName.Replace(" ", "") + "DoorConfig.asset";
        AssetDatabase.CreateAsset(cfg, path);
        _assets[key] = cfg;
        return cfg;
    }

    // ---------------------------------------------------------------
    //  Guards
    // ---------------------------------------------------------------

    static void CreateGuard(Transform parent, string name, Vector3 position, Vector3[] waypoints, Color color)
    {
        GameObject guardObj = CreatePrimitive(name, PrimitiveType.Capsule,
            position, new Vector3(0.5f, 1f, 0.5f), parent, color);

        GuardFSM fsm = guardObj.AddComponent<GuardFSM>();
        fsm.waypoints = new Transform[waypoints.Length];
        GameObject wpRoot = CreateEmpty(name + "_Waypoints", Vector3.zero, parent);
        for (int i = 0; i < waypoints.Length; i++)
        {
            fsm.waypoints[i] = CreateEmpty("WP" + (i + 1), waypoints[i], wpRoot.transform).transform;
        }

        fsm.patrolSpeed = 1.5f;
        fsm.patrolRotateSpeed = 4f;
        fsm.loopPatrol = true;
        fsm.visionRange = 8f;
        fsm.visionAngle = 60f;
        fsm.suspicionTime = 2.5f;
        fsm.suspicionDecayRate = 1f;
        fsm.chaseSpeed = 4f;
        fsm.chaseLostTime = 4f;
        fsm.searchDuration = 5f;
        fsm.investigateSpeed = 2.5f;

        SetupGuardAnimator(guardObj, fsm);
    }

    static void SetupGuardAnimator(GameObject guardObj, GuardFSM fsm)
    {
        var animator = guardObj.AddComponent<Animator>();
        fsm.animator = animator;
        fsm.guardRenderer = guardObj.GetComponent<Renderer>();
    }

    // ---------------------------------------------------------------
    //  Cameras
    // ---------------------------------------------------------------

    static void CreateCamera(Transform parent, string id, Vector3 position,
        float rotAngle, float rotSpeed, float detRange, float fov, bool active)
    {
        GameObject camObj = CreatePrimitive("Camera_" + id, PrimitiveType.Cylinder,
            position, Vector3.one * 0.25f, parent, Color.red);

        var cam = camObj.AddComponent<SecurityCamera>();
        cam.SetPrivateField("cameraID", id);
        cam.SetPrivateField("rotationAngle", rotAngle);
        cam.SetPrivateField("rotationSpeed", rotSpeed);
        cam.SetPrivateField("detectionRange", detRange);
        cam.SetPrivateField("fieldOfView", fov);
        cam.isActive = active;
    }

    // ---------------------------------------------------------------
    //  Terminals
    // ---------------------------------------------------------------

    static void CreateTerminal(Transform parent, string objName, string termID, string termName,
        string accessLabel, Color themeColor, CredentialType credType, UserRole minRole,
        Vector3 position, List<(string, string, string, string, string)> actions)
    {
        GameObject termObj = CreateEmpty(objName, position, parent);

        CreatePrimitive("TerminalBody", PrimitiveType.Cube,
            position + new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.1f, 0.5f), termObj.transform, Color.gray);
        CreatePrimitive("TerminalScreen", PrimitiveType.Cube,
            position + new Vector3(0, 0.55f, 0.2f), new Vector3(0.5f, 0.3f, 0.05f), termObj.transform, themeColor);

        var config = ScriptableObject.CreateInstance<TerminalConfig>();
        config.terminalName = termName;
        config.terminalID = termID;
        config.requiredCredentialType = credType;
        config.minimumRole = minRole;
        config.accessLevelLabel = accessLabel;
        config.themeColor = themeColor;

        if (actions != null)
        {
            foreach (var a in actions)
            {
                config.actions.Add(new TerminalActionEntry
                {
                    actionType = a.Item1,
                    displayName = a.Item2,
                    description = a.Item3,
                    requiredPermission = a.Item4,
                    targetID = a.Item5
                });
            }
        }

        var termCtrl = termObj.AddComponent<TerminalController>();
        termCtrl.SetPrivateField("terminalID", termID);
        termCtrl.SetPrivateField("config", config);

        termObj.AddComponent<TerminalLog>();

        // Connection point
        GameObject connPoint = CreateEmpty("ConnectionPoint", position + new Vector3(0, 0.3f, -0.3f), termObj.transform);
        var cp = connPoint.AddComponent<TerminalConnectionPoint>();
        BoxCollider cpCol = connPoint.AddComponent<BoxCollider>();
        cpCol.isTrigger = true;
        cpCol.size = new Vector3(1.2f, 1f, 0.8f);

        string cfgPath = "Assets/Materials/Generated/" + termID + "Config.asset";
        AssetDatabase.CreateAsset(config, cfgPath);
    }

    // ---------------------------------------------------------------
    //  Player
    // ---------------------------------------------------------------

    static void CreatePlayer(Transform parent, Vector3 position)
    {
        GameObject ccObj = new GameObject("PlayerController");
        ccObj.transform.position = position;
        ccObj.transform.parent = parent;
        ccObj.tag = "Player";

        CharacterController cc = ccObj.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;

        ccObj.AddComponent<PlayerController>();

        GameObject camGO = new GameObject("PlayerCamera");
        camGO.transform.parent = ccObj.transform;
        camGO.transform.localPosition = new Vector3(0, 0.7f, 0);
        camGO.AddComponent<Camera>().nearClipPlane = 0.1f;
        camGO.AddComponent<AudioListener>();

        camGO.AddComponent<PlayerInteraction>();
        camGO.AddComponent<LaptopController>();
        camGO.AddComponent<SecurityConsoleUI>();

        var missionHUD = camGO.AddComponent<MissionHUD>();
        var debugOverlay = camGO.AddComponent<DebugOverlay>();
    }

    // ---------------------------------------------------------------
    //  Mission
    // ---------------------------------------------------------------

    static void SetupMissionManager(Transform parent)
    {
        GameObject mmObj = new GameObject("MissionManager");
        mmObj.transform.SetParent(parent);
        var mission = mmObj.AddComponent<MissionManager>();

        mission.SetObjectiveOrder(new List<ObjectiveID>
        {
            ObjectiveID.EnterMuseumHeist,
            ObjectiveID.FindStaffCredential,
            ObjectiveID.AccessStaffOffice,
            ObjectiveID.UseStaffTerminal,
            ObjectiveID.ReachSecurityOffice,
            ObjectiveID.UseSecurityTerminal,
            ObjectiveID.DisableEastCameras,
            ObjectiveID.FindSecurityCredential,
            ObjectiveID.UnlockVaultArea,
            ObjectiveID.ReachVaultAntechamber,
            ObjectiveID.UseVaultTerminal,
            ObjectiveID.UnlockVault,
            ObjectiveID.StealMainArtifact,
            ObjectiveID.EscapeMuseum,
            ObjectiveID.HeistComplete
        });

        mission.autoStart = true;
    }

    // ---------------------------------------------------------------
    //  Cyber services
    // ---------------------------------------------------------------

    static void CreateCyberService(Transform parent, string name, System.Type type)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.AddComponent(type);
    }

    // ---------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------

    static GameObject CreateEmpty(string name, Vector3 position)
    {
        return new GameObject(name) { transform = { position = position } };
    }

    static GameObject CreateEmpty(string name, Vector3 position, Transform parent)
    {
        var go = CreateEmpty(name, position);
        go.transform.SetParent(parent);
        return go;
    }

    static Material CreateMat(string name, Color color)
    {
        if (_materials.TryGetValue(name, out var existing)) return existing;

        string path = "Assets/Materials/Generated/" + name + ".mat";
        Material loaded = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (loaded != null) { _materials[name] = loaded; return loaded; }

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.name = name;
        if (color.a < 1f)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
        }

        Directory.CreateDirectory(Application.dataPath + "/Materials/Generated");
        AssetDatabase.CreateAsset(mat, path);
        _materials[name] = mat;
        return mat;
    }

    static GameObject CreatePrimitive(string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Transform parent, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.transform.SetParent(parent);
        go.GetComponent<Renderer>().material = CreateMat(name + "_mat", color);
        return go;
    }

    static GameObject CreateLight(string name, LightType type, Vector3 position, Color color, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        go.transform.SetParent(parent);
        Light l = go.AddComponent<Light>();
        l.type = type;
        l.color = color;
        l.intensity = 0.7f;
        return go;
    }
}

// ---------------------------------------------------------------
//  Extension — sets private/serialized fields via reflection
// ---------------------------------------------------------------

static class EditorReflectionExtensions
{
    public static void SetPrivateField<T>(this UnityEngine.Object obj, string fieldName, T value)
    {
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);
        if (field != null)
            field.SetValue(obj, value);
        else
            Debug.LogWarning($"Field '{fieldName}' not found on {obj.GetType().Name}");
    }
}


