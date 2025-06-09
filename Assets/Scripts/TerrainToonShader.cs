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
    public float gazeDistance = 100f;

    [Header("Visual Settings")]
    [SerializeField] private Vector2 minMax = new Vector2(0.1f, 0.9f);

    [Tooltip("Duration in seconds for the color to fully fade from one to another")]
    [SerializeField] private float colorFadeDuration = 10f;

    private int currentPaletteIndex = -1;
    private Color previousColor = Color.black;
    private Color currentColor = Color.black;
    private Color targetColor;

    private float currentShades = 0.1f;
    private float colorFadeTimer = 0f;
    private bool isFading = false;

    private void OnEnable()
    {
        terrain = GetComponent<Terrain>();

        if (terrain.materialTemplate == null)
            Debug.LogWarning("Terrain material must support _Color and _Shades.");

        if (block == null)
            block = new MaterialPropertyBlock();

        currentColor = previousColor = Color.black;
        currentPaletteIndex = Random.Range(0, colorPalette.Length);
        targetColor = colorPalette[currentPaletteIndex];

        colorFadeTimer = 0f;
        isFading = false;

        ApplyPropertyBlock();
    }

    private void Update()
    {
        if (!Application.isPlaying || gazeTarget == null || terrain == null)
            return;

        if (IsTerrainFirstHit())
        {
            if (!isFading)
                StartNextFade();

            colorFadeTimer += Time.deltaTime;

            float t = Mathf.Clamp01(colorFadeTimer / colorFadeDuration);
            currentColor = Color.Lerp(previousColor, targetColor, t);

            ApplyPropertyBlock();

            if (t >= 1f)
                isFading = false;
        }
    }

    private void StartNextFade()
    {
        isFading = true;
        colorFadeTimer = 0f;
        previousColor = currentColor;
        currentPaletteIndex = (currentPaletteIndex + 1) % colorPalette.Length;
        targetColor = colorPalette[currentPaletteIndex];
    }

    private bool IsTerrainFirstHit()
    {
        Ray ray = new Ray(gazeTarget.position, gazeTarget.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject == terrain.gameObject)
                return true;
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
            Gizmos.color = hit.collider.gameObject == terrain.gameObject ? Color.green : Color.red;
            Gizmos.DrawSphere(hit.point, 0.2f);
        }
    }
#endif
}
