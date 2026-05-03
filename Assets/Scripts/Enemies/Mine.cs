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
            Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
            
            // Log everything detected if anything is detected
            if (cols.Length > 0)
            {
                string detectedList = "";
                foreach (var c in cols) detectedList += c.name + " (" + c.tag + "), ";
                Debug.Log($"[Mine] {gameObject.name} at {transform.position} detected: {detectedList}");
            }
            
            foreach (var col in cols)
            {
                if (col.CompareTag("Player"))
                {
                    Debug.Log($"[Mine] Player detected by {gameObject.name}!");
                    if (col.TryGetComponent<PlayerBrain>(out var player))
                    {
                        Debug.Log($"[Mine] Calling ApplyDamage({damage}) on PlayerBrain.");
                        player.ApplyDamage(damage);
                    }
                    else
                    {
                        // Try to find in parent in case collider is on a child
                        var parentBrain = col.GetComponentInParent<PlayerBrain>();
                        if (parentBrain != null)
                        {
                            Debug.Log($"[Mine] Calling ApplyDamage({damage}) on PlayerBrain (found in parent).");
                            parentBrain.ApplyDamage(damage);
                        }
                        else
                        {
                            Debug.LogWarning($"[Mine] Detected object tagged 'Player' but it has no PlayerBrain component!", col.gameObject);
                        }
                    }
                    Detonate();
                    break;
                }
            }
        }
    }

    private void Detonate()
    {
        if (isDetonated) return;
        isDetonated = true;

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
