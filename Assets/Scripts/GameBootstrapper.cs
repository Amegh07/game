using UnityEngine;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;
using System.Collections.Generic;
using System.Reflection;

[DefaultExecutionOrder(-100)]
public class GameBootstrapper : MonoBehaviour
{
    public bool buildOnStart = true;
    public KeyCode toggleCursorKey = KeyCode.Tab;

    void Start()
    {
        if (!buildOnStart) return;
        EnsureManagers();
        EnsurePlayer();
        BuildLevel();
        PlacerGameplay();
        ConfigureEscape();
        SetupMission();
        Debug.Log("[Bootstrapper] Museum Heist vertical slice initialized.");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleCursorKey))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState == CursorLockMode.None;
        }
    }

    void EnsureManagers()
    {
        EnsureSingleton<SecurityManager>();
        EnsureSingleton<MissionManager>();
        EnsureSingleton<InventoryManager>();
        EnsureSingleton<CredentialManager>();
        EnsureSingleton<AuthenticationService>();
        EnsureSingleton<AuthorizationService>();
        EnsureSingleton<CheckpointManager>();
        _ = NoiseManager.Instance;
        EnsureSingleton<LaptopController>();
        EnsureSingleton<CameraDisableTimer>();
        EnsureSingleton<DoorUIFeedback>();
        EnsureComponent<SecurityConsoleUI>("SecurityConsoleUI");
        EnsureComponent<MissionHUD>("MissionHUD");
        EnsureComponent<EscapePhaseController>("EscapePhaseController");
        EnsureComponent<MuseumMissionBridge>("MissionBridge");
        EnsureSingleton<MissionScorer>();
        EnsureComponent<ResultsScreen>("ResultsScreen");
        EnsureComponent<CameraShaker>("CameraShaker");
    }

    void EnsureSingleton<T>() where T : Component
    {
        if (FindFirstObjectByType<T>() != null) return;
        new GameObject(typeof(T).Name).AddComponent<T>();
    }

    void EnsureComponent<T>(string name) where T : Component
    {
        if (FindFirstObjectByType<T>() != null) return;
        new GameObject(name).AddComponent<T>();
    }

    void EnsurePlayer()
    {
        if (FindFirstObjectByType<PlayerController>() != null) return;

        var camera = FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            var camGo = new GameObject("MainCamera");
            camera = camGo.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 1.7f, 0f);
            camGo.tag = "MainCamera";
        }

        var player = new GameObject("Player");
        player.tag = "Player";
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0f, 1f, 0f);

        camera.transform.SetParent(player.transform);
        camera.transform.localPosition = new Vector3(0f, 1.7f, 0f);

        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerInteraction>();

        var spawn = GameObject.Find("SpawnPoint");
        player.transform.position = spawn != null ? spawn.transform.position : new Vector3(0f, 1f, 9f);
    }

    void BuildLevel()
    {
        EnsureComponent<MuseumLevelBuilder>("MuseumLevelBuilder");
        var lb = FindFirstObjectByType<MuseumLevelBuilder>();
        if (lb != null) lb.BuildLevel();
    }

    void PlacerGameplay()
    {
        var placer = new GameObject("MuseumGameplayPlacer").AddComponent<MuseumGameplayPlacer>();
        placer.levelBuilder = FindFirstObjectByType<MuseumLevelBuilder>();
        placer.consoleUI = FindFirstObjectByType<SecurityConsoleUI>();
        placer.escapeController = FindFirstObjectByType<EscapePhaseController>();
        placer.BuildAll();
    }

    void ConfigureEscape()
    {
        var esc = FindFirstObjectByType<EscapePhaseController>();
        if (esc == null) return;
    }

    void SetupMission()
    {
        var mm = MissionManager.Instance;
        if (mm == null) return;

        mm.SetObjectiveOrder(new List<ObjectiveID>
        {
            ObjectiveID.EnterMuseumHeist,
            ObjectiveID.ReachLobby,
            ObjectiveID.FindStaffCredential,
            ObjectiveID.AccessStaffOffice,
            ObjectiveID.UseStaffTerminal,
            ObjectiveID.ReachSecurityOffice,
            ObjectiveID.UseSecurityTerminal,
            ObjectiveID.DisableEastCameras,
            ObjectiveID.ReachRestrictedCorridor,
            ObjectiveID.FindSecurityCredential,
            ObjectiveID.UnlockVaultArea,
            ObjectiveID.ReachVaultAntechamber,
            ObjectiveID.UseVaultTerminal,
            ObjectiveID.UnlockVault,
            ObjectiveID.StealMainArtifact,
            ObjectiveID.EscapeMuseum,
            ObjectiveID.HeistComplete,
        });

        if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasSavedCheckpoint)
        {
            CheckpointManager.Instance.LoadCheckpoint();
            Debug.Log("[Bootstrapper] Restored from checkpoint.");
        }
        else
        {
            mm.StartMission();
        }
    }

    static void SetPrivateField(object obj, string field, object value)
    {
        var f = obj.GetType().GetField(field,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f != null) f.SetValue(obj, value);
    }
}
