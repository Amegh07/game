using System;

namespace MuseumHeist.Cyber
{
    public struct TerminalLogEntry
    {
        public DateTime Timestamp;
        public string UserName;
        public UserRole Role;
        public string Action;
        public string Result;
        public bool Success;

        public TerminalLogEntry(string userName, UserRole role, string action, string result, bool success)
        {
            Timestamp = DateTime.UtcNow;
            UserName = userName;
            Role = role;
            Action = action;
            Result = result;
            Success = success;
        }

        public string FormattedTime => Timestamp.ToLocalTime().ToString("HH:mm:ss");

        public override string ToString()
        {
            return $"[{FormattedTime}] {UserName} ({Role}): {Action} — {Result}";
        }
    }
}
