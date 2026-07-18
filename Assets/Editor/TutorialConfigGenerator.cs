using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

public static class TutorialConfigGenerator
{
    private const string CONFIG_DIR = "Assets/Config/Tutorial";

    [MenuItem("Tools/Tutorial/Generate Config Assets")]
    static void GenerateAll()
    {
        Directory.CreateDirectory(CONFIG_DIR + "/Doors");
        Directory.CreateDirectory(CONFIG_DIR + "/Cameras");
        Directory.CreateDirectory(CONFIG_DIR + "/Terminals");
        Directory.CreateDirectory(CONFIG_DIR + "/Roles");
        Directory.CreateDirectory(CONFIG_DIR + "/Permissions");

        CreateDoorConfig();
        CreateCameraConfig();
        CreateTerminalConfig();
        CreateRoleAndPermissionConfigs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"TutorialConfigGenerator: All configs created in {CONFIG_DIR}/");
    }

    static void CreateDoorConfig()
    {
        string path = CONFIG_DIR + "/Doors/TutorialDoor.asset";
        if (AssetDatabase.LoadAssetAtPath<DoorConfig>(path) != null) return;

        var cfg = ScriptableObject.CreateInstance<DoorConfig>();
        cfg.doorName = "Training Door";
        cfg.requiredKeycard = KeycardType.Public;
        cfg.startsLocked = true;
        cfg.canAutoClose = false;
        cfg.lockOnAlert = false;
        cfg.lockDuringLockdown = false;
        cfg.isEmergencyExit = false;
        cfg.openSpeed = 120f;
        cfg.closeSpeed = 120f;
        cfg.openAngle = 90f;
        cfg.debugColor = new Color(0f, 0.7f, 1f, 0.3f);
        AssetDatabase.CreateAsset(cfg, path);
    }

    static void CreateCameraConfig()
    {
        string path = CONFIG_DIR + "/Cameras/TutorialCamera.asset";
        if (AssetDatabase.LoadAssetAtPath<CameraConfig>(path) != null) return;

        var cfg = ScriptableObject.CreateInstance<CameraConfig>();
        cfg.patrolAngle = 45f;
        cfg.patrolSpeed = 20f;
        cfg.startRotatingLeft = true;
        cfg.detectionRange = 8f;
        cfg.fieldOfView = 60f;
        cfg.detectionTime = 1.5f;
        cfg.detectionDecayRate = 2f;
        cfg.stopRotationOnDetection = true;
        cfg.visionConeColor = new Color(1f, 0f, 0f, 0.1f);
        cfg.detectionProgressColor = Color.yellow;
        AssetDatabase.CreateAsset(cfg, path);
    }

    static void CreateTerminalConfig()
    {
        string path = CONFIG_DIR + "/Terminals/TutorialTerminal.asset";
        if (AssetDatabase.LoadAssetAtPath<TerminalConfig>(path) != null) return;

        var cfg = ScriptableObject.CreateInstance<TerminalConfig>();
        cfg.terminalName = "Training Terminal";
        cfg.terminalID = "terminal_training";
        cfg.requiredCredentialType = CredentialType.Keycard;
        cfg.minimumRole = UserRole.Guest;
        cfg.skipAuthentication = true;
        cfg.accessLevelLabel = "TRAINING — AUTHORIZED ACCESS";
        cfg.themeColor = new Color(0f, 1f, 0.4f);

        cfg.actions.Add(new TerminalActionEntry
        {
            actionType = "DisableCamera",
            displayName = "Disable Camera",
            description = "Disable the training security camera.",
            requiredPermission = Permissions.DisableCameras,
            targetID = "camera_training"
        });

        AssetDatabase.CreateAsset(cfg, path);
    }

    static void CreateRoleAndPermissionConfigs()
    {
        string permPath = CONFIG_DIR + "/Permissions/TutorialPermissions.asset";
        var permSet = AssetDatabase.LoadAssetAtPath<PermissionSet>(permPath);
        if (permSet == null)
        {
            permSet = ScriptableObject.CreateInstance<PermissionSet>();
            permSet.AddPermission(Permissions.DisableCameras);
            AssetDatabase.CreateAsset(permSet, permPath);
        }

        string rolePath = CONFIG_DIR + "/Roles/TutorialRoleConfig.asset";
        if (AssetDatabase.LoadAssetAtPath<RolePermissionsConfig>(rolePath) != null) return;

        var roleCfg = ScriptableObject.CreateInstance<RolePermissionsConfig>();
        var roleField = roleCfg.GetType().GetField("rolePermissions",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (roleField != null)
        {
            var list = roleField.GetValue(roleCfg) as System.Collections.IList;
            var entryType = roleCfg.GetType().GetNestedType("RoleEntry",
                System.Reflection.BindingFlags.NonPublic);
            if (list != null && entryType != null)
            {
                var entry = System.Activator.CreateInstance(entryType);
                var roleField2 = entryType.GetField("role");
                var permField2 = entryType.GetField("permissionSet");
                if (roleField2 != null) roleField2.SetValue(entry, UserRole.Administrator);
                if (permField2 != null) permField2.SetValue(entry, permSet);
                list.Add(entry);
            }
        }
        AssetDatabase.CreateAsset(roleCfg, rolePath);
    }
}
