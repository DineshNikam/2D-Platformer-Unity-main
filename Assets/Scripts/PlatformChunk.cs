using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Attach to the root of each platform chunk (scene or prefab). Set <see cref="chunkWidth"/> for a manual span,
/// or optional <c>leftEdgeAnchor</c> / <c>rightEdgeAnchor</c> child transforms at the playable lips (overrides width and union).
/// </summary>
[DisallowMultipleComponent]
public class PlatformChunk : MonoBehaviour
{
    [Tooltip("Optional child transforms at the playable left/right lip in world space. When both set, they define span and center (overrides Chunk Width and geometry union).")]
    [SerializeField] Transform leftEdgeAnchor;

    [Tooltip("Optional. Place at the playable right lip (world X). Use with Left Edge Anchor; together they override width measurement.")]
    [SerializeField] Transform rightEdgeAnchor;

    [Tooltip("World width along X. If 0, width is the union of measured children (colliders + renderers + tilemaps). Ignored when both edge anchors are set.")]
    [SerializeField] float chunkWidth;

    public float Width { get; private set; }

    /// <summary>World X offset from transform.position to the measured/manual horizontal center.</summary>
    float _centerOffsetX;

    void Awake()
    {
        ResolveWidth();
    }

    /// <summary>Recomputes <see cref="Width"/> from geometry or serialized <see cref="chunkWidth"/>.</summary>
    public void ResolveWidth()
    {
        if (TryResolveFromEdgeAnchors(out float wA, out float offA))
        {
            Width = Mathf.Max(0.01f, wA);
            _centerOffsetX = offA;
            return;
        }

        if (chunkWidth > 0f)
        {
            Width = chunkWidth;
            _centerOffsetX = 0f;
            return;
        }

        if (TryMeasureUnionAllSources(out float w, out float offset))
        {
            Width = Mathf.Max(0.01f, w);
            _centerOffsetX = offset;
            return;
        }

        Width = 0.01f;
        _centerOffsetX = 0f;
        Debug.LogWarning($"{name}: PlatformChunk could not measure width. Add colliders/tilemaps, or set Chunk Width / use Inspector 'Bake measured width'.", this);
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

    /// <summary>Union of colliders + renderers + tilemaps (ignores <see cref="chunkWidth"/> and anchors).</summary>
    public bool TryMeasureGeometryUnion(out float width, out float centerOffsetX)
    {
        return TryMeasureUnionAllSources(out width, out centerOffsetX);
    }

    bool TryResolveFromEdgeAnchors(out float width, out float centerOffsetX)
    {
        width = 0f;
        centerOffsetX = 0f;
        if (leftEdgeAnchor == null || rightEdgeAnchor == null)
            return false;

        float l = leftEdgeAnchor.position.x;
        float r = rightEdgeAnchor.position.x;
        if (r <= l + 0.0001f)
            return false;

        width = r - l;
        centerOffsetX = (l + r) * 0.5f - transform.position.x;
        return true;
    }

    bool TryMeasureUnionAllSources(out float width, out float centerOffsetX)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        bool any = false;

        AddColliderBounds(ref minX, ref maxX, ref any);
        AddRendererBounds(ref minX, ref maxX, ref any);
        AddTilemapBounds(ref minX, ref maxX, ref any);

        return FinalizeSpan(minX, maxX, any, out width, out centerOffsetX);
    }

    void AddColliderBounds(ref float minX, ref float maxX, ref bool any)
    {
        foreach (var col in GetComponentsInChildren<Collider2D>(true))
        {
            var b = col.bounds;
            minX = Mathf.Min(minX, b.min.x);
            maxX = Mathf.Max(maxX, b.max.x);
            any = true;
        }
    }

    void AddRendererBounds(ref float minX, ref float maxX, ref bool any)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (!r.enabled)
                continue;
            var b = r.bounds;
            minX = Mathf.Min(minX, b.min.x);
            maxX = Mathf.Max(maxX, b.max.x);
            any = true;
        }
    }

    void AddTilemapBounds(ref float minX, ref float maxX, ref bool any)
    {
        foreach (var tm in GetComponentsInChildren<Tilemap>(true))
        {
            if (!tm.gameObject.activeInHierarchy)
                continue;
            tm.CompressBounds();
            Bounds lb = tm.localBounds;
            if (lb.size.x <= 0f && lb.size.y <= 0f)
                continue;

            Vector3 c = lb.center;
            Vector3 e = lb.extents;
            for (int i = 0; i < 8; i++)
            {
                float sx = (i & 1) == 0 ? -e.x : e.x;
                float sy = (i & 2) == 0 ? -e.y : e.y;
                float sz = (i & 4) == 0 ? -e.z : e.z;
                Vector3 world = tm.transform.TransformPoint(c + new Vector3(sx, sy, sz));
                minX = Mathf.Min(minX, world.x);
                maxX = Mathf.Max(maxX, world.x);
            }

            any = true;
        }
    }

    bool FinalizeSpan(float minX, float maxX, bool any, out float width, out float centerOffsetX)
    {
        if (!any || minX > maxX)
        {
            width = 0f;
            centerOffsetX = 0f;
            return false;
        }

        float mid = (minX + maxX) * 0.5f;
        centerOffsetX = mid - transform.position.x;
        width = maxX - minX;
        return width > 0.0001f;
    }
}
