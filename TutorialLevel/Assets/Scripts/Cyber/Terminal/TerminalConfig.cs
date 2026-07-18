using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    [CreateAssetMenu(menuName = "Museum Heist/Cyber/Terminal Config", fileName = "NewTerminalConfig")]
    public class TerminalConfig : ScriptableObject
    {
        [Header("Identification")]
        public string terminalName = "Security Terminal";
        public string terminalID = "";

        [Header("Access")]
        public CredentialType requiredCredentialType = CredentialType.Keycard;
        public UserRole minimumRole = UserRole.Staff;

        [Header("Available Actions")]
        public List<TerminalActionEntry> actions = new();

        [Header("Connected Systems")]
        public List<string> connectedCameraIDs = new();
        public List<string> connectedDoorIDs = new();

        [Header("Appearance")]
        public Color themeColor = new Color(0f, 0.5f, 1f);
        public string accessLevelLabel = "CLASSIFIED";

        [Header("Audio")]
        public AudioClip connectSound;
        public AudioClip disconnectSound;
        public AudioClip authenticateSound;
        public AudioClip actionSound;

        [Header("Animation")]
        public RuntimeAnimatorController animatorController;
        public string interactTrigger = "Interact";
    }

    [System.Serializable]
    public class TerminalActionEntry
    {
        public string actionType = "DisableCamera";
        public string displayName = "Disable Camera";
        public string description = "Disable a connected security camera.";
        public string requiredPermission = "";
        public string targetID = "";
    }
}
