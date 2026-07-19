using UnityEngine;

public static class MuseumLightingSystem
{
    public enum RoomMood { Warm, Cool, Cold, Vault, Office, Emergency }

    static Transform _lightRoot;

    public static void Initialize(Transform root)
    {
        _lightRoot = new GameObject("_Lighting").transform;
        _lightRoot.SetParent(root);
    }

    public static void LightRoom(float cx, float cz, float w, float d, float wallH, RoomMood mood)
    {
        Color color;
        float intensity, range, yAngle;
        switch (mood)
        {
            case RoomMood.Warm:
                color = new Color(1f, 0.85f, 0.6f);
                intensity = 0.7f; range = 7f; yAngle = 50f;
                break;
            case RoomMood.Cool:
                color = new Color(0.7f, 0.8f, 1f);
                intensity = 0.8f; range = 6f; yAngle = 40f;
                break;
            case RoomMood.Cold:
                color = new Color(0.6f, 0.7f, 1f);
                intensity = 0.9f; range = 5f; yAngle = 30f;
                break;
            case RoomMood.Vault:
                color = new Color(0.5f, 0.55f, 0.65f);
                intensity = 0.5f; range = 4f; yAngle = 20f;
                break;
            case RoomMood.Office:
                color = new Color(0.8f, 0.85f, 1f);
                intensity = 0.9f; range = 5f; yAngle = 50f;
                break;
            default:
                color = Color.white;
                intensity = 0.6f; range = 6f; yAngle = 45f;
                break;
        }

        float hw = w / 2f, hd = d / 2f;
        float stepX = Mathf.Min(4f, hw * 0.8f);
        float stepZ = Mathf.Min(4f, hd * 0.8f);

        for (float x = cx - hw + 2f; x <= cx + hw - 2f; x += stepX)
        {
            for (float z = cz - hd + 2f; z <= cz + hd - 2f; z += stepZ)
            {
                SpawnLight(x, wallH - 0.3f, z, color, intensity, range);
            }
        }
    }

    static void SpawnLight(float x, float y, float z, Color color, float intensity, float range)
    {
        var go = new GameObject("RoomLight");
        go.transform.SetParent(_lightRoot);
        go.transform.position = new Vector3(x, y, z);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    public static Light SpawnEmergencyLight(float x, float y, float z)
    {
        var go = new GameObject("EmergencyLight");
        go.transform.SetParent(_lightRoot);
        go.transform.position = new Vector3(x, y, z);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Color.red;
        l.intensity = 1.2f;
        l.range = 3f;
        l.enabled = false;
        return l;
    }

    public static Light SpawnDirectional(float x, float y, float z, float rotX, float rotY, Color color, float intensity = 0.5f)
    {
        var go = new GameObject("DirectionalLight");
        go.transform.SetParent(_lightRoot);
        go.transform.position = new Vector3(x, y, z);
        go.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = color;
        l.intensity = intensity;
        l.shadows = LightShadows.Soft;
        return l;
    }
}
