using UnityEngine;

public class EscapePhaseController : MonoBehaviour
{
    [Header("Escape Settings")]
    [SerializeField] private Transform exitTriggerZone;
    [SerializeField] private float exitTriggerRadius = 3f;
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private AudioClip alarmLoopClip;
    [SerializeField] private GameObject[] emergencyLights;

    private bool escapePhaseActive;
    private bool hasExited;
    private Transform cachedPlayerTransform;
    private bool eventsSubscribed = false;

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
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
            eventsSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!eventsSubscribed) return;
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
        }
        eventsSubscribed = false;
    }

    void Start()
    {
        SubscribeToEvents();
        CachePlayerReference();
    }

    private void CachePlayerReference()
    {
        if (cachedPlayerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                cachedPlayerTransform = player.transform;
        }
    }

    private void HandlePhaseChanged(MissionPhase phase)
    {
        if (phase == MissionPhase.Escape && !escapePhaseActive)
        {
            ActivateEscapePhase();
        }
    }

    private void HandleObjectiveCompleted(ObjectiveID id)
    {
        if (id == ObjectiveID.EscapeMuseum && !hasExited)
        {
            CompleteMission();
        }
    }

    private void ActivateEscapePhase()
    {
        escapePhaseActive = true;

        if (alarmAudioSource != null && alarmLoopClip != null)
        {
            alarmAudioSource.clip = alarmLoopClip;
            alarmAudioSource.loop = true;
            alarmAudioSource.Play();
        }

        if (emergencyLights != null)
        {
            foreach (var light in emergencyLights)
                if (light != null) light.SetActive(true);
        }

        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.ReportTrigger(SecurityManager.SecurityTrigger.PlayerDetected, "EscapePhase");
        }

        Debug.Log("EscapePhase: Escape phase activated. Alarm raised. Exit unlocked.");
    }

    void Update()
    {
        if (!escapePhaseActive || hasExited) return;

        if (cachedPlayerTransform == null)
        {
            CachePlayerReference();
            if (cachedPlayerTransform == null) return;
        }

        if (exitTriggerZone == null) return;

        float dist = Vector3.Distance(cachedPlayerTransform.position, exitTriggerZone.position);

        if (dist < exitTriggerRadius)
        {
            OnPlayerExited();
        }
    }

    private void OnPlayerExited()
    {
        if (hasExited) return;
        hasExited = true;

        MissionManager.Instance?.CompleteObjective(ObjectiveID.EscapeMuseum);

        if (MissionManager.Instance != null &&
            MissionManager.Instance.GetCurrentObjective() == ObjectiveID.HeistComplete)
        {
            MissionManager.Instance?.CompleteObjective(ObjectiveID.HeistComplete);
        }

        Debug.Log("EscapePhase: Player has escaped! Mission complete.");
    }

    private void CompleteMission()
    {
        if (alarmAudioSource != null && alarmAudioSource.isPlaying)
            alarmAudioSource.Stop();
    }

    void OnDrawGizmos()
    {
        if (exitTriggerZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(exitTriggerZone.position, exitTriggerRadius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                exitTriggerZone.position + Vector3.up * 1.5f,
                "EXIT ZONE\nReach to escape");
#endif
        }
    }
}
