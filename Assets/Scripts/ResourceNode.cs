using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// ResourceNode - Điểm tài nguyên trên map (cây, đá, thảo mộc...).
/// Nhân vật đến gần và nhấn E để thu hoạch.
/// Linh (Crafter) thu hoạch nhanh gấp 2x.
/// </summary>
public class ResourceNode : MonoBehaviour
{
    // === LOẠI TÀI NGUYÊN ===
    public enum ResourceType { Wood, Stone, Food, Herb, Crystal }

    [Header("Loại tài nguyên")]
    [SerializeField] private ResourceType resourceType = ResourceType.Wood;

    // === ĐỘ BỀN ===
    [Header("Độ bền")]
    [SerializeField] [Tooltip("Độ bền tối đa")]
    private float maxDurability = 100f;

    [SerializeField] [Tooltip("Sát thương mỗi lần thu hoạch")]
    private float harvestDamage = 25f;

    [SerializeField] [Tooltip("Thời gian giữa mỗi lần thu hoạch (giây)")]
    private float harvestCooldown = 0.8f;

    // === HỒI SINH ===
    [Header("Hồi sinh")]
    [SerializeField] [Tooltip("Thời gian hồi sinh sau khi cạn (giây)")]
    private float respawnTime = 120f;

    // === DROP ITEM ===
    [Header("Item drop khi thu hoạch")]
    [SerializeField] [Tooltip("ItemData sẽ drop khi thu hoạch xong")]
    private ItemData dropItem;

    [SerializeField] [Tooltip("Số lượng item drop mỗi lần cạn")]
    private int dropAmount = 3;

    // === TƯƠNG TÁC ===
    [Header("Tương tác")]
    [SerializeField] [Tooltip("Khoảng cách tối đa để thu hoạch")]
    private float interactRange = 2f;

    [SerializeField] [Tooltip("Phím thu hoạch")]
    private Key harvestKey = Key.E;

    // === ANIMATION RUNG ===
    [Header("Hiệu ứng rung khi thu hoạch")]
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 0.3f;

    // === TRẠNG THÁI ===
    private float currentDurability;
    private bool isDepleted = false;     // Đã cạn tài nguyên
    private bool isHarvesting = false;   // Đang trong cooldown thu hoạch
    private SpriteRenderer spriteRenderer;
    private Collider2D nodeCollider;
    private Vector3 originalPosition;    // Vị trí gốc (cho animation rung)
    private Color originalColor;

    /// <summary>Loại tài nguyên của node</summary>
    public ResourceType Type => resourceType;

    /// <summary>Độ bền còn lại (0-max)</summary>
    public float CurrentDurability => currentDurability;

    /// <summary>Đã cạn chưa</summary>
    public bool IsDepleted => isDepleted;

    private void Awake()
    {
        harvestKey = Key.E;
        spriteRenderer = GetComponent<SpriteRenderer>();
        nodeCollider = GetComponent<Collider2D>();
        originalPosition = transform.position;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        currentDurability = maxDurability;
        EnsureDropItem();
    }

    public void Configure(ResourceType type, ItemData item = null, int amount = 3)
    {
        resourceType = type;
        dropItem = item;
        dropAmount = Mathf.Max(1, amount);
        EnsureDropItem();
    }

    private void EnsureDropItem()
    {
        if (dropItem == null)
            dropItem = ItemData.CreateRuntimeResource(resourceType);
    }

    private void Update()
    {
        // Không xử lý nếu đã cạn hoặc game pause
        if (isDepleted) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Kiểm tra input thu hoạch
        if (Keyboard.current != null && Keyboard.current[harvestKey].wasPressedThisFrame && !isHarvesting)
        {
            TryHarvest();
        }
    }

    /// <summary>
    /// Thử thu hoạch tài nguyên.
    /// Kiểm tra nhân vật active có đứng đủ gần không.
    /// </summary>
    private void TryHarvest()
    {
        PlayerController activeChar = GameManager.Instance.ActiveCharacter;
        if (activeChar == null || activeChar.IsDead) return;

        // Kiểm tra khoảng cách
        float distance = Vector2.Distance(
            activeChar.transform.position,
            transform.position
        );

        if (distance > interactRange) return;

        // Lấy CharacterStats để check bonus Linh (Crafter)
        CharacterStats stats = activeChar.GetComponent<CharacterStats>();
        float bonusMultiplier = 1f;

        if (stats != null && stats.Type == CharacterStats.CharacterType.Linh)
        {
            // Linh thu hoạch nhanh gấp 2x (damage x2)
            bonusMultiplier = stats.CraftingBonus;
            Debug.Log("[ResourceNode] Linh dùng kỹ năng Crafter → thu hoạch nhanh x2!");
        }

        // Thực hiện thu hoạch
        Harvest(harvestDamage * bonusMultiplier);
    }

