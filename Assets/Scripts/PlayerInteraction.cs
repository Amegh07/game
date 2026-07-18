using UnityEngine;

public interface IInteractable
{
    void Interact();
}

public class PlayerInteraction : MonoBehaviour
{
    [Header("Settings")]
    public float interactionRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public LayerMask interactionLayer = -1;

    private Camera playerCamera;
    private IInteractable currentInteractable;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = GetComponent<Camera>();
    }

    void Update()
    {
        CheckForInteractables();

        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
            currentInteractable.Interact();
    }

    void CheckForInteractables()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                currentInteractable = interactable;
                return;
            }
        }

        currentInteractable = null;
    }

    void OnGUI()
    {
        if (currentInteractable != null)
            GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height / 2f + 20f, 200f, 30f),
                $"[E] Interact");
    }
}
