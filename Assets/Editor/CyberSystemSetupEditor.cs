using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using MuseumHeist.Cyber;
using MuseumHeist.AccessControl;

public class CyberSystemSetupEditor
{
    private const string CONFIG_DIR = "Assets/CyberConfigs";

    [MenuItem("Tools/Museum Heist/Cyber/Create Example Assets")]
    static void CreateExampleAssets()
    {
        Directory.CreateDirectory(CONFIG_DIR);
        Directory.CreateDirectory(CONFIG_DIR + "/Terminals");
        Directory.CreateDirectory(CONFIG_DIR + "/Permissions");
        Directory.CreateDirectory(CONFIG_DIR + "/Roles");

        CreatePermissionSets();
        CreateRoleConfig();
        CreateTerminalConfigs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"CyberSystemSetup: Created example assets in {CONFIG_DIR}/");
    }

    static void CreatePermissionSets()
    {
        CreatePermissionSet("StaffPermissions", Permissions.ViewCameras, Permissions.OpenStaffDoors);

        CreatePermissionSet("SecurityPermissions",
            Permissions.ViewCameras,
            Permissions.DisableCameras,
            Permissions.OpenStaffDoors,
            Permissions.OpenSecurityDoors,
            Permissions.ResetAlarm,
            Permissions.ViewLogs);

        CreatePermissionSet("AdminPermissions",
            Permissions.ViewCameras,
            Permissions.DisableCameras,
            Permissions.OpenStaffDoors,
            Permissions.OpenSecurityDoors,
            Permissions.OpenVault,
            Permissions.ResetAlarm,
            Permissions.ViewLogs,
            Permissions.DownloadLogs,
            Permissions.EnableMaintenanceMode,
            Permissions.ControlVault,
            Permissions.ShutdownSecurity,
            Permissions.EmergencyLockdown);

        CreatePermissionSet("VaultPermissions",
            Permissions.ControlVault,
            Permissions.OpenVault,
            Permissions.ViewCameras);

        CreatePermissionSet("MaintenancePermissions",
            Permissions.EnableMaintenanceMode,
            Permissions.OpenStaffDoors,
            Permissions.ViewLogs);

        CreatePermissionSet("EmptyPermissions");
    }

    static void CreateRoleConfig()
    {
        string path = CONFIG_DIR + "/Roles/MuseumRoleConfig.asset";
        RolePermissionsConfig existing = AssetDatabase.LoadAssetAtPath<RolePermissionsConfig>(path);
        if (existing != null) return;

        var config = ScriptableObject.CreateInstance<RolePermissionsConfig>();

        string setDir = CONFIG_DIR + "/Permissions";
        SetRoleEntry(config, UserRole.Guest, LoadOrCreateEmpty(setDir + "/EmptyPermissions.asset"));
        SetRoleEntry(config, UserRole.Maintenance, AssetDatabase.LoadAssetAtPath<PermissionSet>(setDir + "/MaintenancePermissions.asset"));
        SetRoleEntry(config, UserRole.Staff, AssetDatabase.LoadAssetAtPath<PermissionSet>(setDir + "/StaffPermissions.asset"));
        SetRoleEntry(config, UserRole.Curator, AssetDatabase.LoadAssetAtPath<PermissionSet>(setDir + "/StaffPermissions.asset"));
        SetRoleEntry(config, UserRole.SecurityOfficer, AssetDatabase.LoadAssetAtPath<PermissionSet>(setDir + "/SecurityPermissions.asset"));
        SetRoleEntry(config, UserRole.Administrator, AssetDatabase.LoadAssetAtPath<PermissionSet>(setDir + "/AdminPermissions.asset"));

        AssetDatabase.CreateAsset(config, path);
        Debug.Log($"Created: {path}");
    }

    static void SetRoleEntry(RolePermissionsConfig config, UserRole role, PermissionSet set)
    {
        var field = config.GetType().GetField("rolePermissions",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field == null) return;

        var list = field.GetValue(config) as System.Collections.IList;
        if (list == null) return;

        var entryType = config.GetType().GetNestedType("RoleEntry",
            System.Reflection.BindingFlags.NonPublic);
        if (entryType == null) return;

        var entry = System.Activator.CreateInstance(entryType);
        var roleField = entryType.GetField("role");
        var permField = entryType.GetField("permissionSet");
        if (roleField != null) roleField.SetValue(entry, role);
        if (permField != null) permField.SetValue(entry, set);

        list.Add(entry);
    }

    static PermissionSet LoadOrCreateEmpty(string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<PermissionSet>(path);
        if (existing != null) return existing;

        var ps = ScriptableObject.CreateInstance<PermissionSet>();
        AssetDatabase.CreateAsset(ps, path);
        return ps;
    }

    static void CreatePermissionSet(string name, params string[] perms)
    {
        string path = CONFIG_DIR + "/Permissions/" + name + ".asset";
        PermissionSet existing = AssetDatabase.LoadAssetAtPath<PermissionSet>(path);
        if (existing != null) return;

        var ps = ScriptableObject.CreateInstance<PermissionSet>();
        foreach (var p in perms)
            ps.AddPermission(p);
        AssetDatabase.CreateAsset(ps, path);
        Debug.Log($"Created: {path}");
    }

    static void CreateTerminalConfigs()
    {
        CreateTerminalConfig("CameraConsole", "Camera Control Station",
            CredentialType.Keycard, UserRole.SecurityOfficer,
            "DISABLE CAMERA NETWORK — AUTHORIZED PERSONNEL ONLY",
            new Color(0f, 0.5f, 1f),
            ("DisableCamera", "Disable Camera", "Disable security camera feed.", Permissions.DisableCameras, "camera_security"));

        CreateTerminalConfig("SecurityTerminal", "Security Operations Terminal",
            CredentialType.Keycard, UserRole.SecurityOfficer,
            "CLASSIFIED — SECURITY OPERATIONS",
            new Color(0.9f, 0.3f, 0.1f),
            ("UnlockDoor", "Unlock Door", "Unlock a security door.", Permissions.OpenSecurityDoors, "door_security"),
            ("ResetAlarm", "Reset Alarm", "Reset alarm to normal.", Permissions.ResetAlarm, ""),
            ("EmergencyLockdown", "Emergency Lockdown", "Lock down facility.", Permissions.EmergencyLockdown, ""));

        CreateTerminalConfig("StaffTerminal", "Staff Terminal",
            CredentialType.Keycard, UserRole.Staff,
            "STAFF ACCESS ONLY",
            new Color(0.2f, 0.6f, 0.2f),
            ("UnlockDoor", "Unlock Door", "Unlock a staff door.", Permissions.OpenStaffDoors, "door_staff"));

        CreateTerminalConfig("VaultTerminal", "Vault Control Terminal",
            CredentialType.Keycard, UserRole.Administrator,
            "MAXIMUM SECURITY — VAULT CONTROL",
            new Color(1f, 0.6f, 0f),
            ("OpenVault", "Open Vault", "Override vault security.", Permissions.OpenVault, "door_vault"),
            ("DownloadLogs", "Download Audit Log", "Export vault audit trail.", Permissions.DownloadLogs, ""));

        CreateTerminalConfig("AdminConsole", "Administrator Console",
            CredentialType.Keycard, UserRole.Administrator,
            "ADMINISTRATOR ACCESS — FULL SYSTEM CONTROL",
            new Color(0.8f, 0.2f, 0.6f),
            ("ShutdownSecurity", "Shutdown Security", "Shut down security system.", Permissions.ShutdownSecurity, ""),
            ("ResetAlarm", "Reset Alarm", "Reset alarm to normal.", Permissions.ResetAlarm, ""),
            ("DownloadLogs", "Download Logs", "Export all system logs.", Permissions.DownloadLogs, ""),
            ("ActivateMaintenanceMode", "Maintenance Mode", "Enable maintenance mode.", Permissions.EnableMaintenanceMode, ""));

        CreateTerminalConfig("MaintenanceTerminal", "Maintenance Terminal",
            CredentialType.Keycard, UserRole.Maintenance,
            "MAINTENANCE ACCESS — RESTRICTED AREA",
            new Color(0.6f, 0.4f, 0.2f),
            ("ActivateMaintenanceMode", "Maintenance Mode", "Enable maintenance mode.", Permissions.EnableMaintenanceMode, ""),
            ("UnlockDoor", "Unlock Door", "Unlock maintenance area.", Permissions.OpenStaffDoors, "door_maintenance"));
    }

    static void CreateTerminalConfig(
        string fileName,
        string terminalName,
        CredentialType credType,
        UserRole minRole,
        string accessLabel,
        Color themeColor,
        params (string type, string name, string desc, string perm, string target)[] actions)
    {
        string path = CONFIG_DIR + "/Terminals/" + fileName + ".asset";
        TerminalConfig existing = AssetDatabase.LoadAssetAtPath<TerminalConfig>(path);
        if (existing != null) return;

        var config = ScriptableObject.CreateInstance<TerminalConfig>();
        config.terminalName = terminalName;
        config.terminalID = fileName;
        config.requiredCredentialType = credType;
        config.minimumRole = minRole;
        config.accessLevelLabel = accessLabel;
        config.themeColor = themeColor;

        foreach (var a in actions)
        {
            config.actions.Add(new TerminalActionEntry
            {
                actionType = a.type,
                displayName = a.name,
                description = a.desc,
                requiredPermission = a.perm,
                targetID = a.target
            });
        }

        AssetDatabase.CreateAsset(config, path);
        Debug.Log($"Created: {path}");
    }

    [MenuItem("Tools/Museum Heist/Cyber/Create Example Assets", true)]
    static bool ValidateCreate()
    {
        return Application.isPlaying == false;
    }
}
