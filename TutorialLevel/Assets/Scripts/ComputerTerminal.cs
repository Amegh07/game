using UnityEngine;

public class ComputerTerminal : MonoBehaviour, IInteractable
{
    [Header("Target")]
    public string targetCameraID = "";

    [Header("Visual")]
    public MeshRenderer screenRenderer;
    public Material screenOnMaterial;
    public Material screenOffMaterial;

    [Header("State")]
    public bool hasBeenUsed = false;

    private Light indicatorLight;

    void Start()
    {
        if (screenRenderer == null)
            screenRenderer = GetComponent<MeshRenderer>();

        indicatorLight = GetComponentInChildren<Light>();

        if (screenRenderer != null && screenOnMaterial != null)
            screenRenderer.material = screenOnMaterial;
    }

    public void Interact()
    {
        if (hasBeenUsed) return;

        hasBeenUsed = true;

        if (SecurityManager.Instance != null && !string.IsNullOrEmpty(targetCameraID))
        {
            bool disabled = SecurityManager.Instance.DisableCamera(targetCameraID);
            if (disabled)
            {
                Debug.Log("Terminal: Security camera disabled via SecurityManager.");
                MissionManager.Instance?.CompleteObjective(ObjectiveID.DisableCamera);
            }
        }

        if (screenRenderer != null && screenOffMaterial != null)
            screenRenderer.material = screenOffMaterial;

        if (indicatorLight != null)
            indicatorLight.color = Color.green;
    }
}
