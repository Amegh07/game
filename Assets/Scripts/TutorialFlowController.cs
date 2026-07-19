using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

/// <summary>
/// Scene-local coordinator for the Training Ground. Gameplay systems remain authoritative;
/// this component only converts their events into ordered teaching objectives and HUD prompts.
/// </summary>
public sealed class TutorialFlowController : MonoBehaviour
{
    private const string StaffCredentialId = "training_staff";
    private PlayerController player;
    private DoorController trainingDoor;
    private TerminalController terminal;
    private SecurityCamera securityCamera;
    private GuardFSM guard;
    private bool moved;
    private bool looked;
    private bool sprinted;
    private bool jumped;
    private bool crouched;
    private bool deniedDoor;
    private bool authorized;
    private bool cameraDetectionStarted;
    private bool guardChased;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddToTutorialScene()
    {
        if (SceneManager.GetActiveScene().name != "Tutorial" || FindFirstObjectByType<TutorialFlowController>() != null)
            return;

        new GameObject("TutorialFlowController").AddComponent<TutorialFlowController>();
    }

    private IEnumerator Start()
    {
        yield return null; // PlayerController auto-spawns after scene load.
        player = FindFirstObjectByType<PlayerController>();
        trainingDoor = FindFirstObjectByType<DoorController>();
        terminal = FindFirstObjectByType<TerminalController>();
        securityCamera = FindFirstObjectByType<SecurityCamera>();
        guard = FindFirstObjectByType<GuardFSM>();

        ConfigureTrainingPickup();
        Subscribe();
        ConfigureExitZone();
        StartTutorialMission();
        Notify("Movement: move with WASD, look with the mouse, sprint with Shift, jump with Space, and crouch with Ctrl.", Color.cyan);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void ConfigureTrainingPickup()
    {
        KeycardItem card = FindFirstObjectByType<KeycardItem>();
        if (card == null) return;

        card.SetKeycardType(KeycardType.Staff);
        card.ConfigureCredentialGrant(StaffCredentialId, CredentialType.Keycard, UserRole.Staff, "Staff Credential");
    }

    private static void ConfigureExitZone()
    {
        MissionZone[] zones = FindObjectsByType<MissionZone>(FindObjectsSortMode.None);
        foreach (MissionZone zone in zones)
        {
            if (zone.name == "Zone_Escape")
                zone.objectiveID = ObjectiveID.Tutorial_Exit;
        }
    }

    private void HandleMissionCompleted()
    {
        Notify("Training Complete — Ready for Museum Infiltration", Color.green);
    }
    private void StartTutorialMission()
    {
        if (MissionManager.Instance == null) return;

        MissionManager.Instance.SetObjectiveOrder(new System.Collections.Generic.List<ObjectiveID>
        {
            ObjectiveID.Tutorial_Movement,
            ObjectiveID.Tutorial_Interaction,
            ObjectiveID.Tutorial_CredentialPickup,
            ObjectiveID.Tutorial_LockedDoor,
            ObjectiveID.Tutorial_Authentication,
            ObjectiveID.Tutorial_Authorization,
            ObjectiveID.Tutorial_CameraAwareness,
            ObjectiveID.Tutorial_GuardAwareness,
            ObjectiveID.Tutorial_SecurityConsole,
            ObjectiveID.Tutorial_Exit
        });
        MissionManager.Instance.StartMission();
    }

    private void Subscribe()
    {
        if (player != null)
        {
            player.OnFootstep += HandleFootstep;
            player.OnJump += HandleJump;
            player.OnCrouchToggle += HandleCrouch;
            player.OnInteract += HandleInteract;
        }
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnKeycardAdded += HandleKeycardAdded;
        if (CredentialManager.Instance != null) CredentialManager.Instance.OnCredentialAdded += HandleCredentialAdded;
        if (trainingDoor != null)
        {
            trainingDoor.OnAccessDenied += HandleDoorDenied;
            trainingDoor.OnDoorOpened += HandleDoorOpened;
        }
        if (LaptopController.Instance != null) LaptopController.Instance.OnConnected += HandleLaptopConnected;
        if (terminal != null)
        {
            terminal.OnSessionStarted += HandleSessionStarted;
            terminal.OnActionExecuted += HandleActionExecuted;
        }
        if (securityCamera != null)
        {
            securityCamera.OnDetectionStarted += HandleCameraDetectionStarted;
            securityCamera.OnPlayerDetected += HandleCameraDetected;
            securityCamera.OnPlayerLost += HandleCameraLost;
        }
        if (guard != null) guard.OnStateChanged += HandleGuardStateChanged;
        if (MissionManager.Instance != null) MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
    }

    private void Unsubscribe()
    {
        if (player != null)
        {
            player.OnFootstep -= HandleFootstep;
            player.OnJump -= HandleJump;
            player.OnCrouchToggle -= HandleCrouch;
            player.OnInteract -= HandleInteract;
        }
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnKeycardAdded -= HandleKeycardAdded;
        if (CredentialManager.Instance != null) CredentialManager.Instance.OnCredentialAdded -= HandleCredentialAdded;
        if (trainingDoor != null)
        {
            trainingDoor.OnAccessDenied -= HandleDoorDenied;
            trainingDoor.OnDoorOpened -= HandleDoorOpened;
        }
        if (LaptopController.Instance != null) LaptopController.Instance.OnConnected -= HandleLaptopConnected;
        if (terminal != null)
        {
            terminal.OnSessionStarted -= HandleSessionStarted;
            terminal.OnActionExecuted -= HandleActionExecuted;
        }
        if (securityCamera != null)
        {
            securityCamera.OnDetectionStarted -= HandleCameraDetectionStarted;
            securityCamera.OnPlayerDetected -= HandleCameraDetected;
            securityCamera.OnPlayerLost -= HandleCameraLost;
        }
        if (guard != null) guard.OnStateChanged -= HandleGuardStateChanged;
        if (MissionManager.Instance != null) MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
    }

    private void HandleFootstep()
    {
        moved = true;
        sprinted |= player != null && player.IsRunning;
        CompleteMovementIfReady();
    }

    private void HandleJump() { jumped = true; CompleteMovementIfReady(); }
    private void HandleCrouch(bool isCrouching) { crouched |= isCrouching; CompleteMovementIfReady(); }
    private void HandleInteract() { looked = true; CompleteMovementIfReady(); Complete(ObjectiveID.Tutorial_Interaction); }

    private void CompleteMovementIfReady()
    {
        if (moved && looked && sprinted && jumped && crouched)
            Complete(ObjectiveID.Tutorial_Movement);
    }

    private void HandleKeycardAdded(KeycardType type)
    {
        if (type == KeycardType.Staff) Notify("Staff keycard added to inventory.", Color.green);
    }

    private void HandleCredentialAdded(StoredCredential credential)
    {
        if (credential.CredentialID != StaffCredentialId) return;
        Notify("Staff Credential obtained. Your inventory and authentication profile are updated.", Color.green);
        Complete(ObjectiveID.Tutorial_CredentialPickup);
        Notify("Try the locked staff door first. It will refuse access until you use the credential.", Color.yellow);
    }

    private void HandleDoorDenied(string _, KeycardType __)
    {
        deniedDoor = true;
        Notify("Access Denied — Staff Credential required.", Color.red);
    }

    private void HandleDoorOpened(string _)
    {
        if (!deniedDoor) return;
        Complete(ObjectiveID.Tutorial_LockedDoor);
        Notify("Door unlocked. Connect your laptop to the terminal and authenticate.", Color.green);
    }

    private void HandleLaptopConnected(TerminalController connectedTerminal)
    {
        if (connectedTerminal != terminal) return;
        AuthenticationResult result = terminal.Authenticate(StaffCredentialId);
        if (!result.Success) Notify(result.ErrorMessage, Color.red);
    }

    private void HandleSessionStarted(NetworkSession session)
    {
        Notify($"Authenticated. Session created for role: {session.Role}.", Color.green);
        Complete(ObjectiveID.Tutorial_Authentication);

        AuthorizationResult granted = AuthorizationService.Instance.Authorize(session.Role, terminal.TerminalID, UserRole.Staff);
        AuthorizationResult denied = AuthorizationService.Instance.Authorize(session.Role, terminal.TerminalID, UserRole.SecurityOfficer);
        authorized = granted.Authorized;
        Notify(authorized ? "Access Granted: Staff operations permitted. Access Denied: Security Officer action blocked." : denied.ErrorMessage, authorized ? Color.green : Color.red);
        Complete(ObjectiveID.Tutorial_Authorization);
        Notify("Camera ahead: enter its vision cone, then break line of sight and watch detection decay.", Color.yellow);
    }

    private void HandleCameraDetectionStarted(string _) { cameraDetectionStarted = true; Notify("Camera detecting — break line of sight.", Color.yellow); }
    private void HandleCameraDetected(string _) { Notify("Camera detected you. Move behind cover to clear detection.", Color.red); }
    private void HandleCameraLost(string _)
    {
        if (!cameraDetectionStarted) return;
        Complete(ObjectiveID.Tutorial_CameraAwareness);
        Notify("Detection decayed. The guard now demonstrates patrol, suspicion, chase, and recovery.", Color.green);
    }

    private void HandleGuardStateChanged(GuardFSM.GuardState _, GuardFSM.GuardState next)
    {
        if (next == GuardFSM.GuardState.Suspicious) Notify("Guard suspicious — stay out of sight.", Color.yellow);
        if (next == GuardFSM.GuardState.Chase) { guardChased = true; Notify("Guard chasing — escape and break line of sight.", Color.red); }
        if (next == GuardFSM.GuardState.ReturnToPatrol && guardChased)
        {
            Complete(ObjectiveID.Tutorial_GuardAwareness);
            Notify("Guard returned to patrol. Use the security console to disable the camera and unlock the door.", Color.green);
        }
    }

    private void HandleActionExecuted(TerminalLogEntry entry)
    {
        if (!entry.Success) return;
        if (entry.Action == "Disable Camera" || entry.Action == "Unlock Door")
        {
            Complete(ObjectiveID.Tutorial_SecurityConsole);
            Notify("Security command accepted. Reach the exit to finish training.", Color.green);
        }
    }

    private static void Complete(ObjectiveID objective)
    {
        MissionManager.Instance?.CompleteObjective(objective);
    }

    private static void Notify(string message, Color color)
    {
        MissionHUD.Instance?.ShowNotification(message, color);
    }
}