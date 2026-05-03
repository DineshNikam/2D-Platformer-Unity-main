using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Menu-only helper: lowers luminance on every TMP under this canvas except the title so URP bloom
/// primarily affects the NEON RUNNER headline. Duplicates the assigned <see cref="Volume"/> profile at runtime
/// and animates bloom + title TMP glow parameters (no edits to shared VolumeProfile assets during play).
/// </summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
public sealed class NeonRunnerMenuTitleBloomController : MonoBehaviour
{
    public const string DefaultVolumeObjectName = "NeonRunner_Menu_GlobalVolume";

    [Header("References (optional — auto-fill from Canvas / hierarchy)")]
    [SerializeField] Canvas targetCanvas;
    [SerializeField] TMP_Text titleText;
    [SerializeField] Volume bloomVolume;

    [Tooltip("If bloom volume lives at the scene root under this name.")]
    [SerializeField] string bloomVolumeObjectName = DefaultVolumeObjectName;

    [Header("Keep bloom on title only")]
    [SerializeField] Color menuButtonTextColor = new Color(0.5f, 0.52f, 0.58f, 1f);
    [SerializeField] float menuButtonOutlineWidth;

    [Header("Bloom pulse (cloned Volume Profile)")]
    [SerializeField] float bloomPulseSpeed = 1.05f;
    [SerializeField] float bloomIntensityMin = 0.28f;
    [SerializeField] float bloomIntensityMax = 0.92f;
    [SerializeField] float bloomScatterMin = 0.8f;
    [SerializeField] float bloomScatterMax = 0.97f;

    [Header("Title TMP glow pulse")]
    [SerializeField] bool animateTitleGlow = true;
    [SerializeField] float glowOuterMin = 0.3f;
    [SerializeField] float glowOuterMax = 0.72f;
    [SerializeField] float glowPhaseOffsetRadians = 0.65f;

    static readonly int GlowOuterId = Shader.PropertyToID("_GlowOuter");

    VolumeProfile _sourceProfileOriginal;
    Transform _titleRoot;

    VolumeProfile _runtimeProfile;
    Bloom _runtimeBloom;
    Material _titleMaterial;
    bool _dimApplied;

    void Awake()
    {
        ResolveReferences();
        DimNonTitleTexts();
        SetupRuntimeVolumeProfile();
        CacheTitleMaterial();
    }

    void OnDestroy()
    {
        if (bloomVolume != null && _sourceProfileOriginal != null && bloomVolume.sharedProfile == _runtimeProfile)
            bloomVolume.sharedProfile = _sourceProfileOriginal;

        if (_runtimeProfile != null)
            Destroy(_runtimeProfile);

        _runtimeProfile = null;
        _runtimeBloom = null;
    }

    void LateUpdate()
    {
        float phase = Mathf.Sin(Time.unscaledTime * bloomPulseSpeed);
        float norm = Mathf.Clamp01(0.5f + 0.5f * phase);

        if (_runtimeBloom != null)
        {
            _runtimeBloom.intensity.overrideState = true;
            _runtimeBloom.intensity.value = Mathf.Lerp(bloomIntensityMin, bloomIntensityMax, norm);

            _runtimeBloom.scatter.overrideState = true;
            _runtimeBloom.scatter.value = Mathf.Lerp(bloomScatterMin, bloomScatterMax, norm);
        }

        if (animateTitleGlow && _titleMaterial != null && _titleMaterial.HasProperty(GlowOuterId))
        {
            float glowWave = Mathf.Sin(Time.unscaledTime * bloomPulseSpeed + glowPhaseOffsetRadians);
            float g = Mathf.Clamp01(0.5f + 0.5f * glowWave);
            _titleMaterial.SetFloat(GlowOuterId, Mathf.Lerp(glowOuterMin, glowOuterMax, g));
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            bloomPulseSpeed = Mathf.Max(0.01f, bloomPulseSpeed);
    }
#endif

    void ResolveReferences()
    {
        if (targetCanvas == null)
            TryGetComponent(out targetCanvas);

        if (titleText == null && targetCanvas != null)
        {
            var t = targetCanvas.transform.Find("Title");
            if (t != null)
                titleText = t.GetComponent<TMP_Text>();
        }

        if (bloomVolume == null && !string.IsNullOrEmpty(bloomVolumeObjectName))
        {
            GameObject vo = GameObject.Find(bloomVolumeObjectName);
            if (vo != null)
                bloomVolume = vo.GetComponent<Volume>();
        }

        _titleRoot = titleText != null ? titleText.transform : null;
    }

    void DimNonTitleTexts()
    {
        if (_dimApplied || targetCanvas == null)
            return;

        var texts = targetCanvas.GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in texts)
        {
            if (_titleRoot != null && (tmp.transform == _titleRoot || tmp.transform.IsChildOf(_titleRoot)))
                continue;
            if (!tmp.gameObject.activeInHierarchy)
                continue;

            tmp.color = menuButtonTextColor;
            tmp.outlineWidth = menuButtonOutlineWidth;

            var mat = tmp.fontMaterial;
            mat.DisableKeyword("GLOW_ON");
        }

        _dimApplied = true;
    }

    void SetupRuntimeVolumeProfile()
    {
        if (bloomVolume == null || bloomVolume.sharedProfile == null)
            return;

        _sourceProfileOriginal = bloomVolume.sharedProfile;
        _runtimeProfile = Instantiate(_sourceProfileOriginal);
        bloomVolume.sharedProfile = _runtimeProfile;

        foreach (var comp in _runtimeProfile.components)
        {
            if (comp is Bloom b)
            {
                _runtimeBloom = b;
                break;
            }
        }
    }

    void CacheTitleMaterial()
    {
        if (titleText == null)
            return;
        _titleMaterial = titleText.fontMaterial;
    }
}
