using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Horizontal strip: optional <see cref="startChunkInScene"/> lives in the level and is never spawned or moved.
/// <see cref="repeatingChunkPrefabs"/> instances recycle to the right/left of the player.
/// </summary>
public class HorizontalChunkStreamer : MonoBehaviour
{
    [SerializeField] Transform player;

    [Tooltip("Already in the scene (e.g. StartChunk). Leave empty if the run starts only from prefabs.")]
    [SerializeField] PlatformChunk startChunkInScene;

    [Tooltip("Cycle these for the moving pool (each root needs a PlatformChunk).")]
    [FormerlySerializedAs("chunkPrefabs")]
    [SerializeField] GameObject[] repeatingChunkPrefabs;

    [Tooltip("How many repeating chunk instances (not counting the scene start chunk).")]
    [SerializeField] int activeChunkCount = 4;

    [Tooltip("Only used when Start Chunk In Scene is empty: left edge of the first pool segment.")]
    [SerializeField] float worldStartLeftX = 0f;

    [Tooltip("X is unused at runtime. Y sets world Z for pool instances. Pool chunks always use world Y = 0.")]
    [SerializeField] Vector2 levelPositionYZ;

    [SerializeField] float recycleBehindDistance = 28f;

    [SerializeField] float recycleAheadDistance = 28f;

