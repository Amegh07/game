using UnityEngine;
using MuseumHeist.Cyber;

namespace MuseumHeist.AccessControl
{
    public class KeycardItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private KeycardType keycardType = KeycardType.Staff;
        public KeycardType Type => keycardType;
        public void SetKeycardType(KeycardType type) { keycardType = type; }
        [SerializeField] private string targetDoorID = "";
        [SerializeField] private float rotationSpeed = 80f;
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float floatHeight = 0.2f;
        [SerializeField] private bool destroyOnPickup = true;

        [Header("Authentication Bridge")]
        [SerializeField] private bool grantsCredential;
        [SerializeField] private string credentialID = "";
        [SerializeField] private CredentialType credentialType = CredentialType.Keycard;
        [SerializeField] private UserRole credentialRole = UserRole.Staff;
        [SerializeField] private string credentialDisplayName = "Staff Credential";

        private Vector3 startPosition;
        private bool collected;

        // Keeps the physical keycard and the cyber credential as one pickup in training
        // and in levels that intentionally use a card for both systems.
        public void ConfigureCredentialGrant(string id, CredentialType type, UserRole role, string displayName)
        {
            grantsCredential = true;
            credentialID = id;
            credentialType = type;
            credentialRole = role;
            credentialDisplayName = displayName;
        }

        void Start()
        {
            startPosition = transform.position;
        }

        void Update()
        {
            if (collected) return;

            Vector3 pos = startPosition;
            pos.y += Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = pos;

            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;
            collected = true;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddKeycard(keycardType);
            }

            if (grantsCredential && CredentialManager.Instance != null && !string.IsNullOrEmpty(credentialID))
            {
                CredentialManager.Instance.AddCredential(
                    credentialID, credentialType, credentialRole, credentialDisplayName);
            }

            if (!string.IsNullOrEmpty(targetDoorID) && SecurityManager.Instance != null)
            {
                bool unlocked = SecurityManager.Instance.UnlockDoor(targetDoorID);
                if (!unlocked)
                    SecurityManager.Instance.RequestDoorUnlock(targetDoorID);
            }

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public bool CanInteract(PlayerController player)
        {
            return !collected;
        }

        public string GetInteractionPrompt()
        {
            return $"Pick Up {keycardType} Keycard";
        }

        public void OnFocus() { }
        public void OnLoseFocus() { }
    }
}