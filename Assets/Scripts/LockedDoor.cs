using UnityEngine;

public class LockedDoor : MonoBehaviour, IInteractable
{
    [Header("Identification")]
    public string doorID = "";

    [Header("State")]
    public bool isLocked = true;
    public bool isOpen = false;

    [Header("Settings")]
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public string lockedMessage = "The door is locked. Find a keycard.";
    public string openedMessage = "The door is now open.";

    private Quaternion closedRotation;
    private Quaternion openRotation;

    void Start()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);

        if (SecurityManager.Instance != null)
            SecurityManager.Instance.RegisterDoor(doorID, this);
    }

    void OnDestroy()
    {
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.UnregisterDoor(doorID);
    }

    void Update()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, openSpeed * Time.deltaTime);
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public void Lock()
    {
        isLocked = true;
    }

    private bool hasEverOpened = false;

    public void Open()
    {
        if (!isLocked)
        {
            isOpen = true;
            if (!hasEverOpened)
            {
                hasEverOpened = true;
                MissionManager.Instance?.CompleteObjective(ObjectiveID.UnlockSecurityDoor);
            }
        }
    }

    public void Close()
    {
        isOpen = false;
    }

    public void Toggle()
    {
        if (!isLocked)
        {
            isOpen = !isOpen;
        }
    }

    public void Interact()
    {
        if (isLocked)
        {
            Debug.Log(lockedMessage);
        }
        else
        {
            isOpen = !isOpen;
            Debug.Log(isOpen ? openedMessage : "The door is now closed.");
        }
    }
}
