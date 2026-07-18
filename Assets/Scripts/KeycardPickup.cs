using UnityEngine;

public class KeycardPickup : MonoBehaviour, IInteractable
{
    [Header("Target")]
    public string targetDoorID = "";

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

    public void Interact()
    {
        if (isCollected) return;

        isCollected = true;

        if (SecurityManager.Instance != null && !string.IsNullOrEmpty(targetDoorID))
        {
            bool unlocked = SecurityManager.Instance.UnlockDoor(targetDoorID);
            if (unlocked)
                Debug.Log("Keycard acquired! Door unlocked via SecurityManager.");
        }

        MissionManager.Instance?.CompleteObjective(ObjectiveID.ObtainKeycard);

        gameObject.SetActive(false);
    }
}
