using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    [CreateAssetMenu(menuName = "Museum Heist/Cyber/Permission Set", fileName = "NewPermissionSet")]
    public class PermissionSet : ScriptableObject
    {
        [SerializeField] private List<string> permissions = new();

        public IReadOnlyList<string> Permissions => permissions;

        public bool HasPermission(string permission)
        {
            return permissions.Contains(permission);
        }

        public void AddPermission(string permission)
        {
            if (!permissions.Contains(permission))
                permissions.Add(permission);
        }

        public void RemovePermission(string permission)
        {
            permissions.Remove(permission);
        }
    }
}
