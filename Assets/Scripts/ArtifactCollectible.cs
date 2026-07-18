using UnityEngine;

public class ArtifactCollectible : MonoBehaviour, IInteractable
{
    [Header("Animation")]
    public float rotationSpeed = 50f;
    public float floatSpeed = 1f;
    public float floatHeight = 0.3f;

    [Header("Objective")]
    public ObjectiveID completeObjective = ObjectiveID.StealArtifact;
    public bool triggerEscapePhase = false;

    [Header("State")]
    public bool isCollected = false;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        if (isCollected) return;

        Vector3 newPos = startPosition;
        newPos.y += Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = newPos;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void Interact()
    {
        if (isCollected) return;

        isCollected = true;
        gameObject.SetActive(false);
        Debug.Log($"Artifact collected! Objective '{completeObjective}' complete.");
        MissionManager.Instance?.CompleteObjective(completeObjective);
        if (triggerEscapePhase)
            MissionManager.Instance?.TriggerEscapePhase();
    }
}
