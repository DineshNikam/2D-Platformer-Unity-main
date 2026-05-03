using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One-click setup for URP Bloom on the menu title TMP: verifies URP in the project, switches the menu Canvas
/// to Screen Space — Camera (required for Post Processing to affect UI), enables post-processing on the main camera,
/// creates a Volume Profile with Bloom, and pushes HDR-ish outline / glow values on TMP so bloom has emission to read.
/// </summary>
public static class NeonRunnerMenuBloomSetup
{
    public const string DefaultMenuScenePath = "Assets/Scenes/Menu.unity";

    /// <summary>Volume profile asset (Bloom override).</summary>
    public const string DefaultVolumeProfilePath = "Assets/Settings/NeonRunner_MenuBloomProfile.asset";

    const string VolumeObjectName = "NeonRunner_Menu_GlobalVolume";
    const string TitleObjectName = "Title";

    [MenuItem("Neon Runner/Menu/Setup TMP Title Bloom (URP)", false, 210)]
    public static void SetupMenuBloom()
    {
        var report = new List<string>();

        TryVerifyManifestDependencies(report);

        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
        {
            EditorUtility.DisplayDialog(
                "Neon Runner · Menu Bloom",
                "Active render pipeline is not URP.\n\n" +
                "Assign your Universal RP asset under Edit → Project Settings → Graphics → Scriptable Render Pipeline Settings.\n\n" +
                WrapReport(report),
                "OK");
            return;
        }

        report.Add($"URP active asset: {(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GetAssetPath(urp))?.name ?? urp.name)}");
        report.Add($"URP.supportsHDR={urp.supportsHDR}");

        if (!TryOpenScene(DefaultMenuScenePath, report))
            return;

        var menuScene = SceneManager.GetSceneByPath(DefaultMenuScenePath);
        Camera mainCam = GetMainCameraInScene(menuScene);
        if (mainCam == null)
        {
            EditorUtility.DisplayDialog("Neon Runner · Menu Bloom", "No tagged Main Camera found in Menu scene.", "OK");
            return;
        }

        if (!EnsureScreenSpaceCanvasForPostFx(menuScene, mainCam, report))
            return;

        if (!EnsureCameraPostProcessing(mainCam, report))
            return;

        var profile = EnsureVolumeProfileWithBloom(DefaultVolumeProfilePath, report);

        Volume menuBloomVolume = EnsureGlobalVolume(menuScene, profile, report);

        if (!EnsureTitleTmpBloomGlow(menuScene, report, out var titleTmp))
            return;

        Canvas menuCanvas = FindFirstCanvas(menuScene);
        if (menuCanvas != null && menuBloomVolume != null && titleTmp != null)
            EnsureMenuBloomRuntimeController(menuCanvas.gameObject, menuCanvas, menuBloomVolume, titleTmp, report);

        EditorSceneManager.MarkSceneDirty(menuScene);

        AssetDatabase.SaveAssets();

        if (titleTmp != null)
            Selection.activeGameObject = titleTmp.gameObject;

        Debug.Log($"[NeonRunnerMenuBloomSetup] Completed.\n{string.Join("\n", report)}");

        EditorUtility.DisplayDialog(
            "Neon Runner · Menu Bloom",
            "Bloom setup applied to Menu scene.\n\n" +
            "Remember: bloom only affects Screen Space — Camera canvases rendered through URP Post Processing.\n\n" +
            WrapReport(report),
            "OK");
    }

    [MenuItem("Neon Runner/Menu/Verify Bloom Prerequisites (Packages + URP)", false, 211)]
    public static void VerifyOnly()
    {
        var lines = new List<string>();
        TryVerifyManifestDependencies(lines);

        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        lines.Add(urp != null ? "Graphics: Universal Render Pipeline is active OK." : "Graphics: Missing URP as active Scriptable RP asset.");

        if (urp != null)
            lines.Add($"URP.supportsHDR={urp.supportsHDR}");

        lines.Add("TextMesh Pro: TMP_Text type is available in this project.");

        EditorUtility.DisplayDialog("Neon Runner · Verification", WrapReport(lines), "OK");

        Debug.Log("[NeonRunnerMenuBloomSetup] Verify:\n" + string.Join("\n", lines));
    }

    static string WrapReport(IReadOnlyList<string> lines)
    {
        if (lines == null || lines.Count == 0)
            return "(no checks)";
        return string.Join("\n", lines);
    }

