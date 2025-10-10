using UnityEngine;
using System.Collections.Generic;
using System.Collections; // ✅ Needed for IEnumerator
using System; // <- add

[ExecuteAlways]
[RequireComponent(typeof(MeshCollider))]
public class ShaderGraphToonController : MonoBehaviour
{
    public static event Action<ShaderGraphToonController> FirstGazed;
    public bool HasBeenGazedOnce => hasBeenGazedOnce;

    [Header("Sky Trigger")]
    public bool isCloudToTriggerSky = false; // set this in the Inspector on the object that should trigger the sky
    public static bool cloudTriggeredSky = false; // this is checked by the sky controller

    public static bool isDevMode = false; // Set this globally in the scene

    [Header("Title")]
    public bool isTitleGroup = false;

    // === Static Tracking ===
    private static List<ShaderGraphToonController> allInstances = new List<ShaderGraphToonController>();
    private bool hasBeenGazedOnce = false;

    public static bool AllObjectsGazedAtLeastOnce()
    {
        foreach (var controller in allInstances)
        {
            if (!controller.hasBeenGazedOnce)
                return false;
        }
        return true;
    }

    public static bool AreAllTitleObjectsGazed()
    {
        foreach (var controller in allInstances)
        {
            if (controller.isTitleGroup && !controller.hasBeenGazedOnce)
                return false;
        }
        return true;
    }

    // === Audio ===
    private AudioSource audioSource;
    public AudioClip[] gazeClips;

    private Coroutine audioTransitionCoroutine;
    private float audioFadeDuration = 0.5f; // Duration of fade in/out

    // === Materials ===
    private Material outlineMaterial;
    private MaterialPropertyBlock block;
    private Renderer rend;

    [Header("Color Palette")]
    private readonly Color[] colorPalette = new Color[]
    {
        new Color(1f, 0.4627f, 0.7059f),
        new Color(0.5569f, 0.898f, 0.7725f),
        new Color(0.2588f, 0.6745f, 0.251f),
        new Color(0.9451f, 0.1294f, 0.1294f),
        new Color(1f, 0.6078f, 0.0039f),
        new Color(1f, 0.9529f, 0.0039f),
        new Color(0.7059f, 0.8509f, 0.1294f),
        new Color(0.0039f, 0.4078f, 0.8509f),
        new Color(0.4392f, 0.647f, 0.9764f)
    };

    private int currentPaletteIndex = -1;
    private Color previousColor;

    // === Shader Graph Properties ===
    private Color targetColor;
    private Vector2 minMax = new Vector2(0.1f, 0.9f);

    // === Transition Settings (non-serialized) ===  // ★
    private float colorFadeDuration = 4f;              // non-title default  // ★
    private float shadeFadeDuration = 5f;              // non-title default  // ★

    // Title overrides (non-serialized)               // ★
    private bool useTitleOverrides = true;             // ★
    private float titleColorFadeDuration = 2.0f;       // ★ (faster color for title)
    private float titleShadeFadeDuration = 2.0f;       // ★ (faster shade for title)
    private bool titleHeadStartOnReset = true;         // ★ (gives a snappy pop on reset)

    // Active duration helpers                         // ★
    private float ActiveColorFadeDuration => (isTitleGroup && useTitleOverrides) ? titleColorFadeDuration : colorFadeDuration;  // ★
    private float ActiveShadeFadeDuration => (isTitleGroup && useTitleOverrides) ? titleShadeFadeDuration : shadeFadeDuration;  // ★

    // Optional easing for nicer feel                  // ★
    private static float EaseInOut(float t) => t * t * (3f - 2f * t);                                                           // ★

    [Range(0f, 1f)]
    private float initialShades = 1f;
    private float targetShades = 0.38f;

    private float colorFadeTimer = 0f;
    private float shadeFadeTimer = 0f;
    private bool colorFadeComplete = false;
    private bool holdComplete = false;

    private Color currentColor = Color.black;
    private float currentShades;

    // === Hold Settings ===
    private float holdDuration = 2f;
    private float holdTimer = 0f;

    // === Gaze Trigger ===
    public Transform vrCamera;
    [Range(0.8f, 1f)]
    public float lookThreshold = 0.95f;
    public bool requireContinuousGaze = false;

    private bool isGazing = false;
    private bool hasStartedTransition = false;
    private bool hasShadedOnce = false;

