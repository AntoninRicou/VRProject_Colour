using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Terrain))]
public class TerrainToonController : MonoBehaviour
{
    private Terrain terrain;
    private MaterialPropertyBlock block;

    [Header("Color Palette")]
    private readonly Color[] colorPalette = new Color[]
    {
        new Color(1f, 0.4627f, 0.7059f),
        new Color(0.5569f, 0.898f, 0.7725f),
        new Color(0.2588f, 0.6745f, 0.251f),
        new Color(1f, 0.9961f, 1f),
        new Color(0.9451f, 0.1294f, 0.1294f),
        new Color(1f, 0.6078f, 0.0039f),
        new Color(1f, 0.9529f, 0.0039f),
        new Color(0.7059f, 0.8509f, 0.1294f),
        new Color(0.0039f, 0.4078f, 0.8509f),
        new Color(0.4392f, 0.647f, 0.9764f)
    };

    [Header("Gaze Settings")]
    public Transform gazeTarget;
    public float gazeDistance = 50f;
    public float sphereCastRadius = 1.0f; // Plus tolérant

    [Header("Visual Settings")]
    [SerializeField] private Vector2 minMax = new Vector2(0.1f, 0.9f);
    [SerializeField] private float colorFadeDuration = 2f;

    private int currentPaletteIndex = -1;
    private Color previousColor = Color.black;
    private Color currentColor = Color.black;
    private Color targetColor;

    private float currentShades = 4f;
    private float colorFadeTimer = 4f;
    private bool isFading = false;
    private bool waitingForNextGaze = true;

    private void OnEnable()
    {
        terrain = GetComponent<Terrain>();

        if (terrain.materialTemplate == null)
        {
            Debug.LogWarning("Assign a terrain material that supports _Color and _Shades.");
            return;
        }

        if (block == null)
            block = new MaterialPropertyBlock();

        currentColor = Color.black;
        previousColor = Color.black;
        currentShades = 0.1f;
        currentPaletteIndex = Random.Range(0, colorPalette.Length);
        targetColor = colorPalette[currentPaletteIndex];

        ApplyPropertyBlock();
    }

    private void Update()
    {
        if (!Application.isPlaying || gazeTarget == null || terrain == null)
            return;

        if (isFading)
        {
            colorFadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(colorFadeTimer / colorFadeDuration);
            currentColor = Color.Lerp(previousColor, targetColor, t);

            if (t >= 1f)
            {
                isFading = false;
                waitingForNextGaze = true;
            }

            ApplyPropertyBlock();
        }
        else if (waitingForNextGaze && IsGazingAtTerrain())
        {
            NextColor();
        }
    }

    private void NextColor()
    {
        waitingForNextGaze = false;
        isFading = true;
        colorFadeTimer = 0f;

        previousColor = currentColor;
        currentPaletteIndex = (currentPaletteIndex + 1) % colorPalette.Length;
        targetColor = colorPalette[currentPaletteIndex];
    }

    private bool IsGazingAtTerrain()
    {
        if (gazeTarget == null)
            return false;

        Ray ray = new Ray(gazeTarget.position, gazeTarget.forward);
        if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, gazeDistance))
        {
            return hit.collider != null && hit.collider.gameObject == terrain.gameObject;
        }

        return false;
    }

    private void ApplyPropertyBlock()
    {
        if (terrain == null || block == null) return;

        block.SetColor("_Color", currentColor);
        block.SetFloat("_Shades", currentShades);
        block.SetVector("_MinMax", new Vector4(minMax.x, minMax.y, 0f, 0f));
        terrain.SetSplatMaterialPropertyBlock(block);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (terrain == null)
            terrain = GetComponent<Terrain>();

        if (block == null)
            block = new MaterialPropertyBlock();

        currentColor = Color.black;
        currentShades = 0.1f;

        ApplyPropertyBlock();
    }

    private void OnDrawGizmos()
    {
        if (gazeTarget == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(gazeTarget.position, gazeTarget.position + gazeTarget.forward * gazeDistance);

        Ray ray = new Ray(gazeTarget.position, gazeTarget.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, gazeDistance))
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(hit.point, 0.2f);
        }
    }
#endif
}
