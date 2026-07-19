using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class AuthenticationService : MonoBehaviour
    {
        public static AuthenticationService Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public AuthenticationResult Authenticate(string credentialID)
        {
            if (CredentialManager.Instance == null)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "Credential system unavailable."
                };
            }

            if (string.IsNullOrEmpty(credentialID))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = "No credential provided."
                };
            }

            if (CredentialManager.Instance.TryGetCredential(credentialID, out StoredCredential credential))
            {
                return new AuthenticationResult
                {
                    Success = true,
                    Role = credential.GrantedRole,
                    UserName = credential.DisplayName,
                    CredentialID = credential.CredentialID
                };
            }

            return new AuthenticationResult
            {
                Success = false,
                ErrorMessage = $"Authentication failed: '{credentialID}' not recognized."
            };
        }
    }

    public struct AuthenticationResult
    {
        public bool Success;
        public UserRole Role;
        public string UserName;
        public string CredentialID;
        public string ErrorMessage;
    }
}
