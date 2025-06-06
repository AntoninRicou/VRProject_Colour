using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshCollider))]


public class ShaderGraphToonController : MonoBehaviour
{

    // Audio variables
    private AudioSource audioSource;
    private float audioFadeTimer = 0f;
    private float audioFadeDuration = 10f; // How long the fade-out takes

    private float desiredPeak = 0.5f; // Desired peak volume for the audio clip
    //make the sound loop few 

    private Material outlineMaterial; // Assign OutlineMat in Inspector

    [Header("Color Palette")]
    private readonly Color[] colorPalette = new Color[]
    {

       new Color(1f, 0.4627f, 0.7059f),     //hsl(333, 100.00%, 50.00%)
        new Color(0.5569f, 0.898f, 0.7725f), //rgb(0, 255, 162)
        new Color(0.2588f, 0.6745f, 0.251f), //rgb(195, 0, 255)
        new Color(0.9451f, 0.1294f, 0.1294f),// #F12121
        new Color(1f, 0.6078f, 0.0039f),     //rgb(255, 107, 1)
        new Color(1f, 0.9529f, 0.0039f),     //rgb(255, 1, 238)
        new Color(0.7059f, 0.8509f, 0.1294f),//rgb(55, 255, 0)
        new Color(0.0039f, 0.4078f, 0.8509f),// #0168D9
        new Color(0.4392f, 0.647f, 0.9764f)  //rgb(255, 0, 0)

    };
    private int currentPaletteIndex = -1;
    private Color previousColor;

    [Header("Shader Graph Properties")]
    private Color targetColor;
    private Vector2 minMax = new Vector2(0.1f, 0.9f);

    [Header("Transition Settings")]
    private float colorFadeDuration = 4f;
    private float shadeFadeDuration = 5f;

    [Range(0f, 1f)]
    private float initialShades = 1f; // .01 is fullz shaded, 1 is no shade
    private float targetShades = 0.38f;

    private float colorFadeTimer = 0f;
    private float shadeFadeTimer = 0f;
    private bool colorFadeComplete = false;

    private Color currentColor = Color.black;
    private float currentShades;

    private Renderer rend;
    private MaterialPropertyBlock block;

    [Header("Hold Settings")]
    private float holdDuration = 2f;

    private float holdTimer = 0f;
    private bool holdComplete = false;

    [Header("Gaze Trigger")]
    public Transform vrCamera;         // Assign your XR Rig Main Camera here
    [Range(0.8f, 1f)]
    public float lookThreshold = 0.95f; // How directly user needs to look
    public bool requireContinuousGaze = false; // If true, animation resets when gaze leaves
    private bool isGazing = false; // Internal state

    private bool hasStartedTransition = false;

