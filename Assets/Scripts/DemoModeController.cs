using UnityEngine;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

public class DemoModeController : MonoBehaviour
{
    [Header("Key Shortcuts")]
    [SerializeField] private KeyCode unlockAllKey = KeyCode.F2;
    [SerializeField] private KeyCode teleportKey = KeyCode.F3;
    [SerializeField] private KeyCode toggleAlarmKey = KeyCode.F4;
    [SerializeField] private KeyCode resetMissionKey = KeyCode.F5;
    [SerializeField] private KeyCode completeObjectiveKey = KeyCode.F6;

    [Header("Teleport Targets")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform vaultPoint;

    void Update()
    {
        if (Input.GetKeyDown(unlockAllKey)) UnlockAllCredentials();
        if (Input.GetKeyDown(toggleAlarmKey)) ToggleAlarm();
        if (Input.GetKeyDown(resetMissionKey)) ResetMission();
        if (Input.GetKeyDown(completeObjectiveKey)) ForceCompleteObjective();
    }

    public void UnlockAllCredentials()
    {
        if (CredentialManager.Instance == null)
        {
            Debug.Log("DemoMode: CredentialManager not found.");
            return;
        }

        CredentialManager.Instance.AddCredential("demo_staff", CredentialType.Keycard, UserRole.Staff, "Staff Keycard (DEMO)");
        CredentialManager.Instance.AddCredential("demo_security", CredentialType.Keycard, UserRole.SecurityOfficer, "Security Credential (DEMO)");
        CredentialManager.Instance.AddCredential("demo_admin", CredentialType.SmartCard, UserRole.Administrator, "Admin Credential (DEMO)");

        Debug.Log("DemoMode: All credentials unlocked.");
    }

    public void ToggleAlarm()
    {
        if (SecurityManager.Instance == null) return;

        if (SecurityManager.Instance.currentAlarmLevel == SecurityManager.AlarmLevel.Normal)
        {
            SecurityManager.Instance.SetAlarmLevel(SecurityManager.AlarmLevel.Alert);
            Debug.Log("DemoMode: Alarm set to ALERT.");
        }
        else
        {
            SecurityManager.Instance.ResetAlarm();
            Debug.Log("DemoMode: Alarm reset to NORMAL.");
        }
    }

    public void ResetMission()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.ResetMission();

        if (SecurityManager.Instance != null)
            SecurityManager.Instance.ResetAlarm();

        Debug.Log("DemoMode: Mission reset.");
    }

    public void ForceCompleteObjective()
    {
        if (MissionManager.Instance != null)
        {
            ObjectiveID current = MissionManager.Instance.GetCurrentObjective();
            MissionManager.Instance.CompleteObjective(current);
            Debug.Log($"DemoMode: Force-completed objective '{current}'.");
        }
    }

    public void TeleportTo(Transform target)
    {
        if (target == null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            player.transform.position = target.position;
            player.transform.rotation = target.rotation;

            if (cc != null)
                cc.enabled = true;

            Debug.Log($"DemoMode: Teleported to {target.name}.");
        }
    }
}
