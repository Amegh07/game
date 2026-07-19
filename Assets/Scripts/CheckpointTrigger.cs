using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public ObjectiveID checkpointObjective;
    public Transform respawnLocation;

    private bool hasFired;

    void OnTriggerEnter(Collider other)
    {
        if (hasFired) return;
        if (!other.CompareTag("Player")) return;

        hasFired = true;

        if (CheckpointManager.Instance != null)
        {
            var def = new CheckpointDefinition
            {
                name = $"Checkpoint_{checkpointObjective}",
                objective = checkpointObjective,
                respawnLocation = respawnLocation
            };
            CheckpointManager.Instance.RegisterCheckpoint(def);
            CheckpointManager.Instance.ForceCheckpoint(checkpointObjective);
        }
    }
}
