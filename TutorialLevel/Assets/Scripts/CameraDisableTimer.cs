using UnityEngine;

public class CameraDisableTimer : MonoBehaviour
{
    [SerializeField] private float defaultDuration = 30f;
    [SerializeField] private string eastCameraID = "camera_east";

    private float remainingTime;
    private bool timerActive;
    private string activeCameraID;

    public static CameraDisableTimer Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.OnCameraDisabled += HandleCameraDisabled;
            SecurityManager.Instance.OnCameraEnabled += HandleCameraEnabled;
        }
    }

    void OnDisable()
    {
        if (SecurityManager.Instance != null)
        {
            SecurityManager.Instance.OnCameraDisabled -= HandleCameraDisabled;
            SecurityManager.Instance.OnCameraEnabled -= HandleCameraEnabled;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void HandleCameraDisabled(string cameraID)
    {
        if (cameraID == eastCameraID)
        {
            StartTimer(cameraID, defaultDuration);
        }
    }

    private void HandleCameraEnabled(string cameraID)
    {
        if (cameraID == activeCameraID)
        {
            timerActive = false;
        }
    }

    public void StartTimer(string cameraID, float duration)
    {
        activeCameraID = cameraID;
        remainingTime = duration;
        timerActive = true;
        Debug.Log($"CameraDisableTimer: {cameraID} disabled — {duration}s countdown started.");
    }

    void Update()
    {
        if (!timerActive) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            timerActive = false;
            if (SecurityManager.Instance != null && !string.IsNullOrEmpty(activeCameraID))
            {
                SecurityManager.Instance.EnableCamera(activeCameraID);
                Debug.Log($"CameraDisableTimer: {activeCameraID} re-enabled.");
            }
        }
    }

    void OnGUI()
    {
        if (!timerActive) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;
        style.alignment = TextAnchor.UpperCenter;

        string label = timerActive
            ? $"EAST CAMERAS OFFLINE: {remainingTime:F1}s"
            : "";

        GUI.Label(new Rect(0, 160, Screen.width, 30), label, style);
    }
}