    /// <summary>Reads Packages/manifest.json for expected package ids.</summary>
    static void TryVerifyManifestDependencies(List<string> report)
    {
        try
        {
            var repoRoot = Path.GetDirectoryName(Application.dataPath);
            var manifest = Path.Combine(repoRoot ?? "", "Packages", "manifest.json");
            if (!File.Exists(manifest))
            {
                report.Add($"Packages manifest not found at {manifest}");
                return;
            }

            var text = File.ReadAllText(manifest);
            if (text.IndexOf("\"com.unity.render-pipelines.universal\"", StringComparison.Ordinal) >= 0)
                report.Add("manifest.json: com.unity.render-pipelines.universal ✓");

            else
                report.Add("manifest.json: com.unity.render-pipelines.universal NOT listed — install URP via Package Manager.");

            if (text.IndexOf("\"com.unity.render-pipelines.core\"", StringComparison.Ordinal) >= 0)
                report.Add("manifest.json: com.unity.render-pipelines.core ✓");

            else
                report.Add("manifest.json: UR core package not listed (comes with URP).");

            if (text.IndexOf("\"com.unity.ugui\"", StringComparison.Ordinal) >= 0)
                report.Add("manifest.json: com.unity.ugui ✓");
        }
        catch (Exception ex)
        {
            report.Add("manifest.json read failed: " + ex.Message);
        }
    }

    static bool TryOpenScene(string scenePath, List<string> report)
    {
        if (!File.Exists(Path.Combine(Application.dataPath, "..", scenePath.Replace("/", Path.DirectorySeparatorChar.ToString()))))
        {
            EditorUtility.DisplayDialog("Neon Runner · Menu Bloom", $"Scene asset not found at:\n{scenePath}", "OK");
            return false;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
            report.Add($"Opened scene: {scenePath}");
            return true;
        }

        return false;
    }

