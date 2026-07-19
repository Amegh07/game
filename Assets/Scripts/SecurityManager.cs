using UnityEngine;
using System.Collections.Generic;

public class SecurityManager : MonoBehaviour
{
    public static SecurityManager Instance { get; private set; }

    public enum AlarmLevel
    {
        Normal     = 0,
        Suspicious = 1,
        Alert      = 2,
        Lockdown   = 3,
        Recovery   = 4
    }

    public enum SecurityTrigger
    {
        PlayerDetected,
        ArtifactStolen,
        AlarmButtonPressed,
        CameraPlayerDetected,
        MultipleGuardsAlerted,
        UnauthorizedTerminalAccess,
        FailedKeycardAttempt,
        GuardChase,
        GuardSuspicious,
        NoiseSpike
    }

    [Header("Alarm State")]
    [SerializeField] private AlarmLevel _alarmLevel = AlarmLevel.Normal;
    public AlarmLevel currentAlarmLevel => _alarmLevel;

    [Header("Recovery Timers")]
    [SerializeField] private float cooldownAfterTrigger = 15f;
    [SerializeField] private float recoveryStepInterval = 10f;

    private Dictionary<string, SecurityCamera> cameras = new();
    private Dictionary<string, LockedDoor> doors = new();
    private List<GuardFSM> guards = new();

    private float lastTriggerTime = Mathf.NegativeInfinity;
    private bool isRecovering;
    private float recoveryTimer;

    public int CameraCount => cameras.Count;
    public int DoorCount => doors.Count;
    public int GuardCount => guards.Count;

    public event System.Action<AlarmLevel, AlarmLevel> OnAlarmLevelChanged;
    public event System.Action<string> OnCameraDisabled;
    public event System.Action<string> OnCameraEnabled;
    public event System.Action<string> OnDoorUnlocked;
    public event System.Action<string> OnDoorLocked;
    public event System.Action<SecurityTrigger, string, AlarmLevel> OnTriggerReported;
    public event System.Action OnRecoveryStarted;

    public event System.Action<string> OnDoorUnlockRequested;
    public event System.Action<string> OnDoorLockRequested;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (_alarmLevel == AlarmLevel.Normal) return;

