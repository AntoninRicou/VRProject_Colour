// using UnityEngine;

// public class ContinuousSkyColorChanger : MonoBehaviour
// {
//     public Color targetSkyColor = Color.blue;     // Final sky color after all objects are gazed
//     public float colorFadeDuration = 2f;         // Time to fade from black to red

//     private bool transitionStarted = false;
//     private bool transitionComplete = false;
//     private float fadeTimer = 0f;

//     void Start()
//     {
//         Camera.main.backgroundColor = Color.black;
//     }

//     void Update()
//     {
//         if (!transitionStarted && ShaderGraphToonController.AllObjectsGazedAtLeastOnce())
//         {
//             transitionStarted = true;
//             fadeTimer = 0f;
//             Debug.Log("🌇 All objects gazed — starting sky transition from black to red.");
//         }

//         if (transitionStarted && !transitionComplete)
//         {
//             fadeTimer += Time.deltaTime;
//             float t = Mathf.Clamp01(fadeTimer / colorFadeDuration);

//             Camera.main.backgroundColor = Color.Lerp(Color.black, targetSkyColor, t);

//             if (t >= 1f)
//             {
//                 transitionComplete = true;
//                 Debug.Log("✅ Sky transition complete.");
//             }
//         }
//     }
// }
