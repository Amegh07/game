using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ArtifactController : MonoBehaviour, IInteractable
{
    [Header("Artifact Settings")]
    [SerializeField] private string artifactName = "The Crown of Kings";
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatHeight = 0.25f;
    [SerializeField] private Light artifactLight;
    [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.2f);

    [Header("Escape Phase")]
    [SerializeField] private AudioClip artifactPickupSound;

    private Vector3 startPosition;
    private bool collected;
    private AudioSource audioSource;
    private Renderer artifactRenderer;

    void Start()
    {
        startPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        artifactRenderer = GetComponent<Renderer>();

        if (artifactLight == null)
            artifactLight = GetComponentInChildren<Light>();

        if (artifactLight != null)
            artifactLight.color = glowColor;
    }

    void Update()
    {
        if (collected) return;

        Vector3 newPos = startPosition;
        newPos.y += Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = newPos;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;

        collected = true;

        if (artifactPickupSound != null && audioSource != null)
            audioSource.PlayOneShot(artifactPickupSound);

        NoiseManager.EmitAt(transform.position, 15f, 0.8f, NoiseType.Interaction, gameObject);

        MissionManager.Instance?.CompleteObjective(ObjectiveID.StealMainArtifact);
        MissionManager.Instance?.TriggerEscapePhase();

        Debug.Log($"Artifact '{artifactName}' stolen! Escape phase initiated.");

        gameObject.SetActive(false);
    }

    public bool CanInteract(PlayerController player)
    {
        return !collected && MissionManager.Instance != null;
    }

    public string GetInteractionPrompt()
    {
        return $"Steal {artifactName}";
    }

    public void OnFocus() { }
    public void OnLoseFocus() { }

    void OnDrawGizmos()
    {
        Gizmos.color = glowColor;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.5f);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.8f,
            $"ARTIFACT: {artifactName}\nInteract to steal");
#endif
    }
}