        if (!isRecovering)
        {
            if (Time.time - lastTriggerTime >= cooldownAfterTrigger)
                BeginRecovery();
        }
        else
        {
            recoveryTimer += Time.deltaTime;
            if (recoveryTimer >= recoveryStepInterval)
            {
                recoveryTimer = 0f;
                StepDown();
            }
        }
    }

    private void BeginRecovery()
    {
        if (_alarmLevel == AlarmLevel.Normal) return;

        isRecovering = true;
        recoveryTimer = 0f;

        var old = _alarmLevel;
        _alarmLevel = AlarmLevel.Recovery;
        OnAlarmLevelChanged?.Invoke(old, _alarmLevel);
        OnRecoveryStarted?.Invoke();
        Debug.Log($"[SecurityManager] Recovery started: {old} → {AlarmLevel.Recovery}");
    }

    private void StepDown()
    {
        var old = _alarmLevel;
        _alarmLevel = AlarmLevel.Normal;
        isRecovering = false;
        OnAlarmLevelChanged?.Invoke(old, _alarmLevel);
        Debug.Log($"[SecurityManager] Recovery complete: {old} → {AlarmLevel.Normal}");
    }

    // ---------------------------------------------------------------
    //  Registration
    // ---------------------------------------------------------------

    public void RegisterCamera(string id, SecurityCamera cam)
    {
        if (!cameras.TryAdd(id, cam))
            Debug.LogWarning($"SecurityManager: Camera '{id}' already registered — overwriting.");
        cameras[id] = cam;
    }

    public void UnregisterCamera(string id)
    {
        cameras.Remove(id);
    }

    public void RegisterDoor(string id, LockedDoor door)
    {
        if (!doors.TryAdd(id, door))
            Debug.LogWarning($"SecurityManager: Door '{id}' already registered — overwriting.");
        doors[id] = door;
    }

    public void UnregisterDoor(string id)
    {
        doors.Remove(id);
    }

    public void RegisterGuard(GuardFSM guard)
    {
        if (!guards.Contains(guard))
            guards.Add(guard);
    }

    public void UnregisterGuard(GuardFSM guard)
    {
        guards.Remove(guard);
    }

    // ---------------------------------------------------------------
    //  Alarm level
    // ---------------------------------------------------------------

    public void SetAlarmLevel(AlarmLevel newLevel)
    {
        if (newLevel == _alarmLevel) return;
        if (newLevel > _alarmLevel || _alarmLevel == AlarmLevel.Recovery)
            RaiseAlarmLevel(newLevel);
    }

    public void ForceSetAlarmLevel(AlarmLevel newLevel)
    {
        if (newLevel == _alarmLevel) return;
        var old = _alarmLevel;
        _alarmLevel = newLevel;
        isRecovering = false;
        recoveryTimer = 0f;
        OnAlarmLevelChanged?.Invoke(old, newLevel);
    }

    public void ResetAlarm()
    {
        if (_alarmLevel <= AlarmLevel.Suspicious)
        {
            var old = _alarmLevel;
            _alarmLevel = AlarmLevel.Normal;
            isRecovering = false;
            recoveryTimer = 0f;
            OnAlarmLevelChanged?.Invoke(old, _alarmLevel);
        }
        else
        {
            BeginRecovery();
        }
    }

    public void ForceResetAlarm()
    {
        var old = _alarmLevel;
        _alarmLevel = AlarmLevel.Normal;
        isRecovering = false;
        recoveryTimer = 0f;
        lastTriggerTime = Time.time;
        OnAlarmLevelChanged?.Invoke(old, _alarmLevel);
    }

    private void RaiseAlarmLevel(AlarmLevel newLevel)
    {
        var old = _alarmLevel;
        _alarmLevel = newLevel;
        isRecovering = false;
        recoveryTimer = 0f;
        lastTriggerTime = Time.time;
        OnAlarmLevelChanged?.Invoke(old, newLevel);
    }

    // ---------------------------------------------------------------
    //  Trigger system
    // ---------------------------------------------------------------

    public void ReportTrigger(SecurityTrigger trigger, string source = "")
    {
        lastTriggerTime = Time.time;

        if (isRecovering)
        {
            isRecovering = false;
            recoveryTimer = 0f;
        }

        AlarmLevel suggested = GetSuggestedLevel(trigger);

        if (suggested > _alarmLevel)
            RaiseAlarmLevel(suggested);
        else if (_alarmLevel == AlarmLevel.Recovery && suggested > AlarmLevel.Normal)
            RaiseAlarmLevel(suggested);

        OnTriggerReported?.Invoke(trigger, source, _alarmLevel);
    }

    private static AlarmLevel GetSuggestedLevel(SecurityTrigger trigger) => trigger switch
    {
        SecurityTrigger.PlayerDetected              => AlarmLevel.Alert,
        SecurityTrigger.ArtifactStolen              => AlarmLevel.Alert,
        SecurityTrigger.AlarmButtonPressed          => AlarmLevel.Lockdown,
        SecurityTrigger.CameraPlayerDetected        => AlarmLevel.Alert,
        SecurityTrigger.MultipleGuardsAlerted       => AlarmLevel.Alert,
        SecurityTrigger.UnauthorizedTerminalAccess  => AlarmLevel.Suspicious,
        SecurityTrigger.FailedKeycardAttempt        => AlarmLevel.Suspicious,
        SecurityTrigger.GuardChase                  => AlarmLevel.Alert,
        SecurityTrigger.GuardSuspicious             => AlarmLevel.Suspicious,
        SecurityTrigger.NoiseSpike                  => AlarmLevel.Suspicious,
        _                                           => AlarmLevel.Normal,
    };

    // ---------------------------------------------------------------
    //  Camera control
    // ---------------------------------------------------------------

    public bool DisableCamera(string id)
    {
        if (cameras.TryGetValue(id, out var cam))
        {
            cam.SetActiveState(false);
            OnCameraDisabled?.Invoke(id);
            return true;
        }
        Debug.LogWarning($"SecurityManager: Camera '{id}' not found.");
        return false;
    }

    public bool EnableCamera(string id)
    {
        if (cameras.TryGetValue(id, out var cam))
        {
            cam.SetActiveState(true);
            OnCameraEnabled?.Invoke(id);
            return true;
        }
        Debug.LogWarning($"SecurityManager: Camera '{id}' not found.");
        return false;
    }

    public bool IsCameraActive(string id)
    {
        return cameras.TryGetValue(id, out var cam) && cam.isActive;
    }

    public IEnumerable<string> GetRegisteredCameraIds()
    {
        return cameras.Keys;
    }

    public int DisableCameraGroup(string[] ids)
    {
        int count = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            if (DisableCamera(ids[i]))
                count++;
        }
        return count;
    }

    // ---------------------------------------------------------------
    //  Door control
    // ---------------------------------------------------------------

    public bool UnlockDoor(string id)
    {
        if (doors.TryGetValue(id, out var door))
        {
            door.Unlock();
            door.Open();
            OnDoorUnlocked?.Invoke(id);
            return true;
        }
        Debug.LogWarning($"SecurityManager: Door '{id}' not found.");
        return false;
    }

    public bool LockDoor(string id)
    {
        if (doors.TryGetValue(id, out var door))
        {
            door.Lock();
            door.Close();
            OnDoorLocked?.Invoke(id);
            return true;
        }
        Debug.LogWarning($"SecurityManager: Door '{id}' not found.");
        return false;
    }

    public void NotifyDoorUnlocked(string id)
    {
        OnDoorUnlocked?.Invoke(id);
    }

    public void NotifyDoorLocked(string id)
    {
        OnDoorLocked?.Invoke(id);
    }

    public void UnlockAllDoors()
    {
        foreach (var kv in doors)
        {
            kv.Value.Unlock();
            OnDoorUnlocked?.Invoke(kv.Key);
        }
    }

    public void LockAllDoors()
    {
        foreach (var kv in doors)
        {
            kv.Value.Lock();
            OnDoorLocked?.Invoke(kv.Key);
        }
    }

    public void RequestDoorUnlock(string doorID)
    {
        OnDoorUnlockRequested?.Invoke(doorID);
    }

    public void RequestDoorLock(string doorID)
    {
        OnDoorLockRequested?.Invoke(doorID);
    }
}
