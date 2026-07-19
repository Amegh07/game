using UnityEngine;

public enum MissionRating
{
    S = 0,
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    F = 5
}

public class MissionScorer : MonoBehaviour
{
    public static MissionScorer Instance { get; private set; }

    [Header("Scoring State")]
    public int cameraDetections;
    public int guardDetections;
    public int alarmsTriggered;
    public float missionStartTime;
    public float missionEndTime;
    public bool secondaryObjectivesAllComplete;
    public int totalSecondaryObjectives;
    public int completedSecondaryObjectives;

    [Header("Rating Thresholds")]
    public int maxDetectionsForS = 0;
    public int maxAlarmsForS = 0;
    public float maxTimeForS = 180f;
    public int maxDetectionsForA = 2;
    public int maxAlarmsForA = 1;
    public float maxTimeForA = 300f;

    public event System.Action<MissionRating, ScoreBreakdown> OnMissionRated;

    private bool missionActive;
    private bool eventsSubscribed = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SubscribeToEvents();
    }

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
        if (Instance == this) Instance = null;
    }

    private void SubscribeToEvents()
    {
        if (eventsSubscribed) return;
        if (SecurityManager.Instance != null && MissionManager.Instance != null)
        {
            SecurityManager.Instance.OnTriggerReported += HandleTrigger;
            MissionManager.Instance.OnObjectiveStarted += HandleObjectiveStarted;
            MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
            MissionManager.Instance.OnMissionCompleted += HandleMissionComplete;
            eventsSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!eventsSubscribed) return;
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnTriggerReported -= HandleTrigger;
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnObjectiveStarted -= HandleObjectiveStarted;
            MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
            MissionManager.Instance.OnMissionCompleted -= HandleMissionComplete;
        }
        eventsSubscribed = false;
    }

    private void HandleObjectiveStarted(ObjectiveID id)
    {
        StartScoring();
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnObjectiveStarted -= HandleObjectiveStarted;
    }

    private void HandleObjectiveCompleted(ObjectiveID id)
    {
        // Intentionally left blank: event subscription is required for compile-time callback registration.
    }

    public void StartScoring()
    {
        cameraDetections = 0;
        guardDetections = 0;
        alarmsTriggered = 0;
        missionStartTime = Time.time;
        secondaryObjectivesAllComplete = true;
        totalSecondaryObjectives = 0;
        completedSecondaryObjectives = 0;
        missionActive = true;
    }

    private void HandleTrigger(SecurityManager.SecurityTrigger trigger, string source, SecurityManager.AlarmLevel level)
    {
        if (!missionActive) return;

        switch (trigger)
        {
            case SecurityManager.SecurityTrigger.CameraPlayerDetected:
                cameraDetections++;
                break;
            case SecurityManager.SecurityTrigger.GuardChase:
                guardDetections++;
                break;
            case SecurityManager.SecurityTrigger.PlayerDetected:
                guardDetections++;
                break;
        }

        if (level >= SecurityManager.AlarmLevel.Alert)
            alarmsTriggered++;
    }

    public void CompleteSecondaryObjective()
    {
        completedSecondaryObjectives++;
        totalSecondaryObjectives++;
    }

    private void HandleMissionComplete()
    {
        if (!missionActive) return;
        missionActive = false;
        missionEndTime = Time.time;

        secondaryObjectivesAllComplete = completedSecondaryObjectives >= totalSecondaryObjectives;

        ScoreBreakdown breakdown = new ScoreBreakdown
        {
            cameraDetections = this.cameraDetections,
            guardDetections = this.guardDetections,
            alarmsTriggered = this.alarmsTriggered,
            totalTime = missionEndTime - missionStartTime,
            secondaryObjectivesComplete = secondaryObjectivesAllComplete,
            totalSecondaryObjectives = this.totalSecondaryObjectives,
            completedSecondaryObjectives = this.completedSecondaryObjectives
        };

        MissionRating rating = CalculateRating(breakdown);
        OnMissionRated?.Invoke(rating, breakdown);
    }

    public MissionRating CalculateRating(ScoreBreakdown s)
    {
        int score = 100;

        score -= s.cameraDetections * 15;
        score -= s.guardDetections * 20;
        score -= s.alarmsTriggered * 25;

        if (s.totalTime > maxTimeForS)
            score -= Mathf.RoundToInt((s.totalTime - maxTimeForS) / 10f) * 2;

        if (!s.secondaryObjectivesComplete)
            score -= 15;

        score = Mathf.Clamp(score, 0, 100);

        if (score >= 95 && s.cameraDetections <= maxDetectionsForS && s.alarmsTriggered <= maxAlarmsForS)
            return MissionRating.S;
        if (score >= 75)
            return MissionRating.A;
        if (score >= 50)
            return MissionRating.B;
        if (s.alarmsTriggered >= 3)
            return MissionRating.F;
        if (score >= 25)
            return MissionRating.C;
        return MissionRating.D;
    }

    public ScoreBreakdown GetFinalBreakdown()
    {
        return new ScoreBreakdown
        {
            cameraDetections = this.cameraDetections,
            guardDetections = this.guardDetections,
            alarmsTriggered = this.alarmsTriggered,
            totalTime = missionEndTime - missionStartTime,
            secondaryObjectivesComplete = secondaryObjectivesAllComplete,
            totalSecondaryObjectives = this.totalSecondaryObjectives,
            completedSecondaryObjectives = this.completedSecondaryObjectives
        };
    }
}

public struct ScoreBreakdown
{
    public int cameraDetections;
    public int guardDetections;
    public int alarmsTriggered;
    public float totalTime;
    public bool secondaryObjectivesComplete;
    public int totalSecondaryObjectives;
    public int completedSecondaryObjectives;
}
