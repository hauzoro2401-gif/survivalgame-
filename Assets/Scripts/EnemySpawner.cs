using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// EnemySpawner - Hệ thống sinh ra kẻ thù theo chu kỳ ngày/đêm.
/// Ban ngày: spawn Gremvak ở rìa map, mỗi 60s, tối đa 8 con.
/// Ban đêm: spawn Bóng Đêm, mỗi 30s, tối đa 5 con.
/// Tăng độ khó theo ngày. Không spawn trong vùng an toàn.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // === CẤU HÌNH CHUNG ===
    [Header("Giới hạn kẻ thù")]
    [SerializeField] [Tooltip("Tổng số kẻ thù tối đa trên map")]
    private int maxEnemies = 12;

    [SerializeField] [Tooltip("Số Gremvak tối đa ban ngày")]
    private int maxGremvak = 8;

    [SerializeField] [Tooltip("Số Bóng Đêm tối đa ban đêm")]
    private int maxBongDem = 5;

    // === THỜI GIAN SPAWN ===
    [Header("Thời gian spawn")]
    [SerializeField] [Tooltip("Gremvak spawn mỗi X giây (ban ngày)")]
    private float gremvakSpawnInterval = 60f;

    [SerializeField] [Tooltip("Bóng Đêm spawn mỗi X giây (ban đêm)")]
    private float bongDemSpawnInterval = 30f;

    // === GREMVAK - NHÓM ===
    [Header("Gremvak - Spawn theo nhóm")]
    [SerializeField] [Tooltip("Số con tối thiểu mỗi nhóm")]
    private int gremvakGroupMin = 3;

    [SerializeField] [Tooltip("Số con tối đa mỗi nhóm")]
    private int gremvakGroupMax = 5;

    // === VÙNG AN TOÀN ===
    [Header("Vùng an toàn (không spawn)")]
    [SerializeField] private float safeZoneHalfWidth = 5f;
    [SerializeField] private float safeZoneHalfHeight = 5f;

    // === KÍCH THƯỚC MAP ===
    [Header("Kích thước map")]
    [SerializeField] private float mapHalfWidth = 25f;
    [SerializeField] private float mapHalfHeight = 15f;

    // === ĐỘ KHÓ THEO NGÀY ===
    [Header("Tăng độ khó")]
    [SerializeField] [Tooltip("Ngày bắt đầu tăng mức 1 (HP +20%)")]
    private int difficultyDay1 = 3;

    [SerializeField] [Tooltip("Ngày bắt đầu tăng mức 2 (HP +40%)")]
    private int difficultyDay2 = 5;

    // === SPRITE PLACEHOLDER ===
    [Header("Sprites (placeholder nếu chưa có)")]
    [SerializeField] private Sprite gremvakSprite;
    [SerializeField] private Color gremvakColor = new Color(0.5f, 0.7f, 0.2f);
    [SerializeField] private Sprite bongDemSprite;
    [SerializeField] private Color bongDemColor = new Color(0.15f, 0.1f, 0.3f);

    // === ITEM DROP ===
    [Header("Item drop kẻ thù")]
    [SerializeField] private List<ItemData> gremvakDrops = new List<ItemData>();
    [SerializeField] private List<ItemData> bongDemDrops = new List<ItemData>();

    // === TRẠNG THÁI ===
    private float spawnTimer = 0f;
    private bool isNight = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    // FIX #5: Boss chỉ spawn 1 lần từ ngày 6
    private bool bossSpawned = false;

    // Containers
    private Transform enemyContainer;

    private void Start()
    {
        // Tạo container
        enemyContainer = new GameObject("--- Kẻ thù ---").transform;
        enemyContainer.SetParent(transform);

        // Đăng ký event ngày/đêm
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart += OnDayStart;
            dayNight.OnNightStart += OnNightStart;
            isNight = dayNight.IsNight;
        }

        // Spawn nhóm Gremvak đầu tiên sau 10 giây
        spawnTimer = 10f;
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Dọn dẹp enemy đã bị destroy
        CleanupDeadEnemies();

        // Giảm timer
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            if (isNight)
            {
                TrySpawnBongDem();
                spawnTimer = bongDemSpawnInterval;
            }
            else
            {
                TrySpawnGremvakGroup();
                spawnTimer = gremvakSpawnInterval;
            }
        }
    }

    private void OnDestroy()
    {
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart -= OnDayStart;
            dayNight.OnNightStart -= OnNightStart;
        }
    }

    // ===================================
    // SPAWN GREMVAK
    // ===================================

    /// <summary>
    /// Thử spawn 1 nhóm Gremvak (3-5 con) ở rìa map.
    /// </summary>
    private void TrySpawnGremvakGroup()
    {
        // Kiểm tra giới hạn tổng
        if (GetActiveEnemyCount() >= maxEnemies) return;

        // Đếm Gremvak hiện tại
        int gremvakCount = CountEnemiesOfType<Enemy_Gremvak>();
        if (gremvakCount >= maxGremvak) return;

        // Số con trong nhóm (không vượt quá limit)
        int groupSize = Random.Range(gremvakGroupMin, gremvakGroupMax + 1);
        int canSpawn = Mathf.Min(groupSize, maxGremvak - gremvakCount, maxEnemies - GetActiveEnemyCount());

        if (canSpawn <= 0) return;

        // Vị trí spawn ở rìa map
        Vector2 basePos = GetEdgeSpawnPosition();

        for (int i = 0; i < canSpawn; i++)
        {
            // Vị trí xung quanh base pos (nhóm)
            Vector2 offset = Random.insideUnitCircle * 2f;
            Vector2 spawnPos = basePos + offset;

            GameObject enemy = CreateGremvak(spawnPos);
            if (enemy != null)
            {
                activeEnemies.Add(enemy);
            }
        }

        Debug.Log($"[Spawner] Spawn nhóm {canSpawn} Gremvak tại {basePos}");
    }

    /// <summary>
    /// Tạo 1 Gremvak tại vị trí chỉ định.
    /// </summary>
    private GameObject CreateGremvak(Vector2 position)
    {
        GameObject obj = new GameObject("Gremvak");
        obj.transform.SetParent(enemyContainer);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.tag = "Enemy";

        // SpriteRenderer
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = gremvakSprite != null ? gremvakSprite : CreatePlaceholderSprite();
        sr.color = gremvakColor;
        sr.sortingOrder = 8;

        // Rigidbody2D
        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Collider
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 0.7f);

        // Enemy script
        Enemy_Gremvak gremvak = obj.AddComponent<Enemy_Gremvak>();

        // Áp dụng difficulty scaling
        ApplyDifficultyToEnemy(gremvak);

        // Gắn HealthBarUI
        obj.AddComponent<HealthBarUI>();

        return obj;
    }

    // ===================================
    // SPAWN BÓNG ĐÊM
    // ===================================

    /// <summary>
    /// Thử spawn 1 Bóng Đêm.
    /// </summary>
    private void TrySpawnBongDem()
    {
        if (GetActiveEnemyCount() >= maxEnemies) return;

        int bongDemCount = CountEnemiesOfType<Enemy_BongDem>();
        if (bongDemCount >= maxBongDem) return;

        Vector2 spawnPos = GetEdgeSpawnPosition();

        GameObject enemy = CreateBongDem(spawnPos);
        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }

        Debug.Log($"[Spawner] Spawn Bóng Đêm tại {spawnPos}");
    }

    /// <summary>
    /// Tạo 1 Bóng Đêm tại vị trí chỉ định.
    /// </summary>
    private GameObject CreateBongDem(Vector2 position)
    {
        GameObject obj = new GameObject("BóngĐêm");
        obj.transform.SetParent(enemyContainer);
        obj.transform.position = new Vector3(position.x, position.y, 0f);
        obj.tag = "Enemy";

        // SpriteRenderer
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = bongDemSprite != null ? bongDemSprite : CreatePlaceholderSprite();
        sr.color = bongDemColor;
        sr.sortingOrder = 8;

        // Rigidbody2D
        Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Collider
        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        // Enemy script
        Enemy_BongDem bongDem = obj.AddComponent<Enemy_BongDem>();

        // Difficulty scaling
        ApplyDifficultyToEnemy(bongDem);

        // HealthBarUI
        obj.AddComponent<HealthBarUI>();

        return obj;
    }

    // ===================================
    // VỊ TRÍ SPAWN
    // ===================================

    /// <summary>
    /// Lấy vị trí spawn ngẫu nhiên ở rìa map (ngoài vùng an toàn).
    /// Chọn 1 trong 4 cạnh map.
    /// </summary>
    private Vector2 GetEdgeSpawnPosition()
    {
        int edge = Random.Range(0, 4);
        Vector2 pos;

        switch (edge)
        {
            case 0: pos = new Vector2(Random.Range(-mapHalfWidth, mapHalfWidth),  mapHalfHeight - 1f); break;
            case 1: pos = new Vector2(Random.Range(-mapHalfWidth, mapHalfWidth), -mapHalfHeight + 1f); break;
            case 2: pos = new Vector2(-mapHalfWidth + 1f, Random.Range(-mapHalfHeight, mapHalfHeight)); break;
            default:pos = new Vector2( mapHalfWidth - 1f, Random.Range(-mapHalfHeight, mapHalfHeight)); break;
        }

        // FIX #10: Safe zone theo campPosition thực tế thay vì hard-code (0,0)
        Vector2 safeCenter = (GameManager.Instance != null)
            ? GameManager.Instance.campPosition
            : Vector2.zero;

        if (Mathf.Abs(pos.x - safeCenter.x) < safeZoneHalfWidth &&
            Mathf.Abs(pos.y - safeCenter.y) < safeZoneHalfHeight)
        {
            pos.x = (pos.x >= safeCenter.x)
                ? safeCenter.x + safeZoneHalfWidth + 2f
                : safeCenter.x - safeZoneHalfWidth - 2f;
        }

        return pos;
    }

    // ===================================
    // ĐỘ KHÓ
    // ===================================

    /// <summary>
    /// Áp dụng buff độ khó theo số ngày hiện tại.
    /// Ngày 3+: HP +20%
    /// Ngày 5+: HP +40%
    /// </summary>
    private void ApplyDifficultyToEnemy(EnemyBase enemy)
    {
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight == null) return;

        int currentDay = dayNight.CurrentDay;

        float hpMult = 1f;
        float dmgMult = 1f;

        if (currentDay >= difficultyDay2)
        {
            hpMult = 1.4f;  // +40% HP
            dmgMult = 1.2f;  // +20% damage
        }
        else if (currentDay >= difficultyDay1)
        {
            hpMult = 1.2f;  // +20% HP
            dmgMult = 1.1f;  // +10% damage
        }

        if (hpMult > 1f || dmgMult > 1f)
        {
            enemy.ApplyDifficultyScaling(hpMult, dmgMult);
            Debug.Log($"[Spawner] Difficulty scaling: HP x{hpMult}, DMG x{dmgMult} (Ngày {currentDay})");
        }
    }

    // ===================================
    // EVENTS NGÀY/ĐÊM
    // ===================================

    private void OnDayStart()
    {
        isNight = false;
        spawnTimer = gremvakSpawnInterval * 0.5f; // Spawn nhanh hơn đầu ngày
        Debug.Log("[Spawner] Ban ngày: chuyển sang spawn Gremvak");
    }

    private void OnNightStart()
    {
        isNight = true;
        spawnTimer = 5f; // Spawn Bóng Đêm nhanh đầu đêm
        Debug.Log("[Spawner] Ban đêm: bắt đầu spawn Bóng Đêm!");

        // FIX #5: Spawn Boss từ đêm ngày 6
        if (!bossSpawned)
        {
            DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
            if (dayNight != null && dayNight.CurrentDay >= 6)
            {
                SpawnBossNow();
            }
        }
    }

    /// <summary>
    /// FIX #5: Spawn Boss "Kẻ Gác Cổng" tại rìa map.
    /// </summary>
    private void SpawnBossNow()
    {
        if (bossSpawned) return;
        bossSpawned = true;

        // Vị trí spawn: phía trên map
        Vector2 spawnPos = new Vector2(0f, mapHalfHeight - 2f);

        // Lấy portal key item nếu có
        ItemData portalKey = null;
        var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (var item in allItems)
            if (item != null && item.itemName.Contains("Đạo") && item.itemName.Contains("Cổng"))
            { portalKey = item; break; }

        BossEnemy.SpawnBoss(spawnPos, portalKey, null);

        Debug.Log("[Spawner] ★★★ Kẻ Gác Cổng đã xuất hiện đêm ngày 6!");
    }

    // ===================================
    // HELPER
    // ===================================

    /// <summary>Dọn dẹp danh sách enemy đã bị destroy</summary>
    private void CleanupDeadEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    /// <summary>Đếm tổng enemy đang sống</summary>
    private int GetActiveEnemyCount()
    {
        CleanupDeadEnemies();
        return activeEnemies.Count;
    }

    /// <summary>Đếm số enemy theo type</summary>
    private int CountEnemiesOfType<T>() where T : EnemyBase
    {
        int count = 0;
        foreach (var e in activeEnemies)
        {
            if (e != null && e.GetComponent<T>() != null)
                count++;
        }
        return count;
    }

    /// <summary>Tạo sprite placeholder</summary>
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

    /// <summary>Gizmos hiển thị vùng spawn và safe zone</summary>
    private void OnDrawGizmos()
    {
        // Vùng map
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapHalfWidth * 2, mapHalfHeight * 2, 0f));

        // Vùng an toàn
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(safeZoneHalfWidth * 2, safeZoneHalfHeight * 2, 0f));
    }
}
