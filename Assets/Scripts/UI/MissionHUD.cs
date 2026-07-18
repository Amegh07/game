using UnityEngine;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;
using System.Collections.Generic;

public class MissionHUD : MonoBehaviour
{
    [Header("Display Settings")]
    [SerializeField] private float objectiveDisplayTime = 6f;
    [SerializeField] private Color objectiveColor = new Color(0f, 0.7f, 1f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color phaseColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color alertColor = new Color(1f, 0.2f, 0.2f);

    private string notificationMessage = "";
    private float notificationTimer = 0f;
    private readonly Queue<string> notificationQueue = new();
    private readonly Dictionary<ObjectiveID, string> objectiveNames = new();

    void Awake()
    {
        objectiveNames[ObjectiveID.EnterMuseumHeist] = "Enter the Museum";
        objectiveNames[ObjectiveID.ReachLobby] = "Reach the Main Lobby";
        objectiveNames[ObjectiveID.FindStaffCredential] = "Find a Staff Credential";
        objectiveNames[ObjectiveID.AccessStaffOffice] = "Access the Staff Office";
        objectiveNames[ObjectiveID.UseStaffTerminal] = "Use the Staff Terminal";
        objectiveNames[ObjectiveID.ReachSecurityOffice] = "Reach the Security Office";
        objectiveNames[ObjectiveID.UseSecurityTerminal] = "Authenticate at Security Terminal";
        objectiveNames[ObjectiveID.DisableEastCameras] = "Disable East Wing Cameras";
        objectiveNames[ObjectiveID.ReachRestrictedCorridor] = "Reach the Restricted Corridor";
        objectiveNames[ObjectiveID.FindSecurityCredential] = "Find a Security Credential";
        objectiveNames[ObjectiveID.UnlockVaultArea] = "Unlock the Vault Area";
        objectiveNames[ObjectiveID.ReachVaultAntechamber] = "Reach the Vault Antechamber";
        objectiveNames[ObjectiveID.UseVaultTerminal] = "Authenticate at Vault Terminal";
        objectiveNames[ObjectiveID.UnlockVault] = "Unlock the Vault";
        objectiveNames[ObjectiveID.StealMainArtifact] = "Steal the Main Artifact";
        objectiveNames[ObjectiveID.EscapeMuseum] = "Escape the Museum";
        objectiveNames[ObjectiveID.HeistComplete] = "Heist Complete";
    }

    void OnEnable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnObjectiveStarted += HandleObjectiveStarted;
            MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
            MissionManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    void OnDisable()
    {
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnObjectiveStarted -= HandleObjectiveStarted;
            MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
            MissionManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandleObjectiveStarted(ObjectiveID id)
    {
        if (objectiveNames.TryGetValue(id, out string name))
            ShowNotification($"OBJECTIVE: {name}", objectiveColor);
    }

    private void HandleObjectiveCompleted(ObjectiveID id)
    {
        if (objectiveNames.TryGetValue(id, out string name))
            ShowNotification($"COMPLETED: {name}", completedColor);
    }

    private void HandlePhaseChanged(MissionPhase phase)
    {
        string phaseName = phase switch
        {
            MissionPhase.Infiltration => "PHASE: INFILTRATION",
            MissionPhase.Exploration => "PHASE: EXPLORATION",
            MissionPhase.TerminalAccess => "PHASE: TERMINAL ACCESS",
            MissionPhase.Heist => "PHASE: HEIST",
            MissionPhase.Escape => "PHASE: ESCAPE — ALARM ACTIVE",
            _ => ""
        };

        Color c = phase == MissionPhase.Escape ? alertColor : phaseColor;
        ShowNotification(phaseName, c);
    }

    public void ShowNotification(string message, Color color)
    {
        notificationMessage = message;
        notificationTimer = objectiveDisplayTime;
    }

    void Update()
    {
        if (notificationTimer > 0f)
            notificationTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        DrawObjectiveDisplay();
        DrawNotification();
        DrawPhaseIndicator();
    }

    private void DrawObjectiveDisplay()
    {
        if (MissionManager.Instance == null) return;

        ObjectiveID current = MissionManager.Instance.GetCurrentObjective();
        string objectiveText = objectiveNames.TryGetValue(current, out string name) ? name : current.ToString();
        string formattedName = FormatObjectiveName(objectiveText);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = objectiveColor;
        style.alignment = TextAnchor.UpperLeft;

        Rect bgRect = new Rect(15, 15, 320, 60);
        Color bgColor = new Color(0.05f, 0.05f, 0.08f, 0.7f);
        Color original = GUI.color;
        GUI.color = bgColor;
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
        GUI.color = original;

        DrawBorder(bgRect, new Color(0.15f, 0.15f, 0.2f, 0.8f));

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 10;
        headerStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        headerStyle.alignment = TextAnchor.UpperLeft;
        GUI.Label(new Rect(25, 18, 300, 16), "CURRENT OBJECTIVE", headerStyle);

        style.fontSize = 13;
        style.normal.textColor = MissionManager.Instance.currentPhase == MissionPhase.Escape ? alertColor : objectiveColor;
        GUI.Label(new Rect(25, 36, 300, 30), formattedName, style);

        float progress = MissionManager.Instance.GetProgress();
        Rect progressBarBg = new Rect(25, 62, 300, 4);
        GUI.color = new Color(0.2f, 0.2f, 0.25f);
        GUI.DrawTexture(progressBarBg, Texture2D.whiteTexture);
        GUI.color = objectiveColor;
        GUI.DrawTexture(new Rect(25, 62, 300 * progress, 4), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawNotification()
    {
        if (notificationTimer <= 0f || string.IsNullOrEmpty(notificationMessage)) return;

        float alpha = Mathf.Clamp01(notificationTimer / 1.5f);
        Color textColor = GUI.color;
        textColor.a = alpha;
        GUI.color = textColor;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        style.normal.background = MakeTex(2, 2, new Color(0.05f, 0.05f, 0.1f, 0.85f));
        style.alignment = TextAnchor.MiddleCenter;

        float width = 500f;
        float height = 50f;
        Rect rect = new Rect(Screen.width / 2f - width / 2f, 90f, width, height);
        GUI.Box(rect, notificationMessage, style);

        GUI.color = Color.white;
    }

    private void DrawPhaseIndicator()
    {
        if (MissionManager.Instance == null) return;

        MissionPhase phase = MissionManager.Instance.currentPhase;
        string phaseText = phase switch
        {
            MissionPhase.Infiltration => "INFILTRATION",
            MissionPhase.Exploration => "EXPLORATION",
            MissionPhase.TerminalAccess => "TERMINAL ACCESS",
            MissionPhase.Heist => "HEIST",
            MissionPhase.Escape => "ESCAPE",
            _ => ""
        };

        Color phaseDisplayColor = phase == MissionPhase.Escape ? alertColor : new Color(0.5f, 0.5f, 0.6f);

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 10;
        style.normal.textColor = phaseDisplayColor;
        style.alignment = TextAnchor.UpperRight;

        string alarmText = "";
        Color alarmColor = Color.green;
        if (SecurityManager.Instance != null)
        {
            switch (SecurityManager.Instance.currentAlarmLevel)
            {
                case SecurityManager.AlarmLevel.Normal:
                    alarmText = "ALARM: NORMAL";
                    alarmColor = Color.green;
                    break;
                case SecurityManager.AlarmLevel.Suspicious:
                    alarmText = "ALARM: SUSPICIOUS";
                    alarmColor = Color.yellow;
                    break;
                case SecurityManager.AlarmLevel.Alert:
                    alarmText = "ALARM: ALERT";
                    alarmColor = Color.red;
                    break;
                case SecurityManager.AlarmLevel.Lockdown:
                    alarmText = "ALARM: LOCKDOWN";
                    alarmColor = Color.magenta;
                    break;
            }
        }

        string displayText = $"{phaseText}  |  {alarmText}";

        Color original = GUI.color;
        GUI.color = alarmColor;
        style.normal.textColor = phase == MissionPhase.Escape ? alertColor : alarmColor;
        GUI.Label(new Rect(Screen.width - 220, 12, 200, 20), displayText, style);
        GUI.color = original;
    }

    private void DrawBorder(Rect rect, Color color)
    {
        Color original = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture);
        GUI.color = original;
    }

    private string FormatObjectiveName(string name)
    {
        return name;
    }

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
