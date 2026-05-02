using UnityEngine;

/// <summary>
/// Infinite horizontal parallax for one sprite: the layer root tracks the camera on X (and optional Y)
/// so it stays on-screen, while two tiled copies slide with a repeating offset so the texture wraps seamlessly.
/// Tune <see cref="horizontalScrollFactor"/> per layer — lower values read as farther / slower drift.
/// </summary>
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public class ParallaxSeamlessLayer : MonoBehaviour
{
    [SerializeField] Transform cameraTransform;
    [SerializeField] Sprite layerSprite;

    [Tooltip("How much this layer’s texture slides along X per unit the camera moves. 0 = locked to camera with no extra drift. Small positive values stack as depth (e.g. 0.04 far, 0.12 near). Negative reverses scroll.")]
    [SerializeField] float horizontalScrollFactor = 0.08f;

    [Tooltip("Layer Y offset from camera Y: finalY = anchorY + (camera.y * this). Usually 0 for a side-scroller.")]
    [SerializeField] float verticalScrollFactor = 0f;

    [Tooltip("Applied to both tile renderers as local scale (X,Y). Use this to fill the orthographic view.")]
    [SerializeField] Vector2 spriteWorldScale = Vector2.one;

    [Tooltip("2D renderer order — higher draws in front.")]
    [SerializeField] int sortingOrder = -100;

    [Tooltip("Leave empty to use each renderer’s current sorting layer.")]
    [SerializeField] string sortingLayerName = "";

    [Tooltip("Added to this object’s initial Y (set in the scene) to form the fixed anchor.")]
    [SerializeField] float anchorYOffset = 0f;

    [Tooltip("World Z written every frame for this root (typical 2D: 0).")]
    [SerializeField] float worldZ = 0f;

    Transform _cam;
    SpriteRenderer _tileA;
    SpriteRenderer _tileB;
    float _tileWidthWorld;
    float _anchorY;

    const string TileAName = "ParallaxTileA";
    const string TileBName = "ParallaxTileB";

    void Awake()
    {
        _cam = cameraTransform != null ? cameraTransform : Camera.main != null ? Camera.main.transform : null;
        _anchorY = transform.position.y + anchorYOffset;
        if (layerSprite == null)
            return;

        EnsureTileRenderers();
        ApplySpriteSortingAndScale();
    }

    void LateUpdate()
    {
        if (_cam == null || layerSprite == null || _tileA == null || _tileB == null)
            return;

        if (_tileWidthWorld < 0.0001f)
            RefreshTileWidth();

        Vector3 c = _cam.position;
        transform.position = new Vector3(c.x, _anchorY + c.y * verticalScrollFactor, worldZ);

        float scroll = c.x * horizontalScrollFactor;
        float offset = PositiveMod(scroll, _tileWidthWorld);
        _tileA.transform.localPosition = new Vector3(-offset, 0f, 0f);
        _tileB.transform.localPosition = new Vector3(-offset + _tileWidthWorld, 0f, 0f);
    }

    void EnsureTileRenderers()
    {
        _tileA = GetOrCreateTile(TileAName);
        _tileB = GetOrCreateTile(TileBName);
        _tileA.sprite = layerSprite;
        _tileB.sprite = layerSprite;
        RefreshTileWidth();
    }

    SpriteRenderer GetOrCreateTile(string objectName)
    {
        Transform child = transform.Find(objectName);
        if (child == null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        var sr = child.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = child.gameObject.AddComponent<SpriteRenderer>();
        return sr;
    }

    void ApplySpriteSortingAndScale()
    {
        if (_tileA == null || _tileB == null)
            return;

        var s = new Vector3(spriteWorldScale.x, spriteWorldScale.y, 1f);
        _tileA.transform.localScale = s;
        _tileB.transform.localScale = s;

        _tileA.sortingOrder = sortingOrder;
        _tileB.sortingOrder = sortingOrder;

        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            _tileA.sortingLayerName = sortingLayerName;
            _tileB.sortingLayerName = sortingLayerName;
        }

        RefreshTileWidth();
    }

    void RefreshTileWidth()
    {
        if (_tileA == null)
            return;
        _tileWidthWorld = Mathf.Max(_tileA.bounds.size.x, 0.0001f);
    }

    static float PositiveMod(float a, float b)
    {
        return ((a % b) + b) % b;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (layerSprite == null)
            return;

        EnsureTileRenderers();
        ApplySpriteSortingAndScale();
    }
#endif
}