    static Camera GetMainCameraInScene(Scene scene)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var found = WalkTransform(go.transform, tf =>
            {
                if (!tf.gameObject.CompareTag("MainCamera"))
                    return false;
                return tf.TryGetComponent<Camera>(out _);
            });
            if (found != null && found.TryGetComponent<Camera>(out var cam))
                return cam;
        }

        return null;
    }

    static Transform WalkTransform(Transform root, Predicate<Transform> match)
    {
        if (match(root))
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var inner = WalkTransform(root.GetChild(i), match);
            if (inner != null)
                return inner;
        }

        return null;
    }

    static bool EnsureScreenSpaceCanvasForPostFx(Scene scene, Camera cam, List<string> report)
    {
        Canvas canvas = FindFirstCanvas(scene);
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Neon Runner · Menu Bloom", "No Canvas in Menu scene.", "OK");
            return false;
        }

        Undo.RecordObject(canvas, "Canvas Screen Space Camera for Bloom");

        var canvasRt = canvas.GetComponent<RectTransform>();
        if (canvasRt != null && canvasRt.localScale.sqrMagnitude < 0.0001f)
        {
            Undo.RecordObject(canvasRt, "Canvas scale for Screen Space Camera");
            canvasRt.localScale = Vector3.one;
            report.Add("Canvas: root scale was (0,0,0); set to (1,1,1) for Screen Space Camera.");
        }

        if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
        {
            report.Add($"Canvas: changed render mode Overlay → Screen Space Camera ({canvas.name}).");
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = Mathf.Clamp(cam.farClipPlane * 0.1f, 10f, 100f);
        }
        else
        {
            if (canvas.worldCamera != cam)
            {
                canvas.worldCamera = cam;
                report.Add("Canvas: assigned Main Camera.");
            }

            report.Add("Canvas: already Screen Space — Camera ✓");
        }

        EditorUtility.SetDirty(canvas);
        return true;
    }

    static Canvas FindFirstCanvas(Scene scene)
    {
        Canvas found = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            found = dfs(go);
            if (found != null)
                break;
        }

        return found;

        Canvas dfs(GameObject o)
        {
            if (o.TryGetComponent<Canvas>(out var c))
                return c;

            foreach (Transform ch in o.transform)
            {
                var x = dfs(ch.gameObject);
                if (x != null)
                    return x;
            }

            return null;
        }
    }

    static bool EnsureCameraPostProcessing(Camera cam, List<string> report)
    {
        var data = cam.GetUniversalAdditionalCameraData();
        if (data == null)
        {
            EditorUtility.DisplayDialog(
                "Neon Runner · Menu Bloom",
                "UniversalAdditionalCameraData missing on Main Camera.\n\nRe-add URP to the project or recreate the Camera.",
                "OK");
            return false;
        }

        Undo.RecordObject(data, "Enable Post Processing");
        Undo.RecordObject(cam, "Camera HDR for Bloom");

        data.renderPostProcessing = true;

        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(cam);

        report.Add("Camera: UniversalAdditionalCameraData.renderPostProcessing = true ✓");
        report.Add($"Camera: allowHDR implicitly used by URP; scene camera HDR should be enabled in Inspector if available.");
        return true;
    }

    /// <returns>Configured or existing Bloom profile asset.</returns>
    static VolumeProfile EnsureVolumeProfileWithBloom(string assetPath, List<string> report)
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(assetPath);
        if (profile == null)
        {
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(dir).Replace('\\', '/'), Path.GetFileName(dir));

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(profile, assetPath);
            report.Add($"Created Volume Profile asset: {assetPath}");
        }
        else
            report.Add($"Using existing Volume Profile: {assetPath}");

        Undo.RecordObject(profile, "Bloom volume profile");

        if (!TryGetBloom(profile, out var bloom))
        {
            bloom = ScriptableObject.CreateInstance<Bloom>();
            bloom.name = nameof(Bloom);
            bloom.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            profile.components.Add(bloom);
            AssetDatabase.AddObjectToAsset(bloom, profile);
            report.Add("Volume Profile: added Bloom volume component.");
        }
        else
            report.Add("Volume Profile: Bloom override already exists.");

        bloom.active = true;

        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.35f;

        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1f;

        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.92f;

        bloom.tint.overrideState = true;
        bloom.tint.value = Color.white;

        EditorUtility.SetDirty(profile);
        return profile;
    }

    static bool TryGetBloom(VolumeProfile profile, out Bloom bloom)
    {
        bloom = null;
        var list = profile.components;
        if (list == null || list.Count == 0)
            return false;

        bloom = profile.components.Find(c => c is Bloom) as Bloom;
        return bloom != null;
    }

    static Volume EnsureGlobalVolume(Scene scene, VolumeProfile profile, List<string> report)
    {
        var existing = SceneVolumeByName(scene, VolumeObjectName);
        Volume volume;
        GameObject volumeGo;

        if (existing != null)
        {
            volumeGo = existing.gameObject;
            volume = existing;
            report.Add($"Volume: reused '{VolumeObjectName}'.");
            Undo.RecordObject(volume, "Configure global volume");
        }
        else
        {
            volumeGo = new GameObject(VolumeObjectName);
            volumeGo.layer = LayerMask.NameToLayer("Default");
            SceneManager.MoveGameObjectToScene(volumeGo, scene);
            volume = volumeGo.AddComponent<Volume>();
            Undo.RegisterCreatedObjectUndo(volumeGo, "Create NeonRunner global volume");
            report.Add($"Volume: created '{VolumeObjectName}' on Default layer.");
        }

        Undo.RecordObject(volume, "Configure global volume profile");
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.weight = 1f;
        AssignVolumeProfile(volume, profile);
        Undo.RecordObject(volumeGo.transform, "Volume transform");
        volumeGo.transform.localPosition = Vector3.zero;

        EditorUtility.SetDirty(volume);
        EditorUtility.SetDirty(volumeGo);
        return volume;
    }

    static void EnsureMenuBloomRuntimeController(
        GameObject canvasRoot,
        Canvas canvasRef,
        Volume volume,
        TMP_Text titleTmp,
        List<string> report)
    {
        if (canvasRoot.GetComponent<NeonRunnerMenuTitleBloomController>() == null)
            Undo.AddComponent<NeonRunnerMenuTitleBloomController>(canvasRoot);

        var ctl = canvasRoot.GetComponent<NeonRunnerMenuTitleBloomController>();
        if (ctl == null)
        {
            report.Add("Bloom controller: NeonRunnerMenuTitleBloomController missing after AddComponent.");
            return;
        }

        Undo.RecordObject(ctl, "Wire NeonRunnerMenuTitleBloomController");
        SerializedObject serialized = new SerializedObject(ctl);
        SetObjectReference(serialized, "targetCanvas", canvasRef);
        SetObjectReference(serialized, "titleText", titleTmp);
        SetObjectReference(serialized, "bloomVolume", volume);
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctl);
        report.Add("Canvas: NeonRunnerMenuTitleBloomController added / rewired.");
    }

    static void SetObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }

    static void AssignVolumeProfile(Volume volume, VolumeProfile profile)
    {
        var so = new SerializedObject(volume);
        foreach (var fieldName in new[] { "m_SharedProfile", "m_Profile", "sharedProfile" })
        {
            var p = so.FindProperty(fieldName);
            if (p == null)
                continue;
            p.objectReferenceValue = profile;
            so.ApplyModifiedProperties();
            return;
        }

        volume.sharedProfile = profile;
    }

    static Volume SceneVolumeByName(Scene scene, string objectName)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var v = FindVolume(go.transform, objectName);
            if (v != null)
                return v;
        }

        return null;
    }

    static Volume FindVolume(Transform tf, string name)
    {
        if (tf.name == name && tf.TryGetComponent<Volume>(out var vol))
            return vol;

        foreach (Transform c in tf)
        {
            var inner = FindVolume(c, name);
            if (inner != null)
                return inner;
        }

        return null;
    }

    static bool EnsureTitleTmpBloomGlow(Scene scene, List<string> report, out TMP_Text titleTmp)
    {
        titleTmp = null;

        if (!TryFindTitleTmp(scene, out var tmp, out var path))
        {
            EditorUtility.DisplayDialog(
                "Neon Runner · Menu Bloom",
                $"No suitable title TextMesh Pro found.\n\n" +
                $"Looked for GameObject named '{TitleObjectName}', or text containing 'NEON' / 'RUNNER'.\n\n" +
                "Adjust the hierarchy or extend NeonRunnerMenuBloomSetup.TryFindTitleTmp.",
                "OK");
            return false;
        }

        titleTmp = tmp;

        Undo.RecordObject(tmp, "TMP bloom-friendly HDR outline/glow");

        var top = new Color(2.2f, 0.35f, 0.9f, 1f);
        var bottom = new Color(0.25f, 0.45f, 3.2f, 1f);
        ApplyVertexGradientSerialized(tmp, top, bottom);

        tmp.outlineWidth = 0.28f;
        tmp.outlineColor = new Color(0.2f, 1.6f, 2.3f, 1f);

        ApplyTmpMaterialGlow(tmp);

        EditorUtility.SetDirty(tmp);
        report.Add($"TMP: configured '{path}' — vertex gradient + HDR outline + Glow material keyword for bloom pickup.");

        return true;
    }

    /// <summary>
    /// Vertex gradient fields are not always public on <see cref="TMP_Text"/> across Unity / TMP package versions;
    /// writing the same serialized fields the Inspector uses keeps this editor script compiling and consistent.
    /// </summary>
    static void ApplyVertexGradientSerialized(TMP_Text tmp, Color top, Color bottom)
    {
        var so = new SerializedObject(tmp);

        void TryBool(string propName, bool value)
        {
            var p = so.FindProperty(propName);
            if (p != null)
                p.boolValue = value;
        }

        void TryEnum(string propName, int fourCornersIndex)
        {
            var p = so.FindProperty(propName);
            if (p != null)
                p.enumValueIndex = fourCornersIndex;
        }

        TryBool("m_enableVertexGradient", true);
        TryBool("m_EnableVertexGradient", true);

        // TMP ColorMode enum: FourCornersGradient = 3 (Single=0, Horizontal=1, Vertical=2).
        TryEnum("m_colorMode", 3);
        TryEnum("m_ColorMode", 3);

        var grad = so.FindProperty("m_fontColorGradient");
        if (grad != null)
        {
            void Corner(string relative, Color c)
            {
                var cp = grad.FindPropertyRelative(relative);
                if (cp != null)
                    cp.colorValue = c;
            }

            Corner("topLeft", top);
            Corner("topRight", top);
            Corner("bottomLeft", bottom);
            Corner("bottomRight", bottom);
        }

        so.ApplyModifiedProperties();
    }

    static void ApplyTmpMaterialGlow(TMP_Text tmp)
    {
        Material mat = tmp.fontMaterial;
        Undo.RecordObject(mat, "TMP material glow keyword");

        const string GlowKeyword = "GLOW_ON";
        if (!mat.IsKeywordEnabled(GlowKeyword))
            mat.EnableKeyword(GlowKeyword);

        int glowColorId = Shader.PropertyToID("_GlowColor");
        int glowOuterId = Shader.PropertyToID("_GlowOuter");

        mat.SetColor(glowColorId, new Color(0.95f, 0.35f, 2.25f));
        mat.SetFloat(glowOuterId, 0.55f);

        EditorUtility.SetDirty(mat);
        tmp.ComputeMarginSize();
        tmp.SetAllDirty();
    }

    static bool TryFindTitleTmp(Scene scene, out TMP_Text tmp, out string breadcrumb)
    {
        tmp = null;
        breadcrumb = null;

        TMP_Text fallbackByText = null;
        string fallbackPath = null;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.name == TitleObjectName)
                {
                    tmp = text;
                    breadcrumb = DescribeTransformPath(text.transform);
                    return true;
                }

                string body = text.text;
                if (!string.IsNullOrEmpty(body))
                {
                    if (body.IndexOf("NEON", StringComparison.OrdinalIgnoreCase) >= 0
                        || body.IndexOf("RUNNER", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        fallbackByText = text;
                        fallbackPath = DescribeTransformPath(text.transform);
                    }
                }
            }
        }

        if (fallbackByText != null)
        {
            tmp = fallbackByText;
            breadcrumb = fallbackPath;
            return true;
        }

        return false;
    }

    static string DescribeTransformPath(Transform t)
    {
        string p = t.name;
        var cur = t.parent;
        while (cur != null)
        {
            p = cur.name + "/" + p;
            cur = cur.parent;
        }

        return p;
    }
}
