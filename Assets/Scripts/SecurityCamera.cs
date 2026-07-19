using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    public enum CameraAlertState
    {
        Idle,
        Detecting,
        Alerted
    }

    [Header("Identification")]
    public string cameraID = "";

    [Header("Configuration")]
    [Tooltip("Optional ScriptableObject asset. When assigned, its values override the individual fields below.")]
    public CameraConfig config;

    [Header("Registration")]
    public bool registerWithSecurity = true;

    [Header("Rotation")]
    public float rotationAngle = 45f;
    public float rotationSpeed = 30f;

    [Header("Detection")]
    public float detectionRange = 10f;
    public float fieldOfView = 60f;
    public float detectionTime = 2f;
    public float detectionDecayRate = 1f;
    public LayerMask obstructionMask = -1;

    [Header("Security Level Multipliers")]
    public float speedMultiplierNormal = 1f;
    public float speedMultiplierSuspicious = 1.3f;
    public float speedMultiplierAlert = 1.6f;
    public float speedMultiplierLockdown = 2f;
    public float rangeMultiplierNormal = 1f;
    public float rangeMultiplierSuspicious = 1.1f;
    public float rangeMultiplierAlert = 1.25f;
    public float rangeMultiplierLockdown = 1.5f;

    [Header("State")]
    public bool isActive = true;
    public CameraAlertState alertState = CameraAlertState.Idle;

    public event System.Action<string> OnPlayerDetected;
    public event System.Action<string> OnPlayerLost;

    private float currentAngle;
    private bool rotatingLeft;
    private float initialYRotation;
    private float detectionProgress;
    private bool hasFiredDetection;
    private bool hasFiredLost = true;
    private bool playerInSight;
    private Transform playerTransform;

    // Base values (resolved from config or field)
    private float baseSpeed;
    private float baseRange;

    // Runtime effective values adjusted by security level
    private float effectiveSpeed;
    private float effectiveRange;
    private bool eventsSubscribed = false;

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (eventsSubscribed) return;
        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.OnAlarmLevelChanged += HandleAlarmLevelChanged;
            UpdateEffectiveValues();
            eventsSubscribed = true;
        }
        if (MissionHUD.Instance != null)
            MissionHUD.Instance.RegisterCamera(this);
    }

    private void UnsubscribeFromEvents()
    {
        if (!eventsSubscribed) return;
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnAlarmLevelChanged -= HandleAlarmLevelChanged;
        if (MissionHUD.Instance != null)
            MissionHUD.Instance.UnregisterCamera(this);
        eventsSubscribed = false;
    }

    void Start()
    {
        initialYRotation = transform.eulerAngles.y;
        rotatingLeft = config != null ? config.startRotatingLeft : true;

        baseSpeed = FromConfig(c => c.patrolSpeed, rotationSpeed);
        baseRange = FromConfig(c => c.detectionRange, detectionRange);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (SecurityManager.Instance != null && registerWithSecurity)
            SecurityManager.Instance.RegisterCamera(cameraID, this);

        UpdateEffectiveValues();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
        if (SecurityManager.Instance != null && registerWithSecurity)
            SecurityManager.Instance.UnregisterCamera(cameraID);
    }

    void Update()
    {
        if (!isActive) return;

        if (registerWithSecurity)
        {
            playerInSight = false;
            if (playerTransform != null)
                DetectPlayer();

            UpdateAlertState();
        }

        UpdateRotation();
    }

    private void HandleAlarmLevelChanged(SecurityManager.AlarmLevel oldLevel, SecurityManager.AlarmLevel newLevel)
    {
        UpdateEffectiveValues();
    }

    private void UpdateEffectiveValues()
    {
        if (SecurityManager.Instance == null)
        {
            effectiveSpeed = baseSpeed;
            effectiveRange = baseRange;
            return;
        }

        switch (SecurityManager.Instance.currentAlarmLevel)
        {
            case SecurityManager.AlarmLevel.Normal:
            case SecurityManager.AlarmLevel.Recovery:
                effectiveSpeed = baseSpeed * speedMultiplierNormal;
                effectiveRange = baseRange * rangeMultiplierNormal;
                break;
            case SecurityManager.AlarmLevel.Suspicious:
                effectiveSpeed = baseSpeed * speedMultiplierSuspicious;
                effectiveRange = baseRange * rangeMultiplierSuspicious;
                break;
            case SecurityManager.AlarmLevel.Alert:
                effectiveSpeed = baseSpeed * speedMultiplierAlert;
                effectiveRange = baseRange * rangeMultiplierAlert;
                break;
            case SecurityManager.AlarmLevel.Lockdown:
                effectiveSpeed = baseSpeed * speedMultiplierLockdown;
                effectiveRange = baseRange * rangeMultiplierLockdown;
                break;
        }
    }

    private void DetectPlayer()
    {
        Vector3 origin = transform.position;
        float range = effectiveRange;
        float fov = FromConfig(c => c.fieldOfView, fieldOfView);

        Vector3 toPlayer = playerTransform.position - origin;
        float dist = toPlayer.magnitude;

        if (dist > range || dist < 0.01f) return;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > fov * 0.5f) return;

        LayerMask mask = FromConfig(c => c.obstructionMask, obstructionMask);
        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, dist, mask))
        {
            if (hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform))
                playerInSight = true;
        }
    }

    private void UpdateRotation()
    {
        if (FromConfig(c => c.stopRotationOnDetection, false) && alertState == CameraAlertState.Alerted)
            return;

        float speed = effectiveSpeed;
        float limit = FromConfig(c => c.patrolAngle, rotationAngle);
        float step = speed * Time.deltaTime;

        if (rotatingLeft)
        {
            currentAngle -= step;
            if (currentAngle <= -limit)
            {
                currentAngle = -limit;
                rotatingLeft = false;
            }
        }
        else
        {
            currentAngle += step;
            if (currentAngle >= limit)
            {
                currentAngle = limit;
                rotatingLeft = true;
            }
        }

        Vector3 euler = transform.eulerAngles;
        euler.y = initialYRotation + currentAngle;
        transform.eulerAngles = euler;
    }

    private void UpdateAlertState()
    {
        float timeNeeded = FromConfig(c => c.detectionTime, detectionTime);
        float decay = FromConfig(c => c.detectionDecayRate, detectionDecayRate);

        if (playerInSight)
        {
            detectionProgress += Time.deltaTime;
            detectionProgress = Mathf.Min(detectionProgress, timeNeeded);
            alertState = CameraAlertState.Detecting;

            if (detectionProgress >= timeNeeded && !hasFiredDetection)
            {
                hasFiredDetection = true;
                hasFiredLost = false;
                alertState = CameraAlertState.Alerted;
                OnPlayerDetected?.Invoke(cameraID);
                SecurityManager.Instance?.ReportTrigger(
                    SecurityManager.SecurityTrigger.CameraPlayerDetected, cameraID);
            }
        }
        else
        {
            if (detectionProgress > 0f)
            {
                detectionProgress -= decay * Time.deltaTime;
                detectionProgress = Mathf.Max(0f, detectionProgress);

                if (detectionProgress <= 0f && !hasFiredLost)
                {
                    hasFiredDetection = false;
                    hasFiredLost = true;
                    alertState = CameraAlertState.Idle;
                    OnPlayerLost?.Invoke(cameraID);
                }
            }
        }
    }

    public float GetDetectionProgress()
    {
        if (!isActive || detectionProgress <= 0f) return 0f;
        float needed = FromConfig(c => c.detectionTime, detectionTime);
        return Mathf.Clamp01(detectionProgress / Mathf.Max(needed, 0.01f));
    }

    public void SetActiveState(bool active)
    {
        isActive = active;
        if (!active)
        {
            detectionProgress = 0f;
            alertState = CameraAlertState.Idle;
            playerInSight = false;
            hasFiredDetection = false;
            hasFiredLost = true;

            var body = transform.Find("Body");
            if (body != null)
            {
                var r = body.GetComponent<Renderer>();
                if (r != null) r.material.color = new Color(0.15f, 0.15f, 0.15f);
            }
        }
        else
        {
            var body = transform.Find("Body");
            if (body != null)
            {
                var r = body.GetComponent<Renderer>();
                if (r != null) r.material.color = Color.gray;
            }
        }
    }

    private T FromConfig<T>(System.Func<CameraConfig, T> selector, T fallback)
    {
        return config != null ? selector(config) : fallback;
    }

    void OnDrawGizmos()
    {
        if (!isActive && Application.isPlaying) return;

        float range = Application.isPlaying ? effectiveRange : FromConfig(c => c.detectionRange, detectionRange);
        float fov = FromConfig(c => c.fieldOfView, fieldOfView);
        Color coneColor = FromConfig(c => c.visionConeColor, new Color(1f, 0f, 0f, 0.08f));

        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        Vector3 left = Quaternion.Euler(0f, -fov * 0.5f, 0f) * fwd;
        Vector3 right = Quaternion.Euler(0f, fov * 0.5f, 0f) * fwd;

        Gizmos.color = alertState == CameraAlertState.Alerted ? Color.red :
                       alertState == CameraAlertState.Detecting ? Color.yellow : coneColor;

        Gizmos.DrawRay(pos, left * range);
        Gizmos.DrawRay(pos, right * range);
        Gizmos.DrawRay(pos, fwd * range);

        if (coneColor.a > 0.01f)
        {
            Gizmos.color = coneColor;
            Vector3 lEnd = pos + left * range;
            Vector3 rEnd = pos + right * range;
            Vector3 fEnd = pos + fwd * range;
            Gizmos.DrawLine(pos, lEnd);
            Gizmos.DrawLine(pos, rEnd);
            Gizmos.DrawLine(lEnd, fEnd);
            Gizmos.DrawLine(rEnd, fEnd);
        }

        if (detectionProgress > 0f)
        {
            float t = Mathf.Clamp01(detectionProgress / Mathf.Max(
                FromConfig(c => c.detectionTime, detectionTime), 0.01f));
            Color barColor = FromConfig(c => c.detectionProgressColor, Color.yellow);

            Gizmos.color = barColor;
            Vector3 barPos = pos + Vector3.up * 2.5f;
            float barWidth = 1.2f;
            Gizmos.DrawWireCube(barPos, new Vector3(barWidth, 0.08f, 0.08f));
            Gizmos.DrawCube(barPos, new Vector3(barWidth * t, 0.08f, 0.08f));
        }

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 3f,
            $"[{cameraID}] {alertState}{(alertState == CameraAlertState.Detecting ? $" ({detectionProgress:F1}s)" : "")}");
        #endif
    }
}
