using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// SpriteMapper - Tự động import và cấu hình sprites từ Pixel Adventure pack.
/// Menu: Tools > CAVE RIFT > Map Sprites
/// </summary>
public static class SpriteMapper
{
    private const string SPRITE_ROOT    = "Assets/Sprites";
    private const string PA_ROOT        = "Assets/Sprites/Pixel Adventure 1";
    private const string CHAR_DIR       = "Assets/Sprites/Characters";
    private const string ENEMY_DIR      = "Assets/Sprites/Enemies";
    private const string ENV_DIR        = "Assets/Sprites/Environment";
    private const string UI_DIR         = "Assets/Sprites/UI/Icons";
    private const string EFFECT_DIR     = "Assets/Sprites/Effects";

    [MenuItem("Tools/CAVE RIFT/Map Sprites")]
    public static void MapSprites()
    {
        int configured = 0;

        // 1. Tìm tất cả PNG trong Sprites/
        string[] allPngs = Directory.GetFiles(SPRITE_ROOT, "*.png", SearchOption.AllDirectories);
        Debug.Log($"[SpriteMapper] Tìm thấy {allPngs.Length} PNG trong {SPRITE_ROOT}");

        foreach (string absolutePath in allPngs)
        {
            // Chuyển sang đường dẫn Unity
            string assetPath = absolutePath.Replace("\\", "/");
            if (!assetPath.StartsWith("Assets/")) continue;

            string fileName    = Path.GetFileNameWithoutExtension(assetPath).ToLower();
            string fullPathLow = assetPath.ToLower();

            // 2. Áp dụng TextureImporter settings
            bool applied = ApplyTextureSettings(assetPath, IsSheet(fileName));
            if (applied) configured++;

            // 3. Log mapping
            if (IsCharacterSprite(fullPathLow))
                Debug.Log($"[SpriteMapper] 👤 Character: {Path.GetFileName(assetPath)}");
            else if (IsEnemySprite(fullPathLow))
                Debug.Log($"[SpriteMapper] 👾 Enemy: {Path.GetFileName(assetPath)}");
            else if (IsTerrainSprite(fileName))
                Debug.Log($"[SpriteMapper] 🗺 Terrain: {Path.GetFileName(assetPath)}");
            else if (IsItemSprite(fileName))
                Debug.Log($"[SpriteMapper] 🎒 Item: {Path.GetFileName(assetPath)}");
        }

        // 4. Scan Pixel Adventure 1 nếu có
        if (Directory.Exists(PA_ROOT))
        {
            configured += MapPixelAdventureSprites();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SpriteMapper] ✅ Đã cấu hình {configured} sprites!");
        EditorUtility.DisplayDialog(
            "CAVE RIFT — Map Sprites",
            $"Hoàn tất! Đã cấu hình {configured} sprites.\n\n" +
            "• Filter Mode: Point (No Filter)\n" +
            "• Compression: None\n" +
            "• Pixels Per Unit: 16\n\n" +
            "Nếu Pixel Adventure pack nằm ở Assets/Sprites/Pixel Adventure 1/,\n" +
            "các sprite đã được ánh xạ tự động.",
            "OK");
    }

    // ──────────────────────────────────────
    // Apply TextureImporter Settings
    // ──────────────────────────────────────
    private static bool ApplyTextureSettings(string assetPath, bool isSheet)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return false;

        bool changed = false;

        // Filter Mode: Point (pixel art)
        if (importer.filterMode != FilterMode.Point)
        { importer.filterMode = FilterMode.Point; changed = true; }

