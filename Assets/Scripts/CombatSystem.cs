using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

/// <summary>
/// CombatSystem - Hệ thống chiến đấu gắn vào mỗi nhân vật.
/// Mỗi nhân vật có kỹ năng tấn công và đặc biệt riêng:
/// - Minh: đánh chậm, damage cao, stun 1s
/// - Khoa: đánh nhanh x2, dash (Shift)
/// - Linh: tầm xa, đánh 2 kẻ thù cùng lúc
/// - Trang: đánh yếu, hồi 2 HP cho đồng đội gần nhất
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(CharacterStats))]
public class CombatSystem : MonoBehaviour
{
    // === CẤU HÌNH TẤN CÔNG ===
    [Header("Cấu hình tấn công")]
    [SerializeField] [Tooltip("Cooldown giữa các đòn đánh (giây)")]
    private float attackCooldown = 0.5f;

    [SerializeField] [Tooltip("Kích thước hitbox tấn công")]
    private Vector2 attackBoxSize = new Vector2(1.5f, 1f);

    [SerializeField] [Tooltip("Khoảng cách hitbox phía trước nhân vật")]
    private float attackBoxOffset = 1f;

    [SerializeField] [Tooltip("Layer kẻ thù (để hitbox chỉ đánh enemy)")]
    private LayerMask enemyLayer;

    // === KỸ NĂNG ĐẶC BIỆT ===
    [Header("Kỹ năng đặc biệt")]
    [SerializeField] [Tooltip("Cooldown kỹ năng đặc biệt (giây)")]
    private float specialCooldown = 3f;

