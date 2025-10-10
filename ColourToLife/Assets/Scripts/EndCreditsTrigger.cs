using UnityEngine;

public class EndCreditsTrigger : MonoBehaviour
{
    public EndCreditsSequence sequence;
    public float delayAfterSky = 5f;
    public bool useSkyFlag = true;  // watches ShaderGraphToonController.cloudTriggeredSky
    public bool useAllGazed = false; // or watch AllObjectsGazedAtLeastOnce()

    bool fired = false;

    void Reset()
    {
        sequence = FindFirstObjectByType<EndCreditsSequence>();
    }

    void Update()
    {
        if (fired || sequence == null) return;

        bool skyOn = useSkyFlag && ShaderGraphToonController.cloudTriggeredSky;
        bool allGazed = useAllGazed && ShaderGraphToonController.AllObjectsGazedAtLeastOnce();

        if (skyOn || allGazed)
        {
            fired = true;
            sequence.StartCredits(delayAfterSky);
        }
    }
}
