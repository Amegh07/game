using System.Collections.Generic;
using UnityEngine;

namespace MuseumHeist.Cyber
{
    public class SecurityConsoleUI : MonoBehaviour
    {
        [SerializeField] private Color backgroundColor = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        [SerializeField] private Color accentColor = new Color(0f, 0.6f, 1f);
        [SerializeField] private Color successColor = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color failureColor = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color textColor = new Color(0.85f, 0.85f, 0.9f);
        [SerializeField] private Color mutedColor = new Color(0.5f, 0.5f, 0.55f);

        [SerializeField] private float logScrollPosition = 0f;
        [SerializeField] private Vector2 scrollPosition;

        private TerminalController activeTerminal;
        private bool isVisible;
        private string statusMessage = "";
        private float statusTimer = 0f;
        private CursorLockMode cursorWasLocked;
        private bool cursorWasVisible;

        private const float PANEL_MARGIN = 40f;
        private const float HEADER_HEIGHT = 50f;
        private const float LOG_HEIGHT = 180f;

        public bool IsVisible => isVisible;

        void OnEnable()
        {
            Hide();
        }

        public void Show(TerminalController terminal)
        {
            activeTerminal = terminal;
            isVisible = true;
            statusMessage = "Connected. Select an action.";
            statusTimer = 3f;

            cursorWasLocked = Cursor.lockState;
            cursorWasVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Hide()
        {
            isVisible = false;
            activeTerminal = null;

            Cursor.lockState = cursorWasLocked;
            Cursor.visible = cursorWasVisible;
        }

        public void RefreshLog()
        {
            scrollPosition = new Vector2(0f, float.MaxValue);
        }

        void Update()
        {
            if (!isVisible) return;

            if (statusTimer > 0f)
                statusTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Disconnect();
            }

            if (activeTerminal != null && (!activeTerminal.IsConnected || activeTerminal.CurrentSession == null))
            {
                Hide();
            }
        }

        private void Disconnect()
        {
            if (activeTerminal != null)
                activeTerminal.Disconnect();

            if (LaptopController.Instance != null)
                LaptopController.Instance.Disconnect();

            Hide();
        }

        void OnGUI()
        {
            if (!isVisible || activeTerminal == null) return;

            DrawBackground();
            DrawHeader();
            DrawUserPanel();
            DrawActionPanel();
            DrawLogPanel();
            DrawStatusMessage();
            DrawFooter();
        }

        private void DrawBackground()
        {
            Rect fullRect = new Rect(0, 0, Screen.width, Screen.height);

            Color original = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(fullRect, Texture2D.whiteTexture);
            GUI.color = original;
        }

        private void DrawHeader()
        {
            Rect headerRect = new Rect(PANEL_MARGIN, 10, Screen.width - PANEL_MARGIN * 2, HEADER_HEIGHT);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = accentColor;
            style.alignment = TextAnchor.MiddleLeft;

            string terminalName = activeTerminal.Config != null
                ? activeTerminal.Config.terminalName
                : activeTerminal.TerminalID;

            string accessLevel = activeTerminal.Config != null
                ? activeTerminal.Config.accessLevelLabel
                : "SECURE";

            string alarmText = "";
            string alarmColor = "#4a4";
            if (SecurityManager.Instance != null)
            {
                switch (SecurityManager.Instance.currentAlarmLevel)
                {
                    case SecurityManager.AlarmLevel.Normal:
                        alarmText = "NORMAL";
                        alarmColor = "#4a4";
                        break;
                    case SecurityManager.AlarmLevel.Suspicious:
                        alarmText = "SUSPICIOUS";
                        alarmColor = "#aa4";
                        break;
                    case SecurityManager.AlarmLevel.Alert:
                        alarmText = "ALERT";
                        alarmColor = "#a44";
                        break;
                    case SecurityManager.AlarmLevel.Lockdown:
                        alarmText = "LOCKDOWN";
                        alarmColor = "#a24";
                        break;
                }
            }

            GUI.Label(headerRect, $"  MUSEUM SECURITY CONSOLE  |  {terminalName}", style);

            GUIStyle alarmStyle = new GUIStyle(GUI.skin.label);
            alarmStyle.fontSize = 14;
            alarmStyle.fontStyle = FontStyle.Bold;
            alarmStyle.alignment = TextAnchor.MiddleRight;
            alarmStyle.normal.textColor = textColor;
            GUI.Label(headerRect, $"ALARM: <color={alarmColor}>{alarmText}</color>  |  {accessLevel}", alarmStyle);

            DrawHorizontalLine(PANEL_MARGIN, HEADER_HEIGHT + 14, Screen.width - PANEL_MARGIN * 2, accentColor);
        }

