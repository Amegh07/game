using UnityEngine;
using MuseumHeist.AccessControl;

public class KeycardPickup : MonoBehaviour, IInteractable
{
    [Header("Target")]
    public string targetDoorID = "";

    [Header("Keycard")]
    public KeycardType keycardType = KeycardType.Staff;
    public bool destroyOnPickup = true;

    [Header("Animation")]
    public float rotationSpeed = 80f;
    public float floatSpeed = 1.5f;
    public float floatHeight = 0.2f;

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

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddKeycard(keycardType);
        }

        if (SecurityManager.Instance != null && !string.IsNullOrEmpty(targetDoorID))
        {
            bool unlocked = SecurityManager.Instance.UnlockDoor(targetDoorID);
            if (!unlocked)
                SecurityManager.Instance.RequestDoorUnlock(targetDoorID);

            Debug.Log("Keycard acquired! Door unlock requested.");
        }

        MissionManager.Instance?.CompleteObjective(ObjectiveID.ObtainKeycard);

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    public bool CanInteract(PlayerController player)
    {
        return !isCollected;
    }

    public string GetInteractionPrompt()
    {
        return "Take Keycard";
    }

    public void OnFocus() { }
    public void OnLoseFocus() { }
}
