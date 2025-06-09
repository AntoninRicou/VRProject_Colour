using UnityEngine;

public class ToonGazeManager : MonoBehaviour
{
    private bool eventTriggered = false;

    void Update()
    {
        if (!eventTriggered && ShaderGraphToonController.AllObjectsGazedAtLeastOnce())
        {
            Debug.Log("✅ All ShaderGraphToonController objects have been gazed at least once!");
            eventTriggered = true;

            // Add your action here:
            // - UI display
            // - next scene
            // - audio
            // etc.
        }
    }
}
