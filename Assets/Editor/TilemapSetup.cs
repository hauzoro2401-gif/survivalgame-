using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Bước 6 - TilemapSetup: Tự động tạo và vẽ map cơ bản 50x30.
/// Menu: Tools > CAVE RIFT > 4. Generate Map
/// </summary>
public class TilemapSetup : EditorWindow
{
    [MenuItem("Tools/CAVE RIFT/4. Generate Map")]
    public static void GenerateMap()
    {
        // 1. Tìm hoặc tạo Grid
        Grid grid = FindAnyObjectByType<Grid>();
        if (grid == null)
        {
            GameObject gridObj = new GameObject("Grid");
            grid = gridObj.AddComponent<Grid>();
        }

        // 2. Tạo các lớp Tilemap
        Tilemap groundMap = CreateTilemap(grid.transform, "Ground", 0, "Default");
        Tilemap decorMap = CreateTilemap(grid.transform, "Decoration", 1, "Default");
        Tilemap collisionMap = CreateTilemap(grid.transform, "Collision", 2, "Default");

        // Cấu hình Collider cho lớp Collision
        if (collisionMap.GetComponent<TilemapCollider2D>() == null)
        {
            TilemapCollider2D col = collisionMap.gameObject.AddComponent<TilemapCollider2D>();
            CompositeCollider2D comp = collisionMap.gameObject.AddComponent<CompositeCollider2D>();
            Rigidbody2D rb = collisionMap.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            col.usedByComposite = true;
        }

        // 3. Load Tile Sprites
        Sprite grassSprite = LoadSpriteByName("grass_tile");
        Sprite rockSprite = LoadSpriteByName("rock_01"); // Dùng sprite đá làm tường
        Sprite dirtSprite = LoadSpriteByName("dirt_tile");
        
        if (grassSprite == null || rockSprite == null)
        {
            Debug.LogError("[TilemapSetup] Không tìm thấy sprite grass_tile hoặc rock_01. Hãy chắc chắn đã chạy Tạo Placeholder Sprites.");
            return;
        }

        // Tạo Tile assets
        Tile grassTile = CreateTileAsset(grassSprite);
        Tile rockTile = CreateTileAsset(rockSprite);
        Tile dirtTile = CreateTileAsset(dirtSprite);

        // 4. Vẽ Map 50x30
        int width = 50;
        int height = 30;
        int startX = -width / 2;
        int startY = -height / 2;

        groundMap.ClearAllTiles();
        decorMap.ClearAllTiles();
        collisionMap.ClearAllTiles();

        for (int x = startX; x <= startX + width; x++)
        {
            for (int y = startY; y <= startY + height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                // Vẽ nền
                groundMap.SetTile(pos, Random.value > 0.1f ? grassTile : dirtTile);

                // Vẽ viền (tường)
                if (x == startX || x == startX + width || y == startY || y == startY + height)
                {
                    collisionMap.SetTile(pos, rockTile);
                }
                else
                {
                    // Random một vài chướng ngại vật nhỏ bên trong
                    if (Random.value > 0.98f)
                    {
                        collisionMap.SetTile(pos, rockTile);
                    }
                }
            }
        }

        Debug.Log("✅ Đã tạo xong Tilemap cơ bản 50x30 cho Vực Thẳm!");
    }

    private static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, string sortingLayer)
    {
        Transform child = parent.Find(name);
        GameObject mapObj;
        if (child != null)
        {
            mapObj = child.gameObject;
        }
        else
        {
            mapObj = new GameObject(name);
            mapObj.transform.SetParent(parent);
        }

        Tilemap tilemap = mapObj.GetComponent<Tilemap>();
        if (tilemap == null) tilemap = mapObj.AddComponent<Tilemap>();

        TilemapRenderer tr = mapObj.GetComponent<TilemapRenderer>();
        if (tr == null) tr = mapObj.AddComponent<TilemapRenderer>();

        tr.sortingOrder = sortingOrder;
        tr.sortingLayerName = sortingLayer;

        return tilemap;
    }

    private static Sprite LoadSpriteByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    private static Tile CreateTileAsset(Sprite sprite)
    {
        if (sprite == null) return null;

        // Kiểm tra xem tile asset đã tồn tại chưa
        string path = $"Assets/Sprites/Environment/Tiles/{sprite.name}_Tile.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);

        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, path);
        }
        else
        {
            tile.sprite = sprite;
            EditorUtility.SetDirty(tile);
        }

        return tile;
    }
}
