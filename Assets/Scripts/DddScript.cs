using UnityEngine;

public class DddScript : MonoBehaviour
{
    Transform vrCamera;

    public GameObject reticle;

    // minimum and maximum scale for the reticle
    public float minScale = 0.1f;
    public float maxScale = 1f;

    void Start()
    {

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            vrCamera = mainCam.transform;
        }

    }

    void Update()
    {
        //create a ray from the camera 
        Ray ray = new Ray(vrCamera.position, vrCamera.forward);

        // ignorer Reticle layer
        LayerMask layerMask = LayerMask.GetMask("Default"); // Adjust as needed


        //Show ray
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);
        RaycastHit hit;
        // Perform raycast to detect surface
        // use layer mask to filter out unwanted layers if necessary
        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            // Get the point where the ray hits the surface
            Vector3 hitPoint = hit.point;

            // Set the reticle position to the hit point
            reticle.transform.position = hitPoint;

            // Set the reticle rotation to face the camera
            reticle.transform.rotation = Quaternion.LookRotation(vrCamera.forward);

            // set the orientation of the reticle to match the surface normal
            reticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * reticle.transform.rotation;
            // Optionally, you can adjust the scale of the reticle based on distance
            float distance = Vector3.Distance(vrCamera.position, hitPoint);
            float scale = Mathf.Clamp(distance / 10f, minScale, maxScale); // Adjust scale as needed
            reticle.transform.localScale = new Vector3(scale, scale, scale);

        
        }

        else
        {
            // Use the reticle renderer component to disable the reticle if no surface is hit
           
        }
       
 // Garde une taille constante (taille angulaire fixe)
        // float scale = 2 * Mathf.Tan(sizeInDegrees * Mathf.Deg2Rad / 2) * Vector3.Distance(cameraTransform.position, targetPosition);
        // transform.localScale = Vector3.one * scale;

    }

    // Smooth transitions

}
