using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class TutorialLevelGenerator
{
    const float ROOM_WIDTH = 5f;
    const float ROOM_DEPTH = 5f;
    const float ROOM_HEIGHT = 3f;
    const float CORRIDOR_WIDTH = 3f;
    const float CORRIDOR_DEPTH = 3f;
    const float WALL_T = 0.2f;
    const float FLOOR_T = 0.1f;

    // Room Z centers
    static readonly float[] ROOM_Z = { -20, -12, -4, 4, 12, 20 };
    static readonly float[] CORRIDOR_Z = { -16, -8, 0, 8, 16 };

    static Dictionary<string, Material> _materials;

    [MenuItem("Tools/Generate Tutorial Level")]
    static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        _materials = new Dictionary<string, Material>();

        // ---- Hierarchy roots ----
        GameObject envRoot  = CreateEmpty("_Environment", Vector3.zero);
        GameObject roomsRoot = CreateEmpty("Rooms", Vector3.zero, envRoot.transform);
        GameObject propsRoot = CreateEmpty("_Props", Vector3.zero);
        GameObject guardsRoot = CreateEmpty("_Guards", Vector3.zero);
        GameObject playerRoot = CreateEmpty("_Player", Vector3.zero);
        GameObject lightsRoot = CreateEmpty("_Lights", Vector3.zero);
        GameObject securityRoot = CreateEmpty("_Security", Vector3.zero);

        // SecurityManager singleton
        GameObject smObj = new GameObject("SecurityManager");
        smObj.transform.SetParent(securityRoot.transform);
        smObj.AddComponent<SecurityManager>();

        // ---- Mission system ----
        GameObject missionRoot = CreateEmpty("_Mission", Vector3.zero);
        GameObject mmObj = new GameObject("MissionManager");
        mmObj.transform.SetParent(missionRoot.transform);
        var missionMgr = mmObj.AddComponent<MissionManager>();

        CreateMissionZone(missionRoot.transform, "Zone_EnterMuseum",   ObjectiveID.EnterMuseum,   new Vector3(0, 1.5f, -20), new Vector3(5, 3, 5));
        CreateMissionZone(missionRoot.transform, "Zone_ObserveGuard",  ObjectiveID.ObserveGuard,  new Vector3(0, 1.5f, -12), new Vector3(5, 3, 5));
        CreateMissionZone(missionRoot.transform, "Zone_EnterVault",    ObjectiveID.EnterVault,    new Vector3(0, 1.5f, 12),  new Vector3(5, 3, 5));
        CreateMissionZone(missionRoot.transform, "Zone_Escape",        ObjectiveID.Escape,        new Vector3(0, 1.5f, 20),  new Vector3(5, 3, 5));

        // ---- Rooms & corridors ----
        CreateRoom(roomsRoot.transform, "Entrance",    ROOM_Z[0], true,  false);
        CreateCorridor(roomsRoot.transform, "Corridor_1", CORRIDOR_Z[0]);
        CreateRoom(roomsRoot.transform, "Reception",   ROOM_Z[1], false, false);
        CreateCorridor(roomsRoot.transform, "Corridor_2", CORRIDOR_Z[1]);
        CreateRoom(roomsRoot.transform, "ExhibitRoom", ROOM_Z[2], false, false);
        CreateCorridor(roomsRoot.transform, "Corridor_3", CORRIDOR_Z[2]);
        CreateRoom(roomsRoot.transform, "SecurityRoom",ROOM_Z[3], false, false);
        CreateCorridor(roomsRoot.transform, "Corridor_4", CORRIDOR_Z[3]);
        CreateRoom(roomsRoot.transform, "Vault",       ROOM_Z[4], false, false);
        CreateCorridor(roomsRoot.transform, "Corridor_5", CORRIDOR_Z[4]);
        CreateRoom(roomsRoot.transform, "Exit",        ROOM_Z[5], false, true);

        // ---- Guard waypoints ----
        GameObject wpRoot = CreateEmpty("GuardWaypoints", Vector3.zero, propsRoot.transform);
        Transform[] waypoints = new Transform[4];
        waypoints[0] = CreateEmpty("WP1", new Vector3(-1.5f, 0, -13.5f), wpRoot.transform).transform;
        waypoints[1] = CreateEmpty("WP2", new Vector3( 1.5f, 0, -13.5f), wpRoot.transform).transform;
        waypoints[2] = CreateEmpty("WP3", new Vector3( 1.5f, 0, -10.5f), wpRoot.transform).transform;
        waypoints[3] = CreateEmpty("WP4", new Vector3(-1.5f, 0, -10.5f), wpRoot.transform).transform;

        // ---- Guard (FSM) ----
        GameObject guardObj = CreatePrimitive("Guard", PrimitiveType.Capsule,
            new Vector3(-1.5f, 0.5f, -13.5f), new Vector3(0.5f, 1f, 0.5f), guardsRoot.transform, Color.blue);
        GuardFSM guardFSM = guardObj.AddComponent<GuardFSM>();
        guardFSM.waypoints = waypoints;
        guardFSM.patrolSpeed = 2f;
        guardFSM.patrolRotateSpeed = 5f;
        guardFSM.loopPatrol = true;
        guardFSM.visionRange = 10f;
        guardFSM.visionAngle = 60f;
        guardFSM.suspicionTime = 2f;
        guardFSM.suspicionDecayRate = 1f;
        guardFSM.chaseSpeed = 5f;
        guardFSM.chaseLostTime = 3f;
        guardFSM.searchDuration = 5f;
        guardFSM.investigateSpeed = 3f;

        SetupGuardAnimator(guardObj, guardFSM);

        // ---- Keycard pickup (Exhibit Room) ----
        CreatePrimitive("KeycardPedestal", PrimitiveType.Cube,
            new Vector3(-1.5f, 0.25f, -4), new Vector3(0.5f, 0.1f, 0.5f), propsRoot.transform, Color.gray);
        GameObject keycardObj = CreatePrimitive("Keycard", PrimitiveType.Cube,
            new Vector3(-1.5f, 0.5f, -4), new Vector3(0.3f, 0.05f, 0.2f), propsRoot.transform, Color.yellow);
        KeycardPickup keycardScript = keycardObj.AddComponent<KeycardPickup>();
        keycardScript.targetDoorID = "door_security";

        // ---- Locked door (blocks Corridor_3 at z = -1.5) ----
        GameObject doorObj = CreatePrimitive("LockedDoor", PrimitiveType.Cube,
            new Vector3(0, 1.5f, -1.5f), new Vector3(2.8f, 2.5f, 0.1f), propsRoot.transform, Color.red);
        LockedDoor doorScript = doorObj.AddComponent<LockedDoor>();
        doorScript.doorID = "door_security";
        doorScript.isLocked = true;
        doorScript.openAngle = 90f;

        // ---- Security camera (Security Room) ----
        GameObject camObj = CreatePrimitive("SecurityCamera", PrimitiveType.Cylinder,
            new Vector3(0, 2.5f, 4), Vector3.one * 0.3f, propsRoot.transform, Color.red);
        SecurityCamera camScript = camObj.AddComponent<SecurityCamera>();
        camScript.cameraID = "camera_security";
        camScript.rotationAngle = 45f;
        camScript.rotationSpeed = 30f;
        camScript.detectionRange = 8f;
        camScript.fieldOfView = 60f;
        camScript.isActive = true;
        // FOV beam visual
        CreatePrimitive("FOV_Beam", PrimitiveType.Cube,
            new Vector3(0, 2.5f, 5f), new Vector3(0.05f, 0.05f, 2f), camObj.transform, new Color(1, 0, 0, 0.25f));

        // ---- Computer terminal (Security Room) ----
        CreatePrimitive("TerminalDesk", PrimitiveType.Cube,
            new Vector3(-1.5f, 0.75f, 4), new Vector3(0.8f, 0.1f, 0.6f), propsRoot.transform, Color.gray);
        CreatePrimitive("TerminalBase", PrimitiveType.Cube,
            new Vector3(-1.5f, 0.95f, 4), new Vector3(0.3f, 0.05f, 0.3f), propsRoot.transform, Color.gray);

        GameObject screenObj = CreatePrimitive("ComputerTerminal", PrimitiveType.Cube,
            new Vector3(-1.5f, 1.2f, 4.3f), new Vector3(0.5f, 0.35f, 0.05f), propsRoot.transform, Color.gray);
        ComputerTerminal terminalScript = screenObj.AddComponent<ComputerTerminal>();
        terminalScript.targetCameraID = "camera_security";
        terminalScript.screenRenderer = screenObj.GetComponent<MeshRenderer>();
        terminalScript.screenOnMaterial = CreateMat("ScreenOn", Color.cyan);
        terminalScript.screenOffMaterial = CreateMat("ScreenOff", new Color(0.15f, 0.15f, 0.15f));

        // ---- Artifact (Vault) ----
        CreatePrimitive("ArtifactPedestal", PrimitiveType.Cube,
            new Vector3(0, 0.35f, 12), new Vector3(0.8f, 0.2f, 0.8f), propsRoot.transform, Color.gray);
        GameObject artifactObj = CreatePrimitive("Artifact", PrimitiveType.Sphere,
            new Vector3(0, 0.75f, 12), Vector3.one * 0.4f, propsRoot.transform, Color.yellow);
        artifactObj.AddComponent<ArtifactCollectible>();

        // ---- Player ----
        GameObject ccObj = new GameObject("PlayerController");
        ccObj.transform.position = new Vector3(0, 1f, -19.5f);
        ccObj.transform.parent = playerRoot.transform;
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
        PlayerInteraction interaction = camGO.AddComponent<PlayerInteraction>();
        interaction.interactionRange = 3f;
        interaction.interactKey = KeyCode.E;

        // ---- Lights ----
        GameObject dirLight = CreateLight("Directional Light", LightType.Directional,
            new Vector3(0, 10, 0), Color.white, lightsRoot.transform);
        dirLight.GetComponent<Light>().intensity = 1f;
        dirLight.transform.eulerAngles = new Vector3(50, -30, 0);

        string[] roomNames = { "Entrance", "Reception", "Exhibit", "Security", "Vault", "Exit" };
        for (int i = 0; i < 6; i++)
            CreateLight("PointLight_" + roomNames[i], LightType.Point,
                new Vector3(0, 2.5f, ROOM_Z[i]), new Color(0.9f, 0.9f, 1f), lightsRoot.transform).GetComponent<Light>().range = 8f;

        // ---- NavMesh ----
        SetupNavMesh();

        // ---- Save ----
        Directory.CreateDirectory(Application.dataPath + "/Scenes");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/TutorialLevel.unity");
        Debug.Log("Tutorial Level generated successfully!  Tools -> Generate Tutorial Level");
    }

    // ---------------------------------------------------------------
    //  Helpers
    // ---------------------------------------------------------------

    static GameObject CreateEmpty(string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        return go;
    }

    static GameObject CreateEmpty(string name, Vector3 position, Transform parent)
    {
        GameObject go = CreateEmpty(name, position);
        go.transform.SetParent(parent);
        return go;
    }

    static Material CreateMat(string name, Color color)
    {
        string key = name;
        if (_materials.ContainsKey(key)) return _materials[key];

        string path = "Assets/Materials/Generated/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            _materials[key] = existing;
            return existing;
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.name = name;

        if (color.a < 1f)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        Directory.CreateDirectory(Application.dataPath + "/Materials/Generated");
        AssetDatabase.CreateAsset(mat, path);
        _materials[key] = mat;
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

    static void CreateRoom(Transform parent, string name, float zCenter,
        bool hasBackWall, bool hasFrontWall, float w = ROOM_WIDTH, float d = ROOM_DEPTH)
    {
        GameObject room = CreateEmpty(name, Vector3.zero, parent);
        float hw = w / 2f, hd = d / 2f, hh = ROOM_HEIGHT / 2f;

        string roomColor = name.ToLower().Contains("exit") ? "white" : "gray";
        Color c = roomColor == "white" ? Color.white : Color.gray;

        CreatePrimitive("Floor",   PrimitiveType.Cube, new Vector3(0, FLOOR_T / 2f, zCenter),   new Vector3(w, FLOOR_T, d),     room.transform, c);
        CreatePrimitive("Ceiling", PrimitiveType.Cube, new Vector3(0, ROOM_HEIGHT - FLOOR_T / 2f, zCenter), new Vector3(w, FLOOR_T, d), room.transform, c);
        CreatePrimitive("LeftWall",  PrimitiveType.Cube, new Vector3(-hw, hh, zCenter), new Vector3(WALL_T, ROOM_HEIGHT, d), room.transform, c);
        CreatePrimitive("RightWall", PrimitiveType.Cube, new Vector3( hw, hh, zCenter), new Vector3(WALL_T, ROOM_HEIGHT, d), room.transform, c);

        if (hasBackWall)
            CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0, hh, zCenter - hd), new Vector3(w, ROOM_HEIGHT, WALL_T), room.transform, c);
        if (hasFrontWall)
            CreatePrimitive("FrontWall", PrimitiveType.Cube, new Vector3(0, hh, zCenter + hd), new Vector3(w, ROOM_HEIGHT, WALL_T), room.transform, c);
    }

    static void CreateCorridor(Transform parent, string name, float zCenter)
    {
        CreateRoom(parent, name, zCenter, false, false, CORRIDOR_WIDTH, CORRIDOR_DEPTH);
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

    static void CreateMissionZone(Transform parent, string name, ObjectiveID id, Vector3 position, Vector3 size)
    {
        GameObject zone = new GameObject(name);
        zone.transform.position = position;
        zone.transform.SetParent(parent);
        BoxCollider bc = zone.AddComponent<BoxCollider>();
        bc.size = size;
        bc.isTrigger = true;
        MissionZone mz = zone.AddComponent<MissionZone>();
        mz.objectiveID = id;
    }

    // ---------------------------------------------------------------
    //  Guard Animator
    // ---------------------------------------------------------------

    static void SetupGuardAnimator(GameObject guardObj, GuardFSM guardFSM)
    {
        string dir = "Assets/Materials/Generated";
        Directory.CreateDirectory(Application.dataPath + "/Materials/Generated");

        // --- create placeholder clips ---
        var clips = new (string name, float length)[]
        {
            ("Guard_Idle",   1f),
            ("Guard_Walk",   0.8f),
            ("Guard_Run",    0.4f),
            ("Guard_Search", 2f),
            ("Guard_Alert",  1f),
        };

        var animClips = new List<AnimationClip>();
        foreach (var (name, length) in clips)
        {
            var c = new AnimationClip { name = name };
            c.SetCurve("", typeof(Transform), "localPosition.x",
                AnimationCurve.Constant(0f, length, 0f));
            c.wrapMode = WrapMode.Loop;
            var s = AnimationUtility.GetAnimationClipSettings(c);
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(c, s);
            animClips.Add(c);
        }

        // --- create controller ---
        var controller = new AnimatorController();
        controller.name = "Guard_AnimatorController";
        controller.AddParameter("State", AnimatorControllerParameterType.Int);

        if (controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        var sm = controller.layers[0].stateMachine;

        // state values must match GuardFSM.AnimState enum
        var states = new (string name, int value)[]
        {
            ("Idle",   0),
            ("Walk",   1),
            ("Run",    2),
            ("Search", 3),
            ("Alert",  4),
        };

        for (int i = 0; i < states.Length; i++)
        {
            var asm = sm.AddState(states[i].name);
            asm.motion = animClips[i];
            if (i == 0) sm.defaultState = asm;

            var t = sm.AddAnyStateTransition(asm);
            t.AddCondition(AnimatorConditionMode.Equals, states[i].value, "State");
            t.duration = 0.12f;
            t.hasExitTime = false;
        }

        // --- save assets ---
        string ctrlPath = dir + "/Guard_AnimatorController.controller";
        AssetDatabase.CreateAsset(controller, ctrlPath);
        foreach (var c in animClips)
            AssetDatabase.AddObjectToAsset(c, ctrlPath);
        AssetDatabase.SaveAssets();

        // --- Animator component ---
        var animator = guardObj.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.Normal;

        guardFSM.animator = animator;
        guardFSM.guardRenderer = guardObj.GetComponent<Renderer>();
    }

    // ---------------------------------------------------------------
    //  NavMesh  (graceful fallback if Unity.AI.Navigation missing)
    // ---------------------------------------------------------------

    static void SetupNavMesh()
    {
        System.Type surfaceType = System.Type.GetType(
            "Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
        if (surfaceType == null)
        {
            Debug.Log("NavMesh: Unity.AI.Navigation package not found. " +
                      "Install via Window -> Package Manager, then re-run generator.");
            return;
        }

        GameObject navRoot = new GameObject("_NavMesh");
        var surface = navRoot.AddComponent(surfaceType);

        var collectProp = surfaceType.GetProperty("collectObjects");
        var enumType = surfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects");
        collectProp.SetValue(surface, System.Enum.ToObject(enumType, 0)); // CollectObjects.All

        var bakeMethod = surfaceType.GetMethod("BakeNavMesh");
        bakeMethod.Invoke(surface, null);

        Debug.Log("NavMesh baked successfully.");
    }
}
