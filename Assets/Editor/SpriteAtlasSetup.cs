using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Bước 3 - SpriteAtlasSetup: Tự động cấu hình toàn bộ sprite.
/// Menu: Tools > CAVE RIFT > 1. Setup Sprites
/// </summary>
public class SpriteAtlasSetup : EditorWindow
{
    [MenuItem("Tools/CAVE RIFT/1. Setup Sprites")]
    public static void SetupSprites()
    {
        string[] spriteGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });
        int count = 0;

        foreach (string guid in spriteGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                bool isSheet = path.Contains("_sheet");

                // Cài đặt cơ bản cho Pixel Art
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 512;
                
                if (isSheet)
                {
                    importer.spriteImportMode = SpriteImportMode.Multiple;
                }
                else
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                }

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                // Slice spritesheet nếu là Multiple
                if (isSheet)
                {
                    SliceSpriteSheet(path, 16, 16);
                }

                count++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Đã setup {count} sprites cho Pixel Art!");
    }

    private static void SliceSpriteSheet(string path, int sliceWidth, int sliceHeight)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        string assetPath = AssetDatabase.GetAssetPath(tex);
        TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null || ti.spriteImportMode != SpriteImportMode.Multiple) return;

        int cols = tex.width / sliceWidth;
        int rows = tex.height / sliceHeight;
        
        List<SpriteMetaData> metaDataList = new List<SpriteMetaData>();

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                SpriteMetaData smd = new SpriteMetaData();
                smd.pivot = new Vector2(0.5f, 0.5f);
                smd.alignment = 9; // Custom pivot or Center
                smd.name = $"{tex.name}_{r}_{c}";
                smd.rect = new Rect(c * sliceWidth, (rows - 1 - r) * sliceHeight, sliceWidth, sliceHeight);
                metaDataList.Add(smd);
            }
        }

        ti.spritesheet = metaDataList.ToArray();
        EditorUtility.SetDirty(ti);
        ti.SaveAndReimport();
    }
}
