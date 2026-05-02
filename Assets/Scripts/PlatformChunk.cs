using UnityEngine;

/// <summary>
/// Attach to the root of each platform chunk prefab (same object you instantiate).
/// Set <see cref="chunkWidth"/> for manual width (transform X = chunk center along the run).
/// Leave width at 0 to measure children: uses union of Collider2D bounds, else Renderer bounds.
/// When auto-measured, left/right edges follow the measured bounds center so the Grid root need not match geometry.
/// </summary>
[DisallowMultipleComponent]
public class PlatformChunk : MonoBehaviour
{
    [Tooltip("World width along X. If 0 or less, width is taken from child Collider2D or Renderer bounds.")]
    [SerializeField] float chunkWidth;

    public float Width { get; private set; }

    /// <summary>World X offset from transform.position to the measured/manual horizontal center.</summary>
    float _centerOffsetX;

    void Awake()
    {
        if (chunkWidth > 0f)
        {
            Width = chunkWidth;
            _centerOffsetX = 0f;
        }
        else if (!TryMeasureFromGeometry(out float w, out float offset))
        {
            Width = 1f;
            _centerOffsetX = 0f;
            Debug.LogWarning($"{name}: PlatformChunk could not measure width; defaulting to 1. Assign chunkWidth on the prefab.", this);
        }
        else
        {
            Width = Mathf.Max(0.01f, w);
            _centerOffsetX = offset;
        }
    }

    public float GetCenterX() => transform.position.x + _centerOffsetX;

    public float GetLeftX() => GetCenterX() - Width * 0.5f;

    public float GetRightX() => GetCenterX() + Width * 0.5f;

    /// <summary>Moves the chunk so its logical horizontal center is at <paramref name="worldCenterX"/>.</summary>
    public void SetHorizontalCenter(float worldCenterX)
    {
        Vector3 pos = transform.position;
        pos.x = worldCenterX - _centerOffsetX;
        transform.position = pos;
    }

    bool TryMeasureFromGeometry(out float width, out float centerOffsetX)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        bool any = false;

        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            var b = col.bounds;
            minX = Mathf.Min(minX, b.min.x);
            maxX = Mathf.Max(maxX, b.max.x);
            any = true;
        }

        if (!any)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled) continue;
                var b = r.bounds;
                minX = Mathf.Min(minX, b.min.x);
                maxX = Mathf.Max(maxX, b.max.x);
                any = true;
            }
        }

        if (!any)
        {
            width = 0f;
            centerOffsetX = 0f;
            return false;
        }

        float mid = (minX + maxX) * 0.5f;
        centerOffsetX = mid - transform.position.x;
        width = maxX - minX;
        return true;
    }
}