    // === DASH (Khoa) ===
    [Header("Dash (Khoa)")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.2f;

    // === DAMAGE POPUP ===
    [Header("Damage Number Popup")]
    [SerializeField] [Tooltip("Prefab text hiển thị damage (tự tạo nếu null)")]
    private GameObject damagePopupPrefab;

    // === PHÍM TẮT ===
    [Header("Phím tấn công")]
    [SerializeField] private Key attackKey = Key.Space;
    [SerializeField] private Key specialKey = Key.LeftShift;

    // === REFERENCES ===
    private PlayerController playerController;
    private CharacterStats characterStats;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    // === TRẠNG THÁI ===
    private float attackTimer = 0f;
    private float specialTimer = 0f;
    private bool isDashing = false;

    /// <summary>Hướng nhân vật đang nhìn (1 = phải, -1 = trái)</summary>
    public int FacingDirection { get; private set; } = 1;

    /// <summary>Đang trong cooldown tấn công</summary>
    public bool IsAttackReady => attackTimer <= 0f;

    /// <summary>Đang trong cooldown kỹ năng</summary>
    public bool IsSpecialReady => specialTimer <= 0f;

    private void Awake()
    {
        attackKey = Key.Space;
        specialKey = Key.LeftShift;

        playerController = GetComponent<PlayerController>();
        characterStats   = GetComponent<CharacterStats>();
        rb               = GetComponent<Rigidbody2D>();
        spriteRenderer   = GetComponent<SpriteRenderer>();

        // FIX #1: Tự động lấy Layer "Enemy" nếu chưa set trong Inspector
        if (enemyLayer == 0)
            enemyLayer = LayerMask.GetMask("Enemy");

        // Thiết lập cooldown theo nhân vật
        InitializeCombatStats();
    }

    /// <summary>
    /// Thiết lập chỉ số chiến đấu riêng cho mỗi nhân vật.
    /// </summary>
    private void InitializeCombatStats()
    {
        switch (characterStats.Type)
        {
            case CharacterStats.CharacterType.Minh:
                // Tank: đánh chậm, hitbox lớn
                attackCooldown = 1.0f;
                attackBoxSize = new Vector2(1.8f, 1.2f);
                break;

            case CharacterStats.CharacterType.Khoa:
                // Scout: đánh nhanh x2
                attackCooldown = 0.25f;
                attackBoxSize = new Vector2(1.2f, 0.8f);
                break;

            case CharacterStats.CharacterType.Linh:
                // Crafter: tầm xa, hitbox rộng
                attackCooldown = 0.6f;
                attackBoxSize = new Vector2(2.5f, 1.5f);
                attackBoxOffset = 1.5f;
                break;

            case CharacterStats.CharacterType.Trang:
                // Healer: đánh nhanh vừa
                attackCooldown = 0.5f;
                attackBoxSize = new Vector2(1.2f, 0.8f);
                break;
        }
    }

    private void Update()
    {
        // Chỉ xử lý khi đang được điều khiển
        if (!playerController.IsControlled || playerController.IsDead) return;
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Cập nhật hướng nhìn từ sprite flip
        if (spriteRenderer != null)
        {
            FacingDirection = spriteRenderer.flipX ? -1 : 1;
        }

        // Giảm timer cooldown
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (specialTimer > 0f) specialTimer -= Time.deltaTime;

        // Input tấn công (Space)
        if (Keyboard.current != null && Keyboard.current[attackKey].wasPressedThisFrame && IsAttackReady)
        {
            Attack();
        }

        // Input kỹ năng đặc biệt (Shift)
        if (Keyboard.current != null && Keyboard.current[specialKey].wasPressedThisFrame && IsSpecialReady)
        {
            UseSpecialAbility();
        }
    }

    /// <summary>
    /// Tấn công cơ bản - tạo hitbox phía trước và gây damage.
    /// Dùng Physics2D.OverlapBoxAll để tìm kẻ thù trong vùng.
    /// </summary>
    public void Attack()
    {
        if (playerController.IsDead) return;

        // Tính vị trí hitbox phía trước nhân vật
        Vector2 hitboxCenter = (Vector2)transform.position +
            new Vector2(FacingDirection * attackBoxOffset, 0f);

        // Tìm tất cả kẻ thù trong hitbox
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            hitboxCenter,
            attackBoxSize,
            0f,
            enemyLayer
        );

        // Nếu không set layer → tìm theo tag "Enemy"
        if (enemyLayer == 0)
        {
            hits = Physics2D.OverlapBoxAll(hitboxCenter, attackBoxSize, 0f);
        }

        // Tính sát thương
        float baseDamage = characterStats.AttackPower;
        // TODO: cộng thêm damage từ vũ khí trang bị khi có EquipmentSystem

        // Giới hạn số mục tiêu (Linh đánh 2, còn lại đánh 1)
        int maxTargets = characterStats.Type == CharacterStats.CharacterType.Linh ? 2 : 1;
        int hitCount = 0;

        foreach (var hit in hits)
        {
            if (hitCount >= maxTargets) break;

            // Bỏ qua bản thân và đồng đội
            if (hit.gameObject == gameObject) continue;

            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) continue;
            if (enemy.CurrentState == EnemyBase.EnemyState.Dead) continue;

            // === ÁP DỤNG ĐẶC BIỆT THEO NHÂN VẬT ===
            float finalDamage = baseDamage;

            // Minh: stun kẻ thù 1 giây
            if (characterStats.Type == CharacterStats.CharacterType.Minh)
            {
                enemy.ApplyStun(1f);
            }

            // Gây sát thương
            enemy.TakeDamage(finalDamage, transform.position);

            // Hiện damage popup
            SpawnDamagePopup(enemy.transform.position, finalDamage);

            hitCount++;

            // Tăng gắn kết khi chiến đấu cùng nhau (BondingSystem)
            TriggerCombatBond();
        }

        // Trang: hồi 2 HP cho đồng đội gần nhất mỗi đòn đánh
        if (characterStats.Type == CharacterStats.CharacterType.Trang)
        {
            HealNearestAlly(2f);
        }

        // Reset cooldown
        attackTimer = attackCooldown;

