using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    [CreateAssetMenu(menuName = "Museum Heist/Cyber/Role Permissions Config", fileName = "RolePermissions")]
    public class RolePermissionsConfig : ScriptableObject
    {
        [System.Serializable]
        public struct RoleEntry
        {
            public UserRole role;
            public PermissionSet permissionSet;
        }

        [SerializeField] private List<RoleEntry> rolePermissions = new();

        private Dictionary<UserRole, PermissionSet> lookup;

        private void BuildLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<UserRole, PermissionSet>();
            foreach (var entry in rolePermissions)
            {
                if (entry.permissionSet != null && !lookup.ContainsKey(entry.role))
                {
                    lookup[entry.role] = entry.permissionSet;
                }
            }
        }

        public PermissionSet GetPermissionSet(UserRole role)
        {
            BuildLookup();
            lookup.TryGetValue(role, out PermissionSet set);
            return set;
        }

        public bool RoleHasPermission(UserRole role, string permission)
        {
            var set = GetPermissionSet(role);
            return set != null && set.HasPermission(permission);
        }
    }
}
