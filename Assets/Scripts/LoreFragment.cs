using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// LoreData - ScriptableObject chứa 1 mảnh lore.
/// Tạo mới: Assets > Create > Vực Thẳm > Lore Data
/// </summary>
[CreateAssetMenu(fileName = "NewLore", menuName = "Vực Thẳm/Lore Data")]
public class LoreData : ScriptableObject
{
    [Header("Thông tin mảnh lore")]
    [Tooltip("Số thứ tự (1-10)")]
    public int fragmentNumber = 1;

    [Tooltip("Tiêu đề mảnh lore")]
    public string title = "Mảnh nhật ký #1";

    [TextArea(3, 6)]
    [Tooltip("Nội dung nhật ký người xưa")]
    public string content = "";

    [TextArea(2, 4)]
    [Tooltip("Chi tiết ẩn chỉ Linh đọc được")]
    public string hiddenContent = "";

    [Tooltip("Đã thu thập chưa (runtime)")]
    [HideInInspector]
    public bool isCollected = false;
}

/// <summary>
/// LoreFragment - Object nhặt được trong tàn tích.
/// Gắn vào GameObject tàn tích trên map.
/// Linh đọc được thêm chi tiết ẩn.
/// Thu thập đủ 10 → mở khóa bí mật.
/// </summary>
public class LoreFragment : MonoBehaviour
{
    // === CẤU HÌNH ===
    [Header("Dữ liệu mảnh lore")]
    [SerializeField] private LoreData loreData;

    [Header("Tương tác")]
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private float interactRange = 2f;

