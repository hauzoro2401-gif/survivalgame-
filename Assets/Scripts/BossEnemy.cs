using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// BossEnemy - Kẻ Gác Cổng: người từng bị dịch chuyển, hóa điên.
/// Xuất hiện ngày 6 đêm. HP 500, 3 phase.
/// Drop "Chìa khóa Cổng" khi chết.
/// </summary>
public class BossEnemy : EnemyBase
{
    // === BOSS PHASE ===
    public enum BossPhase { Phase1, Phase2, Phase3 }

    [Header("Boss - Kẻ Gác Cổng")]
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1;

    // === KỸ NĂNG PHASE 1: Cận chiến + triệu hồi ===
    [Header("Phase 1: HP > 60%")]
    [SerializeField] private float summonCooldown = 15f;
    [SerializeField] private int summonCount = 3;

    // === KỸ NĂNG PHASE 2: AOE sóng xung kích ===
    [Header("Phase 2: HP 30-60%")]
    [SerializeField] private float shockwaveRadius = 5f;
    [SerializeField] private float shockwaveDamage = 20f;
    [SerializeField] private float shockwaveCooldown = 8f;

    // === KỸ NĂNG PHASE 3: Berserk ===
    [Header("Phase 3: HP < 30%")]
    [SerializeField] private float berserkSpeedMultiplier = 2f;
    [SerializeField] private float dashForce = 18f;
    [SerializeField] private float dashCooldown = 3f;

    // === DROP ===
    [Header("Drop khi chết")]
    [SerializeField] private ItemData portalKeyDrop;

    // === THOẠI ===
    [Header("Thoại trước khi chết")]
    [SerializeField] private DialogueData deathDialogue;

    // === TIMERS ===
    private float summonTimer = 0f;
    private float shockwaveTimer = 0f;
    private float dashTimer = 0f;
    private bool hasSpoken = false;
    private bool isBerserk = false;
    private float baseMoveSpeed;

    // === VISUAL ===
    private Color bossBaseColor = new Color(0.4f, 0.1f, 0.15f);

    protected override void Awake()
    {
        base.Awake();

        // Chỉ số Boss
        maxHP = 500f;
        moveSpeed = 3.5f;
        attackPower = 25f;
        detectionRange = 20f; // Boss phát hiện từ xa
        attackRange = 2f;
        attackCooldown = 1.2f;
        knockbackForce = 2f; // Boss ít bị knockback
        deathFadeTime = 5f;

        currentHP = maxHP;
        baseMoveSpeed = moveSpeed;

        gameObject.tag = "Enemy";
        gameObject.name = "KẻGácCổng";

        if (spriteRenderer != null)
        {
            spriteRenderer.color = bossBaseColor;
            // Boss to hơn
            transform.localScale = Vector3.one * 1.8f;
        }
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead || GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Cập nhật phase theo HP
        UpdatePhase();

        // Giảm timers
        if (summonTimer > 0f) summonTimer -= Time.deltaTime;
        if (shockwaveTimer > 0f) shockwaveTimer -= Time.deltaTime;
        if (dashTimer > 0f) dashTimer -= Time.deltaTime;

        // Kỹ năng đặc biệt theo phase
        ExecutePhaseAbilities();
    }

    // ===================================
    // PHASE MANAGEMENT
    // ===================================

