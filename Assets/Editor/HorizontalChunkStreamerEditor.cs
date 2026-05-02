using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HorizontalChunkStreamer))]
public class HorizontalChunkStreamerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Neon Runner — Quick setup", EditorStyles.boldLabel);

        var streamer = (HorizontalChunkStreamer)target;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-configure (player + Chunks folder)"))
        {
            Undo.RecordObject(streamer, "Configure HorizontalChunkStreamer");
            NeonRunnerChunkStreamingSetup.ConfigureStreamerDefaults(streamer);
        }

        if (GUILayout.Button("Recycle distance from Main Camera"))
        {
            var so = new SerializedObject(streamer);
            NeonRunnerChunkStreamingSetup.ApplyRecycleDistancesFromMainCamera(so);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(streamer);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Start: place StartChunk in the scene and assign Start Chunk In Scene (or name it 'StartChunk'). It is not spawned or moved.\n" +
            "Repeating: Chunk 1 + Chunk 2 prefabs. PlatformChunk: bake width if auto measure shows 0.",
            MessageType.Info);
    }
}
