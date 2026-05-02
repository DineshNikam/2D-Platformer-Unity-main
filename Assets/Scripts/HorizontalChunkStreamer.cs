using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a horizontal strip of platform chunks under the player by recycling instances.
/// Only the X axis is streamed; Y/Z stay at <see cref="levelPosition"/>.
/// Assign your chunk prefabs (each must have a <see cref="PlatformChunk"/> on the root).
/// </summary>
public class HorizontalChunkStreamer : MonoBehaviour
{
    [SerializeField] Transform player;

    [Tooltip("Chunk prefabs to cycle through (order used on first layout).")]
    [SerializeField] GameObject[] chunkPrefabs;

    [Tooltip("How many chunk instances stay loaded at once.")]
    [SerializeField] int activeChunkCount = 4;

    [Tooltip("World X where the first chunk's left edge is placed on start.")]
    [SerializeField] float worldStartLeftX = 0f;

    [Tooltip("Locked Y/Z for every chunk (horizontal runner — no vertical streaming).")]
    [SerializeField] Vector2 levelPositionYZ;

    [Tooltip("When a chunk's right edge is this far left of the player, jump it to the far right.")]
    [SerializeField] float recycleBehindDistance = 28f;

    [Tooltip("When a chunk's left edge is this far right of the player, jump it to the far left (walking back).")]
    [SerializeField] float recycleAheadDistance = 28f;

    readonly List<PlatformChunk> _chunks = new List<PlatformChunk>();

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("HorizontalChunkStreamer: assign the player Transform.", this);
            enabled = false;
            return;
        }

        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError("HorizontalChunkStreamer: assign at least one chunk prefab.", this);
            enabled = false;
            return;
        }

        if (activeChunkCount < 2)
        {
            Debug.LogError("HorizontalChunkStreamer: activeChunkCount must be at least 2.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < activeChunkCount; i++)
        {
            GameObject prefab = chunkPrefabs[i % chunkPrefabs.Length];
            GameObject instance = Instantiate(prefab, transform);
            PlatformChunk pc = instance.GetComponent<PlatformChunk>();
            if (pc == null)
            {
                Debug.LogError($"Prefab '{prefab.name}' needs a PlatformChunk on its root.", prefab);
                Destroy(instance);
                enabled = false;
                return;
            }

            _chunks.Add(pc);
        }

        LayoutAllFromLeft(worldStartLeftX);
    }

    void LateUpdate()
    {
        if (player == null || _chunks.Count < 2)
            return;

        SortByX();

        Vector3 p = player.position;

        // Player moved right: recycle chunks that fell too far behind on the left.
        while (_chunks[0].GetRightX() < p.x - recycleBehindDistance)
        {
            PlatformChunk left = _chunks[0];
            PlatformChunk right = _chunks[_chunks.Count - 1];
            float newCenterX = right.GetRightX() + left.Width * 0.5f;
            SetChunkWorldCenter(left, newCenterX);
            SortByX();
        }

        // Player moved left: recycle chunks that are too far ahead on the right.
        while (_chunks[_chunks.Count - 1].GetLeftX() > p.x + recycleAheadDistance)
        {
            PlatformChunk right = _chunks[_chunks.Count - 1];
            PlatformChunk left = _chunks[0];
            float newCenterX = left.GetLeftX() - right.Width * 0.5f;
            SetChunkWorldCenter(right, newCenterX);
            SortByX();
        }
    }

    void LayoutAllFromLeft(float leftX)
    {
        float cursor = leftX;
        foreach (PlatformChunk pc in _chunks)
        {
            float w = pc.Width;
            float cx = cursor + w * 0.5f;
            SetChunkWorldCenter(pc, cx);
            cursor += w;
        }
    }

    void SetChunkWorldCenter(PlatformChunk pc, float centerX)
    {
        pc.SetHorizontalCenter(centerX);
        Vector3 pos = pc.transform.position;
        pos.y = levelPositionYZ.x;
        pos.z = levelPositionYZ.y;
        pc.transform.position = pos;
    }

    void SortByX()
    {
        _chunks.Sort((a, b) => a.GetLeftX().CompareTo(b.GetLeftX()));
    }
}
