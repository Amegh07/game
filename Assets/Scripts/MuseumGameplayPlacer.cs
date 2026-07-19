using UnityEngine;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;
using System.Collections.Generic;

public class MuseumGameplayPlacer : MonoBehaviour
{
    public MuseumLevelBuilder levelBuilder;
    public SecurityConsoleUI consoleUI;
    public EscapePhaseController escapeController;

    private Transform gameRoot;
    private Transform missionRoot;
    private bool hasBuilt;

    private Material cachedGuardMat;
    private Material cachedEliteGuardMat;
    private Material cachedTerminalMat;
    private Material cachedScreenMat;
    private Material cachedCamMat;
    private Material cachedDoorMat;
    private Material cachedKeycardMat;
    private Material cachedKeycardVaultMat;
    private Material cachedCredentialMat;
    private Material cachedArtifactMat;

    public void BuildAll()
    {
        if (hasBuilt) return;
        hasBuilt = true;

        if (levelBuilder == null) levelBuilder = FindObjectOfType<MuseumLevelBuilder>();
        if (levelBuilder == null) { Debug.LogError("[Placer] No level builder."); return; }

        gameRoot = levelBuilder.GameplayRoot;
        missionRoot = levelBuilder.MissionRoot;
        if (consoleUI == null) consoleUI = FindObjectOfType<SecurityConsoleUI>();
        if (escapeController == null) escapeController = FindObjectOfType<EscapePhaseController>();

        InitMaterialCache();

        PlaceEntrance();
        PlaceLobby();
        PlaceExhibition();
        PlaceSecurityOffice();
        PlaceVaultCorridor();
        PlaceMainVault();
        PlaceEscape();
        PlaceTriggers();
        Debug.Log("[Placer] All gameplay elements placed.");
    }