        private void DrawUserPanel()
        {
            float panelWidth = 250f;
            float panelX = PANEL_MARGIN;
            float panelY = HEADER_HEIGHT + 30f;
            float panelHeight = 130f;

            Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            DrawPanelBackground(panelRect);

            NetworkSession session = activeTerminal.CurrentSession;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 13;
            labelStyle.normal.textColor = textColor;
            labelStyle.alignment = TextAnchor.UpperLeft;

            GUIStyle valueStyle = new GUIStyle(GUI.skin.label);
            valueStyle.fontSize = 13;
            valueStyle.fontStyle = FontStyle.Bold;
            valueStyle.normal.textColor = successColor;
            valueStyle.alignment = TextAnchor.UpperLeft;

            GUI.Label(new Rect(panelX + 10, panelY + 8, panelWidth - 20, 20), "SESSION", labelStyle);

            labelStyle.normal.textColor = mutedColor;
            GUI.Label(new Rect(panelX + 10, panelY + 30, 80, 20), "User:", labelStyle);
            valueStyle.normal.textColor = textColor;
            GUI.Label(new Rect(panelX + 100, panelY + 30, panelWidth - 110, 20),
                session?.UserName ?? "—", valueStyle);

            labelStyle.normal.textColor = mutedColor;
            GUI.Label(new Rect(panelX + 10, panelY + 52, 80, 20), "Role:", labelStyle);
            valueStyle.normal.textColor = accentColor;
            GUI.Label(new Rect(panelX + 100, panelY + 52, panelWidth - 110, 20),
                session != null ? session.Role.ToString() : "—", valueStyle);

            labelStyle.normal.textColor = mutedColor;
            GUI.Label(new Rect(panelX + 10, panelY + 74, 80, 20), "Status:", labelStyle);
            valueStyle.normal.textColor = activeTerminal.IsConnected ? successColor : failureColor;
            GUI.Label(new Rect(panelX + 100, panelY + 74, panelWidth - 110, 20),
                activeTerminal.IsConnected ? "Connected" : "Disconnected", valueStyle);

            string permCount = session != null ? session.Permissions.Count.ToString() : "0";
            labelStyle.normal.textColor = mutedColor;
            GUI.Label(new Rect(panelX + 10, panelY + 96, 80, 20), "Perms:", labelStyle);
            valueStyle.normal.textColor = textColor;
            GUI.Label(new Rect(panelX + 100, panelY + 96, panelWidth - 110, 20), permCount, valueStyle);
        }

        private void DrawActionPanel()
        {
            float panelX = PANEL_MARGIN + 260f;
            float panelY = HEADER_HEIGHT + 30f;
            float panelWidth = Screen.width - PANEL_MARGIN * 2 - 260f;
            float panelHeight = Screen.height - HEADER_HEIGHT - LOG_HEIGHT - 70f;

            Rect panelRect = new Rect(panelX, panelY, panelWidth, panelHeight);
            DrawPanelBackground(panelRect);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 13;
            headerStyle.normal.textColor = textColor;
            GUI.Label(new Rect(panelX + 10, panelY + 8, panelWidth - 20, 20), "AVAILABLE ACTIONS", headerStyle);

            List<TerminalActionEntry> actions = activeTerminal.GetAuthorizedActions();

            if (actions.Count == 0)
            {
                GUIStyle emptyStyle = new GUIStyle(GUI.skin.label);
                emptyStyle.fontSize = 12;
                emptyStyle.normal.textColor = mutedColor;
                emptyStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(panelX, panelY + 30, panelWidth, panelHeight - 30),
                    "No actions available for your role.", emptyStyle);
                return;
            }

            float buttonY = panelY + 30f;
            float buttonHeight = 32f;
            float buttonSpacing = 4f;
            float buttonWidth = panelWidth - 20f;

