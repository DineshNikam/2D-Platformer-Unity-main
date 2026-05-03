using UnityEngine;

public class pickup : MonoBehaviour
{
    public enum pickupType { coin,gem,health}

    public pickupType pt;
    [SerializeField] GameObject PickupEffect;
    [SerializeField] AudioClip pickupSFX;

    void SpawnPickupEffectAt(Vector3 worldPos)
    {
        if (PickupEffect == null)
            return;

        GameObject fx = Instantiate(PickupEffect, worldPos, Quaternion.identity);
        float destroyAfter = 3f;
        if (fx.TryGetComponent(out ParticleSystem ps))
        {
            var main = ps.main;
            destroyAfter = main.duration;
            if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                destroyAfter += main.startLifetime.constant;
            else
                destroyAfter += main.startLifetime.constantMax;
            destroyAfter += 0.25f;
        }

        Destroy(fx, destroyAfter);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(pt == pickupType.coin)
        {
            if(collision.gameObject.tag == "Player")
            {
                GameManager.instance.IncrementCoinCount();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSFX(pickupSFX);
                SpawnPickupEffectAt(transform.position);
                Destroy(gameObject, 0.2f);
            }
            
        }

        if (pt == pickupType.gem)
        {
            if (collision.gameObject.tag == "Player")
            {
                GameManager.instance.IncrementGemCount();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSFX(pickupSFX);
                SpawnPickupEffectAt(transform.position);
                Destroy(this.gameObject, 0.2f);
            }

        }

        if (pt == pickupType.health)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Health hp = collision.GetComponent<Health>()
                    ?? collision.GetComponentInParent<Health>();
                if (hp != null)
                    hp.Heal(1f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPickupSFX(pickupSFX);
                SpawnPickupEffectAt(transform.position);
                Destroy(gameObject, 0.2f);
            }
        }
    }
}
