using UnityEngine;
using System.Collections.Generic;

namespace MuseumHeist.AccessControl
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        private readonly HashSet<KeycardType> keycards = new();

        public event System.Action<KeycardType> OnKeycardAdded;
        public event System.Action<KeycardType> OnKeycardRemoved;
        public event System.Action OnInventoryCleared;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddKeycard(KeycardType type)
        {
            if (keycards.Add(type))
            {
                OnKeycardAdded?.Invoke(type);
            }
        }

        public bool HasKeycard(KeycardType type)
        {
            return keycards.Contains(type);
        }

        public void RemoveKeycard(KeycardType type)
        {
            if (keycards.Remove(type))
            {
                OnKeycardRemoved?.Invoke(type);
            }
        }

        public void Clear()
        {
            keycards.Clear();
            OnInventoryCleared?.Invoke();
        }

        public int Count => keycards.Count;
    }
}
