using UnityEngine;

public class AudioHookPlaceholder : MonoBehaviour
{
    public enum AudioZoneType
    {
        AmbientMuseum,
        HVAC,
        ServerHum,
        CameraMotor,
        AlarmSpeaker,
        FootstepCarpet,
        FootstepTile,
        FootstepMetal,
        DoorCreak,
        EchoingHall
    }

    public AudioZoneType zoneType;
    public float radius = 5f;
    public float volume = 0.5f;

    void OnDrawGizmos()
    {
        Gizmos.color = zoneType switch
        {
            AudioZoneType.AmbientMuseum => new Color(0.3f, 0.6f, 1f, 0.2f),
            AudioZoneType.HVAC => new Color(0.6f, 0.6f, 0.8f, 0.2f),
            AudioZoneType.ServerHum => new Color(0.3f, 0.8f, 0.5f, 0.2f),
            AudioZoneType.CameraMotor => new Color(1f, 0.5f, 0f, 0.2f),
            AudioZoneType.AlarmSpeaker => new Color(1f, 0f, 0f, 0.2f),
            AudioZoneType.FootstepCarpet => new Color(0.6f, 0.3f, 0.1f, 0.2f),
            AudioZoneType.FootstepTile => new Color(0.7f, 0.7f, 0.7f, 0.2f),
            AudioZoneType.FootstepMetal => new Color(0.5f, 0.5f, 0.6f, 0.2f),
            AudioZoneType.DoorCreak => new Color(0.4f, 0.2f, 0.1f, 0.2f),
            AudioZoneType.EchoingHall => new Color(0.2f, 0.3f, 0.5f, 0.2f),
            _ => Color.white
        };
        Gizmos.DrawWireSphere(transform.position, radius);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"[Audio] {zoneType}");
#endif
    }

    // --- Static factory ---

    public static AudioHookPlaceholder Create(string name, AudioZoneType type, Vector3 pos, float radius, float vol = 0.5f, Transform parent = null)
    {
        var go = new GameObject($"Audio_{type}_{name}");
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = pos;
        var ah = go.AddComponent<AudioHookPlaceholder>();
        ah.zoneType = type;
        ah.radius = radius;
        ah.volume = vol;
        return ah;
    }
}
