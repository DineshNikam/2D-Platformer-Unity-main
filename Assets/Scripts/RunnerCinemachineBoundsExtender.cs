using Cinemachine;
using UnityEngine;

/// <summary>
/// Grows the Cinemachine Confiner <see cref="PolygonCollider2D"/> on the X axis so the camera can follow
/// an endless runner past the original template bounds (start area only).
/// Attach to the same GameObject as <see cref="PolygonCollider2D"/> (e.g. CameraBounds) and assign the player + streamer.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PolygonCollider2D))]
public class RunnerCinemachineBoundsExtender : MonoBehaviour
{
    [SerializeField] Transform followTarget;
    [SerializeField] HorizontalChunkStreamer chunkStreamer;

    [Tooltip("World units beyond max(player X, streamed content right edge) the collider's right side should reach.")]
    [SerializeField] float rightPadding = 20f;

    [Tooltip("Optional; if set, confiner cache is invalidated when the polygon changes (Cinemachine 2.x: InvalidateCache).")]
    [SerializeField] CinemachineConfiner2D confiner;

    PolygonCollider2D _poly;
    float _baselineMaxRight;

    void Awake()
    {
        _poly = GetComponent<PolygonCollider2D>();
    }

    void Start()
    {
        if (_poly != null)
            _baselineMaxRight = ComputeMaxPathX(_poly);
    }

    void LateUpdate()
    {
        if (_poly == null || followTarget == null)
            return;

        float contentRight = _baselineMaxRight;
        if (chunkStreamer != null)
            contentRight = Mathf.Max(contentRight, chunkStreamer.GetRightmostContentEdgeX());

        float targetRight = Mathf.Max(contentRight, followTarget.position.x) + rightPadding;

        float currentMax = ComputeMaxPathX(_poly);
        if (targetRight <= currentMax + 0.05f)
            return;

        ExpandColliderRightEdge(_poly, targetRight);

        if (confiner != null && confiner.m_BoundingShape2D == _poly)
            confiner.InvalidateCache();
    }

    static float ComputeMaxPathX(PolygonCollider2D poly)
    {
        Vector2[] path = poly.GetPath(0);
        float maxX = float.NegativeInfinity;
        float minX = float.PositiveInfinity;
        for (int i = 0; i < path.Length; i++)
        {
            maxX = Mathf.Max(maxX, path[i].x);
            minX = Mathf.Min(minX, path[i].x);
        }

        return maxX;
    }

    /// <summary>Moves every vertex on the "right column" to <paramref name="newRightX"/> (local space).</summary>
    static void ExpandColliderRightEdge(PolygonCollider2D poly, float newRightX)
    {
        Vector2[] path = poly.GetPath(0);
        float maxX = float.NegativeInfinity;
        float minX = float.PositiveInfinity;
        for (int i = 0; i < path.Length; i++)
        {
            maxX = Mathf.Max(maxX, path[i].x);
            minX = Mathf.Min(minX, path[i].x);
        }

        float span = maxX - minX;
        float slop = Mathf.Max(0.5f, span * 0.02f);
        float cutoff = maxX - slop;

        for (int i = 0; i < path.Length; i++)
        {
            if (path[i].x >= cutoff)
                path[i] = new Vector2(newRightX, path[i].y);
        }

        poly.SetPath(0, path);
    }
}
