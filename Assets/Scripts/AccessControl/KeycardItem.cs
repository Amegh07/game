using UnityEngine;

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

        private Vector3 startPosition;
        private bool collected;

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
