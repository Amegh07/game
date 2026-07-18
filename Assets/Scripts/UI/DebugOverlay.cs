using UnityEngine;
using MuseumHeist.AccessControl;
using MuseumHeist.Cyber;

public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private bool showOverlay = true;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showOverlay = !showOverlay;
    }

    void OnGUI()
    {
        if (!showOverlay) return;

        Color original = GUI.color;
        Color bgColor = new Color(0f, 0f, 0f, 0.75f);

        float x = 10f;
        float y = Screen.height - 260f;
        float width = 380f;
        float height = 250f;

        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = original;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 10;
        labelStyle.normal.textColor = new Color(0.3f, 0.8f, 1f);
        labelStyle.alignment = TextAnchor.UpperLeft;

        GUIStyle valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.fontSize = 10;
        valueStyle.normal.textColor = Color.white;
        valueStyle.alignment = TextAnchor.UpperLeft;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 11;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.5f, 1f, 0.5f);

        float lineY = y + 8;
        float lineHeight = 14f;
        float labelWidth = 160f;

        GUI.Label(new Rect(x + 8, lineY, width - 16, 16), "=== MUSEUM HEIST DEBUG OVERLAY ===", headerStyle);
        lineY += lineHeight + 2;

        if (MissionManager.Instance != null)
        {
            DrawField(ref lineY, x, labelWidth, lineHeight, "Current Objective", MissionManager.Instance.currentObjective.ToString(), labelStyle, valueStyle);
            DrawField(ref lineY, x, labelWidth, lineHeight, "Mission Phase", MissionManager.Instance.currentPhase.ToString(), labelStyle, valueStyle);
            DrawField(ref lineY, x, labelWidth, lineHeight, "Progress", $"{MissionManager.Instance.GetProgress() * 100f:F0}%", labelStyle, valueStyle);
        }

        if (SecurityManager.Instance != null)
        {
            DrawField(ref lineY, x, labelWidth, lineHeight, "Alarm Level", SecurityManager.Instance.currentAlarmLevel.ToString(), labelStyle, valueStyle);
        }

        if (CredentialManager.Instance != null)
        {
            DrawField(ref lineY, x, labelWidth, lineHeight, "Credentials", CredentialManager.Instance.Count.ToString(), labelStyle, valueStyle);
        }

        if (InventoryManager.Instance != null)
        {
            DrawField(ref lineY, x, labelWidth, lineHeight, "Keycards", InventoryManager.Instance.Count.ToString(), labelStyle, valueStyle);
        }

        if (LaptopController.Instance != null)
        {
            DrawField(ref lineY, x, labelWidth, lineHeight, "Laptop Connected", LaptopController.Instance.IsConnected ? "YES" : "NO", labelStyle, valueStyle);
        }

        string detectionStatus = "NONE";
        if (SecurityManager.Instance != null &&
            SecurityManager.Instance.currentAlarmLevel == SecurityManager.AlarmLevel.Alert)
        {
            detectionStatus = "ALERT";
        }
        DrawField(ref lineY, x, labelWidth, lineHeight, "Detection Status", detectionStatus, labelStyle, valueStyle);

        GUI.Label(new Rect(x + 8, y + height - 18, width - 16, 16), $"Press {toggleKey} to hide", labelStyle);
    }

    private void DrawField(ref float y, float x, float labelWidth, float height, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
    {
        GUI.Label(new Rect(x + 8, y, labelWidth, height), label, labelStyle);
        GUI.Label(new Rect(x + 8 + labelWidth, y, 200, height), value, valueStyle);
        y += height;
    }
}
