using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HealthBarUI - Thanh máu nổi phía trên đầu nhân vật và kẻ thù.
/// Tự ẩn khi HP đầy, hiện khi bị damage.
/// Smooth lerp khi máu giảm.
/// Hiện icon trạng thái (đói, khát, mệt) cho nhân vật.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    // === CẤU HÌNH ===
    [Header("Cấu hình thanh máu")]
    [SerializeField] [Tooltip("Offset phía trên đầu (unit)")]
    private float yOffset = 1.2f;

    [SerializeField] [Tooltip("Chiều rộng thanh máu (unit)")]
    private float barWidth = 1f;

    [SerializeField] [Tooltip("Chiều cao thanh máu (unit)")]
    private float barHeight = 0.12f;

    [SerializeField] [Tooltip("Tốc độ lerp thanh máu")]
    private float lerpSpeed = 5f;

    [SerializeField] [Tooltip("Thời gian hiện thanh máu sau khi bị đánh (giây)")]
    private float showDuration = 5f;

    // === MÀU SẮC ===
    [Header("Màu sắc")]
    [SerializeField] private Color playerBarColor = new Color(0.2f, 0.8f, 0.3f);  // Xanh lá
    [SerializeField] private Color enemyBarColor = new Color(0.9f, 0.2f, 0.2f);   // Đỏ
    [SerializeField] private Color lerpBarColor = new Color(0.9f, 0.7f, 0.2f);     // Vàng (lerp)
    [SerializeField] private Color bgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f); // Nền tối

    // === TRẠNG THÁI ===
    private float currentFillAmount = 1f;   // Thanh máu thực tế (lerp)
    private float targetFillAmount = 1f;    // Thanh máu đích
    private float previousHP;               // HP frame trước (phát hiện thay đổi)
    private float showTimer = 0f;           // Timer hiển thị
    private bool isVisible = false;
    private bool isPlayerBar = false;       // True = nhân vật, False = kẻ thù

    // === REFERENCES ===
    private PlayerController playerController;
    private EnemyBase enemyBase;
    private SurvivalSystem survivalSystem;

    // === UI OBJECTS (World Space) ===
    private GameObject barContainer;
    private SpriteRenderer bgSprite;
    private SpriteRenderer fillSprite;
    private SpriteRenderer lerpSprite;

    // === ICON TRẠNG THÁI ===
    private SpriteRenderer hungerIcon;
    private SpriteRenderer thirstIcon;
    private SpriteRenderer energyIcon;

    private void Start()
    {
        // Xác định loại entity
        playerController = GetComponent<PlayerController>();
        enemyBase = GetComponent<EnemyBase>();
        survivalSystem = GetComponent<SurvivalSystem>();

        isPlayerBar = playerController != null;

        // Lấy HP ban đầu
        previousHP = GetCurrentHP();
        targetFillAmount = 1f;
        currentFillAmount = 1f;

        // Tạo thanh máu bằng SpriteRenderer (World Space)
        CreateHealthBar();

        // Ẩn ban đầu (HP đầy)
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (barContainer == null) return;

        // Cập nhật vị trí (theo entity)
        barContainer.transform.position = transform.position + new Vector3(0f, yOffset, 0f);

        // Không xoay theo entity
        barContainer.transform.rotation = Quaternion.identity;

        // Cập nhật HP
        float currentHP = GetCurrentHP();
        float maxHP = GetMaxHP();

        if (maxHP <= 0f) return;

        targetFillAmount = currentHP / maxHP;

        // Phát hiện HP thay đổi → hiện thanh máu
        if (Mathf.Abs(currentHP - previousHP) > 0.01f)
        {
            showTimer = showDuration;
            SetVisible(true);
        }

        previousHP = currentHP;

        // Lerp thanh máu mượt
        currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, lerpSpeed * Time.deltaTime);

        // Cập nhật visual
        UpdateBarVisual();

        // Cập nhật icon trạng thái (chỉ nhân vật)
        if (isPlayerBar && survivalSystem != null)
        {
            UpdateStatusIcons();
        }

        // Timer ẩn thanh máu
        if (showTimer > 0f)
        {
            showTimer -= Time.deltaTime;
            if (showTimer <= 0f && targetFillAmount >= 1f)
            {
                SetVisible(false);
            }
        }

        // Luôn hiện nếu HP không đầy
        if (targetFillAmount < 0.99f)
        {
            SetVisible(true);
        }
    }

    // ===================================
    // TẠO THANH MÁU BẰNG SPRITERENDERER
    // ===================================

    /// <summary>
    /// Tạo thanh máu trong World Space bằng SpriteRenderer.
    /// </summary>
    private void CreateHealthBar()
    {
        // Container chính
        barContainer = new GameObject("HealthBar");
        barContainer.transform.SetParent(transform);
        barContainer.transform.localPosition = new Vector3(0f, yOffset, 0f);

        Sprite pixelSprite = CreatePixelSprite();

        // === Nền (background) ===
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(barContainer.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        bgSprite = bgObj.AddComponent<SpriteRenderer>();
        bgSprite.sprite = pixelSprite;
        bgSprite.color = bgColor;
        bgSprite.sortingOrder = 98;

        // === Thanh lerp (vàng, hiện đằng sau) ===
        GameObject lerpObj = new GameObject("LerpFill");
        lerpObj.transform.SetParent(barContainer.transform);
        lerpObj.transform.localPosition = new Vector3(-barWidth / 2f, 0f, 0f);
        lerpObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        lerpSprite = lerpObj.AddComponent<SpriteRenderer>();
        lerpSprite.sprite = pixelSprite;
        lerpSprite.color = lerpBarColor;
        lerpSprite.sortingOrder = 99;

        // Pivot trái (scale từ trái sang phải)
        // Dùng localPosition để mô phỏng fill

        // === Thanh chính (xanh/đỏ) ===
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barContainer.transform);
        fillObj.transform.localPosition = new Vector3(-barWidth / 2f, 0f, 0f);
        fillObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        fillSprite = fillObj.AddComponent<SpriteRenderer>();
        fillSprite.sprite = pixelSprite;
        fillSprite.color = isPlayerBar ? playerBarColor : enemyBarColor;
        fillSprite.sortingOrder = 100;

        // === Tạo icon trạng thái (chỉ cho nhân vật) ===
        if (isPlayerBar)
        {
            CreateStatusIcons();
        }
    }

    /// <summary>
    /// Cập nhật visual thanh máu.
    /// </summary>
    private void UpdateBarVisual()
    {
        if (fillSprite == null || lerpSprite == null) return;

        // Scale thanh chính theo HP hiện tại
        float fillWidth = barWidth * targetFillAmount;
        fillSprite.transform.localScale = new Vector3(fillWidth, barHeight, 1f);
        fillSprite.transform.localPosition = new Vector3(-barWidth / 2f + fillWidth / 2f, 0f, 0f);

        // Scale thanh lerp (giảm chậm hơn)
        float lerpWidth = barWidth * currentFillAmount;
        lerpSprite.transform.localScale = new Vector3(lerpWidth, barHeight, 1f);
        lerpSprite.transform.localPosition = new Vector3(-barWidth / 2f + lerpWidth / 2f, 0f, 0f);

        // Đổi màu thanh chính theo % HP
        if (isPlayerBar)
        {
            if (targetFillAmount > 0.5f)
                fillSprite.color = playerBarColor;
            else if (targetFillAmount > 0.25f)
                fillSprite.color = lerpBarColor;
            else
                fillSprite.color = enemyBarColor; // Đỏ khi sắp chết
        }
    }

    // ===================================
    // ICON TRẠNG THÁI (NHÂN VẬT)
    // ===================================

    /// <summary>
    /// Tạo icon trạng thái: đói, khát, mệt.
    /// </summary>
    private void CreateStatusIcons()
    {
        Sprite pixelSprite = CreatePixelSprite();
        float iconSize = 0.15f;
        float startX = barWidth / 2f + 0.15f;

        // Icon đói (cam)
        hungerIcon = CreateIcon("HungerIcon", pixelSprite,
            new Color(1f, 0.6f, 0.2f), startX, iconSize);

        // Icon khát (xanh dương)
        thirstIcon = CreateIcon("ThirstIcon", pixelSprite,
            new Color(0.3f, 0.6f, 1f), startX + iconSize + 0.05f, iconSize);

        // Icon mệt (tím)
        energyIcon = CreateIcon("EnergyIcon", pixelSprite,
            new Color(0.7f, 0.3f, 0.9f), startX + (iconSize + 0.05f) * 2f, iconSize);
    }

    private SpriteRenderer CreateIcon(string name, Sprite sprite, Color color, float xPos, float size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(barContainer.transform);
        obj.transform.localPosition = new Vector3(xPos, 0f, 0f);
        obj.transform.localScale = new Vector3(size, size, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = 100;
        sr.enabled = false; // Ẩn mặc định

        return sr;
    }

    /// <summary>
    /// Cập nhật icon trạng thái dựa trên SurvivalSystem.
    /// </summary>
    private void UpdateStatusIcons()
    {
        if (survivalSystem == null) return;

        // Hiện icon khi chỉ số dưới 30%
        if (hungerIcon != null)
            hungerIcon.enabled = survivalSystem.hunger < 30f;

        if (thirstIcon != null)
            thirstIcon.enabled = survivalSystem.thirst < 30f;

        if (energyIcon != null)
            energyIcon.enabled = survivalSystem.energy < 30f;
    }

    // ===================================
    // HELPER
    // ===================================

    /// <summary>Bật/tắt hiển thị thanh máu</summary>
    private void SetVisible(bool visible)
    {
        isVisible = visible;
        if (barContainer != null)
            barContainer.SetActive(visible);
    }

    /// <summary>Lấy HP hiện tại</summary>
    private float GetCurrentHP()
    {
        if (playerController != null) return playerController.health;
        if (enemyBase != null) return enemyBase.CurrentHP;
        return 0f;
    }

    /// <summary>Lấy HP tối đa</summary>
    private float GetMaxHP()
    {
        if (playerController != null) return playerController.maxHealth;
        if (enemyBase != null) return enemyBase.MaxHP;
        return 1f;
    }

    /// <summary>Tạo sprite 1 pixel trắng</summary>
    private Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    private void OnDestroy()
    {
        if (barContainer != null)
            Destroy(barContainer);
    }
}
