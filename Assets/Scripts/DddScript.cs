using UnityEngine;

public class SmoothGazeReticle : MonoBehaviour
{
    public GameObject reticle;
    public float minScale = 0.1f;
    public float maxScale = 1f;
    public float positionSmoothTime = 0.05f;
    public float rotationSmoothTime = 0.05f;

    private Transform vrCamera;
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Renderer reticleRenderer;

    private GameObject currentHitObject = null;

    void Start()
    {
        vrCamera = Camera.main?.transform;

        if (reticle != null)
            reticleRenderer = reticle.GetComponent<Renderer>();

        if (reticleRenderer != null)
            reticleRenderer.enabled = false; // start invisible
    }

    void Update()
    {
        if (vrCamera == null || reticle == null)
            return;

        Ray ray = new Ray(vrCamera.position, vrCamera.forward);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject != currentHitObject)
            {
                // New object hit — snap reticle to hit point instantly and show it
                currentHitObject = hitObject;
                targetPosition = hit.point;
                reticle.transform.position = targetPosition;

                Vector3 toCamera = (vrCamera.position - hit.point).normalized;
                Vector3 projectedForward = Vector3.ProjectOnPlane(-toCamera, hit.normal);
                if (projectedForward.sqrMagnitude < 0.0001f)
                {
                    projectedForward = Vector3.Cross(hit.normal, Vector3.up);
                    if (projectedForward.sqrMagnitude < 0.0001f)
                        projectedForward = Vector3.Cross(hit.normal, Vector3.right);
                    projectedForward.Normalize();
                }
                else
                {
                    projectedForward.Normalize();
                }
                targetRotation = Quaternion.LookRotation(projectedForward, hit.normal);
                reticle.transform.rotation = targetRotation;

                // Scale instantly on new hit
                float distance = Vector3.Distance(vrCamera.position, hit.point);
                float scale = Mathf.Clamp(distance / 10f, minScale, maxScale);
                reticle.transform.localScale = Vector3.one * scale;

                if (reticleRenderer != null && !reticleRenderer.enabled)
                    reticleRenderer.enabled = true;

                // Reset velocity for smooth damping
                velocity = Vector3.zero;
            }
            else
            {
                // Same object — smooth move/rotate/scale reticle
                targetPosition = hit.point;

                Vector3 toCamera = (vrCamera.position - hit.point).normalized;
                Vector3 projectedForward = Vector3.ProjectOnPlane(-toCamera, hit.normal);
                if (projectedForward.sqrMagnitude < 0.0001f)
                {
                    projectedForward = Vector3.Cross(hit.normal, Vector3.up);
                    if (projectedForward.sqrMagnitude < 0.0001f)
                        projectedForward = Vector3.Cross(hit.normal, Vector3.right);
                    projectedForward.Normalize();
                }
                else
                {
                    projectedForward.Normalize();
                }
                targetRotation = Quaternion.LookRotation(projectedForward, hit.normal);

                float distance = Vector3.Distance(vrCamera.position, hit.point);
                float scale = Mathf.Clamp(distance / 10f, minScale, maxScale);
                reticle.transform.localScale = Vector3.one * scale;

                reticle.transform.position = Vector3.SmoothDamp(reticle.transform.position, targetPosition, ref velocity, positionSmoothTime);
                reticle.transform.rotation = Quaternion.Slerp(reticle.transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);
            }
        }
        else
        {
            // No hit — hide reticle and clear current object
            currentHitObject = null;

            if (reticleRenderer != null && reticleRenderer.enabled)
                reticleRenderer.enabled = false;
        }
    }
}