using UnityEngine;

public class FishProjectile : MonoBehaviour
{
    public float speed = 5f;
    public float arcHeight = 2f;
    public float damage = 1f;
    public float lifetime = 4f;
    
    private Vector2 startPos;
    private Vector2 targetDir;
    private float age;

    public void Initialize(Vector2 direction)
    {
        targetDir = direction;
        startPos = transform.position;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age > lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Basic parabolic arc movement approximation
        Vector2 horizontalMove = startPos + targetDir * speed * age;
        float height = Mathf.Sin(Mathf.Clamp01(age / lifetime) * Mathf.PI) * arcHeight;
        
        transform.position = new Vector3(horizontalMove.x, startPos.y + height, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<PlayerBrain>(out var player))
        {
            player.ApplyDamage(damage);
            Destroy(gameObject);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
