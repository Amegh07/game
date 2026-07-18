using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MissionZone : MonoBehaviour
{
    public ObjectiveID objectiveID;
    public bool oneShot = true;

    private bool triggered = false;

    void Start()
    {
        BoxCollider bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneShot && triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        MissionManager.Instance?.CompleteObjective(objectiveID);
    }
}
