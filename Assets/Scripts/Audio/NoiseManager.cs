using UnityEngine;
using System.Collections.Generic;

public class NoiseManager : MonoBehaviour
{
    private static NoiseManager instance;
    public static NoiseManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = new GameObject("NoiseManager");
                instance = obj.AddComponent<NoiseManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private List<GuardFSM> guards = new List<GuardFSM>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void RegisterGuard(GuardFSM guard)
    {
        if (guard != null && !guards.Contains(guard))
            guards.Add(guard);
    }

    public void UnregisterGuard(GuardFSM guard)
    {
        guards.Remove(guard);
    }

    public void EmitNoise(NoiseEvent noise)
    {
        for (int i = guards.Count - 1; i >= 0; i--)
        {
            GuardFSM g = guards[i];
            if (g == null)
            {
                guards.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(noise.Position, g.transform.position);
            float maxRange = noise.Radius;

            if (dist > maxRange) continue;

            float distanceFactor = 1f - Mathf.Clamp01(dist / maxRange);
            float occlusion = 1f;

            if (noise.Type != NoiseType.Alarm)
            {
                Vector3 dir = g.transform.position - noise.Position;
                float rayDist = dist;
                if (Physics.Raycast(noise.Position, dir.normalized, out RaycastHit hit, rayDist, g.VisionMask))
                {
                    occlusion = Mathf.Clamp01(1f - (hit.distance / rayDist) * 0.3f);
                }
            }

            float perceivedStrength = noise.Strength * distanceFactor * occlusion;
            if (perceivedStrength > 0.01f)
                g.HearNoise(noise.Position, perceivedStrength, noise.Type);
        }
    }

    public static void GetGuardsInRadius(Vector3 position, float radius, List<GuardFSM> results)
    {
        results.Clear();
        if (Instance == null) return;
        var list = Instance.guards;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            GuardFSM g = list[i];
            if (g == null)
            {
                list.RemoveAt(i);
                continue;
            }
            if (Vector3.Distance(position, g.transform.position) <= radius)
                results.Add(g);
        }
    }

    public static void EmitAt(Vector3 position, float radius, float strength, NoiseType type, GameObject source = null)
    {
        if (Instance == null) return;
        Instance.EmitNoise(new NoiseEvent(position, radius, Mathf.Clamp01(strength), source, type, Time.time));
    }
}
