using UnityEngine;

public class TutorialObjectiveNotifier : MonoBehaviour
{
    public ObjectiveID objectiveID;

    public void Complete()
    {
        MissionManager.Instance?.CompleteObjective(objectiveID);
    }
}
