using UnityEngine;

public class ContinuousSkyColorChanger : MonoBehaviour
{
    public Color[] skyColors;              // Colors to cycle through (should not include black)
    public float colorFadeDuration = 2f;   // Time to fade between each color

    private int currentColorIndex = -1;    // Start before first color (fade from black)
    private int nextColorIndex = 0;
    private float fadeTimer = 0f;
    private Color currentBaseColor = Color.black;

    void Start()
    {
        if (skyColors == null || skyColors.Length == 0)
        {
            Debug.LogWarning("Assign at least one color in skyColors");
            enabled = false;
            return;
        }

        Camera.main.backgroundColor = Color.black;
        currentColorIndex = -1; // So we fade from black first
        nextColorIndex = 0;
        fadeTimer = 0f;
    }

    void Update()
    {
        fadeTimer += Time.deltaTime;

        float t = Mathf.Clamp01(fadeTimer / colorFadeDuration);

        Color fromColor = (currentColorIndex == -1) ? Color.black : skyColors[currentColorIndex];
        Color toColor = skyColors[nextColorIndex];

        Camera.main.backgroundColor = Color.Lerp(fromColor, toColor, t);

        if (t >= 1f)
        {
            fadeTimer = 0f;
            currentColorIndex = nextColorIndex;
            nextColorIndex = (nextColorIndex + 1) % skyColors.Length;
        }
    }
}
