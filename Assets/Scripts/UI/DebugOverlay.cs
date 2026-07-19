using System;
using UnityEngine;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

public class DebugOverlay : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private KeyCode missionKey = KeyCode.F1;
    [SerializeField] private KeyCode aiKey = KeyCode.F2;
    [SerializeField] private KeyCode perfKey = KeyCode.F3;

    [Header("Colors")]
    [SerializeField] private Color missionColor = new Color(0f, 1f, 0.53f);
    [SerializeField] private Color aiColor = new Color(1f, 0.67f, 0f);
    [SerializeField] private Color perfColor = new Color(0f, 0.67f, 1f);
    [SerializeField] private Color bgColor = new Color(0.07f, 0.07f, 0.07f, 0.95f);

    private enum Panel { None, Mission, AI, Perf }
    private Panel activePanel = Panel.None;

    private GuardFSM[] guards;
    private SecurityCamera[] cameras;
    private int cachedLightCount;
    private int cachedColliderCount;
    private float fpsTimer;
    private int fpsCount;
    private float currentFPS;

    private GUIStyle s_missionHeader;
    private GUIStyle s_missionLabel;
    private GUIStyle s_missionValue;
    private GUIStyle s_aiHeader;
    private GUIStyle s_aiLabel;
    private GUIStyle s_aiValue;
    private GUIStyle s_perfHeader;
    private GUIStyle s_perfLabel;
    private GUIStyle s_perfValue;
    private GUIStyle s_footer;
    private GUIStyle s_stateLabel;

    void Start()
    {
        guards = FindObjectsOfType<GuardFSM>();
        cameras = FindObjectsOfType<SecurityCamera>();
        cachedLightCount = FindObjectsOfType<Light>().Length;
        cachedColliderCount = FindObjectsOfType<Collider>().Length;
        InitStyles();
    }

    private void InitStyles()
    {
        s_missionHeader = new GUIStyle() { fontSize = 12, fontStyle = FontStyle.Bold };
        s_missionHeader.normal.textColor = missionColor;

        s_missionLabel = new GUIStyle() { fontSize = 10 };
        s_missionLabel.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        s_missionValue = new GUIStyle() { fontSize = 10 };
        s_missionValue.normal.textColor = Color.white;

        s_aiHeader = new GUIStyle() { fontSize = 12, fontStyle = FontStyle.Bold };
        s_aiHeader.normal.textColor = aiColor;

        s_aiLabel = new GUIStyle() { fontSize = 10 };
        s_aiLabel.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        s_aiValue = new GUIStyle() { fontSize = 10 };
        s_aiValue.normal.textColor = Color.white;

        s_perfHeader = new GUIStyle() { fontSize = 12, fontStyle = FontStyle.Bold };
        s_perfHeader.normal.textColor = perfColor;

        s_perfLabel = new GUIStyle() { fontSize = 10 };
        s_perfLabel.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

        s_perfValue = new GUIStyle() { fontSize = 10 };
        s_perfValue.normal.textColor = Color.white;

        s_footer = new GUIStyle() { fontSize = 9 };
        s_footer.normal.textColor = new Color(0.4f, 0.4f, 0.4f);

        s_stateLabel = new GUIStyle() { fontSize = 10, fontStyle = FontStyle.Bold };
    }

    void Update()
    {
        if (Input.GetKeyDown(missionKey)) TogglePanel(Panel.Mission);
        else if (Input.GetKeyDown(aiKey)) TogglePanel(Panel.AI);
        else if (Input.GetKeyDown(perfKey)) TogglePanel(Panel.Perf);
        else if (Input.GetKeyDown(KeyCode.Escape)) activePanel = Panel.None;

        if (activePanel == Panel.Perf)
        {
            fpsCount++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f)
            {
                currentFPS = fpsCount / fpsTimer;
                fpsCount = 0;
                fpsTimer = 0;
            }
        }
    }

    void TogglePanel(Panel p)
    {
        activePanel = activePanel == p ? Panel.None : p;
    }

    void OnGUI()
    {
        if (activePanel == Panel.None) return;

        switch (activePanel)
        {
            case Panel.Mission: DrawMissionPanel(); break;
            case Panel.AI: DrawAIPanel(); break;
            case Panel.Perf: DrawPerfPanel(); break;
        }
    }

    private void DrawMissionPanel()
    {
        float x = Screen.width - 370f;
        float y = 10f;
        float w = 360f;
        float h = 480f;

        DrawPanelBg(x, y, w, h, missionColor);

        float ly = y + 8f;
        float lh = 16f;

        GUI.Label(new Rect(x + 10, ly, w - 20, 18), "F1 — MISSION DEBUG", s_missionHeader);
        ly += 22f;

        if (MissionManager.Instance != null)
        {
            DrawField(ref ly, x + 10, w - 20, lh, "Objective", MissionManager.Instance.currentObjective.ToString(), s_missionLabel, s_missionValue);
            DrawField(ref ly, x + 10, w - 20, lh, "Phase", MissionManager.Instance.currentPhase.ToString(), s_missionLabel, s_missionValue);
            DrawField(ref ly, x + 10, w - 20, lh, "Progress", $"{MissionManager.Instance.GetProgress() * 100f:F0}%", s_missionLabel, s_missionValue);
            DrawField(ref ly, x + 10, w - 20, lh, "Escape", MissionManager.Instance.isInEscapePhase ? "YES" : "NO", s_missionLabel, s_missionValue);
        }

        ly += 6f;
        GUI.Label(new Rect(x + 10, ly, w - 20, 18), "— SECURITY —", s_missionHeader);
        ly += 18f;

        if (SecurityManager.Instance != null)
        {
            string alarm = SecurityManager.Instance.currentAlarmLevel.ToString();
            Color alarmColor = SecurityManager.Instance.currentAlarmLevel >= SecurityManager.AlarmLevel.Alert ? Color.red : Color.white;
            DrawField(ref ly, x + 10, w - 20, lh, "Alarm Level", alarm, s_missionLabel, s_missionValue);

            int activeCams = 0;
            foreach (var cam in cameras) if (cam != null && cam.isActive) activeCams++;
            DrawField(ref ly, x + 10, w - 20, lh, "Cameras Active", $"{activeCams}/{cameras.Length}", s_missionLabel, s_missionValue);

            DrawField(ref ly, x + 10, w - 20, lh, "Guards", SecurityManager.Instance.GuardCount.ToString(), s_missionLabel, s_missionValue);
            DrawField(ref ly, x + 10, w - 20, lh, "Doors", SecurityManager.Instance.DoorCount.ToString(), s_missionLabel, s_missionValue);
        }

        ly += 6f;
        GUI.Label(new Rect(x + 10, ly, w - 20, 18), "— PLAYER —", s_missionHeader);
        ly += 18f;

        if (InventoryManager.Instance != null)
            DrawField(ref ly, x + 10, w - 20, lh, "Keycards", InventoryManager.Instance.Count.ToString(), s_missionLabel, s_missionValue);
        if (CredentialManager.Instance != null)
            DrawField(ref ly, x + 10, w - 20, lh, "Credentials", CredentialManager.Instance.Count.ToString(), s_missionLabel, s_missionValue);

        GUI.Label(new Rect(x + 10, y + h - 18, w - 20, 16), "Press F1 to close", s_footer);
    }

    private void DrawAIPanel()
    {
        float x = Screen.width - 420f;
        float y = 10f;
        float w = 410f;
        float h = 520f;

        DrawPanelBg(x, y, w, h, aiColor);

        float ly = y + 8f;
        float lh = 14f;

        GUI.Label(new Rect(x + 10, ly, w - 20, 18), "F2 — AI DEBUG", s_aiHeader);
        ly += 22f;

        for (int i = 0; i < guards.Length && i < 6; i++)
        {
            var g = guards[i];
            if (g == null) continue;

            Color stateColor = g.CurrentState switch
            {
                GuardFSM.GuardState.Chase => Color.red,
                GuardFSM.GuardState.Suspicious => Color.yellow,
                GuardFSM.GuardState.Search => Color.magenta,
                GuardFSM.GuardState.Investigate => new Color(1f, 0.5f, 0f),
                _ => Color.white
            };

            s_stateLabel.normal.textColor = stateColor;
            GUI.Label(new Rect(x + 10, ly, w - 20, 16), $"— {g.name} {(g.isElite ? "[ELITE]" : "")} —", s_stateLabel);
            ly += 16f;

            DrawField(ref ly, x + 10, w - 20, lh, "State", g.CurrentState.ToString(), s_aiLabel, s_aiValue);
            DrawField(ref ly, x + 10, w - 20, lh, "Vision", $"{g.visionAngle:F0}° / {g.visionRange:F0}m", s_aiLabel, s_aiValue);

            float alertPct = g.DisplayAlertLevel;
            DrawField(ref ly, x + 10, w - 20, lh, "Alert", $"{alertPct:F0}%", s_aiLabel, s_aiValue);

            float suspPct = g.CurrentSuspicionMeter / Mathf.Max(g.suspicionTime, 0.01f) * 100f;
            if (g.CurrentState == GuardFSM.GuardState.Suspicious)
                DrawField(ref ly, x + 10, w - 20, lh, "Suspicion", $"{suspPct:F0}%", s_aiLabel, s_aiValue);

            DrawField(ref ly, x + 10, w - 20, lh, "Target", $"{g.CurrentTarget.x:F1}, {g.CurrentTarget.z:F1}", s_aiLabel, s_aiValue);

            ly += 4f;
        }

        ly += 4f;
        GUI.Label(new Rect(x + 10, ly, w - 20, 16), "— CAMERAS —", s_aiHeader);
        ly += 16f;

        for (int i = 0; i < cameras.Length && i < 8; i++)
        {
            var c = cameras[i];
            if (c == null) continue;

            string status = c.isActive ? $"ACTIVE [{c.cameraID}]" : $"DISABLED [{c.cameraID}]";
            float det = c.GetDetectionProgress();

            s_stateLabel.normal.textColor = det > 0f ? Color.red : (c.isActive ? Color.green : Color.gray);
            string detText = det > 0f ? $"Detect: {det * 100:F0}%" : "";
            DrawField(ref ly, x + 10, w - 20, lh, status, detText, s_aiLabel, s_stateLabel);
        }

        GUI.Label(new Rect(x + 10, y + h - 18, w - 20, 16), "Press F2 to close", s_footer);
    }

    private void DrawPerfPanel()
    {
        float x = Screen.width - 370f;
        float y = 10f;
        float w = 360f;
        float h = 300f;

        DrawPanelBg(x, y, w, h, perfColor);

        float ly = y + 8f;
        float lh = 16f;

        GUI.Label(new Rect(x + 10, ly, w - 20, 18), "F3 — PERFORMANCE", s_perfHeader);
        ly += 22f;

        s_stateLabel.normal.textColor = currentFPS >= 60 ? Color.green : currentFPS >= 30 ? Color.yellow : Color.red;
        DrawField(ref ly, x + 10, w - 20, lh, "FPS", $"{currentFPS:F0}", s_perfLabel, s_stateLabel);
        DrawField(ref ly, x + 10, w - 20, lh, "Frame Time", $"{Time.unscaledDeltaTime * 1000:F1}ms", s_perfLabel, s_perfValue);
        DrawField(ref ly, x + 10, w - 20, lh, "Memory", FormatBytes(GC.GetTotalMemory(false)), s_perfLabel, s_perfValue);

        ly += 6f;
        GUI.Label(new Rect(x + 10, ly, w - 20, 16), "— ENTITIES —", s_perfHeader);
        ly += 16f;

        DrawField(ref ly, x + 10, w - 20, lh, "Guards", guards.Length.ToString(), s_perfLabel, s_perfValue);
        DrawField(ref ly, x + 10, w - 20, lh, "Cameras", cameras.Length.ToString(), s_perfLabel, s_perfValue);
        DrawField(ref ly, x + 10, w - 20, lh, "Lights", cachedLightCount.ToString(), s_perfLabel, s_perfValue);
        DrawField(ref ly, x + 10, w - 20, lh, "Colliders", cachedColliderCount.ToString(), s_perfLabel, s_perfValue);

        ly += 6f;
        GUI.Label(new Rect(x + 10, ly, w - 20, 16), "— SCORING —", s_perfHeader);
        ly += 16f;

        if (MissionScorer.Instance != null)
        {
            DrawField(ref ly, x + 10, w - 20, lh, "Camera Detections", MissionScorer.Instance.cameraDetections.ToString(), s_perfLabel, s_perfValue);
            DrawField(ref ly, x + 10, w - 20, lh, "Guard Encounters", MissionScorer.Instance.guardDetections.ToString(), s_perfLabel, s_perfValue);
            DrawField(ref ly, x + 10, w - 20, lh, "Alarms", MissionScorer.Instance.alarmsTriggered.ToString(), s_perfLabel, s_perfValue);

            float elapsed = Time.time - MissionScorer.Instance.missionStartTime;
            DrawField(ref ly, x + 10, w - 20, lh, "Time", FormatTime(elapsed), s_perfLabel, s_perfValue);
        }

        GUI.Label(new Rect(x + 10, y + h - 18, w - 20, 16), "Press F3 to close", s_footer);
    }

    private void DrawPanelBg(float x, float y, float w, float h, Color accent)
    {
        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = accent * 0.8f;
        GUI.DrawTexture(new Rect(x, y, w, 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 2f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - 2f, y, 2f, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - 2f, w, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawField(ref float y, float x, float w, float h, string label, string val, GUIStyle labelStyle, GUIStyle valueStyle)
    {
        GUI.Label(new Rect(x, y, w * 0.55f, h), label, labelStyle);
        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, h), val, valueStyle);
        y += h;
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60);
        int s = Mathf.FloorToInt(seconds % 60);
        return $"{m:D2}:{s:D2}";
    }

    private string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
        if (bytes >= 1024) return $"{bytes / 1024f:F1} KB";
        return $"{bytes} B";
    }
}