    private void InitMaterialCache()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        cachedGuardMat = new Material(shader) { color = Color.blue };
        cachedEliteGuardMat = new Material(shader) { color = new Color(0.6f, 0f, 0f) };
        cachedTerminalMat = new Material(shader) { color = new Color(0.2f, 0.2f, 0.3f) };
        cachedScreenMat = new Material(shader) { color = new Color(0.1f, 0.3f, 0.6f) };
        cachedCamMat = new Material(shader) { color = Color.gray };
        cachedDoorMat = new Material(shader) { color = new Color(0.5f, 0.3f, 0.1f) };
        cachedKeycardMat = new Material(shader) { color = Color.yellow };
        cachedKeycardVaultMat = new Material(shader) { color = Color.red };
        cachedCredentialMat = new Material(shader) { color = new Color(0.2f, 0.4f, 0.8f) };
        cachedArtifactMat = new Material(shader) { color = new Color(1f, 0.8f, 0f) };
    }

    void PlaceEntrance()
    {
        ActionExecutor.RegisterAction("DisableCameraGroup", new DisableCameraGroupAction());

        // SpawnPoint at entrance south side
        var sp = new GameObject("SpawnPoint");
        sp.transform.SetParent(gameRoot);
        sp.transform.position = new Vector3(0f, 1f, 11f);

        // Entrance trigger
        MakeTrigger(0f, 0.5f, 8f, 6f, 3f, 6f, ObjectiveID.EnterMuseumHeist);
    }

    void PlaceLobby()
    {
        // Lobby arrival trigger
        MakeTrigger(0f, 0.5f, -2f, 6f, 3f, 6f, ObjectiveID.ReachLobby);
        // Checkpoint 1 — Lobby
        MakeCheckpoint(new Vector3(0f, 0.5f, 1f), ObjectiveID.ReachLobby);

        // Staff keycard (yellow)
        MakeKeycard(KeycardType.Staff, -3f, 0.5f, -2f);

        // Guard 1 — Lobby patrol
        MakeGuard("Guard_Lobby", new Vector3(3f, 0f, -3f), new Vector3[] {
            new(3f, 0f, -3f), new(-3f, 0f, -3f), new(-3f, 0f, 1f), new(3f, 0f, 1f)
        });

        // Camera 1
        MakeCamera("camera_lobby", new Vector3(6f, 3.5f, 1f), -90f, 40f, 30f, 8f);
    }

    void PlaceExhibition()
    {
        // Guard 2 — Exhibition patrol (starts at west side)
        MakeGuard("Guard_Exhibition_West", new Vector3(-5f, 0f, -16f), new Vector3[] {
            new(-5f, 0f, -16f), new(5f, 0f, -16f), new(5f, 0f, -20f), new(-5f, 0f, -20f)
        });

        // Guard 3 — Exhibition patrol (starts at east side, counter-phase)
        MakeGuard("Guard_Exhibition_East", new Vector3(5f, 0f, -20f), new Vector3[] {
            new(5f, 0f, -20f), new(-5f, 0f, -20f), new(-5f, 0f, -16f), new(5f, 0f, -16f)
        }, 2);

        // Cameras
        MakeCamera("camera_east", new Vector3(8f, 3.5f, -16f), -90f, 50f, 35f, 9f);
        MakeCamera("camera_west", new Vector3(-8f, 3.5f, -20f), 90f, 50f, 35f, 9f);

        // Door to security office (west side of exhibition) — requires Staff keycard
        MakeDoor("door_staff_office", -11f, 1.5f, -15f, KeycardType.Staff, true, false);

        // Door to vault corridor is open (no lock — player progresses naturally)
    }

    void PlaceSecurityOffice()
    {
        // Local security office camera (not on network, cannot be disabled)
        MakeCamera("camera_office_local", new Vector3(-9f, 3.5f, -12f), 180f, 45f, 30f, 8f, false);

        // Computer terminal for camera disable
        MakeSimpleTerminal("camera_east", -9f, 1.2f, -13f);

        // Cyber terminal
        MakeCyberTerminal("terminal_security", -9f, 1f, -15f, new List<TerminalActionEntry>
        {
            new() { actionType = "DisableCameraGroup", displayName = "Disable Camera Network",
                description = "Disable all networked security cameras", requiredPermission = Permissions.DisableCameras,
                targetIDs = new List<string> { "camera_lobby", "camera_east", "camera_west", "camera_vault", "camera_vault_2" } },
            new() { actionType = "UnlockDoor", displayName = "Unlock Vault Door",
                description = "Unlock vault corridor door", requiredPermission = Permissions.OpenSecurityDoors, targetID = "door_vault_corridor" },
            new() { actionType = "ResetAlarm", displayName = "Reset Alarm",
                description = "Begin alarm recovery", requiredPermission = Permissions.ResetAlarm },
        }, "Security Terminal", UserRole.SecurityOfficer);

        // Staff terminal trigger (right at doorway, player passes through before reaching office center)
        MakeTrigger(-11f, 0.5f, -14.5f, 1.5f, 3f, 1.5f, ObjectiveID.UseStaffTerminal);

        // Security office trigger (deeper in room, reached after doorway)
        MakeTrigger(-9f, 0.5f, -14f, 4f, 3f, 4f, ObjectiveID.ReachSecurityOffice);
        // Checkpoint 2 — Security Office
        MakeCheckpoint(new Vector3(-9f, 0.5f, -12f), ObjectiveID.ReachSecurityOffice);
    }

    void PlaceVaultCorridor()
    {
        // Vault corridor door (requires Security keycard)
        MakeDoor("door_vault_corridor", 0f, 1.5f, -31f, KeycardType.Security, true, true);

        // Cameras
        MakeCamera("camera_vault", new Vector3(5f, 3.5f, -34f), -90f, 45f, 30f, 10f);
        MakeCamera("camera_vault_2", new Vector3(-5f, 3.5f, -35f), 90f, 45f, -30f, 10f);

        // Guard 3 — Vault corridor patrol (regular)
        MakeGuard("Guard_Vault", new Vector3(2f, 0f, -34f), new Vector3[] {
            new(2f, 0f, -34f), new(-2f, 0f, -34f), new(-2f, 0f, -36f), new(2f, 0f, -36f)
        });

        // Elite Guard — Vault antechamber, tougher variant
        MakeEliteGuard("Guard_Elite", new Vector3(0f, 0f, -37f), new Vector3[] {
            new(0f, 0f, -38f), new(1f, 0f, -38f), new(1f, 0f, -36f), new(-1f, 0f, -36f), new(-1f, 0f, -37f)
        });

        // Vault keycard
        MakeKeycard(KeycardType.Vault, -2f, 0.5f, -35f);

        // Security credential pickup (in vault corridor, reached after DisableEastCameras)
        var credGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        credGo.name = "SecurityCredential";
        credGo.transform.SetParent(gameRoot);
        credGo.transform.position = new Vector3(-3f, 0.5f, -35f);
        credGo.transform.localScale = new Vector3(0.3f, 0.05f, 0.2f);
        var cr = credGo.GetComponent<Renderer>();
        if (cr != null) cr.material = cachedCredentialMat;
        Destroy(credGo.GetComponent<BoxCollider>());
        var cbc = credGo.AddComponent<BoxCollider>();
        cbc.size = Vector3.one * 0.4f;
        cbc.isTrigger = true;
        var ci = credGo.AddComponent<CredentialItem>();
        ci.SetCredential("sec_cred", CredentialType.Keycard, UserRole.SecurityOfficer, "Security Credential");

        // Restricted corridor trigger (entering vault corridor from north)
        MakeTrigger(0f, 0.5f, -32.5f, 4f, 3f, 2f, ObjectiveID.ReachRestrictedCorridor);
        // Checkpoint 3 — Vault Corridor entrance
        MakeCheckpoint(new Vector3(0f, 0.5f, -31f), ObjectiveID.ReachRestrictedCorridor);

        // Unlock vault area trigger (near south end, reached after restricted corridor)
        MakeTrigger(0f, 0.5f, -36f, 4f, 3f, 2f, ObjectiveID.UnlockVaultArea);
    }

    void PlaceMainVault()
    {
        // Vault door (requires Vault keycard) — between vault corridor (z=-37) and main vault (z=-39)
        MakeDoor("door_main_vault", 0f, 1.5f, -38f, KeycardType.Vault, true, true);

        // Artifact
        var artGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        artGo.name = "MainArtifact";
        artGo.transform.SetParent(gameRoot);
        artGo.transform.position = new Vector3(0f, 0.8f, -44f);
        artGo.transform.localScale = new Vector3(0.4f, 0.5f, 0.4f);
        var ar = artGo.GetComponent<Renderer>();
        if (ar != null) ar.material = cachedArtifactMat;
        Destroy(artGo.GetComponent<Collider>());
        var abc = artGo.AddComponent<BoxCollider>();
        abc.isTrigger = true;
        artGo.AddComponent<ArtifactController>();

        // Simple terminal for vault unlock
        MakeSimpleTerminal("door_main_vault", 0f, 1.2f, -43f);

        // Vault antechamber trigger (between corridor and vault door, reached before unlocking)
        MakeTrigger(0f, 0.5f, -37.5f, 3f, 3f, 2f, ObjectiveID.ReachVaultAntechamber);

        // Vault terminal usage trigger (at terminal position, inside vault)
        MakeTrigger(0f, 0.5f, -44f, 2f, 3f, 2f, ObjectiveID.UseVaultTerminal);
    }

    void PlaceEscape()
    {
        // Exit zone for EscapePhaseController
        var exitGo = new GameObject("ExitZone");
        exitGo.transform.SetParent(missionRoot);
        exitGo.transform.position = new Vector3(0f, 0.5f, -54f);

        // Escape trigger
        MakeTrigger(0f, 0.5f, -54f, 4f, 3f, 4f, ObjectiveID.EscapeMuseum);
        // Heist complete zone (outside)
        MakeTrigger(0f, 0.5f, -58f, 4f, 3f, 4f, ObjectiveID.HeistComplete);
        // Checkpoint 4 — Escape zone
        MakeCheckpoint(new Vector3(0f, 0.5f, -52f), ObjectiveID.EscapeMuseum);

        if (escapeController != null)
        {
            escapeController.transform.position = new Vector3(0f, 0.5f, -52f);
            var exitField = typeof(EscapePhaseController).GetField("exitTriggerZone",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (exitField != null) exitField.SetValue(escapeController, exitGo.transform);
        }
    }

    void PlaceTriggers()
    {
        // All trigger zones are created within their respective zone methods
    }

    // --- Helpers ---

    void MakeTrigger(float x, float y, float z, float w, float h, float d, ObjectiveID id)
    {
        var go = new GameObject($"Trigger_{id}");
        go.transform.SetParent(missionRoot);
        go.transform.position = new Vector3(x, y, z);
        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(w, h, d);
        bc.isTrigger = true;
        var mz = go.AddComponent<MissionZone>();
        mz.objectiveID = id;
    }

    void MakeGuard(string name, Vector3 start, Vector3[] waypoints, int phaseOffset = 0)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name; go.transform.SetParent(gameRoot);
        go.transform.position = start;
        go.transform.localScale = new Vector3(0.6f, 1.8f, 0.6f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = cachedGuardMat;
        Destroy(go.GetComponent<CapsuleCollider>());

        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.3f;

        var gfsm = go.AddComponent<GuardFSM>();
        gfsm.waypoints = new Transform[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = new GameObject($"{name}_WP{i}");
            wp.transform.SetParent(go.transform);
            wp.transform.position = waypoints[i];
            gfsm.waypoints[i] = wp.transform;
        }
        gfsm.patrolSpeed = 1.5f;
        gfsm.visionRange = 8f;
        gfsm.visionAngle = 50f;

        if (phaseOffset > 0)
        {
            var phaseField = typeof(GuardFSM).GetField("patrolPhaseOffset",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (phaseField != null) phaseField.SetValue(gfsm, phaseOffset);
        }

        var child = go.transform.Find("Cylinder");
        if (child != null) Destroy(child.gameObject);
    }

    void MakeCamera(string id, Vector3 pos, float yRot, float angle, float speed, float range)
    {
        MakeCamera(id, pos, yRot, angle, speed, range, true);
    }

    void MakeCamera(string id, Vector3 pos, float yRot, float angle, float speed, float range, bool registerWithSecurity)
    {
        var go = new GameObject(id);
        go.transform.SetParent(gameRoot);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body"; body.transform.SetParent(go.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.2f, 0.15f, 0.3f);
        var r = body.GetComponent<Renderer>();
        if (r != null) r.material = registerWithSecurity ? cachedCamMat : new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.8f, 0.2f, 0.2f) };
        Destroy(body.GetComponent<BoxCollider>());

        var sc = go.AddComponent<SecurityCamera>();
        sc.cameraID = id;
        sc.rotationAngle = angle;
        sc.rotationSpeed = speed;
        sc.detectionRange = range;
        sc.fieldOfView = 60f;
        sc.detectionTime = 2f;
        sc.registerWithSecurity = registerWithSecurity;
    }

    void MakeDoor(string id, float x, float y, float z, KeycardType required, bool startsLocked, bool lockDuringLockdown)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = id; go.transform.SetParent(gameRoot);
        go.transform.position = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(0.1f, 3f, 1.5f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = cachedDoorMat;
        Destroy(go.GetComponent<BoxCollider>());

        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(1.5f, 3f, 0.2f);

        var dc = go.AddComponent<DoorController>();
        var doorIdField = typeof(DoorController).GetField("doorID",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (doorIdField != null) doorIdField.SetValue(dc, id);
        var cfg = ScriptableObject.CreateInstance<DoorConfig>();
        cfg.doorName = id.Replace("_", " ").Replace("door ", "Door ");
        cfg.requiredKeycard = required;
        cfg.startsLocked = startsLocked;
        cfg.openSpeed = 120f; cfg.closeSpeed = 120f; cfg.openAngle = 90f;
        cfg.canAutoClose = true; cfg.autoCloseDelay = 3f;
        cfg.lockOnSuspicious = false; cfg.lockOnAlert = false;
        cfg.lockDuringLockdown = lockDuringLockdown;
        cfg.isEmergencyExit = false;
        dc.AssignConfig(cfg);
    }

    void MakeKeycard(KeycardType type, float x, float y, float z)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Keycard_{type}"; go.transform.SetParent(gameRoot);
        go.transform.position = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(0.3f, 0.05f, 0.2f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = type == KeycardType.Vault ? cachedKeycardVaultMat : cachedKeycardMat;
        Destroy(go.GetComponent<BoxCollider>());
        var bc = go.AddComponent<BoxCollider>();
        bc.size = Vector3.one * 0.4f; bc.isTrigger = true;
        var ki = go.AddComponent<KeycardItem>();
        ki.SetKeycardType(type);
    }

    void MakeSimpleTerminal(string targetId, float x, float y, float z)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Terminal_{targetId}"; go.transform.SetParent(gameRoot);
        go.transform.position = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(0.6f, 0.4f, 0.4f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = cachedTerminalMat;
        Destroy(go.GetComponent<BoxCollider>());
        var bc = go.AddComponent<BoxCollider>();
        bc.size = Vector3.one * 0.6f; bc.isTrigger = true;
        var ct = go.AddComponent<ComputerTerminal>();
        ct.targetCameraID = targetId;

        var screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "Screen"; screen.transform.SetParent(go.transform);
        screen.transform.localPosition = new Vector3(0f, 0.1f, -0.25f);
        screen.transform.localScale = new Vector3(0.5f, 0.3f, 1f);
        var sr = screen.GetComponent<MeshRenderer>();
        if (sr != null) sr.material = cachedScreenMat;
        ct.screenRenderer = sr;
    }

    void MakeEliteGuard(string name, Vector3 start, Vector3[] waypoints)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name; go.transform.SetParent(gameRoot);
        go.transform.position = start;
        go.transform.localScale = new Vector3(0.7f, 2f, 0.7f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = cachedEliteGuardMat;
        Destroy(go.GetComponent<CapsuleCollider>());

        var cc = go.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.35f;

        var gfsm = go.AddComponent<GuardFSM>();
        gfsm.isElite = true;
        gfsm.waypoints = new Transform[waypoints.Length];
        for (int i = 0; i < waypoints.Length; i++)
        {
            var wp = new GameObject($"{name}_WP{i}");
            wp.transform.SetParent(go.transform);
            wp.transform.position = waypoints[i];
            gfsm.waypoints[i] = wp.transform;
        }

        var child = go.transform.Find("Cylinder");
        if (child != null) Destroy(child.gameObject);
    }

    void MakeCheckpoint(Vector3 position, ObjectiveID objective)
    {
        var go = new GameObject($"Checkpoint_{objective}");
        go.transform.SetParent(missionRoot);
        go.transform.position = position;
        var bc = go.AddComponent<BoxCollider>();
        bc.size = new Vector3(4f, 3f, 4f);
        bc.isTrigger = true;
        var ct = go.AddComponent<CheckpointTrigger>();
        ct.checkpointObjective = objective;
        ct.respawnLocation = go.transform;
    }

    void MakeCyberTerminal(string id, float x, float y, float z, List<TerminalActionEntry> actions, string name, UserRole minRole)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = id; go.transform.SetParent(gameRoot);
        go.transform.position = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(0.8f, 0.5f, 0.5f);
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = cachedTerminalMat;
        Destroy(go.GetComponent<BoxCollider>());

        var cfg = ScriptableObject.CreateInstance<TerminalConfig>();
        cfg.terminalName = name; cfg.terminalID = id;
        cfg.minimumRole = minRole; cfg.skipAuthentication = true;
        cfg.actions = actions; cfg.accessLevelLabel = "CLASSIFIED";

        go.AddComponent<TerminalLog>();
        var tc = go.AddComponent<TerminalController>();

        var conn = new GameObject("ConnectionPoint");
        conn.transform.SetParent(go.transform);
        conn.transform.localPosition = new Vector3(0f, -0.3f, 0.5f);
        conn.AddComponent<TerminalConnectionPoint>();
    }
}
