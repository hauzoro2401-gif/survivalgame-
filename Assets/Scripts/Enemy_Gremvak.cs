using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enemy_Gremvak - Quái vật nhỏ đi thành bầy.
/// Khi 1 con bị tấn công → báo động cả bầy trong 8 unit.
/// Đặc biệt: nhảy vào mặt gây choáng 0.5s.
/// HP 40, speed 4, attack 8.
/// </summary>
public class Enemy_Gremvak : EnemyBase
{
    // === ĐẶC BIỆT GREMVAK ===
    [Header("Gremvak - Đặc biệt")]
    [SerializeField] [Tooltip("Bán kính báo động bầy (unit)")]
    private float alertRadius = 8f;

    [SerializeField] [Tooltip("Thời gian choáng khi nhảy vào mặt (giây)")]
    private float leapStunDuration = 0.5f;

    [SerializeField] [Tooltip("Tốc độ nhảy vào target")]
    private float leapSpeed = 12f;

    [SerializeField] [Tooltip("Khoảng cách để kích hoạt nhảy")]
    private float leapRange = 3f;

    [SerializeField] [Tooltip("Cooldown nhảy (giây)")]
    private float leapCooldown = 5f;

    // === TRẠNG THÁI ===
    private float leapTimer = 0f;
    private bool isLeaping = false;
    private bool hasAlerted = false; // Đã báo động bầy chưa (mỗi lần bị đánh chỉ báo 1 lần)

    protected override void Awake()
    {
        base.Awake();

        // Chỉ số Gremvak
        maxHP = 40f;
        moveSpeed = 4f;
        attackPower = 8f;
        detectionRange = 7f;
        attackRange = 1.2f;
        attackCooldown = 1.2f;

        currentHP = maxHP;

        // Tag cho spawner nhận diện
        gameObject.tag = "Enemy";
    }

    protected override void Update()
    {
        base.Update();

        // Giảm leap cooldown
        if (leapTimer > 0f)
            leapTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Override Chase: thêm cơ hội nhảy vào target.
    /// </summary>
    protected override void UpdateChase()
    {
        base.UpdateChase();

        if (currentTarget == null || isLeaping) return;

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Nhảy vào mặt khi trong leap range và hết cooldown
        if (distToTarget <= leapRange && leapTimer <= 0f)
        {
            StartCoroutine(LeapAttack());
        }
    }

    /// <summary>
    /// Đòn đặc biệt: nhảy vào mặt nhân vật gây choáng.
    /// </summary>
    private System.Collections.IEnumerator LeapAttack()
    {
        if (currentTarget == null) yield break;

        isLeaping = true;
        leapTimer = leapCooldown;

        Debug.Log($"[Gremvak] {gameObject.name} nhảy vào mặt!");

        // Lao về phía target
        float elapsed = 0f;
        float maxLeapTime = 0.4f;

        while (elapsed < maxLeapTime && currentTarget != null)
        {
            Vector2 dir = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * leapSpeed;

            // Kiểm tra đã chạm target chưa
            float dist = Vector2.Distance(transform.position, currentTarget.position);
            if (dist < 0.8f)
            {
                // Trúng! Gây damage + choáng
                PlayerController targetPC = currentTarget.GetComponent<PlayerController>();
                if (targetPC != null && !targetPC.IsDead)
                {
                    CharacterStats targetStats = currentTarget.GetComponent<CharacterStats>();
                    float damage = attackPower * 1.5f;
                    if (targetStats != null)
                        damage = targetStats.CalculateDamageTaken(damage);

                    targetPC.TakeDamage(damage);

                    // Choáng nhân vật bằng cách tạm dừng speed
                    StartCoroutine(StunPlayer(targetPC, leapStunDuration));

                    Debug.Log($"[Gremvak] Gây choáng {leapStunDuration}s cho {targetPC.characterName}!");
                }
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isLeaping = false;
    }

    /// <summary>
    /// Choáng nhân vật tạm thời (giảm speed về 0).
    /// </summary>
    private System.Collections.IEnumerator StunPlayer(PlayerController player, float duration)
    {
        float originalMultiplier = player.speedMultiplier;
        player.speedMultiplier = 0f;

        yield return new WaitForSeconds(duration);

        // Trả lại speed bình thường
        if (player != null)
            player.speedMultiplier = originalMultiplier;
    }

    /// <summary>
    /// Khi bị đánh → báo động cả bầy trong radius.
    /// </summary>
    protected override void OnTakeDamageCallback(float damage, Vector2 attackerPosition)
    {
        if (hasAlerted) return;
        hasAlerted = true;

        // Tìm tất cả Gremvak gần đây
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, alertRadius);

        int alertCount = 0;
        foreach (var col in nearbyEnemies)
        {
            if (col.gameObject == gameObject) continue;

            Enemy_Gremvak ally = col.GetComponent<Enemy_Gremvak>();
            if (ally != null && !ally.IsDead && ally.CurrentState != EnemyState.Chase)
            {
                // Chuyển đồng bọn sang Chase
                ally.AlertByAlly(attackerPosition);
                alertCount++;
            }
        }

        if (alertCount > 0)
        {
            Debug.Log($"[Gremvak] {gameObject.name} báo động {alertCount} đồng bọn!");
        }

        // Reset flag sau 10 giây (để có thể báo động lại)
        StartCoroutine(ResetAlertFlag());
    }

    /// <summary>
    /// Được đồng bọn báo động → chuyển sang Chase.
    /// </summary>
    public void AlertByAlly(Vector2 attackerPosition)
    {
        if (IsDead) return;

        // Tìm nhân vật gần nhất để đuổi
        DetectPlayer();

        if (currentTarget == null)
        {
            // Nếu không detect được ai, đi về hướng attacker
            Transform closest = FindClosestCharacter();
            if (closest != null)
                currentTarget = closest;
        }

        if (currentTarget != null)
        {
            SetState(EnemyState.Chase);
        }
    }

    /// <summary>
    /// Tìm nhân vật gần nhất (không giới hạn detection range).
    /// </summary>
    private Transform FindClosestCharacter()
    {
        if (GameManager.Instance == null) return null;

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null || character.IsDead) continue;

            float dist = Vector2.Distance(transform.position, character.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = character.transform;
            }
        }

        return closest;
    }

    private System.Collections.IEnumerator ResetAlertFlag()
    {
        yield return new WaitForSeconds(10f);
        hasAlerted = false;
    }
}
