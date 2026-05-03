using UnityEngine;

/// <summary>
/// Mine (GDD §5.2). Drops from CrabEnemy, detonates on proximity or after lifetime.
/// </summary>
public class Mine : MonoBehaviour
{
    public float delayBeforeActive = 0.5f;
    public float lifetime = 10f;
    public float detectionRadius = 1.2f;
    public float damage = 1f;
    public float explosionDuration = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip blastSFX;

    private float age;
    private bool isDetonated;

    private void Update()
    {
        if (isDetonated) return;

        age += Time.deltaTime;
        
        if (age > lifetime)
        {
            Detonate();
            return;
        }

        if (age > delayBeforeActive)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
            
            // Check for explosion
            if (colliders.Length > 0)
            {
                foreach (var col in colliders)
                {
                    if (col.CompareTag("Player"))
                    {
                        Debug.Log($"[Mine] Player {col.name} triggered mine at {transform.position}");
                        Detonate();
                        break;
                    }
                }
            }
        }
    }

    private void Detonate()
    {
        if (isDetonated) return;
        isDetonated = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayMineBlastSFX(blastSFX);

        // Deal damage in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius * 1.5f);
        foreach (var hit in hits)
        {
            var brain = hit.GetComponent<PlayerBrain>();
            if (brain == null) brain = hit.GetComponentInParent<PlayerBrain>();
            if (brain != null)
            {
                Debug.Log($"[Mine] Damaging {hit.name} for {damage}");
                brain.ApplyDamage((int)damage);
            }
        }

        if (TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = false;
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(true);

        Destroy(gameObject, explosionDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