        // Compression: None
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }

        // Sprite type
        if (importer.textureType != TextureImporterType.Sprite)
        { importer.textureType = TextureImporterType.Sprite; changed = true; }

        // Pixels Per Unit: 16
        if (Mathf.Abs(importer.spritePixelsPerUnit - 16f) > 0.01f)
        { importer.spritePixelsPerUnit = 16f; changed = true; }

        // Sprite Mode: Multiple nếu là sheet
        if (isSheet && importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;

            // Auto slice 32x32
            var slices = new List<SpriteMetaData>();
            var tex    = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (tex != null)
            {
                int cols = Mathf.Max(1, tex.width  / 32);
                int rows = Mathf.Max(1, tex.height / 32);

                for (int row = rows - 1; row >= 0; row--)
                {
                    for (int col = 0; col < cols; col++)
                    {
                        var meta = new SpriteMetaData
                        {
                            name   = $"{Path.GetFileNameWithoutExtension(assetPath)}_{(rows-1-row)*cols + col}",
                            rect   = new Rect(col * 32, row * 32, 32, 32),
                            pivot  = new Vector2(0.5f, 0.5f),
                            alignment = (int)SpriteAlignment.Center
                        };
                        slices.Add(meta);
                    }
                }
                importer.spritesheet = slices.ToArray();
            }
            changed = true;
        }
        else if (!isSheet && importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        return changed;
    }

    // ──────────────────────────────────────
    // Pixel Adventure Sprite Mapping
    // ──────────────────────────────────────
    private static int MapPixelAdventureSprites()
    {
        int count = 0;
        string[] paPngs = Directory.GetFiles(PA_ROOT, "*.png", SearchOption.AllDirectories);

        foreach (string abs in paPngs)
        {
            string assetPath   = abs.Replace("\\", "/");
            string fileName    = Path.GetFileNameWithoutExtension(assetPath).ToLower();
            string parentDir   = Path.GetDirectoryName(assetPath).Replace("\\", "/").ToLower();

            bool applied = false;

            // Main Characters → Characters/
            if (parentDir.Contains("main characters") || fileName.Contains("main_character"))
            {
                applied = ApplyTextureSettings(assetPath, IsSheet(fileName));
                Debug.Log($"[SpriteMapper] ✦ PA MainCharacter: {Path.GetFileName(assetPath)}");
            }
            // Terrain / Tileset → Environment/
            else if (parentDir.Contains("terrain") || fileName.Contains("tileset") || parentDir.Contains("tile"))
            {
                applied = ApplyTextureSettings(assetPath, true);
                Debug.Log($"[SpriteMapper] ✦ PA Terrain: {Path.GetFileName(assetPath)}");
            }
            // Fruits / Items → UI/Icons
            else if (parentDir.Contains("fruit") || parentDir.Contains("item") || fileName.Contains("item"))
            {
                applied = ApplyTextureSettings(assetPath, IsSheet(fileName));
                Debug.Log($"[SpriteMapper] ✦ PA Item/Fruit: {Path.GetFileName(assetPath)}");
            }
            // Enemies
            else if (parentDir.Contains("enemy") || parentDir.Contains("enemies"))
            {
                applied = ApplyTextureSettings(assetPath, IsSheet(fileName));
                Debug.Log($"[SpriteMapper] ✦ PA Enemy: {Path.GetFileName(assetPath)}");
            }
            // Everything else - apply base settings
            else
            {
                applied = ApplyTextureSettings(assetPath, IsSheet(fileName));
            }

            if (applied) count++;
        }

        return count;
    }

    // ──────────────────────────────────────
    // Helper: Nhận biết loại sprite
    // ──────────────────────────────────────
    private static bool IsSheet(string fileName)
    {
        return fileName.Contains("sheet") || fileName.Contains("run") ||
               fileName.Contains("walk") || fileName.Contains("attack") ||
               fileName.Contains("idle") && false; // idle thường là single frame
    }

    private static bool IsCharacterSprite(string path)
    {
        return path.Contains("/characters/") ||
               path.Contains("minh") || path.Contains("linh") ||
               path.Contains("khoa") || path.Contains("trang");
    }

    private static bool IsEnemySprite(string path)
    {
        return path.Contains("/enemies/") ||
               path.Contains("gremvak") || path.Contains("bongdem");
    }

    private static bool IsTerrainSprite(string name)
    {
        return name.Contains("terrain") || name.Contains("tileset") ||
               name.Contains("ground") || name.Contains("cave") || name.Contains("tile");
    }

    private static bool IsItemSprite(string name)
    {
        return name.Contains("fruit") || name.Contains("item") ||
               name.Contains("gem") || name.Contains("crystal");
    }
}
