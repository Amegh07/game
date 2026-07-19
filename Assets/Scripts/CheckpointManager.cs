using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private readonly HashSet<ObjectiveID> reachedCheckpoints = new();
    private readonly List<CheckpointDefinition> checkpointList = new();
    private bool eventsSubscribed = false;

    [Header("Checkpoint Definitions")]
    [SerializeField] private CheckpointDefinition[] checkpoints;

    private const string P = "MH_";
    private const string KHasCheckpoint = P + "HasCheckpoint";
    private const string KCheckpointObjective = P + "CheckpointObjective";
    private const string KPlayerX = P + "PlayerX";
    private const string KPlayerY = P + "PlayerY";
    private const string KPlayerZ = P + "PlayerZ";
    private const string KCompletedCount = P + "CompletedCount";
    private const string KCompletedObjectives = P + "CompletedObjectives";
    private const string KMissionElapsed = P + "MissionElapsed";
    private const string KHasStaffKeycard = P + "HasStaffKeycard";
    private const string KHasLevel2Keycard = P + "HasLevel2Keycard";
    private const string KAlarmLevel = P + "AlarmLevel";
    private const string KDisabledCameras = P + "DisabledCameras";

    public event System.Action<ObjectiveID> OnCheckpointReached;
    public event System.Action OnCheckpointLoaded;

    public bool HasSavedCheckpoint { get; private set; }
    public ObjectiveID SavedCheckpointObjective { get; private set; }
    public Vector3 SavedPlayerPosition { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        HasSavedCheckpoint = PlayerPrefs.GetInt(KHasCheckpoint, 0) == 1;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        SubscribeIfNeeded();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Start()
    {
        SubscribeIfNeeded();
    }

    private void SubscribeIfNeeded()
    {
        if (eventsSubscribed) return;
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
            eventsSubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (!eventsSubscribed) return;
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
        eventsSubscribed = false;
    }

    private void HandleObjectiveCompleted(ObjectiveID id)
    {
        foreach (var cp in checkpoints)
        {
            if (cp.objective == id && reachedCheckpoints.Add(id))
            {
                SaveCheckpoint(cp);
                OnCheckpointReached?.Invoke(id);
                Debug.Log($"Checkpoint reached: {cp.name} ({id})");
                break;
            }
        }
    }

    public bool HasReachedCheckpoint(ObjectiveID id)
    {
        return reachedCheckpoints.Contains(id);
    }

    public void RegisterCheckpoint(CheckpointDefinition def)
    {
        for (int i = 0; i < checkpointList.Count; i++)
        {
            if (checkpointList[i].objective == def.objective)
            {
                checkpointList[i] = def;
                return;
            }
        }
        checkpointList.Add(def);
    }

    public void ForceCheckpoint(ObjectiveID id)
    {
        if (reachedCheckpoints.Add(id))
        {
            CheckpointDefinition def = default;
            def.objective = id;
            def.name = id.ToString();
            SaveCheckpoint(def);
            OnCheckpointReached?.Invoke(id);
            Debug.Log($"Checkpoint reached (trigger): {id}");
        }
    }

    public void ClearCheckpoints()
    {
        reachedCheckpoints.Clear();
        ClearSavedData();
    }

    // ---------------------------------------------------------------
    //  Save / Load
    // ---------------------------------------------------------------

    private void SaveCheckpoint(CheckpointDefinition cp)
    {
        PlayerPrefs.SetInt(KHasCheckpoint, 1);
        PlayerPrefs.SetInt(KCheckpointObjective, (int)cp.objective);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            PlayerPrefs.SetFloat(KPlayerX, pos.x);
            PlayerPrefs.SetFloat(KPlayerY, pos.y);
            PlayerPrefs.SetFloat(KPlayerZ, pos.z);
        }

        if (MissionManager.Instance != null)
        {
            PlayerPrefs.SetInt(KCompletedCount, MissionManager.Instance.completedObjectives.Count);

            string objList = "";
            for (int i = 0; i < MissionManager.Instance.completedObjectives.Count; i++)
            {
                if (i > 0) objList += ",";
                objList += ((int)MissionManager.Instance.completedObjectives[i]).ToString();
            }
            PlayerPrefs.SetString(KCompletedObjectives, objList);
            PlayerPrefs.SetFloat(KMissionElapsed, Time.time);
        }

        if (MuseumHeist.AccessControl.InventoryManager.Instance != null)
        {
            PlayerPrefs.SetInt(KHasStaffKeycard,
                MuseumHeist.AccessControl.InventoryManager.Instance.HasKeycard(MuseumHeist.AccessControl.KeycardType.Staff) ? 1 : 0);
            PlayerPrefs.SetInt(KHasLevel2Keycard,
                MuseumHeist.AccessControl.InventoryManager.Instance.HasKeycard(MuseumHeist.AccessControl.KeycardType.Security) ? 1 : 0);
        }

        if (SecurityManager.Instance != null)
            PlayerPrefs.SetInt(KAlarmLevel, (int)SecurityManager.Instance.currentAlarmLevel);

        string disabledCams = "";
        if (SecurityManager.Instance != null)
        {
            var allCams = new List<string>();
            if (SecurityManager.Instance.CameraCount > 0)
            {
                foreach (var camId in GetRegisteredCameraIds())
                {
                    if (!SecurityManager.Instance.IsCameraActive(camId))
                    {
                        if (disabledCams.Length > 0) disabledCams += ",";
                        disabledCams += camId;
                    }
                }
            }
        }
        PlayerPrefs.SetString(KDisabledCameras, disabledCams);

        PlayerPrefs.Save();
        Debug.Log($"[CheckpointManager] Saved checkpoint: {cp.name} ({cp.objective})");
    }

    public bool LoadCheckpoint()
    {
        if (PlayerPrefs.GetInt(KHasCheckpoint, 0) != 1)
        {
            Debug.Log("[CheckpointManager] No saved checkpoint found.");
            return false;
        }

        SavedCheckpointObjective = (ObjectiveID)PlayerPrefs.GetInt(KCheckpointObjective);
        float x = PlayerPrefs.GetFloat(KPlayerX);
        float y = PlayerPrefs.GetFloat(KPlayerY);
        float z = PlayerPrefs.GetFloat(KPlayerZ);
        SavedPlayerPosition = new Vector3(x, y, z);

        reachedCheckpoints.Clear();
        string objCsv = PlayerPrefs.GetString(KCompletedObjectives, "");
        if (!string.IsNullOrEmpty(objCsv))
        {
            string[] parts = objCsv.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int val))
                {
                    var id = (ObjectiveID)val;
                    foreach (var cp in checkpoints)
                    {
                        if (cp.objective == id)
                        {
                            reachedCheckpoints.Add(id);
                            break;
                        }
                    }
                }
            }
        }

        if (MissionManager.Instance != null)
        {
            int completedCount = PlayerPrefs.GetInt(KCompletedCount, 0);
            MissionManager.Instance.RestoreProgress(completedCount);
        }

        if (MuseumHeist.AccessControl.InventoryManager.Instance != null)
        {
            MuseumHeist.AccessControl.InventoryManager.Instance.Clear();
            if (PlayerPrefs.GetInt(KHasStaffKeycard, 0) == 1)
                MuseumHeist.AccessControl.InventoryManager.Instance.AddKeycard(MuseumHeist.AccessControl.KeycardType.Staff);
            if (PlayerPrefs.GetInt(KHasLevel2Keycard, 0) == 1)
                MuseumHeist.AccessControl.InventoryManager.Instance.AddKeycard(MuseumHeist.AccessControl.KeycardType.Security);
        }

        if (SecurityManager.Instance != null)
        {
            int alarmVal = PlayerPrefs.GetInt(KAlarmLevel, 0);
            SecurityManager.Instance.ForceSetAlarmLevel((SecurityManager.AlarmLevel)alarmVal);
        }

        string disabledCams = PlayerPrefs.GetString(KDisabledCameras, "");
        if (!string.IsNullOrEmpty(disabledCams) && SecurityManager.Instance != null)
        {
            string[] camIds = disabledCams.Split(',');
            for (int i = 0; i < camIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(camIds[i]))
                    SecurityManager.Instance.DisableCamera(camIds[i]);
            }
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = SavedPlayerPosition;
            if (cc != null) cc.enabled = true;
        }

        OnCheckpointLoaded?.Invoke();
        Debug.Log($"[CheckpointManager] Loaded checkpoint: {SavedCheckpointObjective} at {SavedPlayerPosition}");
        return true;
    }

    public void ClearSavedData()
    {
        PlayerPrefs.DeleteKey(KHasCheckpoint);
        PlayerPrefs.DeleteKey(KCheckpointObjective);
        PlayerPrefs.DeleteKey(KPlayerX);
        PlayerPrefs.DeleteKey(KPlayerY);
        PlayerPrefs.DeleteKey(KPlayerZ);
        PlayerPrefs.DeleteKey(KCompletedCount);
        PlayerPrefs.DeleteKey(KCompletedObjectives);
        PlayerPrefs.DeleteKey(KMissionElapsed);
        PlayerPrefs.DeleteKey(KHasStaffKeycard);
        PlayerPrefs.DeleteKey(KHasLevel2Keycard);
        PlayerPrefs.DeleteKey(KAlarmLevel);
        PlayerPrefs.DeleteKey(KDisabledCameras);
        PlayerPrefs.Save();
        HasSavedCheckpoint = false;
        Debug.Log("[CheckpointManager] Saved data cleared.");
    }

    private List<string> GetRegisteredCameraIds()
    {
        var ids = new List<string>();
        if (SecurityManager.Instance != null)
        {
            foreach (var camId in SecurityManager.Instance.GetRegisteredCameraIds())
                ids.Add(camId);
        }
        return ids;
    }
}

[System.Serializable]
public struct CheckpointDefinition
{
    public string name;
    public ObjectiveID objective;
    public Transform respawnLocation;
}
