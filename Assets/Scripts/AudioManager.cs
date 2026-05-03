using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Shared library")]
    [Tooltip("Fallback clips when scene objects leave SFX fields empty.")]
    [SerializeField] private GameAudioLibrary audioLibrary;

    [Header("Clips (assign in Inspector)")]
    [Tooltip("Optional override; if empty, uses library.menuMusic when present.")]
    [SerializeField] private AudioClip menuMusicClip;

    [Header("Default Volumes")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;

    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    private static AudioClip Prefer(AudioClip primary, AudioClip fromLibrary) =>
        primary != null ? primary : fromLibrary;

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
        if (buildIndex != 0) return;
        AudioClip menu = Prefer(menuMusicClip, audioLibrary != null ? audioLibrary.menuMusic : null);
        if (menu == null) return;
        PlayMusic(menu);
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

    public void PlayJumpSFX(bool isDoubleJump, AudioClip jumpOverride, AudioClip doubleJumpOverride)
    {
        AudioClip c = isDoubleJump
            ? Prefer(doubleJumpOverride, audioLibrary != null ? audioLibrary.doubleJump : null)
            : Prefer(jumpOverride, audioLibrary != null ? audioLibrary.jump : null);
        PlaySFX(c);
    }

    public void PlayLandSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.land : null));
    }

    public void PlayShootSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.shoot : null));
    }

    public void PlayButtonSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.buttonClick : null));
    }

    public void PlayPickupSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.pickup : null));
    }

    public void PlayPlayerHurtSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.playerHurt : null));
    }

    public void PlayPlayerDeathSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.playerDeath : null));
    }

    public void PlayShieldAbsorbSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.shieldAbsorb : null));
    }

    public void PlayEnemyHitSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.enemyHit : null));
    }

    public void PlayEnemyDeathSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.enemyDeath : null));
    }

    public void PlayMineBlastSFX(AudioClip overrideClip)
    {
        PlaySFX(Prefer(overrideClip, audioLibrary != null ? audioLibrary.mineBlast : null));
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
