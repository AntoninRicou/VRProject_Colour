using UnityEngine;

[CreateAssetMenu(fileName = "ToonVisualConfig", menuName = "Custom/Toon Visual Config")]
public class ToonVisualConfig : ScriptableObject
{
    [Header("Color Palette")]
    public Color[] colorPalette;

    [Header("Shader Graph Properties")]
    public Vector2 minMax = new Vector2(0.1f, 0.9f);
    [Range(0f, 1f)] public float initialShades = 1f;
    [Range(0f, 1f)] public float targetShades = 0.1f;

    [Header("Transition Durations")]
    public float colorFadeDuration = 4f;
    public float shadeFadeDuration = 4f;
    public float holdDuration = 2f;

    [Header("Gaze Settings")]
    [Range(0.8f, 1f)] public float lookThreshold = 0.95f;
    public bool requireContinuousGaze = false;
}
