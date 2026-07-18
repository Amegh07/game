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

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
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

        void OnGUI()
        {
            if (timer <= 0f || string.IsNullOrEmpty(currentMessage)) return;

            timer -= Time.deltaTime;

            GUI.color = currentColor;
            GUI.skin.box.fontSize = 14;
            GUI.skin.box.alignment = TextAnchor.MiddleCenter;
            GUI.skin.box.normal.textColor = Color.white;

            float width = 380f;
            float height = 50f;
            Rect rect = new Rect(
                Screen.width / 2f - width / 2f,
                Screen.height / 2f + 70f,
                width,
                height
            );

            GUI.Box(rect, currentMessage);
            GUI.color = Color.white;
        }
    }
}
