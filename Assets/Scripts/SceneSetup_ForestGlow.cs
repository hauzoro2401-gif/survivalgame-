using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SceneSetup_ForestGlow - Dựng Scene "Vực Thẳm" (Cave Rift) procedurally.
/// Theme: hang động tối, ánh sáng đuốc/pha lê, tinh thể phát quang.
/// Gắn vào 1 GameObject rỗng trong Scene.
/// </summary>
public class SceneSetup_ForestGlow : MonoBehaviour
{
    // === KÍCH THƯỚC MAP ===
    [Header("Kích thước Map")]
    [SerializeField] private float mapWidth = 50f;
    [SerializeField] private float mapHeight = 30f;

    // === VÙNG AN TOÀN ===
    [Header("Vùng an toàn (giữa map)")]
    [SerializeField] private float safeZoneWidth = 10f;
    [SerializeField] private float safeZoneHeight = 10f;

    // === CÂY ===
    // ─── Màu hang động ──────────────────────────────────
    private static readonly Color CAVE_GROUND   = new Color(0.12f, 0.09f, 0.22f); // tím đen
    private static readonly Color CAVE_ROCK     = new Color(0.20f, 0.15f, 0.35f); // tím xám
    private static readonly Color CAVE_CRYSTAL  = new Color(0.35f, 0.70f, 1.00f); // xanh pha lê
    private static readonly Color CAVE_CRYSTAL2 = new Color(0.65f, 0.30f, 1.00f); // tím pha lê
    private static readonly Color CAVE_CRYSTAL3 = new Color(0.30f, 1.00f, 0.75f); // xanh ngọc
    private static readonly Color TORCH_COLOR   = new Color(1.00f, 0.60f, 0.20f); // cam đuốc
    private static readonly Color SAFE_COLOR    = new Color(0.40f, 1.00f, 0.55f); // xanh an toàn

    [Header("Đá / Chướng ngại")]
    [SerializeField] private int treeCount = 30;
    [SerializeField] private Sprite treeSprite;
    [SerializeField] private Color treeColor = new Color(0.18f, 0.13f, 0.30f);
    [SerializeField] private Vector2 treeScaleRange = new Vector2(0.6f, 1.4f);

    // === ĐÁ HANG ===
    [Header("Đá hang động")]
    [SerializeField] private int rockCount = 25;
    [SerializeField] private Sprite rockSprite;
    [SerializeField] private Color rockColor = new Color(0.20f, 0.15f, 0.35f);
    [SerializeField] private Vector2 rockScaleRange = new Vector2(0.4f, 1.1f);

    // === TINH THỂ ===
    [Header("Tinh thể trang trí")]
    [SerializeField] private int bushCount = 20;
    [SerializeField] private Sprite bushSprite;
    [SerializeField] private Color bushColor = new Color(0.35f, 0.70f, 1.00f);
    [SerializeField] private Vector2 bushScaleRange = new Vector2(0.3f, 0.7f);

    // === TÀI NGUYÊN ===
    [Header("Điểm tài nguyên")]
    [SerializeField] private int resourcePointCount = 5;
    [SerializeField] private Sprite resourceSprite;
    [SerializeField] private Color woodColor  = new Color(0.55f, 0.35f, 0.15f);
    [SerializeField] private Color stoneColor = new Color(0.30f, 0.25f, 0.45f);
    [SerializeField] private Color foodColor  = new Color(0.90f, 0.35f, 0.35f);

    // === NHÂN VẬT ===
    [Header("Prefab nhân vật")]
    [SerializeField] private List<GameObject> characterPrefabs = new List<GameObject>();

    // === NỀN ===
    [Header("Nền map")]
    [SerializeField] private Color groundColor = new Color(0.12f, 0.09f, 0.22f);

    // Containers
    private Transform treeContainer;
    private Transform rockContainer;
    private Transform bushContainer;
    private Transform resourceContainer;

    public enum ResourceType { Wood, Stone, Food }

    private void Start()
    {
        CreateContainers();
        CreateCaveGround();      // nền hang tối
        SpawnTrees();            // đá/chướng ngại
        SpawnRocks();
        SpawnBushes();           // tinh thể
        SpawnCaveLights();       // đèn đuốc + pha lê
        SpawnResourcePoints();
        SpawnCharacters();
        SetupGlobalLight();      // ánh sáng toàn cục

        if (GameManager.Instance != null)
            GameManager.Instance.SetCampPosition(Vector2.zero);

        Debug.Log("[CaveRift] Hang động Vực Thẳm đã được tạo!");
    }

