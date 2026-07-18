using UnityEngine;

namespace MuseumHeist.AccessControl
{
    public class KeycardItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private KeycardType keycardType = KeycardType.Staff;
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

        public void Interact()
        {
            if (collected) return;
            collected = true;

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddKeycard(keycardType);
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
    }
}
