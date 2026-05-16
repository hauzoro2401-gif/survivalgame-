using UnityEngine;
using System.Collections;

/// <summary>
/// PortalSystem - Cổng dịch chuyển về nhà.
/// Xuất hiện khi đủ điều kiện: 7 ngày + 6 lore + camp built + boss key.
/// 4 nhân vật đứng gần → trigger ending.
/// </summary>
public class PortalSystem : MonoBehaviour
{
    // === ĐIỀU KIỆN MỞ CỔNG ===
    [Header("Điều kiện kích hoạt cổng")]
    [SerializeField] [Tooltip("Số ngày tối thiểu để cổng xuất hiện")]
    private int requiredDays = 7;

    [SerializeField] [Tooltip("Số mảnh lore tối thiểu")]
    private int requiredLore = 6;

    [SerializeField] [Tooltip("Cần có Chìa khóa Cổng (drop từ Boss)")]
    private ItemData portalKeyItem;

    // === VỊ TRÍ ===
    [Header("Vị trí cổng")]
    [SerializeField] private Vector2 portalPosition = Vector2.zero;

    [SerializeField] [Tooltip("Bán kính để trigger ending")]
    private float activationRadius = 3f;

    // === VISUAL ===
    [Header("Hiệu ứng")]
    [SerializeField] private Color portalColor = new Color(0.4f, 0.2f, 0.9f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float portalSize = 2f;

    // === TRẠNG THÁI ===
    /// <summary>Cổng đã xuất hiện chưa</summary>
    public bool IsPortalActive { get; private set; } = false;

    /// <summary>Đã build trại chưa (set từ bên ngoài)</summary>
    public bool CampBuilt { get; set; } = false;
    // FIX #8: Flag Ending C — mở cổng khi thiếu lore
    private bool openedWithInsufficientLore = false;

    private GameObject portalObject;
    private SpriteRenderer portalSprite;
    private bool endingTriggered = false;
    private float checkTimer = 0f;

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (endingTriggered) return;

        // Kiểm tra điều kiện mỗi 2 giây
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = 2f;

            if (!IsPortalActive)
            {
                CheckActivationConditions();
            }
        }

