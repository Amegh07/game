using UnityEngine;
using MuseumHeist.AccessControl;

public class MuseumMissionBridge : MonoBehaviour
{
    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnKeycardAdded += HandleKeycardAdded;

        if (MuseumHeist.Cyber.CredentialManager.Instance != null)
            MuseumHeist.Cyber.CredentialManager.Instance.OnCredentialAdded += HandleCredentialAdded;

        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.OnDoorUnlocked += HandleDoorUnlocked;
            SecurityManager.Instance.OnCameraDisabled += HandleCameraDisabled;
            SecurityManager.Instance.OnTriggerReported += HandleSecurityTrigger;
        }
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnKeycardAdded -= HandleKeycardAdded;

        if (MuseumHeist.Cyber.CredentialManager.Instance != null)
            MuseumHeist.Cyber.CredentialManager.Instance.OnCredentialAdded -= HandleCredentialAdded;

        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.OnDoorUnlocked -= HandleDoorUnlocked;
            SecurityManager.Instance.OnCameraDisabled -= HandleCameraDisabled;
            SecurityManager.Instance.OnTriggerReported -= HandleSecurityTrigger;
        }
    }

    void HandleKeycardAdded(KeycardType type)
    {
        switch (type)
        {
            case KeycardType.Staff:
                TryComplete(ObjectiveID.FindStaffCredential);
                break;
            case KeycardType.Vault:
                TryComplete(ObjectiveID.UnlockVaultArea);
                break;
        }
    }

    void HandleCredentialAdded(MuseumHeist.Cyber.StoredCredential cred)
    {
        if (cred.GrantedRole == MuseumHeist.Cyber.UserRole.SecurityOfficer)
            TryComplete(ObjectiveID.FindSecurityCredential);
    }

    void HandleDoorUnlocked(string doorID)
    {
        switch (doorID)
        {
            case "door_staff_office":
                TryComplete(ObjectiveID.AccessStaffOffice);
                break;
            case "door_vault_corridor":
                TryComplete(ObjectiveID.UseSecurityTerminal);
                break;
            case "door_main_vault":
                TryComplete(ObjectiveID.UseVaultTerminal);
                TryComplete(ObjectiveID.UnlockVault);
                break;
        }
    }

    void HandleCameraDisabled(string cameraID)
    {
        if (cameraID == "camera_east" || cameraID == "camera_west")
            TryComplete(ObjectiveID.DisableEastCameras);
    }

    void HandleSecurityTrigger(SecurityManager.SecurityTrigger trigger, string source, SecurityManager.AlarmLevel level)
    {
    }

    void TryComplete(ObjectiveID id)
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.CompleteObjectiveByEvent(id);
    }
}
