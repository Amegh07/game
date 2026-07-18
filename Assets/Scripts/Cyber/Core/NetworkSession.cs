using System;
using System.Collections.Generic;

namespace MuseumHeist.Cyber
{
    public class NetworkSession
    {
        public string UserName { get; set; }
        public UserRole Role { get; set; }
        public bool IsAuthenticated { get; set; }
        public string ConnectedTerminalID { get; set; }
        public HashSet<string> Permissions { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsActive { get; set; } = true;

        public NetworkSession()
        {
            Permissions = new HashSet<string>();
            StartTime = DateTime.UtcNow;
        }

        public bool HasPermission(string permission)
        {
            return Permissions != null && Permissions.Contains(permission);
        }
    }
}
