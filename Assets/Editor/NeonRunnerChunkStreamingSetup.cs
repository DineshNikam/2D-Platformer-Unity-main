using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor utilities for <see cref="HorizontalChunkStreamer"/> and <see cref="PlatformChunk"/>.
/// </summary>
public static class NeonRunnerChunkStreamingSetup
{
    public const string DefaultChunksFolder = "Assets/Chunks";

    [MenuItem("Neon Runner/Chunk Streaming/Create Or Configure Streamer In Active Scene", false, 0)]
    static void CreateOrConfigureStreamerInScene()
    {
        HorizontalChunkStreamer streamer = Object.FindFirstObjectByType<HorizontalChunkStreamer>();
        if (streamer == null)
        {
            GameObject go = new GameObject("HorizontalChunkStreamer");
            Undo.RegisterCreatedObjectUndo(go, "Create HorizontalChunkStreamer");
            streamer = Undo.AddComponent<HorizontalChunkStreamer>(go);
        }
        else
            Selection.activeGameObject = streamer.gameObject;

        ConfigureStreamerDefaults(streamer);
        EditorSceneManager.MarkSceneDirty(streamer.gameObject.scene);
        Selection.activeGameObject = streamer.gameObject;
        Debug.Log($"Neon Runner: configured '{streamer.gameObject.name}'. Check Player and chunk prefabs in the Inspector.", streamer);
    }

