using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsScreen : MonoBehaviour
{
    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private Coroutine _bindRoutine;

    private void Awake()
    {
        TryResolveSliders();
    }

    private void OnEnable()
    {
        TryResolveSliders();
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        _bindRoutine = StartCoroutine(BindSlidersWhenReady());
    }

    private void OnDisable()
    {
        if (_bindRoutine != null)
        {
            StopCoroutine(_bindRoutine);
            _bindRoutine = null;
        }

        RemoveListeners();
    }

    private void TryResolveSliders()
    {
        if (musicSlider != null && sfxSlider != null)
            return;

        Transform volumeParent = transform.Find("bg/bg_Settings/Volume");
        if (volumeParent == null)
            return;

        if (musicSlider == null)
            musicSlider = volumeParent.Find("Slider")?.GetComponent<Slider>();
        if (sfxSlider == null)
            sfxSlider = volumeParent.Find("Slider (1)")?.GetComponent<Slider>();
    }

    private IEnumerator BindSlidersWhenReady()
    {
        int safety = 0;
        while (AudioManager.Instance == null && safety < 120)
        {
            safety++;
            yield return null;
        }

        TryResolveSliders();
        RemoveListeners();

        AudioManager am = AudioManager.Instance;
        if (am == null)
        {
            _bindRoutine = null;
            yield break;
        }

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(am.musicVolume);
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(am.sfxVolume);
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        _bindRoutine = null;
    }

    private void RemoveListeners()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }
}
