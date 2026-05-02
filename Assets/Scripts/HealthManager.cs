using UnityEngine;
using UnityEngine.UI;

public class HealthManager : MonoBehaviour
{
    public static HealthManager instance;
    public GameObject damageEffect;

    [SerializeField] private Image[] hearts;
    [SerializeField] private Sprite FullHeartSprite;
    [SerializeField] private Sprite EmptyHeartSprite;

    Health _health;

    Transform _damageEffectAnchor;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
            return;

        _damageEffectAnchor = player.transform;
        _health = player.GetComponent<Health>();
        if (_health == null)
        {
            Debug.LogWarning("HealthManager: PlayerController has no Health component.");
            return;
        }

        _health.Damaged += OnPlayerDamaged;
        DisplayHearts();
    }

    void OnDestroy()
    {
        if (_health != null)
            _health.Damaged -= OnPlayerDamaged;
    }

    void OnPlayerDamaged(float amount, float remaining)
    {
        DisplayHearts();
        if (damageEffect != null && _damageEffectAnchor != null)
            Instantiate(damageEffect, _damageEffectAnchor.position, Quaternion.identity);
    }

    public void DisplayHearts()
    {
        if (hearts == null || hearts.Length == 0 || _health == null)
            return;

        int maxSlots = Mathf.Min(hearts.Length, Mathf.RoundToInt(_health.MaxHp));
        int filled = Mathf.Clamp((_health != null) ? Mathf.RoundToInt(_health.CurrentHp) : 0, 0, maxSlots);

        for (int i = 0; i < hearts.Length; i++)
        {
            Image heartImage = hearts[i];
            if (heartImage == null)
                continue;
            heartImage.sprite = i < filled ? FullHeartSprite : EmptyHeartSprite;
        }
    }
}