    /// <summary>
    /// Thu hoạch tài nguyên - trừ durability.
    /// </summary>
    public void Harvest(float damage)
    {
        if (isDepleted || isHarvesting) return;

        currentDurability -= damage;
        currentDurability = Mathf.Max(currentDurability, 0f);

        string resName = GetResourceName();
        Debug.Log($"[ResourceNode] Thu hoạch {resName}: độ bền còn {currentDurability:F0}/{maxDurability}");

        // Animation rung nhẹ
        StartCoroutine(ShakeAnimation());

        // Bắt đầu cooldown
        StartCoroutine(HarvestCooldown());

        // Kiểm tra đã cạn chưa
        if (currentDurability <= 0f)
        {
            DepletedAndDrop();
        }
    }

    /// <summary>
    /// Tài nguyên cạn → drop item và biến mất, chờ hồi sinh.
    /// </summary>
    private void DepletedAndDrop()
    {
        isDepleted = true;
        EnsureDropItem();

        // Drop item vào shared inventory
        if (dropItem != null && GameManager.Instance != null)
        {
            Inventory sharedInventory = GameManager.Instance.GetComponent<Inventory>();
            if (sharedInventory == null)
            {
                // Tìm Inventory trên active character
                PlayerController active = GameManager.Instance.ActiveCharacter;
                if (active != null)
                    sharedInventory = active.GetComponent<Inventory>();
            }

            if (sharedInventory != null)
            {
                bool added = sharedInventory.AddItem(dropItem, dropAmount);
                if (added)
                {
                    Debug.Log($"[ResourceNode] Drop {dropAmount}x {dropItem.itemName} vào kho!");
                }
                else
                {
                    Debug.LogWarning("[ResourceNode] Kho đồ đã đầy! Không thể nhặt item.");
                }
            }
        }

        // Ẩn object
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        if (nodeCollider != null)
            nodeCollider.enabled = false;

        Debug.Log($"[ResourceNode] {GetResourceName()} đã cạn! Hồi sinh sau {respawnTime}s");

        // Bắt đầu timer hồi sinh
        StartCoroutine(RespawnTimer());
    }

    /// <summary>
    /// Coroutine hồi sinh tài nguyên sau thời gian chờ.
    /// </summary>
    private IEnumerator RespawnTimer()
    {
        yield return new WaitForSeconds(respawnTime);

        // Hồi sinh
        isDepleted = false;
        currentDurability = maxDurability;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = originalColor;
        }
        if (nodeCollider != null)
            nodeCollider.enabled = true;

        transform.position = originalPosition;

        Debug.Log($"[ResourceNode] {GetResourceName()} đã hồi sinh!");
    }

    /// <summary>
    /// Animation rung nhẹ khi bị thu hoạch (dùng Coroutine).
    /// </summary>
    private IEnumerator ShakeAnimation()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Random vị trí rung nhẹ xung quanh vị trí gốc
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity);

            transform.position = originalPosition + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Trả về vị trí gốc
        transform.position = originalPosition;

        // Hiệu ứng nhấp nháy khi sắp cạn (dưới 30%)
        if (currentDurability > 0f && currentDurability / maxDurability < 0.3f)
        {
            if (spriteRenderer != null)
            {
                Color dimColor = originalColor * 0.6f;
                dimColor.a = 1f;
                spriteRenderer.color = dimColor;
            }
        }
    }

    /// <summary>
    /// Cooldown giữa các lần thu hoạch.
    /// </summary>
    private IEnumerator HarvestCooldown()
    {
        isHarvesting = true;
        yield return new WaitForSeconds(harvestCooldown);
        isHarvesting = false;
    }

    /// <summary>
    /// Lấy tên loại tài nguyên tiếng Việt.
    /// </summary>
    public string GetResourceName()
    {
        return resourceType switch
        {
            ResourceType.Wood => "Gỗ",
            ResourceType.Stone => "Đá",
            ResourceType.Food => "Thức ăn hoang dã",
            ResourceType.Herb => "Cây thuốc",
            ResourceType.Crystal => "Tinh thể",
            _ => "Tài nguyên"
        };
    }

    /// <summary>
    /// Vẽ gizmos hiển thị phạm vi tương tác trong Editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
