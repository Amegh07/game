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

    [Header("Stealth Indicator")]
    [SerializeField] private float stealthScanRadius = 12f;
    [SerializeField] private Color safeColor = new Color(0.2f, 0.9f, 0.2f, 0.6f);
    [SerializeField] private Color warningColor = new Color(1f, 0.8f, 0.2f, 0.8f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.2f, 0.2f, 1f);

    private float stealthDanger;
    private float detectionProgress;
    private bool beingDetected;
    private string detectionSource = "";

    private string notificationMessage = "";
    private float notificationTimer = 0f;
    private Color notificationColor;
    private readonly Queue<string> notificationQueue = new();
    private readonly Dictionary<ObjectiveID, string> objectiveNames = new();

    private readonly List<GuardFSM> registeredGuards = new();
    private readonly List<SecurityCamera> registeredCameras = new();
    private bool eventsSubscribed = false;

    private GUIStyle objectiveStyle;
    private GUIStyle objectiveHeaderStyle;
    private GUIStyle notificationStyle;
    private GUIStyle stealthIconStyle;
    private GUIStyle stealthLabelStyle;
    private GUIStyle detectionStyle;
    private GUIStyle phaseStyle;

    public static MissionHUD Instance { get; private set; }

    public void RegisterGuard(GuardFSM guard)
    {
        if (guard != null && !registeredGuards.Contains(guard))
            registeredGuards.Add(guard);
    }

    public void UnregisterGuard(GuardFSM guard)
    {
        registeredGuards.Remove(guard);
    }

    public void RegisterCamera(SecurityCamera cam)
    {
        if (cam != null && !registeredCameras.Contains(cam))
            registeredCameras.Add(cam);
    }

    public void UnregisterCamera(SecurityCamera cam)
    {
        registeredCameras.Remove(cam);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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

        objectiveNames[ObjectiveID.Tutorial_ReachKeycard] = "Reach the training keycard";
        objectiveNames[ObjectiveID.Tutorial_PickupKeycard] = "Pick up the Training Keycard";
        objectiveNames[ObjectiveID.Tutorial_OpenDoor] = "Use the keycard to open the door";
        objectiveNames[ObjectiveID.Tutorial_SneakPastGuard] = "Sneak past the patrolling guard";
        objectiveNames[ObjectiveID.Tutorial_DisableCamera] = "Disable the security camera";
        objectiveNames[ObjectiveID.Tutorial_StealArtifact] = "Steal the training artifact";
        objectiveNames[ObjectiveID.Tutorial_Escape] = "Escape the training facility";
        objectiveNames[ObjectiveID.Tutorial_Complete] = "Training Complete";
        objectiveNames[ObjectiveID.Tutorial_Movement] = "Learn movement controls";
        objectiveNames[ObjectiveID.Tutorial_Interaction] = "Interact with the training station";
        objectiveNames[ObjectiveID.Tutorial_CredentialPickup] = "Obtain the Staff Credential";
        objectiveNames[ObjectiveID.Tutorial_LockedDoor] = "Open the locked staff door";
        objectiveNames[ObjectiveID.Tutorial_Authentication] = "Authenticate at the training terminal";
        objectiveNames[ObjectiveID.Tutorial_Authorization] = "Check your access permissions";
        objectiveNames[ObjectiveID.Tutorial_CameraAwareness] = "Avoid the security camera";
        objectiveNames[ObjectiveID.Tutorial_GuardAwareness] = "Evade the patrolling guard";
        objectiveNames[ObjectiveID.Tutorial_SecurityConsole] = "Use the security console";
        objectiveNames[ObjectiveID.Tutorial_Exit] = "Reach the training exit";

        objectiveNames[ObjectiveID.Academy_EnterReception] = "Enter the Academy reception";
        objectiveNames[ObjectiveID.Academy_CompleteCheckin] = "Complete security check-in";
        objectiveNames[ObjectiveID.Academy_AttendBriefing] = "Attend the security briefing";
        objectiveNames[ObjectiveID.Academy_CollectEquipment] = "Collect training equipment";
        objectiveNames[ObjectiveID.Academy_CompleteMovementCourse] = "Complete the movement course";
        objectiveNames[ObjectiveID.Academy_CompleteAccessControlLab] = "Complete the access-control lab";
        objectiveNames[ObjectiveID.Academy_CompleteCameraLab] = "Complete the camera-detection lab";
        objectiveNames[ObjectiveID.Academy_CompleteGuardAwarenessLab] = "Complete the guard-awareness lab";
        objectiveNames[ObjectiveID.Academy_CompleteStealthMaze] = "Complete the stealth maze";
        objectiveNames[ObjectiveID.Academy_CompleteSecOpsTraining] = "Complete Security Operations training";
        objectiveNames[ObjectiveID.Academy_SimFindStaffCredential] = "Find the simulated Staff Credential";
        objectiveNames[ObjectiveID.Academy_SimUnlockStaffDoor] = "Unlock the simulated staff door";
        objectiveNames[ObjectiveID.Academy_SimAvoidCamera] = "Avoid the simulated camera";
        objectiveNames[ObjectiveID.Academy_SimAvoidGuard] = "Avoid the simulated guard";
        objectiveNames[ObjectiveID.Academy_SimAuthenticate] = "Authenticate at the simulator terminal";
        objectiveNames[ObjectiveID.Academy_SimDisableCameras] = "Disable simulated cameras";
        objectiveNames[ObjectiveID.Academy_SimEnterRestrictedArea] = "Enter the restricted simulation area";
        objectiveNames[ObjectiveID.Academy_SimRetrieveArtifact] = "Retrieve the simulated artifact";
        objectiveNames[ObjectiveID.Academy_SimEscape] = "Escape the simulation";
        objectiveNames[ObjectiveID.Academy_Graduate] = "Graduate from the Security Academy";
        objectiveStyle = new GUIStyle();
        objectiveStyle.fontSize = 13;

        objectiveHeaderStyle = new GUIStyle();
        objectiveHeaderStyle.fontSize = 10;

        notificationStyle = new GUIStyle();
        notificationStyle.fontSize = 16;
        notificationStyle.alignment = TextAnchor.MiddleCenter;

        stealthIconStyle = new GUIStyle();
        stealthIconStyle.fontSize = 14;
        stealthIconStyle.fontStyle = FontStyle.Bold;
        stealthIconStyle.alignment = TextAnchor.MiddleCenter;

        stealthLabelStyle = new GUIStyle();
        stealthLabelStyle.fontSize = 9;
        stealthLabelStyle.alignment = TextAnchor.MiddleCenter;

        detectionStyle = new GUIStyle();
        detectionStyle.fontSize = 9;
        detectionStyle.alignment = TextAnchor.MiddleCenter;

        phaseStyle = new GUIStyle();
        phaseStyle.fontSize = 10;
        phaseStyle.alignment = TextAnchor.UpperRight;
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
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnObjectiveStarted += HandleObjectiveStarted;
            MissionManager.Instance.OnObjectiveCompleted += HandleObjectiveCompleted;
            MissionManager.Instance.OnPhaseChanged += HandlePhaseChanged;
            eventsSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!eventsSubscribed) return;
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnObjectiveStarted -= HandleObjectiveStarted;
            MissionManager.Instance.OnObjectiveCompleted -= HandleObjectiveCompleted;
            MissionManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
        eventsSubscribed = false;
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
        notificationColor = color;
        notificationTimer = objectiveDisplayTime;
    }

    void Update()
    {
        if (notificationTimer > 0f)
            notificationTimer -= Time.deltaTime;

        UpdateStealthIndicator();
    }

    private void UpdateStealthIndicator()
    {
        stealthDanger = 0f;
        detectionProgress = 0f;
        beingDetected = false;
        detectionSource = "";

        if (Camera.main == null) return;

        Vector3 playerPos = Camera.main.transform.position;

        for (int i = registeredGuards.Count - 1; i >= 0; i--)
        {
            GuardFSM guard = registeredGuards[i];
            if (guard == null) { registeredGuards.RemoveAt(i); continue; }

            float dist = Vector3.Distance(playerPos, guard.transform.position);
            if (dist > stealthScanRadius) continue;

            if (guard.HasPlayerInSight)
            {
                stealthDanger = Mathf.Max(stealthDanger, 1f);
                beingDetected = true;
                detectionSource = "GUARD";
                float suspicionFrac = guard.CurrentSuspicionMeter / Mathf.Max(guard.suspicionTime, 0.01f);
                detectionProgress = Mathf.Max(detectionProgress, suspicionFrac);
            }
            else if (guard.CurrentState == GuardFSM.GuardState.Suspicious)
            {
                stealthDanger = Mathf.Max(stealthDanger, 0.6f);
                detectionSource = "GUARD (SUSPICIOUS)";
                float suspicionFrac = guard.CurrentSuspicionMeter / Mathf.Max(guard.suspicionTime, 0.01f);
                detectionProgress = Mathf.Max(detectionProgress, suspicionFrac);
            }
            else
            {
                float alertFrac = guard.DisplayAlertLevel / 100f;
                if (alertFrac > 0.1f)
                {
                    float proximity = 1f - (dist / stealthScanRadius);
                    stealthDanger = Mathf.Max(stealthDanger, alertFrac * proximity * 0.4f);
                }
            }
        }

        for (int i = registeredCameras.Count - 1; i >= 0; i--)
        {
            SecurityCamera cam = registeredCameras[i];
            if (cam == null) { registeredCameras.RemoveAt(i); continue; }

            float dist = Vector3.Distance(playerPos, cam.transform.position);
            if (dist > stealthScanRadius) continue;

            if (!cam.isActive) continue;

            float camDanger = cam.GetDetectionProgress();
            if (camDanger > 0f)
            {
                stealthDanger = Mathf.Max(stealthDanger, 0.7f + camDanger * 0.3f);
                detectionProgress = Mathf.Max(detectionProgress, camDanger);
                beingDetected = true;
                detectionSource = "CAMERA";
            }
        }

        stealthDanger = Mathf.Clamp01(stealthDanger);
    }

    void OnGUI()
    {
        DrawObjectiveDisplay();
        DrawNotification();
        DrawStealthIndicator();
        DrawDetectionMeter();
        DrawPhaseIndicator();
        DrawDetectionWarning();
    }

    private void DrawObjectiveDisplay()
    {
        if (MissionManager.Instance == null) return;

        ObjectiveID current = MissionManager.Instance.GetCurrentObjective();
        string objectiveText = objectiveNames.TryGetValue(current, out string name) ? name : current.ToString();
        string formattedName = FormatObjectiveName(objectiveText);

        Rect bgRect = new Rect(15, 15, 320, 60);
        Color bgColor = new Color(0.05f, 0.05f, 0.08f, 0.7f);
        Color original = GUI.color;
        GUI.color = bgColor;
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
        GUI.color = original;

        DrawBorder(bgRect, new Color(0.15f, 0.15f, 0.2f, 0.8f));

        objectiveHeaderStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        GUI.Label(new Rect(25, 18, 300, 16), "CURRENT OBJECTIVE", objectiveHeaderStyle);

        objectiveStyle.normal.textColor = MissionManager.Instance.currentPhase == MissionPhase.Escape ? alertColor : objectiveColor;
        GUI.Label(new Rect(25, 36, 300, 30), formattedName, objectiveStyle);

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

        float fadeTime = 0.4f;
        float fadeIn = Mathf.Clamp01((objectiveDisplayTime - notificationTimer) / fadeTime);
        float fadeOut = Mathf.Clamp01(notificationTimer / fadeTime);
        float alpha = Mathf.Min(fadeIn, fadeOut);

        Color bgColor = new Color(0.05f, 0.05f, 0.1f, 0.85f * alpha);
        Color textColor = notificationColor;
        textColor.a = alpha;

        notificationStyle.normal.textColor = textColor;
        notificationStyle.normal.background = MakeTex(2, 2, bgColor);

        float width = 500f;
        float height = 50f;
        Rect rect = new Rect(Screen.width / 2f - width / 2f, 90f, width, height);

        Color original = GUI.color;
        GUI.color = Color.white;
        GUI.Box(rect, notificationMessage, notificationStyle);
        GUI.color = original;
    }

    private void DrawStealthIndicator()
    {
        float iconSize = 28f;
        float padding = 8f;
        float x = Screen.width - iconSize - padding;
        float y = Screen.height - iconSize - padding - 30f;

        Color indicatorColor;
        string label;

        if (stealthDanger >= 0.9f)
        {
            indicatorColor = dangerColor;
            label = "!!";
        }
        else if (stealthDanger >= 0.4f)
        {
            indicatorColor = warningColor;
            label = "!";
        }
        else
        {
            indicatorColor = safeColor;
            label = "\u25CB";
        }

        Color original = GUI.color;
        GUI.color = new Color(indicatorColor.r, indicatorColor.g, indicatorColor.b, Mathf.Lerp(0.3f, 1f, stealthDanger));

        stealthIconStyle.normal.textColor = Color.white;
        stealthIconStyle.normal.background = MakeTex(2, 2, indicatorColor * 0.3f);

        GUI.Box(new Rect(x, y, iconSize, iconSize), label, stealthIconStyle);

        if (stealthDanger > 0.1f)
        {
            stealthLabelStyle.normal.textColor = indicatorColor;
            GUI.Label(new Rect(x - 20f, y + iconSize, iconSize + 40f, 14f),
                $"VISIBILITY: {stealthDanger * 100f:F0}%", stealthLabelStyle);
        }

        GUI.color = original;
    }

    private void DrawDetectionMeter()
    {
        if (!beingDetected || detectionProgress <= 0f) return;

        float barWidth = 140f;
        float barHeight = 14f;
        float x = Screen.width / 2f - barWidth / 2f;
        float y = Screen.height / 2f + 30f;

        Color barColor = detectionProgress >= 0.8f ? alertColor : warningColor;

        Color original = GUI.color;
        Color bgColor = new Color(0.05f, 0.05f, 0.08f, 0.7f);

        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, barWidth * detectionProgress, barHeight), Texture2D.whiteTexture);

        DrawBorder(new Rect(x, y, barWidth, barHeight), new Color(0.15f, 0.15f, 0.2f, 0.8f));

        detectionStyle.normal.textColor = barColor;

        string detText = detectionProgress >= 1f ? "DETECTED!" : $"DETECTING \u2014 {detectionSource}";
        GUI.Label(new Rect(x, y, barWidth, barHeight), detText, detectionStyle);

        GUI.color = original;
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
                case SecurityManager.AlarmLevel.Recovery:
                    alarmText = "ALARM: RECOVERING";
                    alarmColor = Color.cyan;
                    break;
            }
        }

        string displayText = $"{phaseText}  |  {alarmText}";

        Color original = GUI.color;
        GUI.color = alarmColor;
        phaseStyle.normal.textColor = phase == MissionPhase.Escape ? alertColor : alarmColor;
        GUI.Label(new Rect(Screen.width - 220, 12, 200, 20), displayText, phaseStyle);
        GUI.color = original;
    }

    private void DrawDetectionWarning()
    {
        if (detectionProgress <= 0f) return;

        float edgeThickness = Mathf.Lerp(0f, 12f, detectionProgress);
        Color edgeColor = detectionProgress >= 0.8f
            ? new Color(1f, 0.1f, 0.1f, detectionProgress * 0.6f)
            : new Color(1f, 0.6f, 0.1f, detectionProgress * 0.4f);

        Color original = GUI.color;
        GUI.color = edgeColor;

        float w = Screen.width;
        float h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, edgeThickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, h - edgeThickness, w, edgeThickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, 0, edgeThickness, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(w - edgeThickness, 0, edgeThickness, h), Texture2D.whiteTexture);

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

    private static readonly Dictionary<int, Texture2D> texCache = new();

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        int key = (width * 73856093) ^ (height * 19349663) ^ ColorToInt(color);
        if (texCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        texCache[key] = tex;
        return tex;
    }

    private static int ColorToInt(Color c)
    {
        return ((int)(c.r * 255) << 24) | ((int)(c.g * 255) << 16) | ((int)(c.b * 255) << 8) | (int)(c.a * 255);
    }
}
