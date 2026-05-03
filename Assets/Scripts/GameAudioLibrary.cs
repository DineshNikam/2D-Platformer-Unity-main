using UnityEngine;

[CreateAssetMenu(fileName = "GameAudioLibrary", menuName = "Neon Runner/Game Audio Library", order = 0)]
public class GameAudioLibrary : ScriptableObject
{
    [Header("UI & Menu")]
    public AudioClip menuMusic;
    public AudioClip buttonClick;

    [Header("Player movement")]
    public AudioClip jump;
    public AudioClip doubleJump;
    public AudioClip land;

    [Header("Combat")]
    public AudioClip shoot;
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip shieldAbsorb;

    [Header("World")]
    public AudioClip pickup;
    public AudioClip enemyHit;
    public AudioClip enemyDeath;

    [Header("Hazards")]
    public AudioClip mineBlast;
}
