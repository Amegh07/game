using UnityEngine;
using System.Collections.Generic;

public class SecurityManager : MonoBehaviour
{
    // Singleton
    public static SecurityManager Instance { get; private set; }

    public enum AlarmLevel
    {
        Normal     = 0,
        Suspicious = 1,
        Alert      = 2,
        Lockdown   = 3
    }

    [Header("Alarm State")]
    [SerializeField] private AlarmLevel _alarmLevel = AlarmLevel.Normal;
    public AlarmLevel currentAlarmLevel => _alarmLevel;

    // Registries
    private Dictionary<string, SecurityCamera> cameras = new();
    private Dictionary<string, LockedDoor> doors = new();
    private List<GuardFSM> guards = new();

    // Events
    public event System.Action<AlarmLevel, AlarmLevel> OnAlarmLevelChanged;
    public event System.Action<string> OnCameraDisabled;
    public event System.Action<string> OnCameraEnabled;
    public event System.Action<string> OnDoorUnlocked;
    public event System.Action<string> OnDoorLocked;

    // Cyber system integration events (for DoorController and Terminal system)
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
        if (newLevel <= _alarmLevel) return;
        RaiseAlarmLevel(newLevel);
    }

    public void ResetAlarm()
    {
        var old = _alarmLevel;
        _alarmLevel = AlarmLevel.Normal;
        OnAlarmLevelChanged?.Invoke(old, _alarmLevel);
    }

    private void RaiseAlarmLevel(AlarmLevel newLevel)
    {
        var old = _alarmLevel;
        _alarmLevel = newLevel;
        OnAlarmLevelChanged?.Invoke(old, newLevel);
    }

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

    // ---------------------------------------------------------------
    //  Cyber system door control (routes to DoorController via events)
    // ---------------------------------------------------------------

    public void RequestDoorUnlock(string doorID)
    {
        OnDoorUnlockRequested?.Invoke(doorID);
    }

    public void RequestDoorLock(string doorID)
    {
        OnDoorLockRequested?.Invoke(doorID);
    }
}
