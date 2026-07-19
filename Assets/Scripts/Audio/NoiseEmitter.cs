using UnityEngine;

public class NoiseEmitter : MonoBehaviour
{
    [SerializeField] private NoiseType noiseType = NoiseType.Movement;
    [SerializeField] private float radius = 10f;
    [SerializeField] [Range(0f, 1f)] private float strength = 0.5f;

    public void EmitNoise()
    {
        EmitNoise(1f, 1f);
    }

    public void EmitNoise(float strengthMultiplier, float radiusMultiplier)
    {
        if (NoiseManager.Instance == null) return;

        NoiseManager.Instance.EmitNoise(new NoiseEvent(
            transform.position,
            radius * radiusMultiplier,
            Mathf.Clamp01(strength * strengthMultiplier),
            gameObject,
            noiseType,
            Time.time
        ));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