    // === Unity Events ===
    void OnEnable()
    {
        allInstances.Add(this);

        MeshCollider meshCol = GetComponent<MeshCollider>();
        meshCol.convex = false;

        rend = GetComponent<Renderer>();
        if (!Application.isPlaying)
        {
            currentColor = Color.black;
            currentShades = initialShades;
        }
        else
        {
            currentPaletteIndex = UnityEngine.Random.Range(0, colorPalette.Length);
            targetColor = colorPalette[currentPaletteIndex];
            previousColor = Color.black;
            currentColor = Color.black;
            hasShadedOnce = false;
            colorFadeComplete = false;
            holdComplete = false;

            ResetTimers();
        }
        ApplyPropertyBlock();
    }

    void OnDisable()
    {
        allInstances.Remove(this);
    }

    void Start()
    {
        if (vrCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                vrCamera = mainCam.transform;
            else
                Debug.LogWarning("No Main Camera assigned or found for VR gaze tracking.");
        }

        // Ensure AudioSource exists
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = isTitleGroup ? 0f : 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 50f;
        audioSource.volume = isTitleGroup ? 0.5f : 1f;

        // ✅ Always reload audio clips regardless of serialized state
        gazeClips = Resources.LoadAll<AudioClip>("Audio/GazeClips/Relaxing");
        if (gazeClips == null || gazeClips.Length == 0)
        {
            Debug.LogWarning("No gaze audio clips found at Resources/Audio/GazeClips/Relaxing");
        }
        else
        {
            Debug.Log($"✅ Loaded {gazeClips.Length} gaze audio clips from Resources.");
        }

        if (isDevMode && Application.isPlaying)
        {
            currentPaletteIndex = UnityEngine.Random.Range(0, colorPalette.Length);
            targetColor = colorPalette[currentPaletteIndex];
            previousColor = Color.black;

            if (isCloudToTriggerSky)
            {
                // ✅ Cloud starts black, not shaded, and needs to go through full transition
                currentColor = Color.black;
                currentShades = initialShades;
                colorFadeComplete = false;
                hasShadedOnce = false;
                holdComplete = false;
            }
            else
            {
                // ✅ Everything else is already colored and shaded in dev mode
                currentColor = targetColor;
                currentShades = targetShades;
                colorFadeComplete = true;
                hasShadedOnce = true;
                holdComplete = true;
            }

            ApplyPropertyBlock();
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // --- Gaze Detection ---
        if (vrCamera != null)
        {
            Ray ray = new Ray(vrCamera.position, vrCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, LayerMask.GetMask("Default")) && hit.transform == transform)
            {
                if (!isGazing)
                {
                    // Block non-title objects until title group is done
                    if (!isTitleGroup && !ShaderGraphToonController.AreAllTitleObjectsGazed())
                        return;

                    isGazing = true;
                    SetOutlineVisibility(true);

                    if (isCloudToTriggerSky && isDevMode && !cloudTriggeredSky)
                    {
                        cloudTriggeredSky = true;
                        Debug.Log("☁️ Cloud object triggered the sky change in dev mode.");
                    }

                    if (gazeClips.Length > 0)
                    {
                        int i = UnityEngine.Random.Range(0, gazeClips.Length);
                        AudioClip clip = gazeClips[i];

                        if (audioTransitionCoroutine != null)
                            StopCoroutine(audioTransitionCoroutine);

                        audioTransitionCoroutine = StartCoroutine(SmoothAudioTransition(clip));
                    }

                    if (!hasStartedTransition)
                    {
                        hasStartedTransition = true;
                        ResetTransition();
                    }
                }

                // Block non-title objects until title group is done
                if (!isTitleGroup && !ShaderGraphToonController.AreAllTitleObjectsGazed())
                {
                    return; // Skip gaze reaction
                }

                // 🔁 Track first-time gaze
                if (!hasBeenGazedOnce)
                {
                    hasBeenGazedOnce = true;
                    FirstGazed?.Invoke(this); // <- notify the counter exactly once
                }
            }
            else
            {
                if (isGazing)
                {
                    isGazing = false;
                    SetOutlineVisibility(false);

                    if (requireContinuousGaze)
                    {
                        hasStartedTransition = false;
                        ResetVisual();
                        return;
                    }
                }
            }
        }

        // --- Animation ---
        if (hasStartedTransition && isGazing)
        {
            if (!colorFadeComplete)
            {
                colorFadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(colorFadeTimer / ActiveColorFadeDuration); // ★
                t = EaseInOut(t);                                                  // ★
                currentColor = Color.Lerp(previousColor, targetColor, t);
                if (!hasShadedOnce)
                    currentShades = initialShades;

                if (t >= 1f)
                {
                    colorFadeComplete = true;
                    shadeFadeTimer = 0f;
                }
            }
            else if (!hasShadedOnce)
            {
                shadeFadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(shadeFadeTimer / ActiveShadeFadeDuration); // ★
                t = EaseInOut(t);                                                  // ★
                currentShades = Mathf.Lerp(initialShades, targetShades, t);
                if (t >= 1f)
                {
                    currentShades = targetShades;
                    holdTimer = 0f;
                    hasShadedOnce = true;
                }
            }
            else if (!holdComplete)
            {
                holdTimer += Time.deltaTime;
                if (holdTimer >= holdDuration)
                {
                    holdComplete = true;
                    ResetTransition();
                }
            }
        }

        ApplyPropertyBlock();
        UpdateAudioVolume();
    }

