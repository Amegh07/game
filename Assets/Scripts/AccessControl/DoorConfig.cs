using UnityEngine;

namespace MuseumHeist.AccessControl
{
    [CreateAssetMenu(menuName = "Museum Heist/Door Config", fileName = "NewDoorConfig")]
    public class DoorConfig : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Human-readable name shown in UI feedback.")]
        public string doorName = "Security Door";

        [Header("Access Control")]
        [Tooltip("Keycard type required to unlock this door.")]
        public KeycardType requiredKeycard = KeycardType.Staff;

        [Tooltip("If true, the door starts in the Locked state.")]
        public bool startsLocked = true;

        [Header("Movement")]
        [Tooltip("Rotation speed in degrees per second when opening (transform animation).")]
        public float openSpeed = 120f;

        [Tooltip("Rotation speed in degrees per second when closing (transform animation).")]
        public float closeSpeed = 120f;

        [Tooltip("Total rotation angle in degrees when fully opened.")]
        public float openAngle = 90f;

        [Header("Auto Close")]
        [Tooltip("If true, the door automatically closes after the delay.")]
        public bool canAutoClose = true;

        [Tooltip("Seconds before the door auto-closes after opening.")]
        public float autoCloseDelay = 3f;

        [Header("Security Response")]
        [Tooltip("Lock this door when SecurityManager raises Suspicious level.")]
        public bool lockOnSuspicious = false;

        [Tooltip("Lock this door when SecurityManager raises Alert level.")]
        public bool lockOnAlert = false;

        [Tooltip("Lock this door when SecurityManager raises Lockdown level.")]
        public bool lockDuringLockdown = true;

        [Tooltip("If true, the door unlocks during lockdown (overrides lockDuringLockdown).")]
        public bool isEmergencyExit = false;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip lockSound;
        public AudioClip unlockSound;
        public AudioClip accessDeniedSound;

        [Header("Animation")]
        [Tooltip("Optional Animator Controller. When assigned, triggers drive the visual instead of transform animation.")]
        public RuntimeAnimatorController animatorController;
        public string openTrigger = "Open";
        public string closeTrigger = "Close";

        [Header("Debug")]
        [Tooltip("Color of the wireframe gizmo drawn in the Scene view.")]
        public Color debugColor = new Color(0f, 0.5f, 1f, 0.3f);
    }
}