        Debug.Log($"[Combat] {playerController.characterName} tấn công! Trúng {hitCount} mục tiêu");
    }

    /// <summary>
    /// Kỹ năng đặc biệt (nhấn Shift).
    /// </summary>
    private void UseSpecialAbility()
    {
        switch (characterStats.Type)
        {
            case CharacterStats.CharacterType.Minh:
                // Minh: Đòn mạnh gấp 2x damage + stun 2s
                SpecialMinh_PowerStrike();
                break;

            case CharacterStats.CharacterType.Khoa:
                // Khoa: Dash nhanh theo hướng di chuyển
                SpecialKhoa_Dash();
                break;

            case CharacterStats.CharacterType.Linh:
                // Linh: Quét rộng đánh tất cả xung quanh
                SpecialLinh_Sweep();
                break;

            case CharacterStats.CharacterType.Trang:
                // Trang: Hồi máu mạnh cho tất cả đồng đội trong range
                SpecialTrang_GroupHeal();
                break;
        }

        specialTimer = specialCooldown;
    }

    // ===================================
    // KỸ NĂNG RIÊNG TỪNG NHÂN VẬT
    // ===================================

    /// <summary>Minh: Đòn sấm - damage x2, stun 2s</summary>
    private void SpecialMinh_PowerStrike()
    {
        Vector2 hitboxCenter = (Vector2)transform.position +
            new Vector2(FacingDirection * attackBoxOffset, 0f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, attackBoxSize * 1.5f, 0f);

        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null || enemy.CurrentState == EnemyBase.EnemyState.Dead) continue;

            float damage = characterStats.AttackPower * 2f;
            enemy.TakeDamage(damage, transform.position);
            enemy.ApplyStun(2f);
            SpawnDamagePopup(enemy.transform.position, damage, true);
        }

        Debug.Log("[Combat] ⚡ Minh sử dụng Đòn Sấm!");
    }

    /// <summary>Khoa: Dash - lao nhanh theo hướng di chuyển</summary>
    private void SpecialKhoa_Dash()
    {
        if (isDashing) return;
        StartCoroutine(DashCoroutine());
        Debug.Log("[Combat] 💨 Khoa sử dụng Dash!");
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;

        // Lao theo hướng đang nhìn
        Vector2 dashDir = new Vector2(FacingDirection, 0f);
        rb.linearVelocity = dashDir * dashForce;

        // Gây damage khi lao qua kẻ thù
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.8f);
            foreach (var hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy != null && enemy.CurrentState != EnemyBase.EnemyState.Dead)
                {
                    enemy.TakeDamage(characterStats.AttackPower * 0.5f, transform.position);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    /// <summary>Linh: Quét vòng tròn - đánh tất cả xung quanh</summary>
    private void SpecialLinh_Sweep()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 2.5f);

        int hitCount = 0;
        foreach (var hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null || enemy.CurrentState == EnemyBase.EnemyState.Dead) continue;

            enemy.TakeDamage(characterStats.AttackPower * 1.2f, transform.position);
            SpawnDamagePopup(enemy.transform.position, characterStats.AttackPower * 1.2f);
            hitCount++;
        }

        Debug.Log($"[Combat] 🌀 Linh quét vòng tròn! Trúng {hitCount} mục tiêu");
    }

    /// <summary>Trang: Hồi máu mạnh cho tất cả đồng đội trong 8 unit</summary>
    private void SpecialTrang_GroupHeal()
    {
        if (GameManager.Instance == null) return;

        float healAmount = characterStats.HealPower;
        int healed = 0;

        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null || character.IsDead) continue;
            if (character == playerController) continue;

            float dist = Vector2.Distance(transform.position, character.transform.position);
            if (dist <= 8f)
            {
                character.Heal(healAmount);
                SpawnDamagePopup(character.transform.position, healAmount, false, true);
                healed++;

                // Tăng gắn kết khi hồi máu
                int myIndex = GameManager.Instance.ActiveCharacterIndex;
                int targetIndex = GameManager.Instance.characters.IndexOf(character);
                BondingSystem bonding = GameManager.Instance.GetComponent<BondingSystem>();
                if (bonding != null && targetIndex >= 0)
                {
                    bonding.IncreaseBond(myIndex, targetIndex, 3f);
                }
            }
        }

        Debug.Log($"[Combat] 💚 Trang hồi {healAmount} HP cho {healed} đồng đội!");
    }

    // ===================================
    // HELPER
    // ===================================

    /// <summary>
    /// Trang: hồi 2 HP cho đồng đội gần nhất (passive mỗi đòn đánh).
    /// </summary>
    private void HealNearestAlly(float amount)
    {
        if (GameManager.Instance == null) return;

        PlayerController nearest = null;
        float minDist = float.MaxValue;

        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null || character.IsDead || character == playerController) continue;

            float dist = Vector2.Distance(transform.position, character.transform.position);
            if (dist < minDist && dist <= 10f)
            {
                minDist = dist;
                nearest = character;
            }
        }

        if (nearest != null)
        {
            nearest.Heal(amount);
            SpawnDamagePopup(nearest.transform.position, amount, false, true);
        }
    }

    /// <summary>
    /// Tăng gắn kết khi chiến đấu cùng (nhân vật khác ở gần).
    /// </summary>
    private void TriggerCombatBond()
    {
        if (GameManager.Instance == null) return;

        BondingSystem bonding = GameManager.Instance.GetComponent<BondingSystem>();
        if (bonding == null) return;

        int myIndex = GameManager.Instance.ActiveCharacterIndex;

        for (int i = 0; i < GameManager.Instance.characters.Count; i++)
        {
            if (i == myIndex) continue;
            var ally = GameManager.Instance.characters[i];
            if (ally == null || ally.IsDead) continue;

            float dist = Vector2.Distance(transform.position, ally.transform.position);
            if (dist <= 10f)
            {
                bonding.IncreaseBond(myIndex, i, 1f);
            }
        }
    }

    /// <summary>
    /// Tạo damage number popup nổi lên.
    /// </summary>
    public void SpawnDamagePopup(Vector3 position, float value, bool isCritical = false, bool isHeal = false)
    {
        // FIX #16: Ưu tiên dùng UIThemeManager pool (tránh rác GameObject)
        if (UIThemeManager.Instance != null)
        {
            UIThemeManager.Instance.ShowDamageNumber(value, position, isHeal, isCritical);
            return;
        }

        // Fallback: tạo trực tiếp nếu UIThemeManager chưa có
        GameObject popup = new GameObject("DamagePopup");
        popup.transform.position = position + new Vector3(
            Random.Range(-0.3f, 0.3f), 0.5f, 0f);

        TextMeshPro tmp = popup.AddComponent<TextMeshPro>();
        tmp.text      = isHeal ? $"+{value:F0}" : $"-{value:F0}";
        tmp.color     = isHeal ? Color.green : (isCritical ? Color.yellow : Color.white);
        tmp.fontSize  = isCritical ? 6f : 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 200;

        StartCoroutine(AnimateDamagePopup(popup));
    }

    /// <summary>
    /// Animation cho damage popup: nổi lên + fade out.
    /// </summary>
    private IEnumerator AnimateDamagePopup(GameObject popup)
    {
        if (popup == null) yield break;

        TextMeshPro tmp = popup.GetComponent<TextMeshPro>();
        Vector3 startPos = popup.transform.position;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration && popup != null)
        {
            float t = elapsed / duration;

            // Nổi lên
            popup.transform.position = startPos + new Vector3(0f, t * 1.5f, 0f);

            // Fade out
            if (tmp != null)
            {
                Color c = tmp.color;
                c.a = 1f - t;
                tmp.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (popup != null)
            Destroy(popup);
    }

    /// <summary>
    /// Vẽ gizmos hitbox tấn công trong Editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Vector2 hitboxCenter = (Vector2)transform.position +
            new Vector2((spriteRenderer != null && spriteRenderer.flipX ? -1 : 1) * attackBoxOffset, 0f);

        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireCube(hitboxCenter, attackBoxSize);
    }
}
