using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips (assign in Inspector)")]
    [Tooltip("Looped on the Menu (build index 0) whenever that scene loads.")]
    [SerializeField] private AudioClip menuMusicClip;

    [Header("Default Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (musicSource == null) musicSource = CreateAudioSource("MusicSource", true);
            if (sfxSource == null) sfxSource = CreateAudioSource("SFXSource", false);
            
            LoadSettings();
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryPlayMenuMusicForBuildIndex(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlayMenuMusicForBuildIndex(scene.buildIndex);
    }

    /// <summary>Build index 0 is the first scene in File → Build Settings (your Menu).</summary>
    private void TryPlayMenuMusicForBuildIndex(int buildIndex)
    {
        if (buildIndex != 0 || menuMusicClip == null) return;
        PlayMusic(menuMusicClip);
    }

    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        return source;
    }

    private void LoadSettings()
    {
        musicVolume = PlayerPrefs.GetFloat(MUSIC_KEY, 0.5f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_KEY, 0.5f);
        
        if (musicSource != null) musicSource.volume = musicVolume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null) musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat(MUSIC_KEY, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat(SFX_KEY, sfxVolume);
        PlayerPrefs.Save();
    }
}
