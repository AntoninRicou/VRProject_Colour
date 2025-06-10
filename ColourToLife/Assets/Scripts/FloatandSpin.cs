using UnityEngine;

public class DriftAndPulse : MonoBehaviour
{
    public float driftSpeed = 2f;       // Overall speed of drifting movement
    public float driftRadius = 0.5f;      // How far it drifts from the original position

    public float scalePulseSpeed = 0.5f;  // Speed of pulsation
    public float scalePulseAmount = 0.1f; // How much to scale (e.g., 0.1 = ±10%)

    private Vector3 startPosition;
    private Vector3 driftOffset;
    private Vector3 baseScale;

    void Start()
    {
        startPosition = transform.position;
        baseScale = transform.localScale;

        // Optional: Randomize drift offset to give variation across multiple objects
        driftOffset = new Vector3(Random.value * 10f, Random.value * 10f, Random.value * 10f);
    }

    void Update()
    {
        float t = Time.time;

        // Floating drift movement (in XZ plane only)
        float offsetX = Mathf.Sin(t * driftSpeed + driftOffset.x) * driftRadius;
        float offsetZ = Mathf.Cos(t * driftSpeed + driftOffset.z) * driftRadius;
        transform.position = new Vector3(startPosition.x + offsetX, startPosition.y, startPosition.z + offsetZ);

        // Pulsating scale
        float scalePulse = 1f + Mathf.Sin(t * scalePulseSpeed) * scalePulseAmount;
        transform.localScale = baseScale * scalePulse;
    }
}
