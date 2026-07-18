using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using MuseumHeist.AccessControl;

public class DoorSystemSetupEditor
{
    private const string CONFIG_DIR = "Assets/DoorConfigs";

    [MenuItem("Tools/Museum Heist/Create Example Door Configs")]
    static void CreateExampleConfigs()
    {
        Directory.CreateDirectory(CONFIG_DIR);

        CreateConfig("PublicDoor", "Public Door", KeycardType.Public,
            startsLocked: false, lockOnAlert: false, lockDuringLockdown: false, isEmergency: false,
            new Color(0.3f, 0.8f, 0.3f, 0.3f));

        CreateConfig("SecurityDoor", "Security Door", KeycardType.Security,
            startsLocked: true, lockOnAlert: true, lockDuringLockdown: true, isEmergency: false,
            new Color(0.8f, 0.2f, 0.2f, 0.3f));

        CreateConfig("VaultDoor", "Vault Door", KeycardType.Vault,
            startsLocked: true, lockOnAlert: true, lockDuringLockdown: true, isEmergency: false,
            new Color(1f, 0.6f, 0f, 0.3f),
            canAutoClose: false);

        CreateConfig("EmergencyExit", "Emergency Exit", KeycardType.Public,
            startsLocked: false, lockOnAlert: false, lockDuringLockdown: false, isEmergency: true,
            new Color(0f, 1f, 0f, 0.3f));

        CreateConfig("StaffDoor", "Staff Only", KeycardType.Staff,
            startsLocked: true, lockOnAlert: true, lockDuringLockdown: true, isEmergency: false,
            new Color(0.2f, 0.4f, 0.8f, 0.3f));

        CreateConfig("ResearchLab", "Research Lab", KeycardType.Research,
            startsLocked: true, lockOnAlert: false, lockDuringLockdown: true, isEmergency: false,
            new Color(0.6f, 0.3f, 0.8f, 0.3f));

        CreateConfig("MasterDoor", "Master Access", KeycardType.Master,
            startsLocked: true, lockOnAlert: false, lockDuringLockdown: false, isEmergency: false,
            new Color(0.9f, 0.9f, 0.2f, 0.3f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"DoorSystemSetup: Created example configs in {CONFIG_DIR}/");
    }

    static void CreateConfig(
        string name,
        string doorName,
        KeycardType keycard,
        bool startsLocked,
        bool lockOnAlert,
        bool lockDuringLockdown,
        bool isEmergency,
        Color debugColor,
        bool canAutoClose = true)
    {
        string path = $"{CONFIG_DIR}/{name}.asset";

        DoorConfig existing = AssetDatabase.LoadAssetAtPath<DoorConfig>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(ScriptableObject.CreateInstance<DoorConfig>(), existing);
            Debug.Log($"DoorSystemSetup: Updated existing config at {path}");
            return;
        }

        DoorConfig config = ScriptableObject.CreateInstance<DoorConfig>();
        config.doorName = doorName;
        config.requiredKeycard = keycard;
        config.startsLocked = startsLocked;
        config.lockOnAlert = lockOnAlert;
        config.lockDuringLockdown = lockDuringLockdown;
        config.isEmergencyExit = isEmergency;
        config.canAutoClose = canAutoClose;
        config.debugColor = debugColor;
        config.openSpeed = 120f;
        config.closeSpeed = 120f;
        config.openAngle = 90f;
        config.autoCloseDelay = 3f;

        AssetDatabase.CreateAsset(config, path);
        Debug.Log($"DoorSystemSetup: Created {path}");
    }

    [MenuItem("Tools/Museum Heist/Create Example Door Configs", true)]
    static bool ValidateCreateConfigs()
    {
        return Application.isPlaying == false;
    }
}
