using UnityEngine;

namespace MuseumHeist.AccessControl
{
    public class DoorUIFeedback : MonoBehaviour
    {
        public static DoorUIFeedback Instance { get; private set; }

        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private Color deniedColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color grantedColor = new Color(0.3f, 1f, 0.3f);

    private string currentMessage;
    private Color currentColor;
    private float timer;
    private GUIStyle messageStyle;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            messageStyle = new GUIStyle();
            messageStyle.fontSize = 14;
            messageStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle.normal.textColor = Color.white;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowAccessDenied(string doorName, KeycardType required)
        {
            ShowMessage($"ACCESS DENIED\n{doorName} requires {required} keycard", deniedColor);
        }

        public void ShowAccessGranted(string doorName)
        {
            ShowMessage($"ACCESS GRANTED\n{doorName} unlocked", grantedColor);
        }

        public void ShowDoorLocked(string doorName)
        {
            ShowMessage($"LOCKED\n{doorName} is locked down", deniedColor);
        }

        private void ShowMessage(string message, Color color)
        {
            currentMessage = message;
            currentColor = color;
            timer = displayDuration;
        }

        void Update()
        {
            if (timer > 0f)
                timer -= Time.deltaTime;
        }

        void OnGUI()
        {
            if (timer <= 0f || string.IsNullOrEmpty(currentMessage)) return;

            float width = 380f;
            float height = 50f;
            Rect rect = new Rect(
                Screen.width / 2f - width / 2f,
                Screen.height / 2f + 70f,
                width,
                height
            );

            Color original = GUI.color;
            GUI.color = currentColor;
            GUI.Box(rect, currentMessage, messageStyle);
            GUI.color = original;
        }
    }
}