    private void CreateContainers()
    {
        treeContainer = new GameObject("--- Cây ---").transform;
        treeContainer.SetParent(transform);
        rockContainer = new GameObject("--- Đá ---").transform;
        rockContainer.SetParent(transform);
        bushContainer = new GameObject("--- Bụi ---").transform;
        bushContainer.SetParent(transform);
        resourceContainer = new GameObject("--- Tài nguyên ---").transform;
        resourceContainer.SetParent(transform);
    }

    /// <summary>Tạo nền hang động nhiều lớp (3 layer độ sâu)</summary>
    private void CreateCaveGround()
    {
        // FIX #6: Xóa các BG layer cũ để tránh chồng khi Play lại
        string[] bgNames = { "BG_Deep", "BG_Mid", "BG_Near", "Floor" };
        foreach (string bgName in bgNames)
        {
            GameObject old = GameObject.Find(bgName);
            if (old != null) DestroyImmediate(old);
        }

        // Layer sâu nhất - nền đen
        CreateBGLayer("BG_Deep",  new Color(0.04f,0.03f,0.08f), -100,
            new Vector3(mapWidth * 1.2f, mapHeight * 1.2f, 1f));
        // Layer giữa
        CreateBGLayer("BG_Mid",   new Color(0.07f,0.05f,0.14f), -90,
            new Vector3(mapWidth * 1.1f, mapHeight * 1.1f, 1f));
        // Layer gần - màu chính của hang
        CreateBGLayer("BG_Near",  CAVE_GROUND, -80,
            new Vector3(mapWidth, mapHeight, 1f));

        // Sàn hang (đậm hơn)
        GameObject floor = new GameObject("Floor");
        floor.transform.SetParent(transform);
        floor.transform.position  = new Vector3(0f, -mapHeight * 0.5f + 2f, 0f);
        floor.transform.localScale = new Vector3(mapWidth, 4f, 1f);
        SpriteRenderer fsr = floor.AddComponent<SpriteRenderer>();
        fsr.sprite = CreatePlaceholderSprite();
        fsr.color  = CAVE_ROCK;
        fsr.sortingOrder = -70;
    }

