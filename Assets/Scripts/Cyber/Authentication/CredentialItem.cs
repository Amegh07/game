using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class CredentialItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string credentialID = "";
        [SerializeField] private CredentialType credentialType = CredentialType.Keycard;
        [SerializeField] private UserRole grantedRole = UserRole.Staff;
        [SerializeField] private string displayName = "Staff Keycard";

        public void SetCredential(string id, CredentialType type, UserRole role, string name)
        {
            credentialID = id; credentialType = type; grantedRole = role; displayName = name;
        }

        [SerializeField] private float rotationSpeed = 80f;
        [SerializeField] private bool destroyOnPickup = true;

        private bool collected;

        void Update()
        {
            if (collected) return;
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        public void Interact(PlayerController player)
        {
            if (!CanInteract(player)) return;
            collected = true;

            if (CredentialManager.Instance != null)
            {
                CredentialManager.Instance.AddCredential(credentialID, credentialType, grantedRole, displayName);
            }

            if (destroyOnPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        public bool CanInteract(PlayerController player)
        {
            return !collected;
        }

        public string GetInteractionPrompt()
        {
            return $"Pick Up {displayName}";
        }

        public void OnFocus() { }
        public void OnLoseFocus() { }
    }
}
