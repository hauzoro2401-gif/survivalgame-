using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ItemData - ScriptableObject định nghĩa thông tin 1 loại item.
/// Tạo mới trong Unity: Assets > Create > Vực Thẳm > Item Data
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Vực Thẳm/Item Data")]
public class ItemData : ScriptableObject
{
    private static readonly Dictionary<ResourceNode.ResourceType, ItemData> RuntimeResourceCache =
        new Dictionary<ResourceNode.ResourceType, ItemData>();
    private static readonly Dictionary<string, ItemData> RuntimeItemCache =
        new Dictionary<string, ItemData>();

    // === THÔNG TIN CƠ BẢN ===
    [Header("Thông tin cơ bản")]
    [Tooltip("Tên item hiển thị trong game")]
    public string itemName = "Item mới";

    [TextArea(2, 4)]
    [Tooltip("Mô tả item")]
    public string description = "Mô tả item...";

    [Tooltip("Icon hiển thị trong inventory")]
    public Sprite icon;

    // === PHÂN LOẠI ===
    [Header("Phân loại")]
    public ItemType itemType = ItemType.Resource;

    /// <summary>Loại item</summary>
    public enum ItemType
    {
        Resource,   // Nguyên liệu thô (gỗ, đá, thảo mộc...)
        Weapon,     // Vũ khí
        Food,       // Thức ăn (có thể dùng để ăn)
        Medicine,   // Thuốc (hồi máu)
        Tool,       // Công cụ
        Structure   // Công trình (lều, bẫy...)
    }

    // === GIÁ TRỊ SỬ DỤNG ===
    [Header("Giá trị khi sử dụng")]
    [Tooltip("Có thể xếp chồng trong inventory không")]
    public bool isStackable = true;

    [Tooltip("Số lượng tối đa trong 1 stack")]
    public int maxStack = 99;

    [Tooltip("Lượng hunger hồi khi ăn (chỉ Food)")]
    public float hungerRestore = 0f;

    [Tooltip("Lượng thirst hồi khi uống (chỉ Food)")]
    public float thirstRestore = 0f;

    [Tooltip("Lượng HP hồi khi dùng (chỉ Medicine)")]
    public float healthRestore = 0f;

    [Tooltip("Damage bonus khi trang bị (chỉ Weapon)")]
    public float damageBonus = 0f;

    // === LOẠI TÀI NGUYÊN TƯƠNG ỨNG ===
    [Header("Liên kết ResourceNode")]
    [Tooltip("Loại tài nguyên (dùng khi drop từ ResourceNode)")]
    public ResourceNodeType resourceType = ResourceNodeType.None;

    /// <summary>Loại tài nguyên node</summary>
    public enum ResourceNodeType
    {
        None,
        Wood,
        Stone,
        Food,
        Herb,
        Crystal
    }

    /// <summary>
    /// Lấy mô tả đầy đủ item cho tooltip UI.
    /// </summary>
    public string GetTooltipText()
    {
        string tooltip = $"<b>{itemName}</b>\n";
        tooltip += $"<i>[{GetItemTypeName()}]</i>\n";
        tooltip += $"{description}\n";

        // Thêm thông tin giá trị sử dụng
        if (hungerRestore > 0f)
            tooltip += $"\n♦ Hồi đói: +{hungerRestore}";
        if (thirstRestore > 0f)
            tooltip += $"\n♦ Hồi khát: +{thirstRestore}";
        if (healthRestore > 0f)
            tooltip += $"\n♥ Hồi máu: +{healthRestore}";
        if (damageBonus > 0f)
            tooltip += $"\n✦ Sát thương: +{damageBonus}";

        return tooltip;
    }

    /// <summary>Tên loại item bằng tiếng Việt</summary>
    public string GetItemTypeName()
    {
        return itemType switch
        {
            ItemType.Resource => "Nguyên liệu",
            ItemType.Weapon => "Vũ khí",
            ItemType.Food => "Thức ăn",
            ItemType.Medicine => "Thuốc",
            ItemType.Tool => "Công cụ",
            ItemType.Structure => "Công trình",
            _ => "Khác"
        };
    }

    public static ItemData CreateRuntimeResource(ResourceNode.ResourceType type)
    {
        if (RuntimeResourceCache.TryGetValue(type, out ItemData cached))
            return cached;

        ItemData item = CreateInstance<ItemData>();
        item.name = $"Runtime_{type}";
        item.itemName = GetRuntimeResourceName(type);
        item.description = $"Runtime resource generated for {item.itemName}.";
        item.itemType = type == ResourceNode.ResourceType.Food ? ItemType.Food : ItemType.Resource;
        item.isStackable = true;
        item.maxStack = 99;
        item.resourceType = ConvertResourceType(type);

        if (type == ResourceNode.ResourceType.Food)
            item.hungerRestore = 20f;

        RuntimeResourceCache[type] = item;
        RuntimeItemCache[item.itemName] = item;
        return item;
    }

    public static ItemData CreateRuntimeItemByName(string savedItemName)
    {
        if (!string.IsNullOrWhiteSpace(savedItemName) &&
            RuntimeItemCache.TryGetValue(savedItemName, out ItemData cached))
            return cached;

        ItemData item = CreateInstance<ItemData>();
        item.name = $"Runtime_{savedItemName}";
        item.itemName = string.IsNullOrWhiteSpace(savedItemName) ? "Unknown Item" : savedItemName;
        item.description = "Restored from save because the original ItemData asset was not found.";
        item.isStackable = true;
        item.maxStack = 99;

        string normalized = item.itemName.ToLowerInvariant();
        if (normalized.Contains("food") || normalized.Contains("thuc") || normalized.Contains("an"))
        {
            item.itemType = ItemType.Food;
            item.hungerRestore = 20f;
            item.resourceType = ResourceNodeType.Food;
        }
        else if (normalized.Contains("medicine") || normalized.Contains("thuoc"))
        {
            item.itemType = ItemType.Medicine;
            item.healthRestore = 25f;
            item.resourceType = ResourceNodeType.Herb;
        }
        else if (normalized.Contains("key") || normalized.Contains("khoa"))
        {
            item.itemType = ItemType.Tool;
        }
        else if (normalized.Contains("camp") || normalized.Contains("tent") || normalized.Contains("fire"))
        {
            item.itemType = ItemType.Structure;
        }
        else
        {
            item.itemType = ItemType.Resource;
            item.resourceType = ResourceNodeType.None;
        }

        RuntimeItemCache[item.itemName] = item;
        return item;
    }

    private static string GetRuntimeResourceName(ResourceNode.ResourceType type)
    {
        return type switch
        {
            ResourceNode.ResourceType.Wood => "Wood",
            ResourceNode.ResourceType.Stone => "Stone",
            ResourceNode.ResourceType.Food => "Wild Food",
            ResourceNode.ResourceType.Herb => "Herb",
            ResourceNode.ResourceType.Crystal => "Crystal",
            _ => "Resource"
        };
    }

    private static ResourceNodeType ConvertResourceType(ResourceNode.ResourceType type)
    {
        return type switch
        {
            ResourceNode.ResourceType.Wood => ResourceNodeType.Wood,
            ResourceNode.ResourceType.Stone => ResourceNodeType.Stone,
            ResourceNode.ResourceType.Food => ResourceNodeType.Food,
            ResourceNode.ResourceType.Herb => ResourceNodeType.Herb,
            ResourceNode.ResourceType.Crystal => ResourceNodeType.Crystal,
            _ => ResourceNodeType.None
        };
    }
}
