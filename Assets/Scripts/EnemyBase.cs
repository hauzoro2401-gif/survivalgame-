using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// EnemyBase - Lớp trừu tượng cho tất cả kẻ thù trong Vực Thẳm.
/// Quản lý: state machine, patrol, chase, attack, hurt, death.
/// Các enemy cụ thể kế thừa và override hành vi đặc biệt.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public abstract class EnemyBase : MonoBehaviour
{
    // === TRẠNG THÁI ===
    public enum EnemyState { Idle, Patrol, Chase, Attack, Hurt, Dead }

    [Header("Trạng thái hiện tại")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;

    /// <summary>Trạng thái hiện tại của kẻ thù</summary>
    public EnemyState CurrentState => currentState;

    // === CHỈ SỐ CƠ BẢN ===
    [Header("Chỉ số")]
    [SerializeField] protected float maxHP = 50f;
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float attackPower = 10f;
    [SerializeField] protected float detectionRange = 8f;
    [SerializeField] protected float attackRange = 1.5f;
    [SerializeField] protected float attackCooldown = 1.5f;

    // === PATROL ===
    [Header("Tuần tra")]
    [SerializeField] [Tooltip("Khoảng cách tuần tra qua lại (unit)")]
    protected float patrolDistance = 5f;

    [SerializeField] [Tooltip("Thời gian dừng ở mỗi điểm tuần tra (giây)")]
    protected float patrolWaitTime = 2f;

    // === KNOCKBACK ===
    [Header("Knockback khi bị đánh")]
    [SerializeField] protected float knockbackForce = 5f;
    [SerializeField] protected float hurtDuration = 0.3f;

    // === DROP ITEMS ===
    [Header("Drop khi chết")]
    [SerializeField] [Tooltip("Danh sách item có thể drop")]
    protected List<ItemData> possibleDrops = new List<ItemData>();

    [SerializeField] [Tooltip("Xác suất drop item (0-1)")]
    protected float dropChance = 0.5f;

    [SerializeField] [Tooltip("Thời gian xác chết biến mất (giây)")]
    protected float deathFadeTime = 3f;

    // === BUFF BAN ĐÊM ===
    [Header("Buff ban đêm")]
    [SerializeField] protected float nightDetectionMultiplier = 2f;
    [SerializeField] protected float nightSpeedMultiplier = 1.3f;

    // === REFERENCES ===
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D enemyCollider;

    // === TRẠNG THÁI NỘI BỘ ===
    protected float currentHP;
    protected float attackTimer = 0f;
    protected float stunTimer = 0f;
    protected bool isStunned = false;
    protected Transform currentTarget;      // Nhân vật đang đuổi theo
    protected Vector2 patrolPointA;          // Điểm tuần tra A
    protected Vector2 patrolPointB;          // Điểm tuần tra B
    protected bool movingToB = true;         // Đang đi về điểm B
    protected float patrolWaitTimer = 0f;    // Timer chờ tại điểm tuần tra
    protected bool isNight = false;          // Đang ban đêm

    // === PROPERTIES ===
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => currentState == EnemyState.Dead;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();

        // Cấu hình Rigidbody2D
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        currentHP = maxHP;

        // Thiết lập điểm tuần tra dựa trên vị trí spawn
        patrolPointA = (Vector2)transform.position - new Vector2(patrolDistance / 2f, 0f);
        patrolPointB = (Vector2)transform.position + new Vector2(patrolDistance / 2f, 0f);
    }

    protected virtual void Start()
    {
        // Đăng ký event ngày/đêm
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart += OnDayStarted;
            dayNight.OnNightStart += OnNightStarted;
            isNight = dayNight.IsNight;
        }

        // Bắt đầu tuần tra
        SetState(EnemyState.Patrol);
    }

    protected virtual void Update()
    {
        // Không cập nhật khi game pause
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Xử lý stun
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                if (currentState != EnemyState.Dead)
                    SetState(EnemyState.Chase);
            }
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Giảm attack cooldown
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;

        // State machine
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Hurt:
                // Hurt state tự hết sau hurtDuration (xử lý trong coroutine)
                break;
            case EnemyState.Dead:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    protected virtual void OnDestroy()
    {
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart -= OnDayStarted;
            dayNight.OnNightStart -= OnNightStarted;
        }
    }

    // ===================================
    // STATE MACHINE
    // ===================================

    /// <summary>
    /// Chuyển sang state mới.
    /// </summary>
    protected void SetState(EnemyState newState)
    {
        if (currentState == EnemyState.Dead && newState != EnemyState.Dead) return;
        currentState = newState;
        OnStateChanged(newState);
    }

    /// <summary>Override để thêm hành vi khi chuyển state</summary>
    protected virtual void OnStateChanged(EnemyState newState) { }

    /// <summary>
    /// Idle: đứng yên, quét tìm nhân vật.
    /// </summary>
    protected virtual void UpdateIdle()
    {
        rb.linearVelocity = Vector2.zero;

        // Tìm nhân vật trong tầm phát hiện
        if (DetectPlayer())
        {
            SetState(EnemyState.Chase);
        }
        else
        {
            // Chuyển sang tuần tra sau 1 giây
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                SetState(EnemyState.Patrol);
            }
        }
    }

    /// <summary>
    /// Patrol: đi qua lại giữa 2 điểm tuần tra.
    /// </summary>
    protected virtual void UpdatePatrol()
    {
        // Kiểm tra nhân vật trong tầm phát hiện
        if (DetectPlayer())
        {
            SetState(EnemyState.Chase);
            return;
        }

        // Di chuyển đến điểm tuần tra
        Vector2 targetPoint = movingToB ? patrolPointB : patrolPointA;
        Vector2 direction = (targetPoint - (Vector2)transform.position).normalized;
        float currentSpeed = GetCurrentMoveSpeed();

        rb.linearVelocity = direction * currentSpeed;

        // Flip sprite theo hướng di chuyển
        if (direction.x > 0.01f) spriteRenderer.flipX = false;
        else if (direction.x < -0.01f) spriteRenderer.flipX = true;

        // Kiểm tra đã đến điểm tuần tra chưa
        float distToTarget = Vector2.Distance(transform.position, targetPoint);
        if (distToTarget < 0.5f)
        {
            movingToB = !movingToB;
            patrolWaitTimer = patrolWaitTime;
            SetState(EnemyState.Idle);
        }
    }

    /// <summary>
    /// Chase: đuổi theo nhân vật gần nhất.
    /// </summary>
    protected virtual void UpdateChase()
    {
        // Tìm lại target nếu bị mất
        if (currentTarget == null || !IsTargetAlive())
        {
            if (!DetectPlayer())
            {
                SetState(EnemyState.Patrol);
                return;
            }
        }

        if (currentTarget == null) return;

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Nếu ra ngoài detection range → quay lại patrol
        float effectiveDetection = GetEffectiveDetectionRange();
        if (distToTarget > effectiveDetection * 1.5f)
        {
            currentTarget = null;
            SetState(EnemyState.Patrol);
            return;
        }

        // Nếu vào attack range → tấn công
        if (distToTarget <= attackRange)
        {
            SetState(EnemyState.Attack);
            return;
        }

        // Di chuyển về phía target
        Vector2 direction = ((Vector2)currentTarget.position - (Vector2)transform.position).normalized;
        float currentSpeed = GetCurrentMoveSpeed();

        rb.linearVelocity = direction * currentSpeed;

        // Flip sprite
        if (direction.x > 0.01f) spriteRenderer.flipX = false;
        else if (direction.x < -0.01f) spriteRenderer.flipX = true;
    }

    /// <summary>
    /// Attack: tấn công khi nhân vật trong tầm.
    /// </summary>
    protected virtual void UpdateAttack()
    {
        rb.linearVelocity = Vector2.zero;

        if (currentTarget == null || !IsTargetAlive())
        {
            SetState(EnemyState.Patrol);
            return;
        }

        float distToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Nếu target ra khỏi attack range → đuổi tiếp
        if (distToTarget > attackRange * 1.5f)
        {
            SetState(EnemyState.Chase);
            return;
        }

        // Tấn công nếu hết cooldown
        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
    }

    // ===================================
    // HÀNH VI CHIẾN ĐẤU
    // ===================================

    /// <summary>
    /// Thực hiện đòn tấn công. Override trong subclass cho hành vi đặc biệt.
    /// </summary>
    protected virtual void PerformAttack()
    {
        if (currentTarget == null) return;

        PlayerController targetPC = currentTarget.GetComponent<PlayerController>();
        if (targetPC == null || targetPC.IsDead) return;

        // Gây sát thương (có tính defense từ CharacterStats)
        float damage = attackPower;
        CharacterStats targetStats = currentTarget.GetComponent<CharacterStats>();
        if (targetStats != null)
        {
            damage = targetStats.CalculateDamageTaken(attackPower);
        }

        targetPC.TakeDamage(damage);

        Debug.Log($"[Enemy] {gameObject.name} tấn công {targetPC.characterName} gây {damage:F0} damage!");
    }

    /// <summary>
    /// Nhận sát thương từ nhân vật.
    /// </summary>
    public virtual void TakeDamage(float damage, Vector2 attackerPosition)
    {
        if (IsDead) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        if (currentHP <= 0f)
        {
            Die();
        }
        else
        {
            // Chuyển sang Hurt state + knockback
            StartCoroutine(HurtCoroutine(attackerPosition));

            // Callback cho subclass (VD: Gremvak gọi bầy)
            OnTakeDamageCallback(damage, attackerPosition);
        }
    }

    /// <summary>Override trong subclass để thêm hành vi khi bị đánh</summary>
    protected virtual void OnTakeDamageCallback(float damage, Vector2 attackerPosition) { }

    /// <summary>
    /// Coroutine xử lý Hurt state: knockback rồi quay lại Chase.
    /// </summary>
    private IEnumerator HurtCoroutine(Vector2 attackerPosition)
    {
        SetState(EnemyState.Hurt);

        // Knockback nhẹ - đẩy ra xa khỏi attacker
        Vector2 knockDir = ((Vector2)transform.position - attackerPosition).normalized;
        rb.linearVelocity = knockDir * knockbackForce;

        // Hiệu ứng nhấp nháy đỏ
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;

            yield return new WaitForSeconds(hurtDuration);

            if (spriteRenderer != null)
                spriteRenderer.color = original;
        }
        else
        {
            yield return new WaitForSeconds(hurtDuration);
        }

        rb.linearVelocity = Vector2.zero;

        // Quay lại Chase nếu còn sống
        if (!IsDead)
        {
            SetState(EnemyState.Chase);
        }
    }

    /// <summary>
    /// Áp dụng stun (choáng) - dừng hành động trong duration giây.
    /// </summary>
    public void ApplyStun(float duration)
    {
        if (IsDead) return;

        isStunned = true;
        stunTimer = duration;
        rb.linearVelocity = Vector2.zero;

        Debug.Log($"[Enemy] {gameObject.name} bị choáng {duration}s!");
    }

    /// <summary>
    /// Xử lý chết: drop item, fade out, hủy object.
    /// </summary>
    protected virtual void Die()
    {
        if (currentState == EnemyState.Dead) return;

        SetState(EnemyState.Dead);
        rb.linearVelocity = Vector2.zero;

        if (enemyCollider != null)
            enemyCollider.enabled = false;

        Debug.Log($"[Enemy] {gameObject.name} đã bị tiêu diệt!");

        // Drop item ngẫu nhiên
        EndingManager.EnemiesKilled++;
        DropRandomItem();

        // Fade out rồi hủy
        StartCoroutine(DeathFadeCoroutine());
    }

    /// <summary>
    /// Drop item ngẫu nhiên vào shared inventory.
    /// </summary>
    private void DropRandomItem()
    {
        if (possibleDrops.Count == 0) return;
        if (Random.value > dropChance) return;

        ItemData dropItem = possibleDrops[Random.Range(0, possibleDrops.Count)];

        Inventory inventory = null;
        if (GameManager.Instance != null)
            inventory = GameManager.Instance.GetComponent<Inventory>();

        if (inventory != null && dropItem != null)
        {
            inventory.AddItem(dropItem, 1);
            Debug.Log($"[Enemy] {gameObject.name} drop: {dropItem.itemName}!");
        }
    }

    /// <summary>
    /// Fade out xác chết rồi hủy object.
    /// </summary>
    private IEnumerator DeathFadeCoroutine()
    {
        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < deathFadeTime)
        {
            if (spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / deathFadeTime);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // ===================================
    // PHÁT HIỆN NHÂN VẬT
    // ===================================

    /// <summary>
    /// Quét tìm nhân vật gần nhất trong tầm phát hiện.
    /// </summary>
    protected bool DetectPlayer()
    {
        if (GameManager.Instance == null) return false;

        float effectiveRange = GetEffectiveDetectionRange();
        float closestDist = float.MaxValue;
        Transform closestTarget = null;

        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null || character.IsDead) continue;

            float dist = Vector2.Distance(transform.position, character.transform.position);
            if (dist <= effectiveRange && dist < closestDist)
            {
                closestDist = dist;
                closestTarget = character.transform;
            }
        }

        if (closestTarget != null)
        {
            currentTarget = closestTarget;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra target hiện tại còn sống không.
    /// </summary>
    protected bool IsTargetAlive()
    {
        if (currentTarget == null) return false;
        PlayerController pc = currentTarget.GetComponent<PlayerController>();
        return pc != null && !pc.IsDead;
    }

    // ===================================
    // HELPER - TỐC ĐỘ & BUFF ĐÊM
    // ===================================

    /// <summary>Tốc độ di chuyển thực (tính buff đêm)</summary>
    protected float GetCurrentMoveSpeed()
    {
        return isNight ? moveSpeed * nightSpeedMultiplier : moveSpeed;
    }

    /// <summary>Tầm phát hiện thực (tính buff đêm)</summary>
    protected float GetEffectiveDetectionRange()
    {
        return isNight ? detectionRange * nightDetectionMultiplier : detectionRange;
    }

    /// <summary>Callback khi ban ngày bắt đầu</summary>
    protected virtual void OnDayStarted()
    {
        isNight = false;
    }

    /// <summary>Callback khi ban đêm bắt đầu</summary>
    protected virtual void OnNightStarted()
    {
        isNight = true;
    }

    /// <summary>
    /// Áp dụng buff độ khó theo ngày (gọi từ EnemySpawner).
    /// </summary>
    public void ApplyDifficultyScaling(float hpMultiplier, float dmgMultiplier)
    {
        maxHP *= hpMultiplier;
        currentHP = maxHP;
        attackPower *= dmgMultiplier;
    }

    // === GIZMOS ===
    protected virtual void OnDrawGizmosSelected()
    {
        // Vòng detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vòng attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Đường tuần tra
        Gizmos.color = Color.cyan;
        Vector2 pA = Application.isPlaying ? patrolPointA : (Vector2)transform.position - new Vector2(patrolDistance / 2f, 0f);
        Vector2 pB = Application.isPlaying ? patrolPointB : (Vector2)transform.position + new Vector2(patrolDistance / 2f, 0f);
        Gizmos.DrawLine(pA, pB);
    }
}
