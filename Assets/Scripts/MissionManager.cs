using UnityEngine;
using System.Collections.Generic;

public enum ObjectiveID
{
    // Original tutorial objectives (0-7)
    EnterMuseum = 0,
    ObserveGuard = 1,
    ObtainKeycard = 2,
    UnlockSecurityDoor = 3,
    DisableCamera = 4,
    EnterVault = 5,
    StealArtifact = 6,
    Escape = 7,

    // Museum Heist mission objectives (8+)
    EnterMuseumHeist = 8,
    ReachLobby = 9,
    FindStaffCredential = 10,
    AccessStaffOffice = 11,
    UseStaffTerminal = 12,
    ReachSecurityOffice = 13,
    UseSecurityTerminal = 14,
    DisableEastCameras = 15,
    ReachRestrictedCorridor = 16,
    FindSecurityCredential = 17,
    UnlockVaultArea = 18,
    ReachVaultAntechamber = 19,
    UseVaultTerminal = 20,
    UnlockVault = 21,
    StealMainArtifact = 22,
    EscapeMuseum = 23,
    HeistComplete = 24,

    // Tutorial objectives
    Tutorial_ReachKeycard = 100,
    Tutorial_PickupKeycard = 101,
    Tutorial_OpenDoor = 102,
    Tutorial_SneakPastGuard = 103,
    Tutorial_DisableCamera = 104,
    Tutorial_StealArtifact = 105,
    Tutorial_Escape = 106,
    Tutorial_Complete = 107
}

public enum MissionPhase
{
    Infiltration,
    Exploration,
    TerminalAccess,
    Heist,
    Escape
}

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Auto-Start")]
    public bool autoStart = true;

    [Header("Objectives")]
    public List<ObjectiveID> objectiveOrder = new()
    {
        ObjectiveID.EnterMuseum,
        ObjectiveID.ObserveGuard,
        ObjectiveID.ObtainKeycard,
        ObjectiveID.UnlockSecurityDoor,
        ObjectiveID.DisableCamera,
        ObjectiveID.EnterVault,
        ObjectiveID.StealArtifact,
        ObjectiveID.Escape,
    };

    [Header("State")]
    public ObjectiveID currentObjective = ObjectiveID.EnterMuseum;
    public List<ObjectiveID> completedObjectives = new();
    public MissionPhase currentPhase = MissionPhase.Infiltration;
    public bool isInEscapePhase = false;

    private int currentIndex = 0;
    private bool missionActive = false;

    // Events
    public event System.Action<ObjectiveID> OnObjectiveStarted;
    public event System.Action<ObjectiveID> OnObjectiveCompleted;
    public event System.Action OnMissionCompleted;
    public event System.Action<MissionPhase> OnPhaseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (autoStart)
            StartMission();
    }

    public void StartMission()
    {
        if (objectiveOrder.Count == 0) return;

        missionActive = true;
        currentIndex = 0;
        completedObjectives.Clear();
        currentObjective = objectiveOrder[0];
        currentPhase = MissionPhase.Infiltration;
        isInEscapePhase = false;
        OnObjectiveStarted?.Invoke(currentObjective);
    }

    public void CompleteObjective(ObjectiveID id)
    {
        if (!missionActive) return;
        if (currentIndex >= objectiveOrder.Count) return;

        if (id != objectiveOrder[currentIndex])
        {
            Debug.Log($"Mission: '{id}' skipped — must complete '{objectiveOrder[currentIndex]}' first.");
            return;
        }

        completedObjectives.Add(id);
        OnObjectiveCompleted?.Invoke(id);
        currentIndex++;

        if (currentIndex >= objectiveOrder.Count)
        {
            missionActive = false;
            OnMissionCompleted?.Invoke();
            Debug.Log("Mission: All objectives complete!");
        }
        else
        {
            currentObjective = objectiveOrder[currentIndex];
            OnObjectiveStarted?.Invoke(currentObjective);
        }

        UpdatePhase();
    }

    public void CompleteObjectiveByEvent(ObjectiveID id)
    {
        CompleteObjective(id);
    }

    public void TriggerEscapePhase()
    {
        if (isInEscapePhase) return;
        isInEscapePhase = true;
        SetPhase(MissionPhase.Escape);

        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.SetAlarmLevel(SecurityManager.AlarmLevel.Alert);
        }
    }

    public void SetPhase(MissionPhase newPhase)
    {
        if (currentPhase == newPhase) return;
        currentPhase = newPhase;
        OnPhaseChanged?.Invoke(newPhase);
    }

    private void UpdatePhase()
    {
        float progress = GetProgress();
        if (progress >= 0.9f)
            SetPhase(MissionPhase.Escape);
        else if (progress >= 0.6f)
            SetPhase(MissionPhase.Heist);
        else if (progress >= 0.3f)
            SetPhase(MissionPhase.TerminalAccess);
        else if (progress >= 0.1f)
            SetPhase(MissionPhase.Exploration);
    }

    public ObjectiveID GetCurrentObjective()
    {
        return currentObjective;
    }

    public float GetProgress()
    {
        if (objectiveOrder.Count == 0) return 0f;
        return (float)currentIndex / objectiveOrder.Count;
    }

    public bool IsObjectiveComplete(ObjectiveID id)
    {
        return completedObjectives.Contains(id);
    }

    public void SetObjectiveOrder(List<ObjectiveID> newOrder)
    {
        objectiveOrder = newOrder;
    }

    public void ResetMission()
    {
        missionActive = false;
        currentIndex = 0;
        completedObjectives.Clear();
        currentPhase = MissionPhase.Infiltration;
        isInEscapePhase = false;
        currentObjective = ObjectiveID.EnterMuseum;
        StartMission();
    }
}
