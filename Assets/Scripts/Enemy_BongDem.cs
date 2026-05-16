using UnityEngine;
using System.Collections;

/// <summary>
/// Enemy_BongDem (Bóng Đêm) - Kẻ thù chỉ xuất hiện ban đêm.
/// Ban ngày: ẩn hoàn toàn (alpha 0, collider off).
/// Đặc biệt: tàng hình khi không tấn công (alpha 0.2).
/// Chỉ bị tổn thương khi đang tấn công (alpha = 1).
/// HP 25, speed 6, attack 15.
/// </summary>
public class Enemy_BongDem : EnemyBase
{
    // === ĐẶC BIỆT BÓNG ĐÊM ===
    [Header("Bóng Đêm - Đặc biệt")]
    [SerializeField] [Tooltip("Alpha khi tàng hình (di chuyển/idle)")]
    private float stealthAlpha = 0.2f;

    [SerializeField] [Tooltip("Alpha khi tấn công (lộ diện)")]
    private float attackAlpha = 1f;

    [SerializeField] [Tooltip("Tốc độ thay đổi alpha")]
    private float alphaLerpSpeed = 5f;

    // === TRẠNG THÁI ===
    private float targetAlpha = 0f;     // Alpha đích hiện tại
    private bool isActiveAtNight = false; // Đang hoạt động (ban đêm)
    private bool isVulnerable = false;   // Có thể bị đánh (khi alpha = 1)
    private Color baseColor;

    protected override void Awake()
    {
        base.Awake();

        // Chỉ số Bóng Đêm
        maxHP = 25f;
        moveSpeed = 6f;
        attackPower = 15f;
        detectionRange = 10f;
        attackRange = 1.5f;
        attackCooldown = 2f;

        currentHP = maxHP;

        // Tag
        gameObject.tag = "Enemy";

        // Lưu màu gốc
        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
    }

    protected override void Start()
    {
        base.Start();

        // Kiểm tra xem hiện tại có phải ban đêm không
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            if (dayNight.IsNight)
            {
                ActivateAtNight();
            }
            else
            {
                DeactivateAtDay();
            }
        }
        else
        {
            // Mặc định hoạt động nếu không tìm thấy DayNightCycle
            ActivateAtNight();
        }
    }

    protected override void Update()
    {
        // Không update nếu đang ẩn (ban ngày)
        if (!isActiveAtNight && !IsDead) return;

        base.Update();

        // Cập nhật alpha dựa trên state
        UpdateAlpha();
    }

    // ===================================
    // NGÀY / ĐÊM
    // ===================================

    /// <summary>
    /// Khi ban đêm bắt đầu → kích hoạt Bóng Đêm.
    /// </summary>
    protected override void OnNightStarted()
    {
        base.OnNightStarted();
        ActivateAtNight();
    }

    /// <summary>
    /// Khi ban ngày bắt đầu → Bóng Đêm biến mất.
    /// </summary>
    protected override void OnDayStarted()
    {
        base.OnDayStarted();
        DeactivateAtDay();
    }

    /// <summary>
    /// Kích hoạt ban đêm: hiện ra với alpha thấp (tàng hình).
    /// </summary>
    private void ActivateAtNight()
    {
        isActiveAtNight = true;
        targetAlpha = stealthAlpha;
        isVulnerable = false;

        // Bật collider
        if (enemyCollider != null)
            enemyCollider.enabled = true;

        // Bắt đầu tuần tra
        SetState(EnemyState.Patrol);

        Debug.Log($"[BóngĐêm] {gameObject.name} xuất hiện trong bóng tối...");
    }

    /// <summary>
    /// Ẩn khi trời sáng: biến mất hoàn toàn.
    /// </summary>
    private void DeactivateAtDay()
    {
        isActiveAtNight = false;
        targetAlpha = 0f;
        isVulnerable = false;

        // Tắt collider
        if (enemyCollider != null)
            enemyCollider.enabled = false;

        // Dừng di chuyển
        rb.linearVelocity = Vector2.zero;
        SetState(EnemyState.Idle);

        Debug.Log($"[BóngĐêm] {gameObject.name} tan biến khi bình minh ló dạng...");

        // Hủy object khi trời sáng (trả về pool hoặc destroy)
        StartCoroutine(FadeAndDestroy());
    }

    /// <summary>
    /// Fade out rồi hủy khi trời sáng.
    /// </summary>
    private IEnumerator FadeAndDestroy()
    {
        float elapsed = 0f;
        float fadeDuration = 2f;

        while (elapsed < fadeDuration)
        {
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(spriteRenderer.color.a, 0f, elapsed / fadeDuration);
                spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // ===================================
    // TÀNG HÌNH & VULNERABILITY
    // ===================================

    /// <summary>
    /// Cập nhật alpha sprite dựa trên state hiện tại.
    /// Tàng hình khi di chuyển, lộ diện khi tấn công.
    /// </summary>
    private void UpdateAlpha()
    {
        if (spriteRenderer == null) return;

        // Xác định target alpha theo state
        switch (CurrentState)
        {
            case EnemyState.Attack:
                // Lộ diện khi tấn công → có thể bị đánh
                targetAlpha = attackAlpha;
                isVulnerable = true;
                break;

            case EnemyState.Hurt:
                // Bị đánh → lộ diện tạm thời
                targetAlpha = attackAlpha;
                isVulnerable = true;
                break;

            case EnemyState.Dead:
                targetAlpha = 0f;
                break;

            default:
                // Patrol, Chase, Idle → tàng hình
                targetAlpha = stealthAlpha;
                isVulnerable = false;
                break;
        }

        // Lerp alpha mượt mà
        Color c = spriteRenderer.color;
        float currentAlpha = Mathf.Lerp(c.a, targetAlpha, alphaLerpSpeed * Time.deltaTime);
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, currentAlpha);
    }

    /// <summary>
    /// Override TakeDamage: chỉ nhận sát thương khi đang lộ diện (alpha = 1).
    /// </summary>
    public override void TakeDamage(float damage, Vector2 attackerPosition)
    {
        if (!isVulnerable)
        {
            Debug.Log($"[BóngĐêm] {gameObject.name} đang tàng hình! Không thể gây sát thương.");
            return;
        }

        // Nhận damage bình thường khi lộ diện
        base.TakeDamage(damage, attackerPosition);
    }

    /// <summary>
    /// Override PerformAttack: khi tấn công, lộ diện hoàn toàn.
    /// </summary>
    protected override void PerformAttack()
    {
        // Lộ diện ngay khi chuẩn bị tấn công
        targetAlpha = attackAlpha;
        isVulnerable = true;

        base.PerformAttack();

        // Sau khi đánh xong, bắt đầu đếm thời gian trước khi tàng hình lại
        StartCoroutine(DelayedStealth());
    }

    /// <summary>
    /// Sau khi tấn công, chờ 1.5 giây rồi tàng hình lại.
    /// Trong thời gian chờ vẫn có thể bị đánh.
    /// </summary>
    private IEnumerator DelayedStealth()
    {
        yield return new WaitForSeconds(1.5f);

        // Chỉ tàng hình lại nếu không đang tấn công hoặc bị đánh
        if (CurrentState != EnemyState.Attack && CurrentState != EnemyState.Hurt)
        {
            isVulnerable = false;
            targetAlpha = stealthAlpha;
        }
    }

    /// <summary>
    /// Override callback state change để quản lý vulnerability.
    /// </summary>
    protected override void OnStateChanged(EnemyState newState)
    {
        if (newState == EnemyState.Attack)
        {
            isVulnerable = true;
        }
    }
}
