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

    [Tooltip("Seconds to wait after game start before maybe showing the first hint.")]
    [SerializeField] private float delayBeforeHint = 10f;

    [Header("Messages")]
    [SerializeField] private string[] messages = { "Look up!", "Search the sky!" };
    [SerializeField] private float delayBetweenPrompts = 10f; // time between hints after the first

    [Tooltip("Max number of hints to show. <= 0 means unlimited until gaze.")]
    [SerializeField] private int maxShows = 0;

    Coroutine currentRoutine;
    Coroutine delayedRoutine;

    // gating
    bool anyTitleGazed = false;   // flipped by event
    int shownCount = 0;

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
            // Show immediately and start the loop afterward if desired
            Show(messages.Length > 0 ? messages[0] : "...");
        }

        if (delayedRoutine != null) StopCoroutine(delayedRoutine);
        delayedRoutine = StartCoroutine(DelayedMaybeShow());
    }

    void OnAnyFirstGazedBeforeShow(ShaderGraphToonController who)
    {
        if (who != null && who.isTitleGroup) // only letters matter
            anyTitleGazed = true;
    }

    IEnumerator DelayedMaybeShow()
    {
        // Wait once before the very first potential hint
        yield return new WaitForSeconds(delayBeforeHint);

        if (anyTitleGazed || messages == null || messages.Length == 0)
        {
            delayedRoutine = null;
            yield break;
        }

        int index = 0; // which prompt we're on

        // Loop until gaze OR maxShows reached (<=0 means unlimited)
        while (!anyTitleGazed && (maxShows <= 0 || shownCount < maxShows))
        {
            // Show the current message and wait for it to finish
            string messageToShow = messages[index];
            yield return StartCoroutine(ShowRoutine(messageToShow, showDuration));
            shownCount++;

            if (anyTitleGazed) break;

            // Advance index (cycle)
            index = (index + 1) % messages.Length;

            // Wait between prompts before showing the next one
            yield return new WaitForSeconds(delayBetweenPrompts);
        }

        // Done deciding about hints
        ShaderGraphToonController.FirstGazed -= OnAnyFirstGazedBeforeShow;
        delayedRoutine = null;
    }

    // Public API
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
