using UnityEngine;

[CreateAssetMenu(menuName = "Museum Heist/Camera Config", fileName = "NewCameraConfig")]
public class CameraConfig : ScriptableObject
{
    [Header("Patrol Rotation")]
    [Tooltip("Maximum angle to oscillate left and right from the initial facing direction.")]
    public float patrolAngle = 45f;

    [Tooltip("Rotation speed in degrees per second.")]
    public float patrolSpeed = 30f;

    [Tooltip("Initial rotation direction when the camera starts.")]
    public bool startRotatingLeft = true;

    [Header("Detection")]
    [Tooltip("Maximum distance at which the camera can detect the player.")]
    public float detectionRange = 10f;

    [Tooltip("Horizontal field of view in degrees. Half this value is used for cone edge checks.")]
    public float fieldOfView = 60f;

    [Tooltip("Time in seconds the player must remain in the camera's sight before the camera alerts SecurityManager.")]
    public float detectionTime = 2f;

    [Tooltip("Rate at which detection progress decays when the player is not visible (units per second).")]
    public float detectionDecayRate = 1f;

    [Tooltip("Layers that block the camera's line of sight.")]
    public LayerMask obstructionMask = -1;

    [Header("Behavior")]
    [Tooltip("When true, the camera stops its patrol rotation once the player is fully detected.")]
    public bool stopRotationOnDetection = false;

    [Header("Debug")]
    [Tooltip("Color of the vision cone gizmo drawn in the Scene view.")]
    public Color visionConeColor = new Color(1f, 0f, 0f, 0.08f);

    [Tooltip("Color of the detection progress bar shown above the camera in the Scene view.")]
    public Color detectionProgressColor = Color.yellow;
}
