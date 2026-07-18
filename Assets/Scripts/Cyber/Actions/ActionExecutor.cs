using System.Collections.Generic;

namespace MuseumHeist.Cyber
{
    public static class ActionExecutor
    {
        private static readonly Dictionary<string, ITerminalAction> registry = new();
        private static bool initialized;

        private static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            RegisterInternal("DisableCamera", new DisableCameraAction());
            RegisterInternal("UnlockDoor", new UnlockDoorAction());
            RegisterInternal("ResetAlarm", new ResetAlarmAction());
            RegisterInternal("OpenVault", new OpenVaultAction());
            RegisterInternal("EmergencyLockdown", new EmergencyLockdownAction());
            RegisterInternal("ActivateMaintenanceMode", new ActivateMaintenanceModeAction());
            RegisterInternal("DownloadLogs", new DownloadLogsAction());
            RegisterInternal("ShutdownSecurity", new ShutdownSecurityAction());
        }

        private static void RegisterInternal(string type, ITerminalAction action)
        {
            registry[type] = action;
        }

        public static void RegisterAction(string type, ITerminalAction action)
        {
            registry[type] = action;
        }

        public static bool ExecuteAction(
            TerminalActionEntry entry,
            NetworkSession session,
            out string resultMessage)
        {
            EnsureInitialized();

            if (registry.TryGetValue(entry.actionType, out ITerminalAction action))
            {
                var context = new TerminalActionContext
                {
                    TargetID = entry.targetID,
                    Session = session
                };

                return action.Execute(context, out resultMessage);
            }

            resultMessage = $"Unknown action type: '{entry.actionType}'.";
            return false;
        }
    }
}
