// VisualMapBuilder.cs – CAVE RIFT | Unity 6 | URP 2D
// Tools > CAVE RIFT > Build Visual Map
// Dựng map hang động đẹp: parallax BG, Light2D point lights,
// tilemap terrain, décor (cây/đá/bụi) từ Pixel Adventure sprites.

#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CaveRift.Editor
{
    public static class VisualMapBuilder
    {
        // ── Màu sắc chủ đạo hang động ──────────────────────────
        private static readonly Color COL_BG_DEEP   = new Color(0.04f, 0.03f, 0.08f); // tím đen
        private static readonly Color COL_BG_MID    = new Color(0.07f, 0.05f, 0.14f); // tím tối
        private static readonly Color COL_BG_NEAR   = new Color(0.10f, 0.08f, 0.20f); // tím vừa
        private static readonly Color COL_GROUND    = new Color(0.18f, 0.13f, 0.30f); // tím đất
        private static readonly Color COL_AMBIENT   = new Color(0.12f, 0.10f, 0.25f); // ambient đêm

        // ── Màu đèn point light ────────────────────────────────
        private static readonly Color COL_TORCH     = new Color(1.0f, 0.65f, 0.25f); // đuốc cam
        private static readonly Color COL_CRYSTAL   = new Color(0.4f, 0.8f, 1.0f);  // pha lê xanh
        private static readonly Color COL_PORTAL    = new Color(0.7f, 0.3f, 1.0f);  // cổng tím
        private static readonly Color COL_SAFE      = new Color(0.5f, 1.0f, 0.6f);  // vùng an toàn

        private const string PA_ROOT = "Assets/Sprites/Pixel Adventure 1/Free";

        // ══════════════════════════════════════════════════════
        //  MenuItem chính
        // ══════════════════════════════════════════════════════
        [MenuItem("Tools/CAVE RIFT/Build Visual Map")]
        public static void BuildVisualMap()
        {
            Debug.Log("[VisualMapBuilder] ═══ Bắt đầu dựng Map ═══");

            // Xóa map cũ nếu có
            var old = GameObject.Find("=== CAVE RIFT MAP ===");
            if (old != null)
            {
                GameObject.DestroyImmediate(old);
                Debug.Log("[VisualMapBuilder] Đã xóa map cũ.");
            }

            // Root container
            var root = new GameObject("=== CAVE RIFT MAP ===");

            // Pipeline
            BuildParallaxBackground(root.transform);
            BuildGround(root.transform);
            BuildDecorLayer(root.transform);
            BuildLightingSetup(root.transform);
            BuildCampZone(root.transform);
            BuildBorderWalls(root.transform);

            // Đánh dấu scene dirty để lưu
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[VisualMapBuilder] ✅ Map đã dựng xong!");
            EditorUtility.DisplayDialog("CAVE RIFT",
                "✅ Map đã được dựng!\n\nCấu trúc:\n" +
                "• 3 lớp Parallax Background\n" +
                "• Ground + Border walls\n" +
                "• Décor: cây/đá/tinh thể\n" +
                "• 12+ Point Lights (đuốc/pha lê)\n" +
                "• Vùng an toàn với ánh sáng xanh",
                "OK");
        }

        // ══════════════════════════════════════════════════════
        //  1. Parallax Background (3 lớp độ sâu)
        // ══════════════════════════════════════════════════════
        private static void BuildParallaxBackground(Transform root)
        {
            var bgRoot = new GameObject("--- Parallax BG ---").transform;
            bgRoot.SetParent(root);

            // Lấy sprites background từ PA1
            Sprite[] bgSprites = LoadSpritesFromFolder($"{PA_ROOT}/Background");

            // Layer 1: Xa nhất – màu đen tím thẫm
            CreateBGLayer(bgRoot, "BG_Deep",   COL_BG_DEEP,  -30, 0f,
                bgSprites.Length > 0 ? bgSprites[0] : null,
                new Vector3(60f, 40f, 1f));

            // Layer 2: Giữa – hang động mờ
            CreateBGLayer(bgRoot, "BG_Mid",    COL_BG_MID,   -20, 0f,
                bgSprites.Length > 1 ? bgSprites[1] : null,
                new Vector3(56f, 36f, 1f));

            // Layer 3: Gần – silhouette núi đá
            CreateBGLayer(bgRoot, "BG_Near",   COL_BG_NEAR,  -10, 0f,
                bgSprites.Length > 2 ? bgSprites[2] : null,
                new Vector3(52f, 32f, 1f));

            // Thêm component parallax tự cuộn
            var parallaxObj = bgRoot.gameObject;
            if (parallaxObj.GetComponent<ParallaxScroller>() == null)
                parallaxObj.AddComponent<ParallaxScroller>();

            Debug.Log("[VisualMapBuilder] 🌌 Parallax BG: 3 lớp");
        }

        private static void CreateBGLayer(Transform parent, string name,
            Color color, int sortOrder, float z, Sprite sprite, Vector3 scale)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(0, 0, z);
            obj.transform.localScale = scale;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : CreateSolidSprite(color);
            sr.color  = sprite != null ? color  : Color.white;
            sr.sortingLayerName = "Background";
            sr.sortingOrder     = sortOrder;
        }

        // ══════════════════════════════════════════════════════
        //  2. Ground + Sàn hang động
        // ══════════════════════════════════════════════════════
        private static void BuildGround(Transform root)
        {
            var gRoot = new GameObject("--- Ground ---").transform;
            gRoot.SetParent(root);

            // Terrain sprite từ PA1
            Sprite terrainSprite = LoadFirstSprite($"{PA_ROOT}/Terrain");

            // Sàn chính (nhiều tile ghép ngang)
            int tileCount = 35; // 35 tile * 2 unit = 70 unit width
            for (int i = 0; i < tileCount; i++)
            {
                float x = -34f + i * 2f;

                // Sàn bề mặt
                CreateTile(gRoot, $"Floor_{i}", new Vector2(x, -8f),
                    terrainSprite, COL_GROUND, 0, true);

                // Lớp đất dưới (tối hơn)
                for (int d = 1; d <= 3; d++)
                    CreateTile(gRoot, $"Dirt_{i}_{d}", new Vector2(x, -8f - d * 2f),
                        terrainSprite, COL_GROUND * (1f - d * 0.12f), -1 - d, false);
            }

            // Nền hang (platforms giữa không khí)
            CreatePlatform(gRoot, "Platform_L", new Vector2(-18f, -2f), 6, terrainSprite);
            CreatePlatform(gRoot, "Platform_C", new Vector2(0f,   2f),  8, terrainSprite);
            CreatePlatform(gRoot, "Platform_R", new Vector2(18f, -2f),  6, terrainSprite);

            // Trần hang
            for (int i = 0; i < 28; i++)
            {
                float x = -27f + i * 2f;
                CreateTile(gRoot, $"Ceil_{i}", new Vector2(x, 12f),
                    terrainSprite, COL_GROUND * 0.8f, 0, true);
            }

            Debug.Log("[VisualMapBuilder] 🟫 Ground: sàn + trần + 3 platform");
        }

        private static void CreatePlatform(Transform parent, string name,
            Vector2 center, int tiles, Sprite sprite)
        {
            var pObj = new GameObject(name).transform;
            pObj.SetParent(parent);
            for (int i = 0; i < tiles; i++)
            {
                float x = center.x - (tiles / 2f) * 2f + i * 2f;
                CreateTile(pObj, $"T_{i}", new Vector2(x, center.y),
                    sprite, COL_GROUND * 1.1f, 1, true);
            }
        }

        private static void CreateTile(Transform parent, string name,
            Vector2 pos, Sprite sprite, Color color, int order, bool hasCollider)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(pos.x, pos.y, 0f);
            obj.transform.localScale = Vector3.one * 2f;

            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : CreateSolidSprite(color);
            sr.color  = sprite != null ? color  : Color.white;
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = order;

            if (hasCollider)
            {
                var col = obj.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
            }
        }

        // ══════════════════════════════════════════════════════
        //  3. Décor Layer: tinh thể, stalactite, bụi cây
        // ══════════════════════════════════════════════════════
        private static void BuildDecorLayer(Transform root)
        {
            var dRoot = new GameObject("--- Décor ---").transform;
            dRoot.SetParent(root);

            Sprite[] fruits   = LoadSpritesFromFolder($"{PA_ROOT}/Items/Fruits");
            Sprite   terrain  = LoadFirstSprite($"{PA_ROOT}/Terrain");

            // Tinh thể phát sáng dọc tường trái/phải
            SpawnCrystals(dRoot, fruits, 16);

            // Bụi cây trang trí trên sàn
            SpawnGroundDecor(dRoot, terrain, 20);

            // Stalagmite trên trần (hình tam giác lật)
            SpawnStalactites(dRoot, terrain, 12);

            Debug.Log("[VisualMapBuilder] 💎 Décor: tinh thể + bụi + nhũ đá");
        }

        private static void SpawnCrystals(Transform parent, Sprite[] sprites, int count)
        {
            Color[] crystalColors = {
                new Color(0.5f, 0.8f, 1.0f, 0.9f),  // xanh lam
                new Color(0.8f, 0.5f, 1.0f, 0.9f),  // tím nhạt
                new Color(0.5f, 1.0f, 0.8f, 0.9f),  // xanh lá nhạt
                new Color(1.0f, 0.7f, 0.4f, 0.9f),  // cam ấm
            };

            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-28f, 28f);
                float y = Random.Range(-7.5f, 11f);
                // Không đặt trong vùng camp
                if (Mathf.Abs(x) < 6f && Mathf.Abs(y) < 5f) continue;

                var obj = new GameObject($"Crystal_{i}");
                obj.transform.SetParent(parent);
                obj.transform.position = new Vector3(x, y, 0f);

                float s = Random.Range(0.3f, 0.9f);
                obj.transform.localScale = new Vector3(
                    s * (Random.value > 0.5f ? 1 : -1), s, 1f);

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite       = sprites.Length > 0
                    ? sprites[Random.Range(0, sprites.Length)]
                    : CreateSolidSprite(crystalColors[i % 4]);
                sr.color        = crystalColors[i % 4];
                sr.sortingOrder = 5;

                // Thêm GlowPulse component
                obj.AddComponent<GlowPulse>();
            }
        }

        private static void SpawnGroundDecor(Transform parent, Sprite sprite, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-28f, 28f);
                var obj = new GameObject($"Decor_{i}");
                obj.transform.SetParent(parent);
                obj.transform.position = new Vector3(x, -7f, 0f);
                float s = Random.Range(0.4f, 0.8f);
                obj.transform.localScale = new Vector3(
                    s * (Random.value > 0.5f ? 1 : -1), s, 1f);

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite       = sprite != null ? sprite : CreateSolidSprite(COL_GROUND);
                sr.color        = COL_GROUND * 1.3f;
                sr.sortingOrder = 2;
            }
        }

        private static void SpawnStalactites(Transform parent, Sprite sprite, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = Random.Range(-26f, 26f);
                var obj = new GameObject($"Stalac_{i}");
                obj.transform.SetParent(parent);
                obj.transform.position = new Vector3(x, 11.5f, 0f);
                float s = Random.Range(0.3f, 0.7f);
                // Lật ngược để thành nhũ đá
                obj.transform.localScale = new Vector3(s, -s, 1f);

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite       = sprite != null ? sprite : CreateSolidSprite(COL_GROUND);
                sr.color        = COL_GROUND * 0.9f;
                sr.sortingOrder = 3;
            }
        }

        // ══════════════════════════════════════════════════════
        //  4. Lighting Setup: Global + Point Lights
        // ══════════════════════════════════════════════════════
        private static void BuildLightingSetup(Transform root)
        {
            var lRoot = new GameObject("--- Lighting ---").transform;
            lRoot.SetParent(root);

            // Global ambient (tối hang động)
            CreateGlobalLight(lRoot, COL_AMBIENT, 0.08f);

            // Đuốc dọc map (cam ấm)
            float[] torchX = { -24f, -16f, -8f, 0f, 8f, 16f, 24f };
            foreach (float x in torchX)
                CreatePointLight(lRoot, $"Torch_{x}", new Vector2(x, -5.5f),
                    COL_TORCH, 10f, 1.8f, isFlicker: true);

            // Tinh thể pha lê (xanh lam lạnh)
            float[] crystalX = { -20f, -4f, 12f, 22f };
            foreach (float x in crystalX)
                CreatePointLight(lRoot, $"Crystal_{x}",
                    new Vector2(x, Random.Range(-4f, 8f)),
                    COL_CRYSTAL, 8f, 1.2f, isFlicker: false);

            // Cổng/portal (tím huyền bí)
            CreatePointLight(lRoot, "Portal_L", new Vector2(-26f, 0f),
                COL_PORTAL, 12f, 2f, isFlicker: true);
            CreatePointLight(lRoot, "Portal_R", new Vector2(26f, 0f),
                COL_PORTAL, 12f, 2f, isFlicker: true);

            Debug.Log("[VisualMapBuilder] 💡 Lighting: 1 global + 13 point lights");
        }

        private static void CreateGlobalLight(Transform parent, Color color, float intensity)
        {
            var obj = new GameObject("GlobalLight2D");
            obj.transform.SetParent(parent);
            obj.transform.position = Vector3.zero;

            var light = obj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color     = color;
            light.intensity = intensity;
        }

        private static void CreatePointLight(Transform parent, string name,
            Vector2 pos, Color color, float radius, float intensity, bool isFlicker)
        {
            var obj = new GameObject($"Light_{name}");
            obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(pos.x, pos.y, 0f);

            var light = obj.AddComponent<Light2D>();
            light.lightType           = Light2D.LightType.Point;
            light.color               = color;
            light.intensity           = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.3f;
            light.falloffIntensity    = 1f;
            light.shadowsEnabled      = true;

            if (isFlicker)
                obj.AddComponent<LightFlicker>();
        }

        // ══════════════════════════════════════════════════════
        //  5. Camp Zone: vùng an toàn giữa map
        // ══════════════════════════════════════════════════════
        private static void BuildCampZone(Transform root)
        {
            var cRoot = new GameObject("--- Camp Zone ---").transform;
            cRoot.SetParent(root);

            // Ánh sáng an toàn xanh dịu
            CreatePointLight(cRoot, "CampLight", new Vector2(0f, 1f),
                COL_SAFE, 14f, 1.5f, isFlicker: false);

            // Đốm lửa trại
            var fireObj = new GameObject("CampFire");
            fireObj.transform.SetParent(cRoot);
            fireObj.transform.position = new Vector3(0f, -6.5f, 0f);
            fireObj.transform.localScale = Vector3.one * 0.6f;

            var fireSR = fireObj.AddComponent<SpriteRenderer>();
            fireSR.sprite       = CreateSolidSprite(COL_TORCH);
            fireSR.color        = COL_TORCH;
            fireSR.sortingOrder = 10;

            // Đèn đốm lửa (cam mạnh)
            CreatePointLight(cRoot, "CampFire", new Vector2(0f, -6f),
                COL_TORCH, 6f, 3f, isFlicker: true);

            // Nền camp (vòng tròn tối nhẹ)
            var campBase = new GameObject("CampBase");
            campBase.transform.SetParent(cRoot);
            campBase.transform.position = new Vector3(0f, -5f, 0f);
            campBase.transform.localScale = new Vector3(10f, 4f, 1f);

            var baseSR = campBase.AddComponent<SpriteRenderer>();
            baseSR.sprite       = CreateSolidSprite(new Color(0.15f, 0.10f, 0.25f, 0.6f));
            baseSR.sortingOrder = -1;

            Debug.Log("[VisualMapBuilder] 🏕️  Camp Zone đã dựng xong");
        }

        // ══════════════════════════════════════════════════════
        //  6. Border Walls (vô hình, chặn nhân vật)
        // ══════════════════════════════════════════════════════
        private static void BuildBorderWalls(Transform root)
        {
            var wRoot = new GameObject("--- Borders ---").transform;
            wRoot.SetParent(root);

            CreateWall(wRoot, "Wall_L",   new Vector2(-31f, 0f),  new Vector2(1f, 26f));
            CreateWall(wRoot, "Wall_R",   new Vector2( 31f, 0f),  new Vector2(1f, 26f));
            CreateWall(wRoot, "Wall_Top", new Vector2(0f,  13f),  new Vector2(62f, 1f));

            Debug.Log("[VisualMapBuilder] 🧱 Borders: 3 tường biên");
        }

        private static void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(pos.x, pos.y, 0f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        // ══════════════════════════════════════════════════════
        //  Helpers
        // ══════════════════════════════════════════════════════
        private static Sprite LoadFirstSprite(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().ToArray();
                if (sprites.Length > 0) return sprites[0];
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                if (tex != null)
                {
                    return Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 16f);
                }
            }
            return null;
        }

        private static Sprite[] LoadSpritesFromFolder(string folder)
        {
            if (!Directory.Exists(folder.Replace("Assets/", Application.dataPath + "/"))) return new Sprite[0];
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            return guids
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .SelectMany(p => AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>())
                .ToArray();
        }

        private static Sprite CreateSolidSprite(Color color)
        {
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
            var px  = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
#endif
