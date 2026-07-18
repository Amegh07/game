using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class AuthorizationService : MonoBehaviour
    {
        public static AuthorizationService Instance { get; private set; }

        [SerializeField] private RolePermissionsConfig defaultRoleConfig;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public AuthorizationResult Authorize(UserRole role, string terminalID, UserRole minimumRole)
        {
            if (role < minimumRole)
            {
                return new AuthorizationResult
                {
                    Authorized = false,
                    ErrorMessage = $"Access denied. Role '{role}' insufficient. Requires '{minimumRole}' or higher."
                };
            }

            HashSet<string> permissions = BuildPermissionSet(role);

            return new AuthorizationResult
            {
                Authorized = true,
                Role = role,
                Permissions = permissions
            };
        }

        private HashSet<string> BuildPermissionSet(UserRole role)
        {
            HashSet<string> result = new();

            if (defaultRoleConfig != null)
            {
                var set = defaultRoleConfig.GetPermissionSet(role);
                if (set != null)
                {
                    foreach (var perm in set.Permissions)
                    {
                        result.Add(perm);
                    }
                }
            }

            return result;
        }

        public void SetRoleConfig(RolePermissionsConfig config)
        {
            defaultRoleConfig = config;
        }
    }

    public struct AuthorizationResult
    {
        public bool Authorized;
        public UserRole Role;
        public HashSet<string> Permissions;
        public string ErrorMessage;
    }
}
