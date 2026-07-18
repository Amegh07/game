using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class CredentialItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string credentialID = "";
        [SerializeField] private CredentialType credentialType = CredentialType.Keycard;
        [SerializeField] private UserRole grantedRole = UserRole.Staff;
        [SerializeField] private string displayName = "Staff Keycard";

        [SerializeField] private float rotationSpeed = 80f;
        [SerializeField] private bool destroyOnPickup = true;

        private bool collected;

        void Update()
        {
            if (collected) return;
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        public void Interact()
        {
            if (collected) return;
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
    }
}
