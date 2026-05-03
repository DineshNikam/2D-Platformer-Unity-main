using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for all enemies (GDD §5.4).
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Enemy Base Stats")]
    public float maxHP = 2f;
    public float powerUpDropChance = 0.2f;

    [Header("Audio")]
    public AudioClip hitSFX;
    public AudioClip deathSFX;

    protected float currentHP;
    protected SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private static Material flashMaterial;

    protected virtual void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalMaterial = spriteRenderer.material;
        }

        if (flashMaterial == null)
        {
            // Use a built-in unlit white or sprite flash material if available
            Shader shader = Shader.Find("GUI/Text Shader");
            if (shader != null)
            {
                flashMaterial = new Material(shader);
                flashMaterial.color = Color.white;
            }
        }
    }

    protected virtual void OnEnable()
    {
        currentHP = maxHP;
        if (spriteRenderer != null && originalMaterial != null)
        {
            spriteRenderer.material = originalMaterial;
        }
    }

    public virtual void TakeDamage(float amount)
    {
        currentHP -= amount;
        
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(FlashWhite(0.1f));
        }

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(hitSFX);
        }
    }

    protected virtual void Die()
    {
        if (GameFeel.Instance != null)
        {
            GameFeel.Instance.Freeze(0.08f, 0f);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(deathSFX);

        // TODO: ParticleManager.Instance.PlayAt("EnemyExplode", transform.position);
        Debug.Log($"Enemy {gameObject.name} exploded at {transform.position}");

        if (Random.value < powerUpDropChance)
        {
            if (PowerUpManager.Instance != null)
            {
                PowerUpManager.Instance.SpawnAt(transform.position);
            }
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddKill(maxHP);
        }

        // Using simple Destroy for now if EnemyPool is not set up
        Destroy(gameObject);
    }

    protected IEnumerator FlashWhite(float duration)
    {
        if (spriteRenderer != null && flashMaterial != null)
        {
            spriteRenderer.material = flashMaterial;
            yield return new WaitForSeconds(duration);
            if (spriteRenderer != null && originalMaterial != null)
            {
                spriteRenderer.material = originalMaterial;
            }
        }
        else
        {
            yield return null;
        }
    }
}
