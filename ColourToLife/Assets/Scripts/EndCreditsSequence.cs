using System.Collections;
using UnityEngine;

public class EndCreditsSequence : MonoBehaviour
{
    [Tooltip("Assign each credit parent object here (each should have a CanvasGroup).")]
    public CanvasGroup[] creditGroups;

    [Header("Timing (seconds)")]
    public float fadeInTime = 1f;
    public float holdTime = 10f;
    public float fadeOutTime = 1f;
    public float delayBeforeStart = 2f;
    public float gapBetweenSlides = 3f;

    [Header("Options")]
    [Tooltip("If true, the credits start automatically on play (useful for testing).")]
    public bool showOnStart = false;

    Coroutine runCo;

    void Start()
    {
        // Make sure all start invisible
        foreach (var cg in creditGroups)
        {
            if (cg != null)
                cg.alpha = 0f;
        }

        // If testing, run automatically
        if (showOnStart)
        {
            StartCredits();
        }
    }

    public void StartCredits(float delayOverride = -1f)
    {
        if (runCo != null)
            StopCoroutine(runCo);

        // If delayOverride < 0, use default delayBeforeStart
        float delay = delayOverride >= 0f ? delayOverride : delayBeforeStart;
        runCo = StartCoroutine(RunCredits(delay));
    }

    IEnumerator RunCredits(float startDelay)
    {
        yield return new WaitForSeconds(startDelay);

        foreach (var cg in creditGroups)
        {
            if (cg == null) continue;

            // Fade in
            yield return Fade(cg, 0f, 1f, fadeInTime);

            // Hold
            yield return new WaitForSeconds(holdTime);

            // Fade out
            yield return Fade(cg, 1f, 0f, fadeOutTime);

            // Optional gap between slides
            if (gapBetweenSlides > 0f)
                yield return new WaitForSeconds(gapBetweenSlides);
        }

        runCo = null;
    }

    IEnumerator Fade(CanvasGroup cg, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            float smooth = k * k * (3f - 2f * k); // smoothstep easing
            cg.alpha = Mathf.Lerp(from, to, smooth);
            yield return null;
        }
        cg.alpha = to;
    }
}