            GUIStyle actionStyle = new GUIStyle(GUI.skin.button);
            actionStyle.fontSize = 12;
            actionStyle.alignment = TextAnchor.MiddleLeft;
            actionStyle.normal.textColor = textColor;
            actionStyle.hover.textColor = Color.white;
            actionStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.2f));
            actionStyle.hover.background = MakeTex(2, 2, new Color(0.2f, 0.25f, 0.35f));

            foreach (var action in actions)
            {
                Rect btnRect = new Rect(panelX + 10, buttonY, buttonWidth, buttonHeight);

                if (GUI.Button(btnRect, $"  {action.displayName}"))
                {
                    activeTerminal.ExecuteAction(action);
                    statusMessage = $"Executing: {action.displayName}...";
                    statusTimer = 3f;
                }

                GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                descStyle.fontSize = 10;
                descStyle.normal.textColor = mutedColor;
                descStyle.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(btnRect.x + 180, btnRect.y, btnRect.width - 190, btnRect.height),
                    action.description, descStyle);

                buttonY += buttonHeight + buttonSpacing;
            }
        }

        private void DrawLogPanel()
        {
            float panelX = PANEL_MARGIN;
            float panelY = Screen.height - LOG_HEIGHT - PANEL_MARGIN - 10f;
            float panelWidth = Screen.width - PANEL_MARGIN * 2;

            Rect panelRect = new Rect(panelX - 5, panelY - 5, panelWidth + 10, LOG_HEIGHT + 10);
            DrawPanelBackground(panelRect);

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 13;
            headerStyle.normal.textColor = textColor;
            GUI.Label(new Rect(panelX, panelY + 5, panelWidth, 20), "AUDIT LOG", headerStyle);

            IReadOnlyList<TerminalLogEntry> entries = null;
            if (activeTerminal.Log != null)
                entries = activeTerminal.Log.GetEntries();

            float logAreaY = panelY + 28f;
            float logAreaHeight = LOG_HEIGHT - 38f;
            float entryHeight = 18f;
            float totalHeight = entries != null ? entries.Count * entryHeight : 0f;

            Rect viewRect = new Rect(panelX, logAreaY, panelWidth - 20f, Mathf.Max(totalHeight, logAreaHeight));
            Rect scrollRect = new Rect(panelX, logAreaY, panelWidth - 20f, logAreaHeight);

            scrollPosition = GUI.BeginScrollView(scrollRect, scrollPosition, viewRect);

            if (entries != null)
            {
                float entryY = viewRect.y;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    GUIStyle logStyle = new GUIStyle(GUI.skin.label);
                    logStyle.fontSize = 11;
                    logStyle.normal.textColor = entry.Success ? textColor : failureColor;
                    logStyle.alignment = TextAnchor.UpperLeft;

                    string prefix = entry.Success ? "✓" : "✗";
                    GUI.Label(new Rect(viewRect.x + 5, entryY, viewRect.width - 10, entryHeight),
                        $"{prefix} [{entry.FormattedTime}] {entry.UserName}: {entry.Action} — {entry.Result}",
                        logStyle);

                    entryY += entryHeight;
                }
            }

            GUI.EndScrollView();
        }

        private void DrawStatusMessage()
        {
            if (statusTimer <= 0f || string.IsNullOrEmpty(statusMessage)) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 13;
            style.normal.textColor = accentColor;
            style.alignment = TextAnchor.LowerCenter;

            float alpha = Mathf.Clamp01(statusTimer / 1f);
            Color c = style.normal.textColor;
            c.a = alpha;
            style.normal.textColor = c;

            GUI.Label(new Rect(0, Screen.height - LOG_HEIGHT - PANEL_MARGIN - 40f, Screen.width, 30f),
                statusMessage, style);
        }

        private void DrawFooter()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 10;
            style.normal.textColor = mutedColor;
            style.alignment = TextAnchor.LowerRight;

            GUI.Label(new Rect(PANEL_MARGIN, Screen.height - 22, Screen.width - PANEL_MARGIN * 2, 20),
                "Press ESC to disconnect  |  Museum Heist — Cyber Operations System v1.0", style);
        }

        private void DrawPanelBackground(Rect rect)
        {
            Color original = GUI.color;
            GUI.color = new Color(0.12f, 0.12f, 0.15f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = original;

            DrawBorder(rect, new Color(0.2f, 0.2f, 0.25f, 0.9f));
        }

        private void DrawHorizontalLine(float x, float y, float width, Color color)
        {
            Color original = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, width, 1f), Texture2D.whiteTexture);
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

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