    // === VISUAL ===
    [Header("Hiệu ứng")]
    [SerializeField] private Color glowColor = new Color(0.8f, 0.7f, 0.3f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.3f;

    // === STATIC: quản lý tất cả mảnh lore ===
    private static List<LoreData> collectedLore = new List<LoreData>();
    private static int totalFragments = 10;

    /// <summary>Số mảnh đã thu thập</summary>
    public static int CollectedCount => collectedLore.Count;

    /// <summary>Đã thu thập đủ 10 mảnh chưa</summary>
    public static bool AllCollected => collectedLore.Count >= totalFragments;

    /// <summary>Danh sách lore đã thu thập</summary>
    public static List<LoreData> CollectedLore => collectedLore;

    // === TRẠNG THÁI ===
    private SpriteRenderer spriteRenderer;
    private bool isCollected = false;
    private Color baseColor;

    private void Awake()
    {
        interactKey = Key.E;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;
    }

    private void Update()
    {
        if (isCollected) return;
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Hiệu ứng pulse sáng
        UpdatePulseEffect();

        // Kiểm tra input nhặt
        if (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            TryCollect();
        }
    }

    /// <summary>
    /// Hiệu ứng sáng nhấp nháy để thu hút chú ý.
    /// </summary>
    private void UpdatePulseEffect()
    {
        if (spriteRenderer == null) return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
        Color c = Color.Lerp(baseColor, glowColor, 0.5f + pulse);
        spriteRenderer.color = c;
    }

    /// <summary>
    /// Thử nhặt mảnh lore (kiểm tra khoảng cách).
    /// </summary>
    private void TryCollect()
    {
        PlayerController activeChar = GameManager.Instance.ActiveCharacter;
        if (activeChar == null || activeChar.IsDead) return;

        float dist = Vector2.Distance(activeChar.transform.position, transform.position);
        if (dist > interactRange) return;

        CollectLore(activeChar);
    }

    /// <summary>
    /// Thu thập mảnh lore. Linh đọc được thêm chi tiết ẩn.
    /// </summary>
    private void CollectLore(PlayerController collector)
    {
        if (loreData == null || isCollected) return;

        isCollected = true;
        loreData.isCollected = true;

        // Thêm vào danh sách đã thu thập (tránh trùng)
        if (!collectedLore.Contains(loreData))
        {
            collectedLore.Add(loreData);
        }

        // Kiểm tra Linh có đọc được chi tiết ẩn không
        CharacterStats stats = collector.GetComponent<CharacterStats>();
        bool isLinh = stats != null && stats.Type == CharacterStats.CharacterType.Linh;

        // Hiện nội dung lore
        string content = $"═══ {loreData.title} ═══\n\n{loreData.content}";
        if (isLinh && !string.IsNullOrEmpty(loreData.hiddenContent))
        {
            content += $"\n\n★ Linh phát hiện thêm:\n{loreData.hiddenContent}";
        }

        Debug.Log($"[Lore] Thu thập mảnh #{loreData.fragmentNumber}: {loreData.title}");
        Debug.Log(content);

        // Trigger thoại tàn tích (lần đầu)
        if (CollectedCount == 1)
        {
            DialogueSystem dialogue = DialogueSystem.Instance;
            if (dialogue != null) dialogue.TriggerRuinsDialogue();
        }

        // Kiểm tra đã đủ 10 mảnh
        if (AllCollected)
        {
            Debug.Log("[Lore] ★★★ Đã thu thập đủ 10 mảnh! Bí mật được mở khóa:");
            Debug.Log("[Lore] Cổng không tự nhiên tồn tại — có ai đó đã tạo ra nó...");
            Debug.Log("[Lore] Và người tạo ra nó... vẫn còn ở đây.");
        }

        // Ẩn object
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    /// <summary>Reset static data (khi restart game)</summary>
    public static void ResetAll()
    {
        collectedLore.Clear();
        foreach (var fragment in FindObjectsByType<LoreFragment>(FindObjectsSortMode.None))
            fragment.SetCollectedState(false);
    }

    /// <summary>Gizmos tầm tương tác</summary>
    public static void RestoreCollectedFragments(List<int> fragmentNumbers)
    {
        ResetAll();
        if (fragmentNumbers == null) return;

        HashSet<int> collectedNumbers = new HashSet<int>(fragmentNumbers);
        foreach (var fragment in FindObjectsByType<LoreFragment>(FindObjectsSortMode.None))
        {
            if (fragment.loreData == null) continue;
            if (!collectedNumbers.Contains(fragment.loreData.fragmentNumber)) continue;

            fragment.SetCollectedState(true);
            if (!collectedLore.Contains(fragment.loreData))
                collectedLore.Add(fragment.loreData);
        }
    }

    private void SetCollectedState(bool collected)
    {
        isCollected = collected;

        if (loreData != null)
            loreData.isCollected = collected;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.enabled = !collected;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = !collected;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

/// <summary>
/// LoreJournalUI - Giao diện nhật ký lore. Phím J để mở.
/// </summary>
public class LoreJournalUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Key toggleKey = Key.J;
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private TextMeshProUGUI journalText;

    private bool isOpen = false;

    private void Start()
    {
        toggleKey = Key.J;
        if (journalPanel == null) CreateJournalUI();
        else journalPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        isOpen = !isOpen;
        if (journalPanel != null) journalPanel.SetActive(isOpen);
        if (isOpen) RefreshJournal();
    }

    private void RefreshJournal()
    {
        if (journalText == null) return;

        string text = $"═══ NHẬT KÝ VỰC THẲM ({LoreFragment.CollectedCount}/10) ═══\n\n";

        if (LoreFragment.CollectedCount == 0)
        {
            text += "Chưa tìm thấy mảnh nhật ký nào...\nHãy khám phá tàn tích trong Rừng Phát Quang.";
        }
        else
        {
            // Sắp xếp theo số thứ tự
            var sorted = new List<LoreData>(LoreFragment.CollectedLore);
            sorted.Sort((a, b) => a.fragmentNumber.CompareTo(b.fragmentNumber));

            foreach (var lore in sorted)
            {
                text += $"── Mảnh #{lore.fragmentNumber}: {lore.title} ──\n";
                text += $"{lore.content}\n\n";
            }

            if (LoreFragment.AllCollected)
            {
                text += "\n★★★ BÍ MẬT ĐƯỢC MỞ KHÓA ★★★\n";
                text += "Cổng dịch chuyển không phải hiện tượng tự nhiên.\n";
                text += "Có người đã tạo ra nó — và người ấy vẫn còn ở đây,\n";
                text += "bị mắc kẹt, hóa điên, trở thành... Kẻ Gác Cổng.";
            }
        }

        journalText.text = text;
    }

    private void CreateJournalUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("UICanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        journalPanel = new GameObject("JournalPanel");
        journalPanel.transform.SetParent(canvas.transform, false);
        RectTransform rect = journalPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.1f);
        rect.anchorMax = new Vector2(0.85f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = journalPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.12f, 0.95f);

        // Scroll text
        GameObject textObj = new GameObject("JournalText");
        textObj.transform.SetParent(journalPanel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(30, 30);
        textRect.offsetMax = new Vector2(-30, -30);

        journalText = textObj.AddComponent<TextMeshProUGUI>();
        journalText.fontSize = 14;
        journalText.color = new Color(0.85f, 0.8f, 0.7f);
        journalText.enableWordWrapping = true;

        journalPanel.SetActive(false);
    }
}
