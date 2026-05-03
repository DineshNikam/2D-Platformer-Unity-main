using UnityEngine;
using UnityEngine.UI;

public class SettingsScreen : MonoBehaviour
{
    [Header("Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Awake()
    {
        if (musicSlider == null || sfxSlider == null)
        {
            Transform volumeParent = transform.Find("bg/bg_Settings/Volume");
            if (volumeParent != null)
            {
                if (musicSlider == null) musicSlider = volumeParent.Find("Slider")?.GetComponent<Slider>();
                if (sfxSlider == null) sfxSlider = volumeParent.Find("Slider (1)")?.GetComponent<Slider>();
            }
        }
    }

    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
            {
                musicSlider.value = AudioManager.Instance.musicVolume;
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = AudioManager.Instance.sfxVolume;
                sfxSlider.onValueChanged.AddListener(SetSFXVolume);
            }
        }
    }

    private void OnDisable()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    public void SetMusicVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
        }
    }
}
