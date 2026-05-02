using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

    static bool _agentLoggedRecycleAheadGate;
    static bool _agentLoggedRecycleAheadGatePool;
    static bool _agentLoggedRecycleAheadBlockedFlush;

    // #region agent log
    const string AgentSessionId = "fc3014";
    const string AgentLogRelativePath = "debug-fc3014.log";

    static string AgentSanName(string s) => (s ?? "").Replace("\"", "'");

    static string AgentLogPath()
    {
        try
        {
            DirectoryInfo parent = Directory.GetParent(Application.dataPath);
            if (parent != null)
                return Path.Combine(parent.FullName, AgentLogRelativePath);
        }
        catch
        {
            // fallback below
        }

        return Path.Combine(Application.dataPath, "..", AgentLogRelativePath);
    }

    static void AgentNdjson(string hypothesisId, string location, string message, string dataJsonObject)
    {
        try
        {
            long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var sb = new StringBuilder(256);
            sb.Append("{\"sessionId\":\"").Append(AgentSessionId)
                .Append("\",\"hypothesisId\":\"").Append(hypothesisId)
                .Append("\",\"location\":\"").Append(location)
                .Append("\",\"message\":\"").Append(message.Replace("\\", "\\\\").Replace("\"", "\\\""))
                .Append("\",\"data\":").Append(dataJsonObject)
                .Append(",\"timestamp\":").Append(ts).Append("}\n");
            File.AppendAllText(AgentLogPath(), sb.ToString());
        }
        catch
        {
            // ignore logging failures in build/player
        }
    }

    string AgentPoolJson()
    {
        var sb = new StringBuilder();
        sb.Append("[");
        for (int i = 0; i < _pool.Count; i++)
        {
            if (i > 0) sb.Append(',');
            PlatformChunk pc = _pool[i];
            sb.Append("{\"n\":\"").Append(AgentSanName(pc.name))
                .Append("\",\"id\":").Append(pc.GetInstanceID())
                .Append(",\"L\":").Append(pc.GetLeftX().ToString("F4"))
                .Append(",\"R\":").Append(pc.GetRightX().ToString("F4"))
                .Append(",\"W\":").Append(pc.Width.ToString("F4"))
                .Append(",\"cx\":").Append(pc.GetCenterX().ToString("F4"))
                .Append(",\"tx\":").Append(pc.transform.position.x.ToString("F4"))
                .Append('}');
        }

        sb.Append(']');
        return sb.ToString();
    }

    void AgentLogOverlapPairs(string hypothesisId, string location)
    {
        if (_pool.Count < 2)
            return;

        for (int i = 0; i < _pool.Count; i++)
        {
            for (int j = i + 1; j < _pool.Count; j++)
            {
                PlatformChunk a = _pool[i];
                PlatformChunk b = _pool[j];
                float aL = a.GetLeftX(), aR = a.GetRightX();
                float bL = b.GetLeftX(), bR = b.GetRightX();
                float overlap = Mathf.Min(aR, bR) - Mathf.Max(aL, bL);
                if (overlap > 0.02f)
                {
                    string an = AgentSanName(a.name);
                    string bn = AgentSanName(b.name);
                    string data = "{\"pair\":\"" + an + "\"/\"" + bn + "\",\"overlap\":" + overlap.ToString("F4") +
                                  ",\"aL\":" + aL.ToString("F4") + ",\"aR\":" + aR.ToString("F4") +
                                  ",\"bL\":" + bL.ToString("F4") + ",\"bR\":" + bR.ToString("F4") + "}";
                    AgentNdjson(hypothesisId, location, "interval_overlap", data);
                }
            }
        }
    }

    // #endregion

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
        // #region agent log
        AgentNdjson("H2", "HorizontalChunkStreamer.cs:Start", "after_initial_layout",
            "{\"playerX\":" + player.position.x.ToString("F4") + ",\"pool\":" + AgentPoolJson() + "}");
        // #endregion
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
            {
                // #region agent log
                AgentNdjson("H4", "HorizontalChunkStreamer.cs:recycle_behind", "break_eps_no_move",
                    "{\"step\":" + step + ",\"left\":\"" + AgentSanName(leftPool.name) + "\",\"anchorRight\":" + anchorRight.ToString("F4") + ",\"targetCx\":" + targetCenter.ToString("F4") + ",\"curCx\":" + leftPool.GetCenterX().ToString("F4") + ",\"playerX\":" + p.x.ToString("F4") + "}");
                // #endregion
                break;
            }

            // #region agent log
            AgentNdjson("H1", "HorizontalChunkStreamer.cs:recycle_behind", "before_move",
                "{\"step\":" + step + ",\"left\":\"" + AgentSanName(leftPool.name) + "\",\"anchorRight\":" + anchorRight.ToString("F4") + ",\"targetCx\":" + targetCenter.ToString("F4") + ",\"prevL\":" + leftPool.GetLeftX().ToString("F4") + ",\"prevR\":" + leftPool.GetRightX().ToString("F4") + ",\"W\":" + leftPool.Width.ToString("F4") + ",\"playerX\":" + p.x.ToString("F4") + ",\"worldStartLeftX\":" + worldStartLeftX.ToString("F4") + "}");
            // #endregion
            SetChunkWorldCenter(leftPool, targetCenter, "recycle_behind", step);
        }

        // Recycle segments that are too far ahead when the player moves left (can't place before scene start).
        // If the player is still west of where pool content begins, every chunk reads as "ahead" of spawn X and
        // the minCenter clamp stacks all segments on one position (see debug-fc3014 recycle_ahead / interval_overlap).
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

        // #region agent log
        if (!pastStartStrip && !_agentLoggedRecycleAheadGate)
        {
            _agentLoggedRecycleAheadGate = true;
            string gateData;
            if (startChunkInScene != null)
            {
                startChunkInScene.ResolveWidth();
                gateData = "{\"playerX\":" + p.x.ToString("F4") + ",\"recycleAhead\":" +
                           recycleAheadDistance.ToString("F4") + ",\"startRight\":" +
                           startChunkInScene.GetRightX().ToString("F4") + ",\"allow\":false,\"reason\":\"west_of_start_margin\"}";
            }
            else
                gateData = "{\"playerX\":" + p.x.ToString("F4") + ",\"recycleAhead\":" +
                           recycleAheadDistance.ToString("F4") + ",\"worldStartLeftX\":" +
                           worldStartLeftX.ToString("F4") + ",\"allow\":false,\"reason\":\"west_of_start_margin\"}";

            AgentNdjson("H5", "HorizontalChunkStreamer.cs:recycle_ahead", "gate_skip_west", gateData);
        }
        if (!playerReachedPool && pastStartStrip && !_agentLoggedRecycleAheadGatePool)
        {
            _agentLoggedRecycleAheadGatePool = true;
            AgentNdjson("H5", "HorizontalChunkStreamer.cs:recycle_ahead", "gate_skip_before_pool_lead",
                "{\"playerX\":" + p.x.ToString("F4") + ",\"poolLeadX\":" + poolLeadX.ToString("F4") + ",\"allow\":false}");
        }
        // #endregion

        if (!allowRecycleAhead)
        {
            // #region agent log
            AgentOverlapPairsFrameEnd(p);
            // #endregion
            return;
        }

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
        {
            // #region agent log
            if (!_agentLoggedRecycleAheadBlockedFlush)
            {
                _agentLoggedRecycleAheadBlockedFlush = true;
                startChunkInScene.ResolveWidth();
                PlatformChunk lm = GetLeftmostPool();
                PlatformChunk rm = GetRightmostPool();
                string d = "{\"sr\":" + startChunkInScene.GetRightX().ToString("F4") +
                           ",\"lmL\":" + (lm != null ? lm.GetLeftX().ToString("F4") : "0") +
                           ",\"note\":\"recycle_ahead would only clamp-stack; skipped\"}";
                AgentNdjson("H6", "HorizontalChunkStreamer.cs:recycle_ahead", "skipped_start_flush_block", d);
            }
            // #endregion
            AgentOverlapPairsFrameEnd(p);
            return;
        }

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
                {
                    // #region agent log
                    if (Time.frameCount % 120 == 0)
                        AgentNdjson("H6", "HorizontalChunkStreamer.cs:recycle_ahead", "break_flush_clamp_would_stack",
                            "{\"step\":" + step + ",\"playerX\":" + p.x.ToString("F4") + ",\"leftEdge\":" + leftEdge.ToString("F4") + ",\"sr\":" + sr.ToString("F4") + ",\"rawCx\":" + rawCenterX.ToString("F4") + ",\"minCx\":" + minCenter.ToString("F4") + "}");
                    // #endregion
                    break;
                }

                if (rawCenterX < minCenter)
                    newCenterX = minCenter;
            }

            // Clamp keeps chunks after the start but can repeat the same position while the player is still
            // left of recycleAhead — without this check, LateUpdate would spin forever.
            if (Mathf.Abs(newCenterX - rightPool.GetCenterX()) < centerEps)
            {
                // #region agent log
                AgentNdjson("H4", "HorizontalChunkStreamer.cs:recycle_ahead", "break_eps_no_move",
                    "{\"step\":" + step + ",\"right\":\"" + AgentSanName(rightPool.name) + "\",\"newCx\":" + newCenterX.ToString("F4") + ",\"curCx\":" + rightPool.GetCenterX().ToString("F4") + ",\"playerX\":" + p.x.ToString("F4") + "}");
                // #endregion
                break;
            }

            // #region agent log
            AgentNdjson("H5", "HorizontalChunkStreamer.cs:recycle_ahead", "before_move",
                "{\"step\":" + step + ",\"left\":\"" + AgentSanName(leftPool.name) + "\",\"right\":\"" + AgentSanName(rightPool.name) + "\",\"leftEdge\":" + leftEdge.ToString("F4") + ",\"newCx\":" + newCenterX.ToString("F4") + ",\"prevL\":" + rightPool.GetLeftX().ToString("F4") + ",\"prevR\":" + rightPool.GetRightX().ToString("F4") + ",\"W\":" + rightPool.Width.ToString("F4") + ",\"playerX\":" + p.x.ToString("F4") + "}");
            // #endregion
            SetChunkWorldCenter(rightPool, newCenterX, "recycle_ahead", step);
        }

        // #region agent log
        AgentOverlapPairsFrameEnd(p);
        // #endregion
    }

    void AgentOverlapPairsFrameEnd(Vector3 _)
    {
        AgentLogOverlapPairs("H2", "HorizontalChunkStreamer.cs:LateUpdate_end");
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
            SetChunkWorldCenter(pc, cx, "layout", -1);
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

    void SetChunkWorldCenter(PlatformChunk pc, float centerX, string reason, int recycleStep)
    {
        pc.SetHorizontalCenter(centerX);
        Vector3 pos = pc.transform.position;
        pos.y = 0f;
        pos.z = levelPositionYZ.y;
        pc.transform.position = pos;

        // #region agent log
        string data = "{\"reason\":\"" + reason + "\",\"step\":" + recycleStep +
                      ",\"chunk\":\"" + AgentSanName(pc.name) + "\",\"centerX\":" + centerX.ToString("F4") +
                      ",\"L\":" + pc.GetLeftX().ToString("F4") + ",\"R\":" + pc.GetRightX().ToString("F4") +
                      ",\"W\":" + pc.Width.ToString("F4") + ",\"cx\":" + pc.GetCenterX().ToString("F4") +
                      ",\"tx\":" + pc.transform.position.x.ToString("F4") + "}";
        AgentNdjson("H3", "HorizontalChunkStreamer.cs:SetChunkWorldCenter", "after_set", data);
        // #endregion
    }
}
