using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Inventory - Kho đồ dùng chung cho cả nhóm (shared inventory).
/// Gắn vào GameObject GameManager để 4 nhân vật dùng chung.
/// Giới hạn 20 slot, lưu dạng Dictionary.
/// </summary>
public class Inventory : MonoBehaviour
{
    // === CẤU HÌNH ===
    [Header("Cấu hình kho đồ")]
    [SerializeField] [Tooltip("Số slot tối đa")]
    private int maxSlots = 20;

    // === DỮ LIỆU ===
    // Dictionary lưu item và số lượng
    private Dictionary<ItemData, int> items = new Dictionary<ItemData, int>();

    // === EVENTS ===
    /// <summary>Event phát ra khi kho đồ thay đổi (UI lắng nghe)</summary>
    public event Action OnInventoryChanged;

    // === PROPERTIES ===
    /// <summary>Số slot đang sử dụng</summary>
    public int UsedSlots => items.Count;

    /// <summary>Số slot tối đa</summary>
    public int MaxSlots => maxSlots;

    /// <summary>Kho đồ đã đầy chưa</summary>
    public bool IsFull => items.Count >= maxSlots;

    /// <summary>
    /// Thêm item vào kho.
    /// Trả về true nếu thêm thành công, false nếu đầy.
    /// </summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // Nếu item đã có trong kho → tăng số lượng
        if (items.ContainsKey(item))
        {
            if (item.isStackable)
            {
                items[item] += amount;
                // Giới hạn stack tối đa
                items[item] = Mathf.Min(items[item], item.maxStack);
            }
            else
            {
                // Không stackable → cần slot mới
                if (IsFull) return false;
                items[item] += amount;
            }
        }
        else
        {
            // Item mới → kiểm tra còn slot không
            if (IsFull)
            {
                Debug.LogWarning("[Inventory] Kho đồ đã đầy! Không thể thêm item.");
                return false;
            }
            items[item] = amount;
        }

        Debug.Log($"[Inventory] +{amount} {item.itemName} (tổng: {items[item]})");

        // Thông báo UI cập nhật
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Xóa item khỏi kho.
    /// Trả về true nếu xóa thành công.
    /// </summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (!items.ContainsKey(item))
        {
            Debug.LogWarning($"[Inventory] Không tìm thấy {item.itemName} trong kho!");
            return false;
        }

        if (items[item] < amount)
        {
            Debug.LogWarning($"[Inventory] Không đủ {item.itemName}! Cần {amount}, có {items[item]}");
            return false;
        }

        items[item] -= amount;

        // Xóa khỏi dictionary nếu hết
        if (items[item] <= 0)
        {
            items.Remove(item);
        }

        Debug.Log($"[Inventory] -{amount} {item.itemName}");

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Kiểm tra có item trong kho không (với số lượng tối thiểu).
    /// </summary>
    public bool HasItem(ItemData item, int minAmount = 1)
    {
        if (item == null) return false;
        return items.ContainsKey(item) && items[item] >= minAmount;
    }

    /// <summary>
    /// Lấy số lượng item hiện có trong kho.
    /// </summary>
    public int GetItemCount(ItemData item)
    {
        if (item == null) return 0;
        return items.TryGetValue(item, out int count) ? count : 0;
    }

    /// <summary>
    /// Lấy toàn bộ danh sách item (dạng copy, an toàn).
    /// </summary>
    public Dictionary<ItemData, int> GetAllItems()
    {
        return new Dictionary<ItemData, int>(items);
    }

    /// <summary>
    /// Lấy danh sách item dưới dạng List (cho UI grid).
    /// </summary>
    public List<KeyValuePair<ItemData, int>> GetItemList()
    {
        return items.ToList();
    }

    /// <summary>
    /// Xóa toàn bộ kho đồ.
    /// </summary>
    public void ClearAll()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
        Debug.Log("[Inventory] Đã xóa toàn bộ kho đồ!");
    }

    /// <summary>
    /// Sử dụng item (ăn thức ăn, dùng thuốc...).
    /// Tự động áp dụng hiệu ứng lên nhân vật active.
    /// </summary>
    public bool UseItem(ItemData item)
    {
        if (!HasItem(item)) return false;

        PlayerController activeChar = GameManager.Instance?.ActiveCharacter;
        if (activeChar == null) return false;

        bool used = false;

        // Áp dụng hiệu ứng theo loại item
        switch (item.itemType)
        {
            case ItemData.ItemType.Food:
                // Ăn → hồi hunger + thirst
                SurvivalSystem survival = activeChar.GetComponent<SurvivalSystem>();
                if (survival != null)
                {
                    if (item.hungerRestore > 0f) survival.Eat(item.hungerRestore);
                    if (item.thirstRestore > 0f) survival.Drink(item.thirstRestore);
                    used = true;
                }
                break;

            case ItemData.ItemType.Medicine:
                // Dùng thuốc → hồi HP
                if (item.healthRestore > 0f)
                {
                    activeChar.Heal(item.healthRestore);
                    used = true;
                }
                break;
        }

        if (used)
        {
            RemoveItem(item, 1);
            Debug.Log($"[Inventory] {activeChar.characterName} đã sử dụng {item.itemName}");
        }

        return used;
    }

    /// <summary>
    /// Mô tả kho đồ hiện tại (debug/console).
    /// </summary>
    public string GetInventoryDescription()
    {
        string desc = $"═══ KHO ĐỒ ({UsedSlots}/{maxSlots} slot) ═══\n";

        if (items.Count == 0)
        {
            desc += "  (Trống)\n";
            return desc;
        }

        foreach (var pair in items)
        {
            desc += $"  • {pair.Key.itemName} x{pair.Value}\n";
        }

        return desc;
    }
}
