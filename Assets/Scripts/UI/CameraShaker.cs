using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance { get; private set; }

    private Vector3 originalPos;
    private bool showFlash;
    private Color flashColor;
    private float flashTimer;
    private Coroutine shakeRoutine;

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

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnAlarmLevelChanged += HandleAlarmChange;
    }

    void OnDisable()
    {
        if (SecurityManager.Instance != null)
            SecurityManager.Instance.OnAlarmLevelChanged -= HandleAlarmChange;
    }

    void OnGUI()
    {
        if (!showFlash) return;
        GUI.color = flashColor;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void HandleAlarmChange(SecurityManager.AlarmLevel oldLevel, SecurityManager.AlarmLevel newLevel)
    {
        if (newLevel == SecurityManager.AlarmLevel.Lockdown)
        {
            Shake(0.15f, 0.8f);
            ScreenFlash(new Color(1f, 0f, 0f, 0.3f), 0.4f);
        }
        else if (newLevel == SecurityManager.AlarmLevel.Alert)
        {
            Shake(0.08f, 0.4f);
            ScreenFlash(new Color(1f, 0.2f, 0f, 0.2f), 0.25f);
        }
    }

    public void Shake(float magnitude, float duration)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(magnitude, duration));
    }

    public void ScreenFlash(Color color, float duration)
    {
        flashColor = color;
        showFlash = true;
        flashTimer = duration;
    }

    void Update()
    {
        if (showFlash)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
                showFlash = false;
        }
    }

    IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : transform;
        originalPos = cam.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cam.localPosition = originalPos + new Vector3(x, y, 0f);
            magnitude = Mathf.Lerp(magnitude, 0f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.localPosition = originalPos;
        shakeRoutine = null;
    }
}
