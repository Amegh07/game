using UnityEngine;

public readonly struct NoiseEvent
{
    public Vector3 Position { get; }
    public float Radius { get; }
    public float Strength { get; }
    public GameObject Source { get; }
    public NoiseType Type { get; }
    public float Timestamp { get; }

    public NoiseEvent(Vector3 position, float radius, float strength, GameObject source, NoiseType type, float timestamp)
    {
        Position = position;
        Radius = radius;
        Strength = strength;
        Source = source;
        Type = type;
        Timestamp = timestamp;
    }
}