    readonly List<PlatformChunk> _pool = new List<PlatformChunk>();

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("HorizontalChunkStreamer: assign the player Transform.", this);
            enabled = false;
            return;
        }

        if (repeatingChunkPrefabs == null || repeatingChunkPrefabs.Length == 0)
        {
            Debug.LogError("HorizontalChunkStreamer: assign at least one repeating chunk prefab.", this);
            enabled = false;
            return;
        }

        if (activeChunkCount < 2)
        {
            Debug.LogError("HorizontalChunkStreamer: activeChunkCount must be at least 2.", this);
            enabled = false;
            return;
        }

        _pool.Clear();

        if (startChunkInScene != null)
            startChunkInScene.ResolveWidth();

        for (int i = 0; i < activeChunkCount; i++)
        {
            GameObject prefab = repeatingChunkPrefabs[i % repeatingChunkPrefabs.Length];
            GameObject instance = Instantiate(prefab, transform);
            PlatformChunk pc = instance.GetComponent<PlatformChunk>();
            if (pc == null)
            {
                Debug.LogError($"Prefab '{prefab.name}' needs a PlatformChunk on its root.", prefab);
                Destroy(instance);
                enabled = false;
                return;
            }

            pc.ResolveWidth();
            _pool.Add(pc);
        }

        LayoutPoolOnly();
    }

    /// <summary>Rightmost world X of the scene start chunk (if any) and all pool chunks.</summary>
    public float GetRightmostContentEdgeX()
    {
        float r = float.NegativeInfinity;
        if (startChunkInScene != null)
        {
            startChunkInScene.ResolveWidth();
            r = Mathf.Max(r, startChunkInScene.GetRightX());
        }

        foreach (PlatformChunk pc in _pool)
            r = Mathf.Max(r, pc.GetRightX());

        if (float.IsNegativeInfinity(r))
            return worldStartLeftX;
        return r;
    }

    void LateUpdate()
    {
        if (player == null || _pool.Count < 2)
            return;

        Vector3 p = player.position;
        const int maxRecycleSteps = 64;
        const float centerEps = 0.001f;

        // Recycle pool segments that fell far behind the player (snap to the right of the world).
        for (int step = 0; step < maxRecycleSteps; step++)
        {
            PlatformChunk leftPool = GetLeftmostPool();
            if (leftPool == null)
                break;
            if (leftPool.GetRightX() >= p.x - recycleBehindDistance)
                break;

            // Exclude the chunk being moved — otherwise when it is the furthest-right pool piece,
            // rightEdge includes its own right edge and it gets stacked on itself (overlap).
            float anchorRight = GetRightmostEdgeOverallExcluding(leftPool);
            float targetCenter = anchorRight + leftPool.Width * 0.5f;
            if (Mathf.Abs(targetCenter - leftPool.GetCenterX()) < centerEps)
                break;

            SetChunkWorldCenter(leftPool, targetCenter);
        }

        // Recycle segments that are too far ahead when the player moves left (can't place before scene start).
        bool pastStartStrip;
        if (startChunkInScene != null)
        {
            startChunkInScene.ResolveWidth();
            float startRight = startChunkInScene.GetRightX();
            pastStartStrip = p.x >= startRight - recycleAheadDistance;
        }
        else
            pastStartStrip = p.x >= worldStartLeftX - recycleAheadDistance;

        PlatformChunk gateLeftmost = GetLeftmostPool();
        float poolLeadX = gateLeftmost != null ? gateLeftmost.GetLeftX() : float.NegativeInfinity;
        bool playerReachedPool = gateLeftmost != null && p.x >= poolLeadX;
        bool allowRecycleAhead = pastStartStrip && playerReachedPool;

        if (!allowRecycleAhead)
            return;

        // When the leftmost pool is flush with the start chunk, moving the rightmost segment would only
        // clamp to the same X and stack — skip the whole ahead-recycle block for this layout.
        bool recycleAheadBlockedByStartFlush = false;
        if (startChunkInScene != null)
        {
            startChunkInScene.ResolveWidth();
            float sr = startChunkInScene.GetRightX();
            PlatformChunk lm = GetLeftmostPool();
            PlatformChunk rm = GetRightmostPool();
            if (lm != null && rm != null && lm != rm && Mathf.Abs(lm.GetLeftX() - sr) < 0.05f)
            {
                float rawCx = lm.GetLeftX() - rm.Width * 0.5f;
                float minCx = sr + rm.Width * 0.5f;
                if (rawCx < minCx - 0.01f)
                    recycleAheadBlockedByStartFlush = true;
            }
        }

        if (recycleAheadBlockedByStartFlush)
            return;

        for (int step = 0; step < maxRecycleSteps; step++)
        {
            PlatformChunk rightPool = GetRightmostPool();
            if (rightPool == null)
                break;
            if (rightPool.GetLeftX() <= p.x + recycleAheadDistance)
                break;

            PlatformChunk leftPool = GetLeftmostPool();
            if (leftPool == null)
                break;
            if (leftPool == rightPool)
                break;

            float leftEdge = leftPool.GetLeftX();
            float rawCenterX = leftEdge - rightPool.Width * 0.5f;
            float newCenterX = rawCenterX;

            if (startChunkInScene != null)
            {
                startChunkInScene.ResolveWidth();
                float sr = startChunkInScene.GetRightX();
                float minCenter = sr + rightPool.Width * 0.5f;
                if (rawCenterX < minCenter - 0.01f && Mathf.Abs(leftEdge - sr) < 0.05f)
                    break;

                if (rawCenterX < minCenter)
                    newCenterX = minCenter;
            }

            if (Mathf.Abs(newCenterX - rightPool.GetCenterX()) < centerEps)
                break;

            SetChunkWorldCenter(rightPool, newCenterX);
        }
    }

    /// <summary>First pool chunk starts at the scene start's right edge; order is never before that.</summary>
    void LayoutPoolOnly()
    {
        float cursor;
        if (startChunkInScene != null)
        {
            startChunkInScene.ResolveWidth();
            cursor = startChunkInScene.GetRightX();
        }
        else
            cursor = worldStartLeftX;

        foreach (PlatformChunk pc in _pool)
        {
            float w = pc.Width;
            float cx = cursor + w * 0.5f;
            SetChunkWorldCenter(pc, cx);
            cursor += w;
        }
    }

    float GetRightmostEdgeOverall()
    {
        float r = float.NegativeInfinity;
        if (startChunkInScene != null)
            r = Mathf.Max(r, startChunkInScene.GetRightX());
        foreach (PlatformChunk pc in _pool)
            r = Mathf.Max(r, pc.GetRightX());
        return r;
    }

    float GetRightmostEdgeOverallExcluding(PlatformChunk exclude)
    {
        float r = float.NegativeInfinity;
        if (startChunkInScene != null)
            r = Mathf.Max(r, startChunkInScene.GetRightX());
        foreach (PlatformChunk pc in _pool)
        {
            if (pc == exclude)
                continue;
            r = Mathf.Max(r, pc.GetRightX());
        }

        if (float.IsNegativeInfinity(r))
            r = worldStartLeftX;
        return r;
    }

    PlatformChunk GetLeftmostPool()
    {
        PlatformChunk best = null;
        foreach (PlatformChunk pc in _pool)
        {
            if (best == null || pc.GetLeftX() < best.GetLeftX())
                best = pc;
        }

        return best;
    }

    PlatformChunk GetRightmostPool()
    {
        PlatformChunk best = null;
        foreach (PlatformChunk pc in _pool)
        {
            if (best == null || pc.GetRightX() > best.GetRightX())
                best = pc;
        }

        return best;
    }

    void SetChunkWorldCenter(PlatformChunk pc, float centerX)
    {
        pc.SetHorizontalCenter(centerX);
        Vector3 pos = pc.transform.position;
        pos.y = 0f;
        pos.z = levelPositionYZ.y;
        pc.transform.position = pos;
    }
}