    private void CreateBGLayer(string name, Color color, int order, Vector3 scale)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform);
        obj.transform.position   = Vector3.zero;
        obj.transform.localScale = scale;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = CreatePlaceholderSprite();
        sr.color        = color;
        sr.sortingOrder = order;
    }

    /// <summary>Spawn đá/chướng ngại vật hang động</summary>
    private void SpawnTrees()
    {
        Sprite sprite = treeSprite != null ? treeSprite : CreatePlaceholderSprite();
        // Màu đá biến thiên – mỗi viên đá hơi khác nhau
        Color[] rockVariants = {
            CAVE_ROCK,
            new Color(0.16f, 0.12f, 0.28f),
            new Color(0.24f, 0.18f, 0.40f),
        };
        for (int i = 0; i < treeCount; i++)
        {
            Vector2 pos   = GetRandomPositionOutsideSafeZone();
            float   scale = Random.Range(treeScaleRange.x, treeScaleRange.y);
            Color   col   = rockVariants[i % rockVariants.Length];
            var obj = CreateObstacle($"Đá_{i}", pos, sprite, col, scale, treeContainer);
            BoxCollider2D bc = obj.GetComponent<BoxCollider2D>();
            if (bc != null) bc.size = new Vector2(0.6f, 0.7f);
        }
    }

    /// <summary>Spawn đá</summary>
    private void SpawnRocks()
    {
        Sprite sprite = rockSprite != null ? rockSprite : CreatePlaceholderSprite();
        for (int i = 0; i < rockCount; i++)
        {
            Vector2 pos = GetRandomPositionOutsideSafeZone();
            float scale = Random.Range(rockScaleRange.x, rockScaleRange.y);
            CreateObstacle($"Đá_{i}", pos, sprite, rockColor, scale, rockContainer);
        }
    }

    /// <summary>Spawn tinh thể phát quang (thay bụi cây)</summary>
    private void SpawnBushes()
    {
        Sprite sprite = bushSprite != null ? bushSprite : CreatePlaceholderSprite();
        Color[] crystalColors = { CAVE_CRYSTAL, CAVE_CRYSTAL2, CAVE_CRYSTAL3 };
        for (int i = 0; i < bushCount; i++)
        {
            Vector2 pos   = GetRandomPositionOutsideSafeZone();
            float   scale = Random.Range(bushScaleRange.x, bushScaleRange.y);
            Color   col   = crystalColors[i % crystalColors.Length];
            col.a = 0.85f;
            var obj = CreateObstacle($"Crystal_{i}", pos, sprite, col, scale, bushContainer);
            // Thêm GlowPulse nếu script tồn tại
            if (obj.GetComponent<GlowPulse>() == null)
                obj.AddComponent<GlowPulse>();
        }
    }

    /// <summary>Tạo 1 obstacle với SpriteRenderer + Collider2D</summary>
    /// <summary>Spawn đèn đuốc + tinh thể phát sáng (Point Light 2D)</summary>
    private void SpawnCaveLights()
    {
        var lightContainer = new GameObject("--- Lights ---").transform;
        lightContainer.SetParent(transform);

        // Đuốc dọc map (cam)
        float[] torchX = { -20f, -12f, -4f, 4f, 12f, 20f };
        foreach (float x in torchX)
        {
            float y = Random.Range(-mapHeight*0.35f, mapHeight*0.35f);
            AddPointLight(lightContainer, $"Torch_{x}",
                new Vector2(x, y), TORCH_COLOR, 10f, 1.8f, flicker: true);
        }

        // Tinh thể xanh lam (lạnh)
        float[] crystalX = { -18f, -6f, 8f, 18f };
        foreach (float x in crystalX)
        {
            float y = Random.Range(-mapHeight*0.3f, mapHeight*0.4f);
            AddPointLight(lightContainer, $"CrystalLight_{x}",
                new Vector2(x, y), CAVE_CRYSTAL, 7f, 1.2f, flicker: false);
        }

        // Ánh sáng camp trung tâm (xanh lá)
        AddPointLight(lightContainer, "CampLight",
            Vector2.zero, SAFE_COLOR, 14f, 1.6f, flicker: false);

        // Đốm lửa trại
        AddPointLight(lightContainer, "CampFire",
            new Vector2(0f, -mapHeight*0.45f + 2f),
            TORCH_COLOR, 6f, 3f, flicker: true);
    }

    private void AddPointLight(Transform parent, string name,
        Vector2 pos, Color color, float radius, float intensity, bool flicker)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(pos.x, pos.y, 0f);

        var lt = obj.AddComponent<Light2D>();
        lt.lightType             = Light2D.LightType.Point;
        lt.color                 = color;
        lt.intensity             = intensity;
        lt.pointLightOuterRadius = radius;
        lt.pointLightInnerRadius = radius * 0.25f;
        lt.falloffIntensity      = 1f;
        lt.shadowsEnabled        = true;

        if (flicker && obj.GetComponent<LightFlicker>() == null)
            obj.AddComponent<LightFlicker>();
    }

    /// <summary>Thiết lập Global Light 2D hang động</summary>
    private void SetupGlobalLight()
    {
        // Tìm Global Light đã có trong scene
        var existing = FindAnyObjectByType<Light2D>();
        if (existing != null && existing.lightType == Light2D.LightType.Global)
        {
            existing.color     = new Color(0.12f, 0.10f, 0.25f);
            existing.intensity = 0.10f;
            return;
        }
        // Tạo mới nếu chưa có
        var glObj = new GameObject("GlobalLight2D_Cave");
        glObj.transform.SetParent(transform);
        var gl = glObj.AddComponent<Light2D>();
        gl.lightType = Light2D.LightType.Global;
        gl.color     = new Color(0.12f, 0.10f, 0.25f);
        gl.intensity = 0.10f;
    }

    private GameObject CreateObstacle(string name, Vector2 pos, Sprite sprite,
        Color color, float scale, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(pos.x, pos.y, 0f);
        obj.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.6f);

        return obj;
    }

    /// <summary>Spawn 5 điểm tài nguyên (gỗ, đá, thức ăn)</summary>
    private void SpawnResourcePoints()
    {
        Sprite sprite = resourceSprite != null ? resourceSprite : CreatePlaceholderSprite();
        ResourceType[] types = { ResourceType.Wood, ResourceType.Stone, ResourceType.Food,
                                  ResourceType.Wood, ResourceType.Food };

        for (int i = 0; i < resourcePointCount && i < types.Length; i++)
        {
            Vector2 pos = GetRandomPositionOutsideSafeZone();
            ResourceType type = types[i];

            Color resColor = type switch
            {
                ResourceType.Wood => woodColor,
                ResourceType.Stone => stoneColor,
                ResourceType.Food => foodColor,
                _ => Color.white
            };

            string typeName = type switch
            {
                ResourceType.Wood => "Gỗ",
                ResourceType.Stone => "Đá_TN",
                ResourceType.Food => "Thức_ăn",
                _ => "Tài_nguyên"
            };

            GameObject resource = new GameObject($"{typeName}_{i}");
            resource.transform.SetParent(resourceContainer);
            resource.transform.position = new Vector3(pos.x, pos.y, 0f);
            resource.transform.localScale = Vector3.one * 0.7f;

            SpriteRenderer sr = resource.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = resColor;
            sr.sortingOrder = 5;

            CircleCollider2D triggerCol = resource.AddComponent<CircleCollider2D>();
            triggerCol.isTrigger = true;
            triggerCol.radius = 1f;

            ResourceNode node = resource.AddComponent<ResourceNode>();
            node.Configure(ConvertResourceType(type), null, 3);
        }
    }

    private ResourceNode.ResourceType ConvertResourceType(ResourceType type)
    {
        return type switch
        {
            ResourceType.Wood => ResourceNode.ResourceType.Wood,
            ResourceType.Stone => ResourceNode.ResourceType.Stone,
            ResourceType.Food => ResourceNode.ResourceType.Food,
            _ => ResourceNode.ResourceType.Wood
        };
    }

    /// <summary>Spawn 4 nhân vật ở vùng an toàn giữa map</summary>
    private void SpawnCharacters()
    {
        Vector2[] spawnPos = {
            new Vector2(-2f, 1f),   // Minh
            new Vector2(2f, 1f),    // Linh
            new Vector2(0f, -1f),   // Khoa
            new Vector2(0f, 2f)     // Trang
        };

        List<PlayerController> spawned = new List<PlayerController>();

        for (int i = 0; i < 4; i++)
        {
            GameObject charObj;
            if (i < characterPrefabs.Count && characterPrefabs[i] != null)
            {
                charObj = Instantiate(characterPrefabs[i], spawnPos[i], Quaternion.identity);
            }
            else
            {
                charObj = CreatePlaceholderCharacter(i, spawnPos[i]);
            }

            EnsureCharacterComponents(charObj, i);
            PlayerController pc = charObj.GetComponent<PlayerController>();
            if (pc != null) spawned.Add(pc);
        }

        if (GameManager.Instance != null && spawned.Count > 0)
        {
            GameManager.Instance.characters = spawned;
            GameManager.Instance.SwitchCharacter(0);
        }
    }

    /// <summary>Tạo nhân vật placeholder khi chưa có prefab</summary>
    private GameObject CreatePlaceholderCharacter(int index, Vector2 position)
    {
        string[] names = { "Minh", "Linh", "Khoa", "Trang" };
        // Màu sắc nổi bật trên nền hang tối
        Color[] colors = {
            new Color(0.40f, 0.65f, 1.00f),  // Minh - xanh lam sáng
            new Color(1.00f, 0.65f, 0.20f),  // Linh - cam ấm
            new Color(0.30f, 1.00f, 0.55f),  // Khoa - xanh ngọc
            new Color(1.00f, 0.50f, 0.80f)   // Trang - hồng tím
        };

        GameObject obj = new GameObject(names[index]);
        obj.transform.position = new Vector3(position.x, position.y, 0f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreatePlaceholderSprite();
        sr.color = colors[index];
        sr.sortingOrder = 10;

        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.8f);

        obj.AddComponent<PlayerController>();
        CharacterStats stats = obj.AddComponent<CharacterStats>();
        stats.SetCharacterType((CharacterStats.CharacterType)index);
        obj.AddComponent<SurvivalSystem>();
        obj.AddComponent<CombatSystem>();
        obj.AddComponent<HealthBarUI>();

        return obj;
    }

    private void EnsureCharacterComponents(GameObject charObj, int index)
    {
        if (charObj.GetComponent<PlayerController>() == null)
            charObj.AddComponent<PlayerController>();

        CharacterStats stats = charObj.GetComponent<CharacterStats>();
        if (stats == null)
            stats = charObj.AddComponent<CharacterStats>();

        if (index >= 0 && index <= (int)CharacterStats.CharacterType.Trang)
            stats.SetCharacterType((CharacterStats.CharacterType)index);

        if (charObj.GetComponent<SurvivalSystem>() == null)
            charObj.AddComponent<SurvivalSystem>();
        if (charObj.GetComponent<CombatSystem>() == null)
            charObj.AddComponent<CombatSystem>();
        if (charObj.GetComponent<HealthBarUI>() == null)
            charObj.AddComponent<HealthBarUI>();
    }

    /// <summary>Vị trí ngẫu nhiên NGOÀI vùng an toàn</summary>
    private Vector2 GetRandomPositionOutsideSafeZone()
    {
        float halfW = mapWidth / 2f;
        float halfH = mapHeight / 2f;
        float safeHalfW = safeZoneWidth / 2f;
        float safeHalfH = safeZoneHeight / 2f;

        Vector2 pos;
        int attempts = 0;
        do
        {
            pos = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
            attempts++;
            if (attempts > 100) break;
        } while (Mathf.Abs(pos.x) < safeHalfW && Mathf.Abs(pos.y) < safeHalfH);

        return pos;
    }

    /// <summary>Tạo sprite placeholder 4x4 pixel trắng</summary>
    private Sprite CreatePlaceholderSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    /// <summary>Gizmos hiển thị boundary và safe zone trong Editor</summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapWidth, mapHeight, 0f));
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(safeZoneWidth, safeZoneHeight, 0f));
    }
}
