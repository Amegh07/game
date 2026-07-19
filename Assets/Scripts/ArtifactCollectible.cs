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

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;

        isCollected = true;
        gameObject.SetActive(false);
        NoiseManager.EmitAt(transform.position, 10f, 0.5f, NoiseType.Interaction, gameObject);
        Debug.Log($"Artifact collected! Objective '{completeObjective}' complete.");
        MissionManager.Instance?.CompleteObjective(completeObjective);
        if (triggerEscapePhase)
            MissionManager.Instance?.TriggerEscapePhase();
    }

    public bool CanInteract(PlayerController player)
    {
        return !isCollected;
    }

    public string GetInteractionPrompt()
    {
        return "Collect Artifact";
    }

    public void OnFocus() { }
    public void OnLoseFocus() { }
}
