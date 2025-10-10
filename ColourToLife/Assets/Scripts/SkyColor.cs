using UnityEngine;

public class ContinuousSkyColorChanger : MonoBehaviour
{

    [Header("UI References")]
    public GameObject progressCounterUI; // Drag your counter object here

    [Header("Sky")]
    public Color targetSkyColor = Color.blue;    // Final sky color after all objects are gazed
    public float colorFadeDuration = 2f;         // Time to fade from black to target

    [Header("Audio")]
    public AudioClip completionClip;             // 🔊 Assign in Inspector
    private AudioSource audioSource;

    [Header("End Credits")]
    public EndCreditsSequence endCredits;        // Drag your EndCreditsSequence here
    public float creditsDelay = 2f;              // Delay after sky completes

    [Header("Dev")]
    public bool devForceCompleteOnStart = false; // For quick testing in Editor/device

    private bool transitionStarted = false;
    private bool transitionComplete = false;
    private bool creditsFired = false;
    private float fadeTimer = 0f;

    void Start()
    {
        if (Camera.main) Camera.main.backgroundColor = Color.black;

        // Ensure AudioSource exists
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        // Auto-find EndCreditsSequence if not assigned (Unity 2023+ API)
        if (!endCredits) endCredits = FindFirstObjectByType<EndCreditsSequence>();

        // Dev: instantly finish sky & trigger credits
        if (devForceCompleteOnStart)
        {
            if (Camera.main) Camera.main.backgroundColor = targetSkyColor;
            transitionStarted = true;
            transitionComplete = true;
            FireCreditsOnce();
        }
    }

    void Update()
    {
        // Kick off sky transition when all are gazed (or your dev/cloud shortcut)
        if (!transitionStarted && (
            ShaderGraphToonController.AllObjectsGazedAtLeastOnce() ||
            (ShaderGraphToonController.isDevMode && ShaderGraphToonController.cloudTriggeredSky)))
        {
            transitionStarted = true;
            fadeTimer = 0f;
            Debug.Log("🌇 All objects gazed — starting sky transition from black to blue.");

            if (completionClip) audioSource.PlayOneShot(completionClip);
            Debug.Log("☁️ Sky script detected cloud trigger!");
        }

        // Drive the fade
        if (transitionStarted && !transitionComplete)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / colorFadeDuration);

            if (Camera.main)
                Camera.main.backgroundColor = Color.Lerp(Color.black, targetSkyColor, t);

            if (t >= 1f)
            {
                transitionComplete = true;
                Debug.Log("✅ Sky transition complete.");
            }
        }

        // When sky is done, start the credits once
        if (transitionComplete && !creditsFired)
        {
            FireCreditsOnce();
        }
    }

    private void FireCreditsOnce()
    {
        creditsFired = true;
        if (endCredits)
        {
            Debug.Log("[Sky] Firing end credits…");
            endCredits.StartCredits(creditsDelay);  // uses your latest API
        }
        else
        {
            Debug.LogWarning("EndCreditsSequence not found/assigned.");
        }
        if (progressCounterUI)
        {
            progressCounterUI.SetActive(false);
            Debug.Log("[Sky] Progress counter hidden.");
        }
    }

    // Optional: right-click the component header → “Force Complete Now”
    [ContextMenu("Force Complete Now (Fire Credits)")]
    void ForceCompleteNow()
    {
        if (Camera.main) Camera.main.backgroundColor = targetSkyColor;
        transitionStarted = true;
        transitionComplete = true;
        FireCreditsOnce();
    }
}
