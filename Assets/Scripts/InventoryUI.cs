using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// InventoryUI - Giao diện kho đồ và chế tạo.
/// Grid 4x5 (20 slot), tooltip, panel crafting.
/// Phím I để toggle mở/đóng.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // === PANELS ===
    [Header("Panels chính")]
    [SerializeField] [Tooltip("Panel chứa toàn bộ UI inventory")]
    private GameObject inventoryPanel;

    [SerializeField] [Tooltip("Panel crafting (bên cạnh inventory)")]
    private GameObject craftingPanel;

    // === INVENTORY GRID ===
    [Header("Inventory Grid")]
    [SerializeField] [Tooltip("Container chứa các slot (GridLayoutGroup)")]
    private Transform slotContainer;

    [SerializeField] [Tooltip("Prefab cho 1 slot inventory")]
    private GameObject slotPrefab;

    // === TOOLTIP ===
    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;

    // === CRAFTING ===
    [Header("Crafting")]
    [SerializeField] [Tooltip("Container chứa các recipe")]
    private Transform recipeContainer;

    [SerializeField] [Tooltip("Prefab cho 1 dòng recipe")]
    private GameObject recipePrefab;

    // === PHÍM TẮT ===
    [Header("Phím tắt")]
    [SerializeField] private Key toggleKey = Key.I;

    // === MÀUSTATUS ===
    [Header("Màu trạng thái recipe")]
    [SerializeField] private Color canCraftColor = new Color(0.3f, 0.8f, 0.3f);    // Xanh
    [SerializeField] private Color cannotCraftColor = new Color(0.8f, 0.3f, 0.3f);  // Đỏ
    [SerializeField] private Color slotEmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color slotFilledColor = new Color(0.3f, 0.3f, 0.4f, 0.8f);

    // === TRẠNG THÁI ===
    private bool isOpen = false;
    private List<GameObject> slotInstances = new List<GameObject>();
    private List<GameObject> recipeInstances = new List<GameObject>();

    private void Start()
    {
        // Fix for corrupted Inspector values
        toggleKey = Key.I;

        // Ẩn UI khi bắt đầu
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (craftingPanel != null) craftingPanel.SetActive(false);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);

        // Nếu chưa có UI elements → tự tạo
        if (inventoryPanel == null)
        {
            CreateInventoryUI();
        }

        // Lắng nghe event inventory changed
        Inventory inventory = GetSharedInventory();
        if (inventory != null)
        {
            inventory.OnInventoryChanged += RefreshUI;
        }
    }

    private void Update()
    {
        // Toggle inventory bằng phím I
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    private void OnDestroy()
    {
        // Hủy đăng ký event
        Inventory inventory = GetSharedInventory();
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= RefreshUI;
        }
    }

    /// <summary>
    /// Toggle mở/đóng inventory UI.
    /// </summary>
    public void ToggleInventory()
    {
        isOpen = !isOpen;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(isOpen);

        if (isOpen)
        {
            RefreshUI();
            HideTooltip();
        }
        else
        {
            if (craftingPanel != null) craftingPanel.SetActive(false);
            HideTooltip();
        }

        Debug.Log($"[InventoryUI] Kho đồ {(isOpen ? "MỞ" : "ĐÓNG")}");
    }

    /// <summary>
    /// Toggle panel crafting.
    /// </summary>
    public void ToggleCrafting()
    {
        if (craftingPanel == null) return;

        bool active = !craftingPanel.activeSelf;
        craftingPanel.SetActive(active);

        if (active)
        {
            RefreshCraftingPanel();
        }
    }

    /// <summary>
    /// Làm mới toàn bộ UI (khi inventory thay đổi).
    /// </summary>
    public void RefreshUI()
    {
        RefreshSlots();

        if (craftingPanel != null && craftingPanel.activeSelf)
        {
            RefreshCraftingPanel();
        }
    }

    /// <summary>
    /// Làm mới grid slot inventory (4x5 = 20 slot).
    /// </summary>
    private void RefreshSlots()
    {
        if (slotContainer == null) return;

        Inventory inventory = GetSharedInventory();
        if (inventory == null) return;

        // Xóa slot cũ
        foreach (var slot in slotInstances)
        {
            if (slot != null) Destroy(slot);
        }
        slotInstances.Clear();

        // Lấy danh sách item
        var itemList = inventory.GetItemList();

        // Tạo 20 slot
        for (int i = 0; i < inventory.MaxSlots; i++)
        {
            GameObject slot;

            if (slotPrefab != null)
            {
                slot = Instantiate(slotPrefab, slotContainer);
            }
            else
            {
                slot = CreateDefaultSlot(slotContainer);
            }

            // Điền thông tin item nếu có
            if (i < itemList.Count)
            {
                var pair = itemList[i];
                FillSlot(slot, pair.Key, pair.Value);
            }
            else
            {
                // Slot trống
                ClearSlot(slot);
            }

            slotInstances.Add(slot);
        }
    }

    /// <summary>
    /// Điền thông tin item vào 1 slot.
    /// </summary>
    private void FillSlot(GameObject slot, ItemData item, int count)
    {
        // Tìm Image cho icon
        Image iconImage = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null)
        {
            if (item.icon != null)
            {
                iconImage.sprite = item.icon;
                iconImage.color = Color.white;
                iconImage.enabled = true;
            }
            else
            {
                // Placeholder icon (màu theo loại item)
                iconImage.color = GetItemTypeColor(item);
                iconImage.enabled = true;
            }
        }

        // Tìm Text cho số lượng
        TextMeshProUGUI countText = slot.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
        if (countText != null)
        {
            countText.text = count > 1 ? count.ToString() : "";
        }

        // Background slot
        Image bg = slot.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = slotFilledColor;
        }

        // Thêm click handler cho tooltip
        Button btn = slot.GetComponent<Button>();
        if (btn == null) btn = slot.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        // Capture item cho closure
        ItemData capturedItem = item;
        btn.onClick.AddListener(() => ShowTooltip(capturedItem));
    }

    /// <summary>
    /// Xóa thông tin slot (slot trống).
    /// </summary>
    private void ClearSlot(GameObject slot)
    {
        Image iconImage = slot.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null)
        {
            iconImage.enabled = false;
        }

        TextMeshProUGUI countText = slot.transform.Find("Count")?.GetComponent<TextMeshProUGUI>();
        if (countText != null)
        {
            countText.text = "";
        }

        Image bg = slot.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = slotEmptyColor;
        }

        Button btn = slot.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(HideTooltip);
        }
    }

    /// <summary>
    /// Hiện tooltip khi click vào item.
    /// </summary>
    private void ShowTooltip(ItemData item)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        if (item == null) { HideTooltip(); return; }

        tooltipPanel.SetActive(true);
        tooltipText.text = item.GetTooltipText();
    }

    /// <summary>
    /// Ẩn tooltip.
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Làm mới panel crafting - liệt kê các recipe.
    /// </summary>
    private void RefreshCraftingPanel()
    {
        if (recipeContainer == null) return;

        CraftingSystem craftingSystem = FindAnyObjectByType<CraftingSystem>();
        if (craftingSystem == null) return;

        // Xóa recipe cũ
        foreach (var ri in recipeInstances)
        {
            if (ri != null) Destroy(ri);
        }
        recipeInstances.Clear();

        // Tạo entry cho mỗi recipe
        foreach (var recipe in craftingSystem.Recipes)
        {
            GameObject entry;

            if (recipePrefab != null)
            {
                entry = Instantiate(recipePrefab, recipeContainer);
            }
            else
            {
                entry = CreateDefaultRecipeEntry(recipeContainer);
            }

            // Điền thông tin recipe
            FillRecipeEntry(entry, recipe, craftingSystem);
            recipeInstances.Add(entry);
        }
    }

    /// <summary>
    /// Điền thông tin vào 1 dòng recipe.
    /// Xanh = đủ nguyên liệu, Đỏ = thiếu.
    /// </summary>
    private void FillRecipeEntry(GameObject entry, CraftingRecipe recipe, CraftingSystem craftingSystem)
    {
        bool canCraft = craftingSystem.CanCraft(recipe);

        // Tìm text tên recipe
        TextMeshProUGUI nameText = entry.transform.Find("RecipeName")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = recipe.recipeName;
            // Highlight màu xanh/đỏ theo trạng thái
            nameText.color = canCraft ? canCraftColor : cannotCraftColor;
        }

        // Tìm text mô tả nguyên liệu
        TextMeshProUGUI infoText = entry.transform.Find("RecipeInfo")?.GetComponent<TextMeshProUGUI>();
        if (infoText != null)
        {
            infoText.text = GetRecipeShortDescription(recipe);
            infoText.color = canCraft ? canCraftColor : cannotCraftColor;
        }

        // Background
        Image bg = entry.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = canCraft
                ? new Color(canCraftColor.r, canCraftColor.g, canCraftColor.b, 0.15f)
                : new Color(cannotCraftColor.r, cannotCraftColor.g, cannotCraftColor.b, 0.15f);
        }

        // Nút Craft
        Button craftBtn = entry.transform.Find("CraftButton")?.GetComponent<Button>();
        if (craftBtn != null)
        {
            craftBtn.onClick.RemoveAllListeners();
            CraftingRecipe capturedRecipe = recipe;
            craftBtn.onClick.AddListener(() =>
            {
                craftingSystem.Craft(capturedRecipe);
                RefreshUI();
            });
            craftBtn.interactable = canCraft;
        }
    }

    /// <summary>
    /// Mô tả ngắn nguyên liệu recipe.
    /// </summary>
    private string GetRecipeShortDescription(CraftingRecipe recipe)
    {
        string desc = "";
        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            var ing = recipe.ingredients[i];
            if (i > 0) desc += " + ";
            desc += $"{ing.amount}x {ing.item.itemName}";
        }
        desc += $" → {recipe.resultAmount}x {recipe.result.itemName}";
        return desc;
    }

    /// <summary>
    /// Lấy màu theo loại item (khi chưa có icon).
    /// </summary>
    private Color GetItemTypeColor(ItemData item)
    {
        return item.itemType switch
        {
            ItemData.ItemType.Resource => new Color(0.6f, 0.5f, 0.3f),
            ItemData.ItemType.Weapon => new Color(0.7f, 0.3f, 0.3f),
            ItemData.ItemType.Food => new Color(0.3f, 0.7f, 0.3f),
            ItemData.ItemType.Medicine => new Color(0.3f, 0.6f, 0.8f),
            ItemData.ItemType.Structure => new Color(0.6f, 0.6f, 0.6f),
            _ => Color.white
        };
    }

    /// <summary>
    /// Lấy shared inventory từ GameManager.
    /// </summary>
    private Inventory GetSharedInventory()
    {
        if (GameManager.Instance == null) return null;
        return GameManager.Instance.GetComponent<Inventory>();
    }

    // ===========================================
    // TỰ ĐỘNG TẠO UI NẾU CHƯA CÓ (runtime)
    // ===========================================

    /// <summary>
    /// Tạo toàn bộ Inventory UI bằng code (khi chưa setup prefab).
    /// </summary>
    private void CreateInventoryUI()
    {
        // Tìm hoặc tạo Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("UICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // === INVENTORY PANEL ===
        inventoryPanel = CreatePanel(canvas.transform, "InventoryPanel",
            new Vector2(350, 480), new Vector2(-200, 0));

        // Tiêu đề
        CreateText(inventoryPanel.transform, "Title", "KHO ĐỒ",
            new Vector2(0, 210), 22, TextAlignmentOptions.Center);

        // Grid container cho 20 slot (4x5)
        GameObject gridObj = new GameObject("SlotGrid");
        gridObj.transform.SetParent(inventoryPanel.transform, false);

        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.anchoredPosition = new Vector2(0, 20);
        gridRect.sizeDelta = new Vector2(320, 400);

        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(6, 6);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperCenter;

        slotContainer = gridObj.transform;

        // Nút Craft
        GameObject craftBtnObj = CreateButton(inventoryPanel.transform, "CraftToggleBtn",
            "Chế tạo ⚒", new Vector2(0, -210), new Vector2(150, 40));
        craftBtnObj.GetComponent<Button>().onClick.AddListener(ToggleCrafting);

        // === TOOLTIP PANEL ===
        tooltipPanel = CreatePanel(canvas.transform, "TooltipPanel",
            new Vector2(250, 150), new Vector2(-200, -280));
        tooltipText = CreateText(tooltipPanel.transform, "TooltipText", "",
            Vector2.zero, 14, TextAlignmentOptions.TopLeft);
        RectTransform ttRect = tooltipText.GetComponent<RectTransform>();
        ttRect.sizeDelta = new Vector2(230, 130);
        tooltipPanel.SetActive(false);

        // === CRAFTING PANEL ===
        craftingPanel = CreatePanel(canvas.transform, "CraftingPanel",
            new Vector2(350, 480), new Vector2(170, 0));

        CreateText(craftingPanel.transform, "CraftTitle", "CHẾ TẠO ⚒",
            new Vector2(0, 210), 22, TextAlignmentOptions.Center);

        // Recipe container (vertical layout)
        GameObject recipeGrid = new GameObject("RecipeList");
        recipeGrid.transform.SetParent(craftingPanel.transform, false);

        RectTransform recipeRect = recipeGrid.AddComponent<RectTransform>();
        recipeRect.anchoredPosition = new Vector2(0, 0);
        recipeRect.sizeDelta = new Vector2(320, 380);

        VerticalLayoutGroup vLayout = recipeGrid.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 5;
        vLayout.padding = new RectOffset(10, 10, 10, 10);
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;

        recipeContainer = recipeGrid.transform;
        craftingPanel.SetActive(false);

        // Ẩn inventory panel ban đầu
        inventoryPanel.SetActive(false);
    }

    /// <summary>Tạo slot mặc định khi chưa có prefab</summary>
    private GameObject CreateDefaultSlot(Transform parent)
    {
        GameObject slot = new GameObject("Slot");
        slot.transform.SetParent(parent, false);

        // Background
        Image bg = slot.AddComponent<Image>();
        bg.color = slotEmptyColor;

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(slot.transform, false);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.enabled = false;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // Count text
        GameObject countObj = new GameObject("Count");
        countObj.transform.SetParent(slot.transform, false);
        TextMeshProUGUI countTmp = countObj.AddComponent<TextMeshProUGUI>();
        countTmp.text = "";
        countTmp.fontSize = 14;
        countTmp.fontStyle = FontStyles.Bold;
        countTmp.alignment = TextAlignmentOptions.BottomRight;
        RectTransform countRect = countObj.GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.offsetMin = new Vector2(2, 2);
        countRect.offsetMax = new Vector2(-4, -4);

        return slot;
    }

    /// <summary>Tạo recipe entry mặc định</summary>
    private GameObject CreateDefaultRecipeEntry(Transform parent)
    {
        GameObject entry = new GameObject("RecipeEntry");
        entry.transform.SetParent(parent, false);

        // Background
        Image bg = entry.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        LayoutElement le = entry.AddComponent<LayoutElement>();
        le.preferredHeight = 70;
        le.minHeight = 70;

        // Tên recipe
        CreateText(entry.transform, "RecipeName", "Recipe",
            new Vector2(-30, 15), 15, TextAlignmentOptions.Left);

        // Info
        TextMeshProUGUI info = CreateText(entry.transform, "RecipeInfo", "",
            new Vector2(-30, -10), 11, TextAlignmentOptions.Left);
        RectTransform infoRect = info.GetComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(230, 25);

        // Nút craft
        CreateButton(entry.transform, "CraftButton", "Tạo",
            new Vector2(130, 0), new Vector2(60, 50));

        return entry;
    }

    // ===========================================
    // HELPER TẠO UI ELEMENTS
    // ===========================================

    private GameObject CreatePanel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.92f);

        return panel;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string content,
        Vector2 pos, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(300, 30);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;

        return tmp;
    }

    private GameObject CreateButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.5f, 0.7f, 0.9f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
        btn.colors = colors;

        // Label text
        TextMeshProUGUI txt = CreateText(btnObj.transform, "Label", label,
            Vector2.zero, 14, TextAlignmentOptions.Center);
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btnObj;
    }
}
