using UnityEngine;

public class EscapePhaseController : MonoBehaviour
{
    [Header("Escape Settings")]
    [SerializeField] private Transform exitTriggerZone;
    [SerializeField] private string exitDoorID = "door_emergency_exit";
    [SerializeField] private float exitTriggerRadius = 3f;
    [SerializeField] private AudioSource alarmAudioSource;
    [SerializeField] private AudioClip alarmLoopClip;
    [SerializeField] private GameObject[] emergencyLights;

    private bool escapePhaseActive;
    private bool hasExited;

    void OnEnable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
        }
    }

    void OnDisable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
            MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
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
            SecurityManager.Instance.SetAlarmLevel(SecurityManager.AlarmLevel.Alert);
            SecurityManager.Instance.RequestDoorUnlock(exitDoorID);
        }

        Debug.Log("EscapePhase: Escape phase activated. Alarm raised. Exit unlocked.");
    }

    void Update()
    {
        if (!escapePhaseActive || hasExited) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(player.transform.position, exitTriggerZone != null
            ? exitTriggerZone.position
            : Vector3.zero);

        if (exitTriggerZone != null && dist < exitTriggerRadius)
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