    void ApplyPropertyBlock()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();

        rend.GetPropertyBlock(block);
        block.SetColor("_Color", currentColor);
        block.SetFloat("_Shades", currentShades);
        block.SetVector("_MinMax", new Vector4(minMax.x, minMax.y, 0f, 0f));
        rend.SetPropertyBlock(block);
    }

    void SetOutlineVisibility(bool visible)
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();

        rend.GetPropertyBlock(block, 0);
        block.SetFloat("_IsOutlined", visible ? 1f : 0f);

        if (visible && vrCamera != null)
        {
            float distance = Vector3.Distance(vrCamera.position, transform.position);
            float baseThickness = 0.005f;
            float scaleFactor = 0.001f;
            float boost = distance > 4f ? Mathf.Lerp(1f, 2f, Mathf.InverseLerp(4f, 15f, distance)) : 1f;
            float thickness = ((baseThickness + (distance * scaleFactor)) * boost) / (transform.lossyScale.magnitude / Mathf.Sqrt(3));
            block.SetFloat("_OutlineThickness", thickness);
        }

        rend.SetPropertyBlock(block, 0);
    }

    void ResetTransition()
    {
        colorFadeTimer = 0f;
        shadeFadeTimer = 0f;
        holdTimer = 0f;

        colorFadeComplete = false;
        holdComplete = false;

        previousColor = currentColor;
        currentPaletteIndex = (currentPaletteIndex + 1) % colorPalette.Length;
        targetColor = colorPalette[currentPaletteIndex];

        // Optional head start for title pop
        if (isTitleGroup && useTitleOverrides && titleHeadStartOnReset) // ★
        {
            colorFadeTimer = ActiveColorFadeDuration * 0.35f; // ★
        }
    }

    void ResetVisual()
    {
        currentColor = Color.black;
        currentShades = initialShades;
        colorFadeTimer = 0f;
        shadeFadeTimer = 0f;
        colorFadeComplete = false;
        ApplyPropertyBlock();
    }

    void ResetTimers()
    {
        colorFadeTimer = 0f;
        shadeFadeTimer = 0f;
        holdTimer = 0f;
    }

    void UpdateAudioVolume()
    {
        if (audioSource == null) return;

        float targetVolume = 0f;

        if (isGazing)
        {
            // Calculate progress based on full visual transition (color + shade)
            float colorProgress = Mathf.Clamp01(colorFadeTimer / ActiveColorFadeDuration); // ★
            float shadeProgress = Mathf.Clamp01(shadeFadeTimer / ActiveShadeFadeDuration); // ★

            // Use a blend — we want volume to respond immediately to color, and grow with shading
            float progress = hasShadedOnce ? shadeProgress : colorProgress;

            targetVolume = Mathf.Lerp(0f, 1f, progress);

            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            targetVolume = audioSource.volume;
        }

        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * 4f);
    }

    private IEnumerator SmoothAudioTransition(AudioClip newClip)
    {
        float startVolume = audioSource.volume;

        // Fade out
        for (float t = 0; t < audioFadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / audioFadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in
        for (float t = 0; t < audioFadeDuration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, startVolume, t / audioFadeDuration);
            yield return null;
        }

        audioSource.volume = startVolume;
    }
}
