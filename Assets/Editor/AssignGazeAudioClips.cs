using UnityEngine;
using UnityEditor;

public class AssignGazeAudioClips : EditorWindow
{
    [MenuItem("Tools/Assign Gaze Audio Clips")]
    public static void AssignClips()
    {
        // string path = "Assets/Audio/GazeClips";
        var clips = Resources.LoadAll<AudioClip>("Audio/GazeClips"); // or use AssetDatabase
        if (clips.Length == 0)
        {
            Debug.LogWarning("No audio clips found in Resources/Audio/GazeClips");
            return;
        }

        var controllers = FindObjectsByType<ShaderGraphToonController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            Undo.RecordObject(controller, "Assign Gaze Clips");
            controller.gazeClips = clips;
            EditorUtility.SetDirty(controller);
        }

        Debug.Log($"Assigned {clips.Length} audio clips to {controllers.Length} objects.");
    }
}