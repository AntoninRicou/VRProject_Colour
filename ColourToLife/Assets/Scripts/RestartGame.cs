using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class HeadsetStillnessRestart : MonoBehaviour
{
    // add near your other state fields
    private bool wasPaused = false;
    private System.DateTime pauseStartUtc;

    [Header("Timing")]
    [Tooltip("Seconds the off-head condition (tilt/pitch OR pause) must hold before restarting.")]
    public float restartDelay = 5f;
    [Tooltip("Seconds the UPRIGHT off-head condition must hold before restarting.")]
    public float uprightRestartDelay = 5f;          // longer to avoid false positives
    [Tooltip("How often to poll (seconds).")]
    public float pollInterval = 0.25f;
    [Tooltip("Ignore monitoring for the first seconds after scene load.")]
    public float gracePeriodAtStart = 5f;

    [Header("Stillness thresholds")]
    [Tooltip("Linear velocity magnitude below this is considered still (m/s).")]
    public float velocityThreshold = 0.02f;
    [Tooltip("Angular velocity magnitude below this is considered still (rad/s).")]
    public float angularVelocityThreshold = 0.02f;

    [Header("Tilt/Pitch off-head gate")]
    [Tooltip("Minimum tilt from world-up to consider off-head (deg).")]
    public float minTiltDegrees = 70f;
    [Tooltip("Alternatively consider face-up/down as off-head if |pitch| exceeds this (deg).")]
    public float extremePitchDegrees = 80f;

    [Header("UPRIGHT off-head gate")]
    [Tooltip("Max tilt (deg) to be considered upright (on a stand).")]
    public float uprightTiltMax = 15f;
    [Tooltip("Max pitch from horizon (deg) to be considered 'looking forward'.")]
    public float uprightPitchMax = 15f;
    // NOTE: requireControllersUntrackedForUpright and controllerVelocityInputThreshold are removed

    [Header("Input gate")]
    [Tooltip("Cancel countdown if any input in the last N seconds.")]
    public float noInputWindow = 0f;
    // NOTE: This input gate will now rely ONLY on any non-HMD input you implement later (e.g., keyboard, hand gestures).
    // If your game is truly passive (only HMD movement), you can set noInputWindow to a very high value or 0 to ignore it.

    [Header("Debug")]
    public bool debugLog = false;

    // internal state
    private InputDevice headDevice;
    // Removed: private readonly List<InputDevice> handDevices = new();
    private bool countingDown = false;
    private float holdTimer = 0f;
    private float startTime;
    private float lastInputTime = -999f;
    private bool pausedCountdown = false;

    void Awake()
    {
        startTime = Time.unscaledTime;
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            if (debugLog) Debug.Log("[StillnessRestart] Application paused → start countdown (wall-clock).");
            pausedCountdown = true;
            wasPaused = true;
            pauseStartUtc = System.DateTime.UtcNow;

            holdTimer = 0f;
        }
        else
        {
            if (wasPaused && pausedCountdown)
            {
                float pausedSecs = (float)(System.DateTime.UtcNow - pauseStartUtc).TotalSeconds;
                if (debugLog) Debug.Log($"[StillnessRestart] Resumed after {pausedSecs:F2}s pause.");

                if (pausedSecs >= restartDelay)
                {
                    if (debugLog) Debug.Log("[StillnessRestart] Pause exceeded delay → restarting.");
                    RestartScene();
                    return;
                }
                else
                {
                    float remaining = restartDelay - pausedSecs;
                    if (debugLog) Debug.Log($"[StillnessRestart] Scheduling remaining {remaining:F2}s to restart.");
                    StartCoroutine(RestartAfterRemaining(remaining));
                }
            }

            wasPaused = false;
            pausedCountdown = false;
            holdTimer = 0f;
            if (debugLog) Debug.Log("[StillnessRestart] Application resumed → cancel in-loop countdown.");
        }
    }

    private IEnumerator RestartAfterRemaining(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSecondsRealtime(seconds);
        RestartScene();
    }


    IEnumerator Start()
    {
        var wait = new WaitForSecondsRealtime(pollInterval);

        while (true)
        {
            // grace window
            if (Time.unscaledTime - startTime < gracePeriodAtStart)
            {
                ResetCountdown();
                yield return wait;
                continue;
            }

            // ensure HMD device
            if (!headDevice.isValid)
            {
                var heads = new List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, heads);
                if (heads.Count > 0) headDevice = heads[0];
            }

            // NOTE: RefreshHands and TrackRecentInput are removed.
            // You should manually update lastInputTime here if you have other input methods (e.g. keyboard, hand gestures).
            // For now, noRecentInput is always true if no other input is implemented here.
            bool noRecentInput = (Time.unscaledTime - lastInputTime) >= noInputWindow;


            // ----- 1. STILLNESS (HMD) -----
            Vector3 v = Vector3.zero, w = Vector3.zero;
            bool veryStill = false;
            if (headDevice.isValid)
            {
                bool hasVel = headDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out v);
                bool hasAng = headDevice.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out w);
                if (hasVel && hasAng)
                    veryStill = (v.magnitude < velocityThreshold) && (w.magnitude < angularVelocityThreshold);
            }

            // ----- 2. ORIENTATION (via camera) -----
            bool offHeadByTiltPitch = false;
            bool uprightPose = false;

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 upWorld = Vector3.up;
                Vector3 hmdUp = cam.transform.up;
                Vector3 hmdFwd = cam.transform.forward;

                float tilt = Vector3.Angle(hmdUp, upWorld);
                float pitchFromHorizon = 90f - Vector3.Angle(hmdFwd, upWorld);

                bool bigTilt = tilt >= minTiltDegrees;
                bool extremePitch = Mathf.Abs(pitchFromHorizon) >= extremePitchDegrees;

                offHeadByTiltPitch = bigTilt || extremePitch;

                // upright band (on a stand): small tilt and small pitch
                bool smallTilt = tilt <= uprightTiltMax;
                bool smallPitch = Mathf.Abs(pitchFromHorizon) <= uprightPitchMax;
                uprightPose = smallTilt && smallPitch;
            }

            // ----- CANDIDATES (Simplified) -----
            bool candidateTiltPitch = veryStill && offHeadByTiltPitch && noRecentInput;
            // Upright candidate no longer needs the controller check
            bool candidateUpright = veryStill && uprightPose && noRecentInput;

            // include pause as valid trigger
            bool shouldCount = pausedCountdown || candidateTiltPitch || candidateUpright;

            // choose target delay (upright path uses longer delay)
            float targetDelay = (candidateUpright && !pausedCountdown && !candidateTiltPitch)
                                ? Mathf.Max(uprightRestartDelay, restartDelay)
                                : restartDelay;

            if (shouldCount)
            {
                if (!countingDown)
                {
                    countingDown = true;
                    holdTimer = 0f;
                    if (debugLog)
                    {
                        string reason = pausedCountdown ? "pause"
                                        : candidateTiltPitch ? "tilt/pitch"
                                        : "upright";
                        Debug.Log($"[StillnessRestart] Start countdown ({reason}). Target {targetDelay:F1}s");
                    }
                }
                else
                {
                    holdTimer += pollInterval;
                    if (holdTimer >= targetDelay)
                    {
                        if (debugLog) Debug.Log("[StillnessRestart] Restarting scene (off-head sustained).");
                        RestartScene();
                        yield break;
                    }
                }
            }
            else
            {
                if (countingDown && debugLog) Debug.Log("[StillnessRestart] Conditions broken → cancel.");
                ResetCountdown();
            }

            yield return wait;
        }
    }

    void ResetCountdown()
    {
        countingDown = false;
        holdTimer = 0f;
    }

    void RestartScene()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
}