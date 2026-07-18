using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class CredentialManager : MonoBehaviour
    {
        public static CredentialManager Instance { get; private set; }

        private readonly Dictionary<string, StoredCredential> credentials = new();

        public event System.Action<StoredCredential> OnCredentialAdded;
        public event System.Action<string> OnCredentialRemoved;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddCredential(string credentialID, CredentialType type, UserRole grantedRole, string displayName)
        {
            var cred = new StoredCredential(credentialID, type, grantedRole, displayName);
            if (credentials.TryAdd(credentialID, cred))
            {
                OnCredentialAdded?.Invoke(cred);
            }
        }

        public bool HasCredential(string credentialID)
        {
            return credentials.ContainsKey(credentialID);
        }

        public bool TryGetCredential(string credentialID, out StoredCredential credential)
        {
            return credentials.TryGetValue(credentialID, out credential);
        }

        public void RemoveCredential(string credentialID)
        {
            if (credentials.Remove(credentialID))
            {
                OnCredentialRemoved?.Invoke(credentialID);
            }
        }

        public IReadOnlyCollection<StoredCredential> GetAllCredentials()
        {
            return credentials.Values;
        }

        public void Clear()
        {
            credentials.Clear();
        }

        public int Count => credentials.Count;
    }

    public struct StoredCredential
    {
        public readonly string CredentialID;
        public readonly CredentialType Type;
        public readonly UserRole GrantedRole;
        public readonly string DisplayName;

        public StoredCredential(string id, CredentialType type, UserRole role, string displayName)
        {
            CredentialID = id;
            Type = type;
            GrantedRole = role;
            DisplayName = displayName;
        }
    }
}
