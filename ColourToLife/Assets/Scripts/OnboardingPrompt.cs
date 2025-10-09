using System.Collections;
using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class OnboardingPrompt : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behavior")]
    [SerializeField] private float fadeIn = 0.5f;
    [SerializeField] private float showDuration = 3f;
    [SerializeField] private float fadeOut = 0.5f;

    [Tooltip("Show immediately on Start (bypasses delayed logic).")]
    [SerializeField] private bool showOnStart = false;

    [Tooltip("Seconds to wait after game start before maybe showing the hint.")]
    [SerializeField] private float delayBeforeHint = 10f;

    [SerializeField] private string firstMessage = "Look around";

    Coroutine currentRoutine;
    Coroutine delayedRoutine;

    // gating
    bool anyTitleGazed = false;   // updated before we decide to show
    bool hasShownOnce = false;    // ensure we only show once

    void Reset()
    {
        promptText = GetComponentInChildren<TMP_Text>(true);
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        // Listen only to decide whether to show at all
        ShaderGraphToonController.FirstGazed += OnAnyFirstGazedBeforeShow;
    }

    void OnDisable()
    {
        ShaderGraphToonController.FirstGazed -= OnAnyFirstGazedBeforeShow;
    }

    void Start()
    {
        if (showOnStart)
        {
            BeginShow(firstMessage, showDuration);
        }
        else
        {
            if (delayedRoutine != null) StopCoroutine(delayedRoutine);
            delayedRoutine = StartCoroutine(DelayedMaybeShow());
        }
    }

    void OnAnyFirstGazedBeforeShow(ShaderGraphToonController who)
    {
        if (hasShownOnce) return;            // already showing or shown — ignore
        if (who != null && who.isTitleGroup) // only letters matter
            anyTitleGazed = true;
    }

    IEnumerator DelayedMaybeShow()
    {
        yield return new WaitForSeconds(delayBeforeHint);

        // If player already found a letter, skip the hint
        if (!anyTitleGazed)
            BeginShow(firstMessage, showDuration);

        delayedRoutine = null;
    }

    void BeginShow(string message, float duration)
    {
        if (hasShownOnce) return;
        hasShownOnce = true;

        // We no longer need to listen for gazes (we won't dismiss)
        ShaderGraphToonController.FirstGazed -= OnAnyFirstGazedBeforeShow;

        Show(message, duration);
    }

    /// Public API (kept same)
    public void Show(string message, float duration = 3f)
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowRoutine(message, duration));
    }

    IEnumerator ShowRoutine(string message, float duration)
    {
        if (promptText) promptText.text = message;

        if (canvasGroup) yield return FadeTo(1f, fadeIn);

        yield return new WaitForSeconds(duration);

        if (canvasGroup) yield return FadeTo(0f, fadeOut);

        currentRoutine = null;
    }

    IEnumerator FadeTo(float target, float time)
    {
        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / time);
            float eased = k * k * (3f - 2f * k);
            canvasGroup.alpha = Mathf.Lerp(start, target, eased);
            // keep it centered each frame (optional)
            transform.localPosition = new Vector3(0f, -0.05f, 1.5f);
            transform.localRotation = Quaternion.identity;
            yield return null;
        }
        canvasGroup.alpha = target;
    }
}
