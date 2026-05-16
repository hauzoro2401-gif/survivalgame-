using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerController - Điều khiển di chuyển 2D cho từng nhân vật.
/// Hỗ trợ WASD / Arrow Keys, flip sprite, animation states.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    // === THÔNG TIN NHÂN VẬT ===
    [Header("Thông tin nhân vật")]
    [Tooltip("Tên nhân vật (Minh, Linh, Khoa, Trang)")]
    public string characterName = "Nhân vật";

    [Tooltip("Vai trò: Tank, Crafter, Scout, Healer")]
    public string characterRole = "None";

    // === CHỈ SỐ NHÂN VẬT ===
    [Header("Chỉ số")]
    [Tooltip("Tốc độ di chuyển cơ bản")]
    public float speed = 5f;

    [Tooltip("Máu hiện tại")]
    public float health = 100f;

    [Tooltip("Máu tối đa")]
    public float maxHealth = 100f;

    // === TRẠNG THÁI ===
    /// <summary>Nhân vật có đang được người chơi điều khiển không</summary>
    public bool IsControlled { get; private set; } = false;

    /// <summary>Nhân vật đã chết chưa</summary>
    public bool IsDead { get; private set; } = false;

    /// <summary>
    /// Hệ số nhân tốc độ (1.0 = bình thường, 0.5 = chậm 50%).
    /// Được SurvivalSystem điều chỉnh khi energy = 0.
    /// </summary>
    [HideInInspector]
    public float speedMultiplier = 1f;

    // === REFERENCES ===
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // === INPUT ===
    private Vector2 moveInput;

    // === ANIMATOR HASHES ===
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimHurt = Animator.StringToHash("Hurt");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Cấu hình Rigidbody2D cho top-down 2D
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (!IsControlled || IsDead) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Đọc input WASD / Arrow Keys bằng Input System
        moveInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1f;
        }

        // Chuẩn hóa để di chuyển chéo không nhanh hơn
        if (moveInput.sqrMagnitude > 1f)
            moveInput.Normalize();

        FlipSprite();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        if (!IsControlled || IsDead) return;

        float actualSpeed = speed * speedMultiplier;
        rb.linearVelocity = moveInput * actualSpeed;
    }

    /// <summary>Flip sprite theo hướng ngang</summary>
    private void FlipSprite()
    {
        if (moveInput.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveInput.x < -0.01f)
            spriteRenderer.flipX = true;
    }

    /// <summary>Cập nhật animation (idle/run)</summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetFloat(AnimSpeed, moveInput.sqrMagnitude);
    }

    /// <summary>Bật/tắt quyền điều khiển nhân vật</summary>
    public void SetControlled(bool controlled)
    {
        IsControlled = controlled;
        if (!controlled)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            if (animator != null)
                animator.SetFloat(AnimSpeed, 0f);
        }
    }

    /// <summary>Nhận sát thương</summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        health -= damage;
        health = Mathf.Max(health, 0f);

        if (animator != null)
            animator.SetTrigger(AnimHurt);

        if (health <= 0f)
            Die();
    }

    /// <summary>Hồi máu</summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        health += amount;
        health = Mathf.Min(health, maxHealth);
    }

    /// <summary>Xử lý chết</summary>
    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        health = 0f;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetTrigger(AnimDie);

        SetControlled(false);

        if (GameManager.Instance != null)
            GameManager.Instance.CheckGameOver();
    }

    /// <summary>Hồi sinh nhân vật</summary>
    public void Revive(float healthAmount)
    {
        if (!IsDead) return;
        IsDead = false;
        health = Mathf.Min(healthAmount, maxHealth);
    }

    public void RestoreState(float savedHealth, float savedMaxHealth, bool savedIsDead)
    {
        maxHealth = Mathf.Max(1f, savedMaxHealth);
        health = Mathf.Clamp(savedHealth, 0f, maxHealth);
        IsDead = savedIsDead || health <= 0f;

        moveInput = Vector2.zero;
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        SetControlled(false);
    }
}
