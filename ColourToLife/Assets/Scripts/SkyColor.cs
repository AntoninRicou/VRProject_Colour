using UnityEngine;

public class ContinuousSkyColorChanger : MonoBehaviour
{
    public Color targetSkyColor = Color.blue;     // Final sky color after all objects are gazed
    public float colorFadeDuration = 2f;         // Time to fade from black to red

    public AudioClip completionClip; // 🔊 Assign in Inspector
    private AudioSource audioSource;

    private bool transitionStarted = false;
    private bool transitionComplete = false;
    private float fadeTimer = 0f;

    void Start()
    {
        Camera.main.backgroundColor = Color.black;

        // Ensure AudioSource exists
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound (non-directional)
    }

    void Update()
    {
        if (!transitionStarted && ShaderGraphToonController.AllObjectsGazedAtLeastOnce())
        {
            transitionStarted = true;
            fadeTimer = 0f;
            Debug.Log("🌇 All objects gazed — starting sky transition from black to blue.");

            // 🔊 Play the sound once
            if (completionClip != null)
                audioSource.PlayOneShot(completionClip);
        }

        if (transitionStarted && !transitionComplete)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / colorFadeDuration);

            Camera.main.backgroundColor = Color.Lerp(Color.black, targetSkyColor, t);

            if (t >= 1f)
            {
                transitionComplete = true;
                Debug.Log("✅ Sky transition complete.");
            }
        }
    }
}
