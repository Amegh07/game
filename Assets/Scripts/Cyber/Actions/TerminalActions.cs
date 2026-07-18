namespace MuseumHeist.Cyber
{
    public class DisableCameraAction : ITerminalAction
    {
        public string ActionName => "Disable Camera";
        public string Description => "Disable a connected security camera.";
        public string RequiredPermission => Permissions.DisableCameras;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            if (SecurityManager.Instance == null)
            {
                resultMessage = "Security system unavailable.";
                return false;
            }

            if (SecurityManager.Instance.DisableCamera(context.TargetID))
            {
                resultMessage = $"Camera '{context.TargetID}' disabled successfully.";
                return true;
            }

            resultMessage = $"Failed: Camera '{context.TargetID}' not found.";
            return false;
        }
    }

    public class UnlockDoorAction : ITerminalAction
    {
        public string ActionName => "Unlock Door";
        public string Description => "Unlock a connected security door.";
        public string RequiredPermission => Permissions.OpenSecurityDoors;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            if (SecurityManager.Instance == null)
            {
                resultMessage = "Security system unavailable.";
                return false;
            }

            SecurityManager.Instance.RequestDoorUnlock(context.TargetID);
            resultMessage = $"Door '{context.TargetID}' unlock requested.";
            return true;
        }
    }

    public class ResetAlarmAction : ITerminalAction
    {
        public string ActionName => "Reset Alarm";
        public string Description => "Reset the security alarm to normal state.";
        public string RequiredPermission => Permissions.ResetAlarm;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            if (SecurityManager.Instance == null)
            {
                resultMessage = "Security system unavailable.";
                return false;
            }

            SecurityManager.Instance.ResetAlarm();
            resultMessage = "Alarm reset to normal.";
            return true;
        }
    }

    public class OpenVaultAction : ITerminalAction
    {
        public string ActionName => "Open Vault";
        public string Description => "Override vault door security.";
        public string RequiredPermission => Permissions.OpenVault;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            if (SecurityManager.Instance == null)
            {
                resultMessage = "Security system unavailable.";
                return false;
            }

            SecurityManager.Instance.RequestDoorUnlock(context.TargetID);
            resultMessage = $"Vault door '{context.TargetID}' unlock requested.";
            return true;
        }
    }

    public class EmergencyLockdownAction : ITerminalAction
    {
        public string ActionName => "Emergency Lockdown";
        public string Description => "Initiate full facility lockdown.";
        public string RequiredPermission => Permissions.EmergencyLockdown;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            if (SecurityManager.Instance == null)
            {
                resultMessage = "Security system unavailable.";
                return false;
            }

            SecurityManager.Instance.SetAlarmLevel(SecurityManager.AlarmLevel.Lockdown);
            resultMessage = "Emergency lockdown initiated.";
            return true;
        }
    }

    public class ActivateMaintenanceModeAction : ITerminalAction
    {
        public string ActionName => "Maintenance Mode";
        public string Description => "Enable security maintenance mode.";
        public string RequiredPermission => Permissions.EnableMaintenanceMode;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            if (SecurityManager.Instance == null)
            {
                resultMessage = "Security system unavailable.";
                return false;
            }

            SecurityManager.Instance.SetAlarmLevel(SecurityManager.AlarmLevel.Suspicious);
            resultMessage = "Maintenance mode activated. Security level: Suspicious.";
            return true;
        }
    }

    public class DownloadLogsAction : ITerminalAction
    {
        public string ActionName => "Download Logs";
        public string Description => "Export audit log data.";
        public string RequiredPermission => Permissions.DownloadLogs;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            resultMessage = "Audit logs downloaded successfully. (Simulated)";
            return true;
        }
    }

    public class ShutdownSecurityAction : ITerminalAction
    {
        public string ActionName => "Shutdown Security";
        public string Description => "Shut down the entire security system.";
        public string RequiredPermission => Permissions.ShutdownSecurity;

        public bool Execute(TerminalActionContext context, out string resultMessage)
        {
            if (SecurityManager.Instance == null)
            {
                resultMessage = "Security system unavailable.";
                return false;
            }

            SecurityManager.Instance.ResetAlarm();

            SecurityManager.Instance.LockAllDoors();

            resultMessage = "Security system shutdown initiated. All doors locked. Alarm reset.";
            return true;
        }
    }
}
