using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private readonly HashSet<ObjectiveID> reachedCheckpoints = new();

    [Header("Checkpoint Definitions")]
    [SerializeField] private CheckpointDefinition[] checkpoints;

    public event System.Action<ObjectiveID> OnCheckpointReached;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
    }

    void OnDisable()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
    }

    private void HandleObjectiveCompleted(ObjectiveID id)
    {
        foreach (var cp in checkpoints)
        {
            if (cp.objective == id && reachedCheckpoints.Add(id))
            {
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

    public void ClearCheckpoints()
    {
        reachedCheckpoints.Clear();
    }
}

[System.Serializable]
public struct CheckpointDefinition
{
    public string name;
    public ObjectiveID objective;
    public Transform respawnLocation;
}
