using System.Collections;
using UnityEngine;

/// <summary>
/// Game-feel singleton: time freeze and screen shake (GDD §13.1).
/// Shake uses unscaledDeltaTime so it works during time-freeze.
/// </summary>
public class GameFeel : MonoBehaviour
{
    public static GameFeel Instance { get; private set; }

    [Header("Shake Target")]
    [Tooltip("Leave null to auto-find Camera.main.")]
    [SerializeField] Camera cam;

    Coroutine _freezeCoroutine;
    Coroutine _shakeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (cam == null)
            cam = Camera.main;
    }

    // ─── TIME FREEZE ──────────────────────────────────────────────

    /// <summary>
    /// Freeze time for <paramref name="duration"/> seconds at <paramref name="timeScale"/>.
    /// GDD §4.3 examples: Freeze(0.05, 0.1), Freeze(0.2, 0), Freeze(0.5, 0).
    /// </summary>
    public void Freeze(float duration, float timeScale = 0f)
    {
        if (_freezeCoroutine != null)
            StopCoroutine(_freezeCoroutine);
        _freezeCoroutine = StartCoroutine(DoFreeze(duration, timeScale));
    }

    IEnumerator DoFreeze(float dur, float ts)
    {
        Time.timeScale = ts;
        yield return new WaitForSecondsRealtime(dur);
        Time.timeScale = 1f;
        _freezeCoroutine = null;
    }

    // ─── SCREEN SHAKE ─────────────────────────────────────────────

    /// <summary>
    /// Shake the camera for <paramref name="duration"/> at <paramref name="intensity"/>.
    /// GDD §4.3 examples: Shake(0.05, ...), Shake(0.3, 0.15), Shake(0.6, 0.4).
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        if (cam == null) return;
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(DoShake(intensity, duration));
    }

    IEnumerator DoShake(float intensity, float duration)
    {
        Vector3 originalPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            cam.transform.localPosition = new Vector3(
                originalPos.x + x,
                originalPos.y + y,
                originalPos.z
            );
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalPos;
        _shakeCoroutine = null;
    }
}
