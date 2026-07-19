using UnityEngine;
using System.Collections.Generic;

public class LockdownVisualController : MonoBehaviour
{
    [System.Serializable]
    public class AlertLight
    {
        public Light lightSource;
        public float baseIntensity = 1.2f;
        public float flashSpeed = 4f;
    }

    public List<AlertLight> alertLights = new();
    public Material warningOverlayMat;
    public Material doorLockedIndicatorMat;

    private bool active;
    private float flashTimer;
    private bool eventsSubscribed = false;
    
    void Start()
    {
        SubscribeToEvents();
    }

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (eventsSubscribed) return;
        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.OnAlarmLevelChanged += HandleAlarmChanged;
            eventsSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!eventsSubscribed) return;
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnAlarmLevelChanged -= HandleAlarmChanged;
        eventsSubscribed = false;
    }

    void HandleAlarmChanged(SecurityManager.AlarmLevel oldLevel, SecurityManager.AlarmLevel newLevel)
    {
        bool shouldActivate = newLevel == SecurityManager.AlarmLevel.Alert
                           || newLevel == SecurityManager.AlarmLevel.Lockdown;
        SetActive(shouldActivate);
    }

    void SetActive(bool state)
    {
        active = state;
        if (warningOverlayMat != null)
            warningOverlayMat.color = state
                ? new Color(1f, 0f, 0f, 0.15f)
                : new Color(0f, 0f, 0f, 0f);
        foreach (var al in alertLights)
        {
            if (al.lightSource != null)
                al.lightSource.enabled = state;
        }
        if (doorLockedIndicatorMat != null)
            doorLockedIndicatorMat.color = state
                ? new Color(1f, 0.2f, 0f, 0.8f)
                : new Color(0f, 0.5f, 0f, 0.3f);
    }

    void Update()
    {
        if (!active) return;
        flashTimer += Time.deltaTime;
        float flash = Mathf.PingPong(flashTimer * 4f, 1f);
        foreach (var al in alertLights)
        {
            if (al.lightSource != null)
                al.lightSource.intensity = al.baseIntensity * (0.3f + flash * 0.7f);
        }
    }

    // --- Static helper to register lights ---

    public static LockdownVisualController EnsureInstance()
    {
        var existing = FindFirstObjectByType<LockdownVisualController>();
        if (existing != null) return existing;
        var go = new GameObject("LockdownVisualController");
        return go.AddComponent<LockdownVisualController>();
    }

    public void RegisterEmergencyLight(Light l)
    {
        alertLights.Add(new AlertLight { lightSource = l });
    }

    public void SetWarningOverlay(Material mat)
    {
        warningOverlayMat = mat;
    }

    public void SetDoorIndicator(Material mat)
    {
        doorLockedIndicatorMat = mat;
    }
}