    /// <summary>
    /// Cập nhật phase dựa trên HP hiện tại.
    /// </summary>
    private void UpdatePhase()
    {
        float hpPercent = currentHP / maxHP;

        BossPhase newPhase;
        if (hpPercent > 0.6f)
            newPhase = BossPhase.Phase1;
        else if (hpPercent > 0.3f)
            newPhase = BossPhase.Phase2;
        else
            newPhase = BossPhase.Phase3;

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged(newPhase);
        }
    }

    /// <summary>
    /// Callback khi chuyển phase.
    /// </summary>
    private void OnPhaseChanged(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2:
                Debug.Log("[Boss] ⚡ Kẻ Gác Cổng chuyển Phase 2 - Sóng Xung Kích!");
                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(0.6f, 0.15f, 0.1f);
                break;

            case BossPhase.Phase3:
                Debug.Log("[Boss] 🔥 Kẻ Gác Cổng BẢO NỘ! Phase 3 - Berserk!");
                isBerserk = true;
                moveSpeed = baseMoveSpeed * berserkSpeedMultiplier;
                attackCooldown *= 0.5f;
                if (spriteRenderer != null)
                    spriteRenderer.color = new Color(0.8f, 0.1f, 0.05f);
                break;
        }
    }

    // ===================================
    // KỸ NĂNG ĐẶC BIỆT
    // ===================================

    /// <summary>
    /// Thực thi kỹ năng theo phase hiện tại.
    /// </summary>
    private void ExecutePhaseAbilities()
    {
        if (CurrentState == EnemyState.Dead || CurrentState == EnemyState.Hurt) return;

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                // Triệu hồi Gremvak
                if (summonTimer <= 0f && CurrentState == EnemyState.Attack)
                {
                    SummonMinions();
                    summonTimer = summonCooldown;
                }
                break;

            case BossPhase.Phase2:
                // Sóng xung kích
                if (shockwaveTimer <= 0f)
                {
                    PerformShockwave();
                    shockwaveTimer = shockwaveCooldown;
                }
                // Vẫn triệu hồi (ít hơn)
                if (summonTimer <= 0f)
                {
                    SummonMinions();
                    summonTimer = summonCooldown * 1.5f;
                }
                break;

            case BossPhase.Phase3:
                // Dash liên tục
                if (dashTimer <= 0f && currentTarget != null)
                {
                    PerformDash();
                    dashTimer = dashCooldown;
                }
                // Hiệu ứng berserk nhấp nháy
                if (spriteRenderer != null)
                {
                    float flash = Mathf.Sin(Time.time * 10f);
                    spriteRenderer.color = flash > 0
                        ? new Color(0.8f, 0.1f, 0.05f)
                        : new Color(1f, 0.3f, 0.1f);
                }
                break;
        }
    }

    /// <summary>
    /// Phase 1: Triệu hồi Gremvak quanh Boss.
    /// </summary>
    private void SummonMinions()
    {
        Debug.Log($"[Boss] Kẻ Gác Cổng triệu hồi {summonCount} Gremvak!");

        for (int i = 0; i < summonCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 3f;
            Vector2 spawnPos = (Vector2)transform.position + offset;

            GameObject minion = new GameObject("Gremvak_Triệu hồi");
            minion.transform.position = spawnPos;
            minion.tag = "Enemy";

            SpriteRenderer sr = minion.AddComponent<SpriteRenderer>();
            Texture2D tex = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int j = 0; j < px.Length; j++) px[j] = Color.white;
            tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.color = new Color(0.5f, 0.7f, 0.2f);
            sr.sortingOrder = 8;

            Rigidbody2D minionRb = minion.AddComponent<Rigidbody2D>();
            minionRb.gravityScale = 0f;
            minionRb.freezeRotation = true;
            minion.AddComponent<BoxCollider2D>();
            minion.AddComponent<Enemy_Gremvak>();
            minion.AddComponent<HealthBarUI>();
            // FIX #12: Set đúng layer "Enemy" để CombatSystem nhận ra
            minion.layer = LayerMask.NameToLayer("Enemy");
            if (minion.layer < 0) minion.layer = 0; // Fallback nếu layer chưa được tạo
        }
    }

    /// <summary>
    /// Phase 2: Sóng xung kích AOE - gây damage tất cả trong radius.
    /// </summary>
    private void PerformShockwave()
    {
        Debug.Log("[Boss] ⚡ SÓNG XUNG KÍCH!");

        // Tìm tất cả nhân vật trong radius
        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null || character.IsDead) continue;

            float dist = Vector2.Distance(transform.position, character.transform.position);
            if (dist <= shockwaveRadius)
            {
                CharacterStats stats = character.GetComponent<CharacterStats>();
                float damage = shockwaveDamage;
                if (stats != null)
                    damage = stats.CalculateDamageTaken(damage);

                character.TakeDamage(damage);
                Debug.Log($"[Boss] Sóng xung kích trúng {character.characterName}: {damage:F0} damage");
            }
        }

        // Hiệu ứng visual: flash trắng nhẹ
        StartCoroutine(ShockwaveVisual());
    }

    private IEnumerator ShockwaveVisual()
    {
        // Tạo vòng sóng mở rộng
        GameObject wave = new GameObject("Shockwave");
        wave.transform.position = transform.position;

        SpriteRenderer waveSr = wave.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(8, 8);
        Color[] px = new Color[64];
        for (int i = 0; i < px.Length; i++)
        {
            int x = i % 8, y = i / 8;
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f));
            px[i] = d < 3.5f && d > 2.5f ? Color.white : new Color(1, 1, 1, 0);
        }
        tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
        waveSr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 4f);
        waveSr.color = new Color(1f, 0.5f, 0.2f, 0.8f);
        waveSr.sortingOrder = 90;

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            wave.transform.localScale = Vector3.one * (t * shockwaveRadius * 2f);
            waveSr.color = new Color(1f, 0.5f, 0.2f, 1f - t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(wave);
    }

    /// <summary>
    /// Phase 3: Dash về phía target.
    /// </summary>
    private void PerformDash()
    {
        if (currentTarget == null) return;

        Vector2 dir = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * dashForce;

        Debug.Log("[Boss] 💀 Kẻ Gác Cổng DASH!");

        StartCoroutine(DashDamage(dir));
    }

    private IEnumerator DashDamage(Vector2 dir)
    {
        float elapsed = 0f;
        float dashDuration = 0.3f;

        while (elapsed < dashDuration)
        {
            // Gây damage khi đâm qua
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
            foreach (var hit in hits)
            {
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null && !pc.IsDead)
                {
                    pc.TakeDamage(attackPower * 0.7f);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
    }

    // ===================================
    // OVERRIDE DEATH: THOẠI + DROP KEY
    // ===================================

    protected override void Die()
    {
        if (CurrentState == EnemyState.Dead) return;

        // Thoại trước khi chết
        if (!hasSpoken && deathDialogue != null)
        {
            hasSpoken = true;
            if (DialogueSystem.Instance != null)
            {
                DialogueSystem.Instance.TriggerDialogue(deathDialogue, "boss_death");
            }
        }

        // Log thoại nếu không có DialogueData
        if (deathDialogue == null)
        {
            Debug.Log("[Boss] Kẻ Gác Cổng: \"Các em... cuối cùng cũng đến...\"");
            Debug.Log("[Boss] Kẻ Gác Cổng: \"Ta từng là giống như các em... một học sinh...\"");
            Debug.Log("[Boss] Kẻ Gác Cổng: \"Cổng đó... ta tạo ra nó... nhưng nó đã nuốt chửng ta...\"");
            Debug.Log("[Boss] Kẻ Gác Cổng: \"Hãy... về nhà... thay ta...\"");
        }

        // Drop Chìa khóa Cổng
        if (portalKeyDrop != null)
        {
            Inventory inv = GameManager.Instance?.GetComponent<Inventory>();
            if (inv != null)
            {
                inv.AddItem(portalKeyDrop, 1);
                Debug.Log("[Boss] ★ Drop: Chìa khóa Cổng!");
            }
        }

        Debug.Log("[Boss] ★★★ KẺ GÁC CỔNG ĐÃ BỊ ĐÁNH BẠI! ★★★");

        base.Die();
    }

    /// <summary>
    /// Override TakeDamage để thêm visual feedback mạnh hơn.
    /// </summary>
    public override void TakeDamage(float damage, Vector2 attackerPosition)
    {
        base.TakeDamage(damage, attackerPosition);

        // Hiện HP%
        if (!IsDead)
        {
            float hpPercent = currentHP / maxHP * 100f;
            Debug.Log($"[Boss] Kẻ Gác Cổng HP: {currentHP:F0}/{maxHP:F0} ({hpPercent:F0}%)");
        }
    }

    /// <summary>
    /// Spawn Boss tại vị trí chỉ định (gọi từ EnemySpawner hoặc event).
    /// </summary>
    public static GameObject SpawnBoss(Vector2 position, ItemData keyItem = null,
        DialogueData deathDialogue = null)
    {
        GameObject bossObj = new GameObject("KẻGácCổng");
        bossObj.transform.position = position;
        bossObj.transform.localScale = Vector3.one * 1.8f;
        bossObj.tag = "Enemy";

        SpriteRenderer sr = bossObj.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        sr.color = new Color(0.4f, 0.1f, 0.15f);
        sr.sortingOrder = 9;

        Rigidbody2D rb = bossObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        BoxCollider2D col = bossObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.2f, 1.2f);

        BossEnemy boss = bossObj.AddComponent<BossEnemy>();
        if (keyItem != null)
        {
            boss.portalKeyDrop = keyItem;
        }
        boss.deathDialogue = deathDialogue;

        bossObj.AddComponent<HealthBarUI>();

        Debug.Log("[Boss] ★ Kẻ Gác Cổng xuất hiện!");

        return bossObj;
    }
}
