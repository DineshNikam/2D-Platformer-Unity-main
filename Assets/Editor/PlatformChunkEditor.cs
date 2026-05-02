using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlatformChunk))]
public class PlatformChunkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        var chunk = (PlatformChunk)target;

        SerializedProperty leftA = serializedObject.FindProperty("leftEdgeAnchor");
        SerializedProperty rightA = serializedObject.FindProperty("rightEdgeAnchor");
        if (leftA.objectReferenceValue != null && rightA.objectReferenceValue != null)
        {
            var la = (Transform)leftA.objectReferenceValue;
            var ra = (Transform)rightA.objectReferenceValue;
            float span = ra.position.x - la.position.x;
            if (span > 0.0001f)
                EditorGUILayout.HelpBox($"Edge anchors: span {span:F2} (world X {la.position.x:F2} → {ra.position.x:F2}). Overrides Chunk Width / union.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Right anchor must be right of left anchor in X.", MessageType.Warning);
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Width tools", EditorStyles.boldLabel);

        SerializedProperty cw = serializedObject.FindProperty("chunkWidth");
        if (cw.floatValue > 0f)
            EditorGUILayout.HelpBox($"Using baked Chunk Width: {cw.floatValue:F2} (runtime uses this; transform X is chunk center).", MessageType.Info);

        if (chunk.TryMeasureGeometryUnion(out float unionW, out float unionOff))
        {
            EditorGUILayout.HelpBox(
                $"Geometry union (colliders + renderers + tilemaps): {unionW:F2} wide\nCenter offset from transform X: {unionOff:F2}",
                MessageType.None);
        }
        else
        {
            EditorGUILayout.HelpBox("Geometry union is empty — add Collider2D / Tilemap or set Chunk Width manually.", MessageType.Warning);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Bake union into Chunk Width"))
        {
            if (chunk.TryMeasureGeometryUnion(out float measured, out _) && measured > 0.0001f)
            {
                Undo.RecordObject(chunk, "Bake PlatformChunk width");
                serializedObject.Update();
                serializedObject.FindProperty("chunkWidth").floatValue = measured;
                serializedObject.ApplyModifiedProperties();
                chunk.ResolveWidth();
                EditorUtility.SetDirty(chunk);
            }
        }

        if (GUILayout.Button("Clear Chunk Width (auto union)"))
        {
            Undo.RecordObject(chunk, "Clear PlatformChunk width");
            serializedObject.Update();
            serializedObject.FindProperty("chunkWidth").floatValue = 0f;
            serializedObject.ApplyModifiedProperties();
            chunk.ResolveWidth();
            EditorUtility.SetDirty(chunk);
        }

        EditorGUILayout.EndHorizontal();
    }
}
