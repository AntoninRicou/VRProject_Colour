using UnityEngine;

public class DddScript : MonoBehaviour
{
    private Transform vrCamera;
    private Renderer reticleRenderer;

    public GameObject reticle;           // Parent GameObject of the reticle (e.g., an empty with the mesh as child)
    public float minScale = 0.1f;
    public float maxScale = 1f;

    void Start()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            vrCamera = mainCam.transform;
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }

        // Find MeshRenderer in child (e.g., the cylinder)
        if (reticle != null)
        {
            reticleRenderer = reticle.GetComponentInChildren<Renderer>();
            if (reticleRenderer != null)
                reticleRenderer.enabled = false;
            else
                Debug.LogError("Reticle Renderer not found!");
        }
    }

    void Update()
    {
        if (vrCamera == null || reticle == null)
            return;

        Ray ray = new Ray(vrCamera.position, vrCamera.forward);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        LayerMask layerMask = LayerMask.GetMask("Default"); // Adjust if needed
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, layerMask))
        {
            // Position
            Vector3 hitPoint = hit.point;
            reticle.transform.position = hitPoint;

            // Rotation: lay flat on the surface, and face the camera properly
            Vector3 up = hit.normal;
            Vector3 toCamera = (vrCamera.position - hitPoint).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(toCamera, up).normalized;

            if (forward.sqrMagnitude > 0.001f)
                reticle.transform.rotation = Quaternion.LookRotation(forward, up);
            else
                reticle.transform.rotation = Quaternion.LookRotation(Vector3.forward, up); // fallback

            // Scale by distance
            float distance = Vector3.Distance(vrCamera.position, hitPoint);
            float scale = Mathf.Clamp(distance / 10f, minScale, maxScale);
            reticle.transform.localScale = Vector3.one * scale;

            // Show reticle
            if (reticleRenderer != null && !reticleRenderer.enabled)
                reticleRenderer.enabled = true;
        }
        else
        {
            // Hide reticle if no hit
            if (reticleRenderer != null && reticleRenderer.enabled)
                reticleRenderer.enabled = false;
        }
    }
}
