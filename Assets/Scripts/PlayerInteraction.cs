using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactionLayer = -1;
    [SerializeField] private float interactionCheckInterval = 0.05f;

    [Header("Raycast")]
    [SerializeField] private bool drawDebugRay = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Camera playerCamera;
    private PlayerController playerController;
    private IInteractable currentInteractable;
    private IInteractable previousInteractable;
    private float checkTimer;
    private float distanceToTarget;
    private string currentInteractableName;
    private GUIStyle interactStyle;
    private GUIStyle debugStyle;

    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();

        playerController = GetComponent<PlayerController>();
        if (playerController != null)
            playerController.OnInteract += TryInteract;

        interactStyle = new GUIStyle();
        interactStyle.alignment = TextAnchor.MiddleCenter;
        interactStyle.fontSize = 14;
        interactStyle.fontStyle = FontStyle.Bold;

        debugStyle = new GUIStyle();
        debugStyle.fontSize = 11;
    }

    private void OnDestroy()
    {
        if (playerController != null)
            playerController.OnInteract -= TryInteract;
    }

    private void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            CheckForInteractables();
            checkTimer = interactionCheckInterval;
        }
    }

    private void TryInteract()
    {
        if (currentInteractable == null) return;
        if (!currentInteractable.CanInteract(playerController)) return;
        currentInteractable.Interact(playerController);
    }

    private void CheckForInteractables()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.yellow, interactionCheckInterval);

        IInteractable newTarget = null;
        distanceToTarget = 0f;
        currentInteractableName = "";

        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayer, QueryTriggerInteraction.Collide))
        {
            IInteractable found = hit.collider.GetComponentInParent<IInteractable>();
            if (found != null)
            {
                newTarget = found;
                distanceToTarget = hit.distance;
                currentInteractableName = hit.collider.name;
            }
        }

        currentInteractable = newTarget;

        if (newTarget != previousInteractable)
        {
            previousInteractable?.OnLoseFocus();
            newTarget?.OnFocus();
            previousInteractable = newTarget;
        }
    }

    private void OnGUI()
    {
        if (currentInteractable != null)
        {
            bool canInteract = currentInteractable.CanInteract(playerController);
            string prompt = currentInteractable.GetInteractionPrompt();
            string text = canInteract ? $"[E] {prompt}" : prompt;

            float x = Screen.width / 2f - 150f;
            float y = Screen.height / 2f + 20f;
            float width = 300f;
            float height = 30f;

            interactStyle.normal.textColor = canInteract ? Color.white : new Color(0.7f, 0.7f, 0.7f);

            GUI.Label(new Rect(x, y, width, height), text, interactStyle);
        }

        if (showDebugInfo)
        {
            debugStyle.normal.textColor = Color.white;

            float dy = Screen.height - 160f;
            GUI.Label(new Rect(10, dy, 400, 20), $"Target: {currentInteractableName}", debugStyle);
            dy += 18;
            GUI.Label(new Rect(10, dy, 400, 20), $"Distance: {distanceToTarget:F2}m", debugStyle);
            dy += 18;
            GUI.Label(new Rect(10, dy, 400, 20), $"Can Interact: {currentInteractable?.CanInteract(playerController)}", debugStyle);
        }
    }
}
