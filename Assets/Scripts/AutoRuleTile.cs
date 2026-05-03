using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New AutoRuleTile", menuName = "Tiles/AutoRuleTile")]
public class AutoRuleTile : Tile
{
    [Header("Tiles")]
    public Tile top_left;
    public Tile top_mid;
    public Tile top_right;
    public Tile mid_left;
    public Tile mid_mid;
    public Tile mid_right;
    public Tile bot_left;
    public Tile bot_mid; // Often tile 5 or similar if not specified
    public Tile bot_right;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                Vector3Int neighborPos = new Vector3Int(position.x + x, position.y + y, position.z);
                if (HasTile(tilemap, neighborPos))
                {
                    tilemap.RefreshTile(neighborPos);
                }
            }
        }
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);

        bool up = HasTile(tilemap, position + Vector3Int.up);
        bool down = HasTile(tilemap, position + Vector3Int.down);
        bool left = HasTile(tilemap, position + Vector3Int.left);
        bool right = HasTile(tilemap, position + Vector3Int.right);

        // Simple 4-neighbor rule logic for the tiles requested
        if (!up && !left && right && down) tileData.sprite = top_left?.sprite;
        else if (!up && left && right && down) tileData.sprite = top_mid?.sprite;
        else if (!up && left && !right && down) tileData.sprite = top_right?.sprite;
        else if (up && !left && right && down) tileData.sprite = mid_left?.sprite;
        else if (up && left && right && down) tileData.sprite = mid_mid?.sprite;
        else if (up && left && !right && down) tileData.sprite = mid_right?.sprite;
        else if (up && !left && right && !down) tileData.sprite = bot_left?.sprite;
        else if (up && left && !right && !down) tileData.sprite = bot_right?.sprite;
        else if (up && left && right && !down) tileData.sprite = bot_mid?.sprite ?? mid_mid?.sprite;
        else tileData.sprite = mid_mid?.sprite;
    }

    private bool HasTile(ITilemap tilemap, Vector3Int position)
    {
        return tilemap.GetTile(position) == this;
    }
}
