using UnityEngine;

public class SunCycle : MonoBehaviour
{
    [Header("Day Cycle Settings")]
    [Tooltip("Duration of a full day-night cycle in seconds")]
    public float dayDuration = 700f; // 5 minutes

    [Tooltip("Rotation axis of the sun (usually X or Z axis)")]
    public Vector3 rotationAxis = Vector3.right;

    private void Update()
    {
        if (dayDuration <= 0f)
            return;

        // Degrees per second to complete 360° in 'dayDuration' seconds
        float degreesPerSecond = 360f / dayDuration;

        // Apply rotation based on time
        transform.Rotate(rotationAxis, degreesPerSecond * Time.deltaTime);
    }
}