        // Cập nhật hiệu ứng cổng
        if (IsPortalActive)
        {
            UpdatePortalVisual();
            CheckCharactersNearPortal();
        }
    }

    /// <summary>
    /// Kiểm tra tất cả điều kiện để mở cổng.
    /// </summary>
    private void CheckActivationConditions()
    {
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight == null) return;

        // Điều kiện 1: Đủ ngày
        if (dayNight.CurrentDay < requiredDays) return;

        // Kiểm tra chìa khóa (boss key)
        bool hasKey = false;
        if (portalKeyItem != null)
        {
            Inventory inv = GameManager.Instance?.GetComponent<Inventory>();
            hasKey = inv != null && inv.HasItem(portalKeyItem);
        }
        else
        {
            // Không cần key nếu chưa config
            hasKey = true;
        }
        if (!hasKey) return;

        // Điều kiện 3: Đã build trại
        if (!CampBuilt)
        {
            if (GameManager.Instance != null && GameManager.Instance.CampBuilt)
                CampBuilt = true;
            else
                return;
        }

        // FIX #8: Cho phép mở cổng dù thiếu lore, nhưng đánh dấu Ending C
        bool hasEnoughLore = LoreFragment.CollectedCount >= requiredLore;
        if (!hasEnoughLore)
        {
            openedWithInsufficientLore = true;
            Debug.LogWarning($"[Portal] Cừng mở cổng với chỉ {LoreFragment.CollectedCount}/{requiredLore} lore → Ending C!");
        }

        // Tất cả điều kiện đạt (hoặc bypass lore) → kích hoạt cổng!
        ActivatePortal();
    }

    /// <summary>
    /// Kích hoạt cổng dịch chuyển.
    /// </summary>
    private void ActivatePortal()
    {
        IsPortalActive = true;

        // Tạo visual cổng
        CreatePortalVisual();

        Debug.Log("[Portal] ★★★ CỔNG DỊCH CHUYỂN ĐÃ XUẤT HIỆN! ★★★");
        Debug.Log("[Portal] Tập hợp cả 4 nhân vật đến cổng để về nhà!");

        // FIX #14: Chỉ trigger dialogue nếu có DialogueData hợp lệ
        // (Không gọi TriggerDialogue(null) vì sẽ crash DialogueSystem)
        // Khi có DialogueData, gán vào portalOpenDialogue trong Inspector
    }

    /// <summary>
    /// Tạo visual cổng bằng SpriteRenderer.
    /// </summary>
    private void CreatePortalVisual()
    {
        portalObject = new GameObject("Portal_CổngVề");
        portalObject.transform.position = new Vector3(portalPosition.x, portalPosition.y, 0f);
        portalObject.transform.localScale = Vector3.one * portalSize;

        portalSprite = portalObject.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(8, 8);
        Color[] px = new Color[64];
        for (int i = 0; i < px.Length; i++)
        {
            // Tạo hình tròn đơn giản
            int x = i % 8, y = i / 8;
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(3.5f, 3.5f));
            px[i] = dist < 3.5f ? Color.white : new Color(1, 1, 1, 0);
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        portalSprite.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f);
        portalSprite.color = portalColor;
        portalSprite.sortingOrder = 50;

        // Trigger collider
        CircleCollider2D col = portalObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = activationRadius / portalSize;
    }

    /// <summary>
    /// Hiệu ứng pulse sáng cho cổng.
    /// </summary>
    private void UpdatePortalVisual()
    {
        if (portalSprite == null) return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.3f + 0.7f;
        portalSprite.color = new Color(portalColor.r, portalColor.g, portalColor.b, pulse);

        // Xoay nhẹ
        portalObject.transform.Rotate(0f, 0f, 30f * Time.deltaTime);
    }

    /// <summary>
    /// Kiểm tra bao nhiêu nhân vật đứng gần cổng.
    /// </summary>
    private void CheckCharactersNearPortal()
    {
        if (endingTriggered) return;

        int aliveCount = 0;
        int nearPortalCount = 0;

        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null) continue;

            if (!character.IsDead)
            {
                aliveCount++;

                float dist = Vector2.Distance(character.transform.position, portalPosition);
                if (dist <= activationRadius)
                {
                    nearPortalCount++;
                }
            }
        }

        // Tất cả người sống đứng gần cổng
        if (nearPortalCount >= aliveCount && aliveCount > 0)
        {
            endingTriggered = true;
            TriggerEnding(aliveCount);
        }
    }

    /// <summary>
    /// Trigger ending dựa trên số người còn sống.
    /// </summary>
    private void TriggerEnding(int aliveCount)
    {
        EndingManager ending = FindAnyObjectByType<EndingManager>();
        if (ending == null) return;

        // FIX #8: Ưu tiên kiểm tra flag thiếu lore (set khi mở cổng bằng key mà chưa đủ lore)
        bool hasEnoughLore = LoreFragment.CollectedCount >= requiredLore;
        if (!hasEnoughLore || openedWithInsufficientLore)
        {
            // Ending C: mở cổng khi chưa đủ lore
            ending.TriggerEnding(EndingManager.EndingType.EndingC);
        }
        else if (aliveCount >= 4)
        {
            // Ending A: cả 4 sống
            ending.TriggerEnding(EndingManager.EndingType.EndingA);
        }
        else
        {
            // Ending B: 3 người sống (1 ở lại)
            ending.TriggerEnding(EndingManager.EndingType.EndingB);
        }
    }

    /// <summary>Kích hoạt cổng thủ công (debug/test)</summary>
    public void ForceActivatePortal()
    {
        ActivatePortal();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.2f, 0.9f, 0.3f);
        Gizmos.DrawSphere(portalPosition, activationRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(portalPosition, activationRadius);
    }
}
