using System.Linq;
using UnityEngine;
using TMPro;

public class ProgressManager : MonoBehaviour
{
    [Header("HUD (optional)")]
    [SerializeField] TMP_Text hud;                 // world-space TMP under the VR camera
    [SerializeField] bool showPhaseLabel = true;

    public enum Phase { Title, World }
    public Phase CurrentPhase { get; private set; } = Phase.Title;

    int titleTotal, titleCurrent;
    int worldTotal, worldCurrent;

    void OnEnable()
    {
        ShaderGraphToonController.FirstGazed += OnFirstGazed;
        RebuildTotals();
        RecountCurrents();   // in case some start pre-gazed (dev mode)
        UpdateHUD();
    }

    void OnDisable()
    {
        ShaderGraphToonController.FirstGazed -= OnFirstGazed;
    }

    // ---- Totals & current counts ----
    void RebuildTotals()
    {
        // Unity 2023+: use FindObjectsByType instead of FindObjectsOfType
        var all = Object.FindObjectsByType<ShaderGraphToonController>(FindObjectsSortMode.None);
        titleTotal = all.Count(a => a.isTitleGroup);
        worldTotal = all.Count(a => !a.isTitleGroup);
    }

    void RecountCurrents()
    {
        var all = Object.FindObjectsByType<ShaderGraphToonController>(FindObjectsSortMode.None);
        titleCurrent = all.Count(a => a.isTitleGroup && a.HasBeenGazedOnce);
        worldCurrent = all.Count(a => !a.isTitleGroup && a.HasBeenGazedOnce);

        if (titleCurrent >= titleTotal)
            CurrentPhase = Phase.World;
    }

    // Call this if you add/remove objects at runtime and want to rebuild numbers.
    public void Refresh()
    {
        RebuildTotals();
        RecountCurrents();
        UpdateHUD();
    }

    // ---- Event from ShaderGraphToonController ----
    void OnFirstGazed(ShaderGraphToonController who)
    {
        if (who.isTitleGroup)
        {
            titleCurrent++;
            if (titleCurrent >= titleTotal)
            {
                CurrentPhase = Phase.World;
                // TODO: activate world here if needed
            }
        }
        else
        {
            if (CurrentPhase == Phase.World)
                worldCurrent++;
        }
        UpdateHUD();
    }

    // ---- UI ----
    void UpdateHUD()
    {
        if (!hud) return;

        string frac(int num, int den) =>
            $"<sup=100%>{num}</sup>⁄<sub=100%>{den}</sub>";

        if (CurrentPhase == Phase.Title)
            hud.text = showPhaseLabel
                ? $"{frac(titleCurrent, Mathf.Max(1, titleTotal))}" // title
                : frac(titleCurrent, Mathf.Max(1, titleTotal));
        else
            hud.text = showPhaseLabel
                ? $"{frac(worldCurrent, Mathf.Max(1, worldTotal))}"
                : frac(worldCurrent, Mathf.Max(1, worldTotal));
    }

}
