using UnityEngine;
using System.Collections;

public class ResultsScreen : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private float showDelay = 1.5f;
    [SerializeField] private KeyCode dismissKey = KeyCode.Escape;
    [SerializeField] private KeyCode retryKey = KeyCode.R;

    [Header("Colors")]
    [SerializeField] private Color headerColor = new Color(0.5f, 1f, 0.5f);
    [SerializeField] private Color labelColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color valueColor = Color.white;

    private bool showResults;
    private ScoreBreakdown currentBreakdown;
    private MissionRating currentRating;
    private float showTimer;

    private float alphaFade;
    private bool eventsSubscribed = false;

    private GUIStyle s_header;
    private GUIStyle s_rank;
    private GUIStyle s_rankLabel;
    private GUIStyle s_star;
    private GUIStyle s_label;
    private GUIStyle s_value;
    private GUIStyle s_hint;

    private struct RankDef
    {
        public string letter;
        public string label;
        public Color color;
        public int minScore;
    }

    private static readonly RankDef[] ranks = new RankDef[]
    {
        new RankDef { letter = "S", label = "GHOST", color = new Color(1f, 0.84f, 0f), minScore = 95 },
        new RankDef { letter = "A", label = "PROFESSIONAL", color = new Color(0.4f, 0.53f, 1f), minScore = 75 },
        new RankDef { letter = "B", label = "OPERATIVE", color = new Color(0.53f, 1f, 0.53f), minScore = 50 },
        new RankDef { letter = "C", label = "ROOKIE", color = new Color(0.67f, 0.67f, 0.67f), minScore = 25 },
        new RankDef { letter = "D", label = "TRAINEE", color = new Color(1f, 0.53f, 0.27f), minScore = 10 },
        new RankDef { letter = "F", label = "CAUGHT", color = new Color(0.8f, 0.1f, 0.1f), minScore = 0 },
    };

    void Awake()
    {
        s_header = new GUIStyle() { fontSize = 14, fontStyle = FontStyle.Bold };
        s_header.normal.textColor = headerColor;

        s_rank = new GUIStyle() { fontSize = 56, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        s_rankLabel = new GUIStyle() { fontSize = 11, alignment = TextAnchor.MiddleCenter };

        s_star = new GUIStyle() { fontSize = 16, alignment = TextAnchor.MiddleCenter };
        s_star.normal.textColor = Color.yellow;

        s_label = new GUIStyle() { fontSize = 11 };
        s_label.normal.textColor = labelColor;

        s_value = new GUIStyle() { fontSize = 11 };
        s_value.normal.textColor = valueColor;

        s_hint = new GUIStyle() { fontSize = 9, alignment = TextAnchor.MiddleCenter };
        s_hint.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
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
    }

    private void SubscribeToEvents()
    {
        if (eventsSubscribed) return;
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnMissionCompleted += HandleMissionCompleted;
            eventsSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!eventsSubscribed) return;
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted -= HandleMissionCompleted;
        eventsSubscribed = false;
    }

    private void HandleMissionCompleted()
    {
        showTimer = showDelay;
    }

    void Update()
    {
        if (showTimer > 0f)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0f && MissionScorer.Instance != null)
            {
                showResults = true;
                currentBreakdown = MissionScorer.Instance.GetFinalBreakdown();
                currentRating = MissionScorer.Instance.CalculateRating(currentBreakdown);
                alphaFade = 0f;
            }
        }

        if (showResults)
        {
            alphaFade = Mathf.Clamp01(alphaFade + Time.deltaTime * 3f);

            if (Input.GetKeyDown(retryKey))
            {
                if (CheckpointManager.Instance != null)
                    CheckpointManager.Instance.ClearCheckpoints();
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            }
            if (Input.GetKeyDown(dismissKey))
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    void OnGUI()
    {
        if (!showResults) return;

        float panelW = 440f;
        float panelH = 440f;
        float px = Screen.width / 2f - panelW / 2f;
        float py = Screen.height / 2f - panelH / 2f;

        Color bg = new Color(0.04f, 0.04f, 0.06f, 0.95f * alphaFade);
        GUI.color = bg;
        GUI.DrawTexture(new Rect(px, py, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        Color rc = GetRankColor();
        DrawBorder(new Rect(px, py, panelW, panelH), rc * 0.8f);
        GUI.color = rc * 0.3f;
        GUI.DrawTexture(new Rect(px, py, panelW, 3f), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float x = px + 20f;
        float y = py + 16f;
        float lineH = 22f;

        s_header.normal.textColor = headerColor * alphaFade;
        GUI.Label(new Rect(px, y, panelW, 24f), "MISSION COMPLETE", s_header);
        y += 30f;

        s_rank.normal.textColor = rc * alphaFade;
        GUI.Label(new Rect(px, y, panelW, 60f), currentRating.ToString(), s_rank);
        y += 62f;

        s_rankLabel.normal.textColor = rc * alphaFade;
        GUI.Label(new Rect(px, y, panelW, 16f), GetRankLabel(), s_rankLabel);
        y += 22f;

        int stars = GetStarCount();
        s_star.normal.textColor = Color.yellow * alphaFade;
        string starStr = new string('★', stars) + new string('☆', 5 - stars);
        GUI.Label(new Rect(px, y, panelW, 20f), starStr, s_star);
        y += 24f;

        GUI.color = new Color(0.3f, 0.3f, 0.3f, alphaFade);
        GUI.DrawTexture(new Rect(x, y, panelW - 40f, 1f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        y += 8f;

        s_label.normal.textColor = labelColor * alphaFade;
        s_value.normal.textColor = valueColor * alphaFade;

        int totalSec = Mathf.RoundToInt(currentBreakdown.totalTime);
        int mins = totalSec / 60;
        int secs = totalSec % 60;

        DrawField(x, y, panelW - 40f, lineH, "Completion Time", $"{mins:D2}:{secs:D2}", s_label, s_value);
        y += lineH;
        DrawField(x, y, panelW - 40f, lineH, "Camera Detections", currentBreakdown.cameraDetections.ToString(), s_label, s_value);
        y += lineH;
        DrawField(x, y, panelW - 40f, lineH, "Guard Encounters", currentBreakdown.guardDetections.ToString(), s_label, s_value);
        y += lineH;
        DrawField(x, y, panelW - 40f, lineH, "Alarms Triggered", currentBreakdown.alarmsTriggered.ToString(), s_label, s_value);
        y += lineH;

        if (currentBreakdown.totalSecondaryObjectives > 0)
        {
            DrawField(x, y, panelW - 40f, lineH, "Secondary Objectives",
                $"{currentBreakdown.completedSecondaryObjectives}/{currentBreakdown.totalSecondaryObjectives}", s_label, s_value);
            y += lineH;
        }

        y += 8f;
        s_hint.normal.textColor = new Color(0.4f, 0.4f, 0.4f, alphaFade);
        GUI.Label(new Rect(px, py + panelH - 22f, panelW, 16f), "[R] Retry   [ESC] Continue", s_hint);
    }

    private void DrawField(float x, float y, float w, float h, string label, string val, GUIStyle ls, GUIStyle vs)
    {
        GUI.Label(new Rect(x, y, w * 0.55f, h), label, ls);
        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, h), val, vs);
    }

    private void DrawBorder(Rect r, Color c)
    {
        Color o = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y + r.height - 1f, r.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, 1f, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x + r.width - 1f, r.y, 1f, r.height), Texture2D.whiteTexture);
        GUI.color = o;
    }

    private Color GetRankColor()
    {
        for (int i = 0; i < ranks.Length; i++)
            if (currentRating.ToString() == ranks[i].letter) return ranks[i].color;
        return ranks[ranks.Length - 1].color;
    }

    private string GetRankLabel()
    {
        for (int i = 0; i < ranks.Length; i++)
            if (currentRating.ToString() == ranks[i].letter) return ranks[i].label;
        return ranks[ranks.Length - 1].label;
    }

    private int GetStarCount()
    {
        return currentRating switch
        {
            MissionRating.S => 5,
            MissionRating.A => 4,
            MissionRating.B => 3,
            MissionRating.C => 2,
            MissionRating.D => 1,
            _ => 0,
        };
    }
}
