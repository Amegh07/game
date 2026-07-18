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
    [SerializeField] private GameObject[] emergencyLights;
    [SerializeField] private AudioClip alarmSound;
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

    public void Interact()
    {
        if (collected) return;

        if (MissionManager.Instance == null) return;

        collected = true;

        if (artifactPickupSound != null && audioSource != null)
            audioSource.PlayOneShot(artifactPickupSound);

        MissionManager.Instance.CompleteObjective(ObjectiveID.StealMainArtifact);
        MissionManager.Instance.TriggerEscapePhase();

        ActivateEmergencyLights();

        Debug.Log($"Artifact '{artifactName}' stolen! Escape phase initiated.");

        gameObject.SetActive(false);
    }

    private void ActivateEmergencyLights()
    {
        if (emergencyLights == null) return;

        foreach (var lightObj in emergencyLights)
        {
            if (lightObj != null)
                lightObj.SetActive(true);
        }
    }

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