    private bool hasShadedOnce = false;

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

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = 0f; // Start silent
         NormalizeAudio(audioSource.clip);
        if (audioSource == null)
        {
            Debug.LogWarning("No AudioSource attached to " + gameObject.name);
        }
    }



    void OnEnable()
    {
        MeshCollider meshCol = GetComponent<MeshCollider>();
        if (meshCol == null)
        {
            meshCol = gameObject.AddComponent<MeshCollider>();
        }
        meshCol.convex = false;
        rend = GetComponent<Renderer>();


        if (!Application.isPlaying)
        {
            currentColor = Color.black;
            currentShades = initialShades;
        }
        else
        {
            // Pick a random color to start
            currentPaletteIndex = Random.Range(0, colorPalette.Length);
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


    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            currentColor = Color.black;
            currentShades = initialShades;
            ApplyPropertyBlock();
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // ---- Gaze Detection ----
        if (vrCamera != null)
        {
            Ray centerRay = new Ray(vrCamera.position, vrCamera.forward);
            RaycastHit hit;

            float gazeDistance = 100f; // Adjust as needed
            LayerMask mask = LayerMask.GetMask("Default");

            // Check if the ray hits this object only
            bool hitThisObject = Physics.Raycast(centerRay, out hit, gazeDistance, mask) && hit.transform == transform;
            if (hitThisObject)
            {
                if (!isGazing)
                {
                    isGazing = true;
                    SetOutlineVisibility(true);

                    if (audioSource != null && !audioSource.isPlaying)
                        audioSource.Play();

                    if (!hasStartedTransition)
                    {
                        hasStartedTransition = true;
                        ResetTransition();
                    }
                }
            }
            else
            {
                if (isGazing)
                {
                    isGazing = false;
                    SetOutlineVisibility(false);

                    if (!isGazing && audioSource.isPlaying && audioSource.volume <= 0.01f)
                        audioSource.Stop();

                    if (requireContinuousGaze)
                    {
                        hasStartedTransition = false;
                        ResetVisual();
                        return;
                    }
                }
            }

        }

        // ---- Transition Animation ----
        if (hasStartedTransition && isGazing) // ✅ Add isGazing check here
        {
            if (!colorFadeComplete)
            {
                colorFadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(colorFadeTimer / colorFadeDuration);
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
                // First-time shading only
                shadeFadeTimer += Time.deltaTime;
                float t = Mathf.Clamp01(shadeFadeTimer / shadeFadeDuration);
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
                // Hold phase
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
    void UpdateAudioVolume()
    {
        if (audioSource == null) return;

        float targetVolume = 0f;

        if (isGazing)
        {
            // Calculate progress based on full visual transition (color + shade)
            float colorProgress = Mathf.Clamp01(colorFadeTimer / colorFadeDuration);
            float shadeProgress = Mathf.Clamp01(shadeFadeTimer / shadeFadeDuration);

            // Use a blend — we want volume to respond immediately to color, and grow with shading
            float progress = hasShadedOnce ? shadeProgress : colorProgress;

            targetVolume = Mathf.Lerp(0f, 1f, progress); // Adjust max volume here if needed

            // Start audio if not playing
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            // Fade out gracefully
            targetVolume = 0f;
        }

        // Smoothly transition to new volume
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * 4f);

        // Stop only after volume fades out completely
        if (!isGazing && audioSource.isPlaying && audioSource.volume <= 0.01f)
            audioSource.Stop();
    }
void NormalizeAudio(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        float max = 0f;
        foreach (float sample in samples)
        {
            if (Mathf.Abs(sample) > max)
                max = Mathf.Abs(sample);
        }

        float volumeFactor = desiredPeak / max;
        audioSource.volume = Mathf.Clamp(volumeFactor, 0f, 1f);
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

    public void ResetTransition()
    {
        colorFadeTimer = 0f;
        shadeFadeTimer = 0f;
        holdTimer = 0f;

        colorFadeComplete = false;
        holdComplete = false;

        previousColor = currentColor;

        // Advance index from the current one
        currentPaletteIndex = (currentPaletteIndex + 1) % colorPalette.Length;
        targetColor = colorPalette[currentPaletteIndex];
    }



    private void ResetVisual()
    {
        currentColor = Color.black;
        currentShades = initialShades;
        colorFadeTimer = 0f;
        shadeFadeTimer = 0f;
        colorFadeComplete = false;
        ApplyPropertyBlock();
    }

    private void ResetTimers()
    {
        colorFadeTimer = 0f;
        shadeFadeTimer = 0f;
        holdTimer = 0f;
    }

    void SetOutlineVisibility(bool visible)
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (block == null) block = new MaterialPropertyBlock();

        rend.GetPropertyBlock(block, 0); // 0 = outline slot

        block.SetFloat("_IsOutlined", visible ? 1f : 0f);

        if (visible && vrCamera != null)
        {
            float distance = Vector3.Distance(vrCamera.position, transform.position);

            float baseThickness = 0.01f;
            float scaleFactor = 0.001f;

            float distanceAdjusted = baseThickness + (distance * scaleFactor);

            // 🔥 NEW: Adjust for world scale
            float scale = transform.lossyScale.x; // assuming uniform scale
            float thickness = distanceAdjusted / scale;

            block.SetFloat("_OutlineThickness", thickness);
        }

        rend.SetPropertyBlock(block, 0);
    }


}