    [MenuItem("Neon Runner/Camera/Add Runner Bounds Extender (CameraBounds)", false, 20)]
    static void AddRunnerBoundsExtenderToCameraBounds()
    {
        GameObject boundsGo = GameObject.Find("CameraBounds");
        if (boundsGo == null)
        {
            EditorUtility.DisplayDialog(
                "Neon Runner",
                "No GameObject named 'CameraBounds' in the active scene. Rename your Cinemachine confiner collider object or add the component manually.",
                "OK");
            return;
        }

        if (boundsGo.GetComponent<PolygonCollider2D>() == null)
        {
            EditorUtility.DisplayDialog("Neon Runner", "CameraBounds needs a PolygonCollider2D (Cinemachine Confiner shape).", "OK");
            return;
        }

        RunnerCinemachineBoundsExtender ext = boundsGo.GetComponent<RunnerCinemachineBoundsExtender>();
        if (ext == null)
        {
            Undo.RegisterCompleteObjectUndo(boundsGo, "Add RunnerCinemachineBoundsExtender");
            ext = Undo.AddComponent<RunnerCinemachineBoundsExtender>(boundsGo);
        }

        SerializedObject so = new SerializedObject(ext);
        Transform playerTf = FindPlayerTransform();
        if (playerTf != null)
            so.FindProperty("followTarget").objectReferenceValue = playerTf;

        HorizontalChunkStreamer streamer = Object.FindFirstObjectByType<HorizontalChunkStreamer>();
        if (streamer != null)
            so.FindProperty("chunkStreamer").objectReferenceValue = streamer;

        PolygonCollider2D poly = boundsGo.GetComponent<PolygonCollider2D>();
        CinemachineConfiner2D confiner = Object.FindFirstObjectByType<CinemachineConfiner2D>();
        if (confiner != null && confiner.m_BoundingShape2D == poly)
            so.FindProperty("confiner").objectReferenceValue = confiner;

        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float aspect = cam.pixelWidth > 0 ? (float)cam.pixelWidth / cam.pixelHeight : 16f / 9f;
            float halfW = cam.orthographicSize * aspect;
            so.FindProperty("rightPadding").floatValue = Mathf.Max(8f, halfW + 4f);
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ext);
        EditorSceneManager.MarkSceneDirty(boundsGo.scene);
        Selection.activeGameObject = boundsGo;
        Debug.Log("Neon Runner: RunnerCinemachineBoundsExtender added on CameraBounds (assign references if any were missing).", ext);
    }

    [MenuItem("Neon Runner/Chunk Streaming/Ensure PlatformChunk On Start + Chunk 1 + 2", false, 10)]
    static void EnsureChunksHavePlatformComponent()
    {
        int added = 0;
        foreach (string file in new[] { "StartChunk.prefab", "Chunk 1.prefab", "Chunk 2.prefab" })
        {
            string path = $"{DefaultChunksFolder}/{file}";
            if (AddPlatformChunkToPrefabAssetPath(path))
                added++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Neon Runner: added PlatformChunk on {added} prefab(s) (3 paths checked). Already had component = skip.");
    }

    [MenuItem("Neon Runner/Chunk Streaming/Add PlatformChunk To Prefabs In Assets/Chunks", false, 11)]
    static void AddPlatformChunkToChunksFolderPrefabs()
    {
        int added = AddPlatformChunkToPrefabsInFolder(DefaultChunksFolder);
        Debug.Log($"Neon Runner: added PlatformChunk to {added} prefab root(s) under {DefaultChunksFolder}.");
    }

    [MenuItem("Neon Runner/Chunk Streaming/Add PlatformChunk To Selected Prefabs", false, 12)]
    static void AddPlatformChunkToSelection()
    {
        List<string> paths = CollectPrefabPathsFromSelection();
        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog("Neon Runner", "Select one or more prefab assets in the Project window.", "OK");
            return;
        }

        int added = 0;
        foreach (string path in paths)
        {
            if (AddPlatformChunkToPrefabAssetPath(path))
                added++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Neon Runner: added PlatformChunk on {added} prefab root(s) ({paths.Count} selected).");
    }

    /// <summary>Used by custom inspector.</summary>
    public static void ConfigureStreamerDefaults(HorizontalChunkStreamer streamer)
    {
        if (streamer == null)
            return;

        var so = new SerializedObject(streamer);

        Transform playerTf = FindPlayerTransform();
        if (playerTf != null)
            so.FindProperty("player").objectReferenceValue = playerTf;

        GameObject startGo = GameObject.Find("StartChunk");
        PlatformChunk startPc = null;
        if (startGo != null)
            startPc = startGo.GetComponent<PlatformChunk>() ?? startGo.GetComponentInChildren<PlatformChunk>(true);

        if (startPc != null)
            so.FindProperty("startChunkInScene").objectReferenceValue = startPc;
        else
            so.FindProperty("startChunkInScene").objectReferenceValue = null;

        List<GameObject> repeating = LoadRepeatingChunkPrefabs();
        SerializedProperty chunkProp = so.FindProperty("repeatingChunkPrefabs");
        chunkProp.ClearArray();
        for (int i = 0; i < repeating.Count; i++)
        {
            chunkProp.InsertArrayElementAtIndex(i);
            chunkProp.GetArrayElementAtIndex(i).objectReferenceValue = repeating[i];
        }

        SerializedProperty acc = so.FindProperty("activeChunkCount");
        if (acc.intValue < 2)
            acc.intValue = Mathf.Max(2, Mathf.Min(4, repeating.Count > 0 ? repeating.Count : 4));

        if (playerTf != null)
        {
            SerializedProperty yz = so.FindProperty("levelPositionYZ");
            yz.vector2Value = new Vector2(0f, playerTf.position.z);

            SerializedProperty startX = so.FindProperty("worldStartLeftX");
            if (Mathf.Approximately(startX.floatValue, 0f))
                startX.floatValue = Mathf.Round(playerTf.position.x * 100f) / 100f - 12f;
        }

        ApplyRecycleDistancesFromMainCamera(so);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(streamer);
    }

    public static void ApplyRecycleDistancesFromMainCamera(SerializedObject streamerSo)
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        float aspect = cam.pixelWidth > 0 ? (float)cam.pixelWidth / cam.pixelHeight : 16f / 9f;
        float halfWorldWidth = cam.orthographicSize * aspect;
        float margin = 4f;
        float suggested = halfWorldWidth + margin;

        SerializedProperty behind = streamerSo.FindProperty("recycleBehindDistance");
        SerializedProperty ahead = streamerSo.FindProperty("recycleAheadDistance");
        if (suggested > 0f)
        {
            behind.floatValue = Mathf.Max(behind.floatValue, suggested);
            ahead.floatValue = Mathf.Max(ahead.floatValue, suggested);
        }
    }

    public static int AddPlatformChunkToPrefabsInFolder(string folder)
    {
        string normalized = folder.Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(normalized))
            return 0;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { normalized });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AddPlatformChunkToPrefabAssetPath(path))
                count++;
        }

        AssetDatabase.SaveAssets();
        return count;
    }

    static bool AddPlatformChunkToPrefabAssetPath(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
            return false;

        try
        {
            if (root.GetComponent<PlatformChunk>() != null)
                return false;

            root.AddComponent<PlatformChunk>();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static List<string> CollectPrefabPathsFromSelection()
    {
        var paths = new HashSet<string>();
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab"))
                paths.Add(path);
        }

        return new List<string>(paths);
    }

    static List<GameObject> LoadPrefabsFromFolder(string folder)
    {
        var list = new List<GameObject>();
        string normalized = folder.Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(normalized))
            return list;

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { normalized });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p != null)
                list.Add(p);
        }

        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }

    /// <summary>Prefer Chunk 1 + Chunk 2; otherwise every prefab in the folder except StartChunk.</summary>
    static List<GameObject> LoadRepeatingChunkPrefabs()
    {
        var named = new List<GameObject>();
        foreach (string fileName in new[] { "Chunk 1.prefab", "Chunk 2.prefab" })
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>($"{DefaultChunksFolder}/{fileName}");
            if (p != null)
                named.Add(p);
        }

        if (named.Count > 0)
            return named;

        List<GameObject> all = LoadPrefabsFromFolder(DefaultChunksFolder);
        var filtered = new List<GameObject>();
        foreach (GameObject go in all)
        {
            if (go != null && go.name != "StartChunk")
                filtered.Add(go);
        }

        return filtered;
    }

    static Transform FindPlayerTransform()
    {
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
            return pc.transform;

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            return tagged.transform;

        return null;
    }
}
