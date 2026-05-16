using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// CraftingSystem - Hệ thống chế tạo vật phẩm.
/// Gắn vào GameObject GameManager.
/// Linh (Crafter) có bonus: craft nhanh, tốn ít nguyên liệu hơn 1.
/// </summary>
public class CraftingSystem : MonoBehaviour
{
    // === CÔNG THỨC CHẾ TẠO ===
    [Header("Danh sách công thức chế tạo")]
    [SerializeField] [Tooltip("Kéo các CraftingRecipe ScriptableObject vào đây")]
    private List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    /// <summary>Danh sách công thức (read-only)</summary>
    public List<CraftingRecipe> Recipes => recipes;

    // === EVENTS ===
    /// <summary>Event khi craft thành công</summary>
    public event Action<CraftingRecipe> OnCraftSuccess;

    private void Awake()
    {
        CreateFallbackRecipesIfNeeded();
    }

    /// <summary>
    /// Kiểm tra xem có thể craft recipe này không.
    /// Xét cả bonus của Linh (giảm 1 nguyên liệu mỗi loại).
    /// </summary>
    public bool CanCraft(CraftingRecipe recipe)
    {
        if (recipe == null) return false;

        Inventory inventory = GetSharedInventory();
        if (inventory == null) return false;

        // Kiểm tra nhân vật hiện tại có phải Linh không
        bool isCrafter = IsActiveCrafter();

        // Kiểm tra từng nguyên liệu
        foreach (var ingredient in recipe.ingredients)
        {
            int required = ingredient.amount;

            // FIX #7: Dùng CraftingBonus multiplier nhất quán với ResourceNode
            // Linh craft → tốn ít nguyên liệu hơn (chia cho craftingBonus, tối thiểu 1)
            if (isCrafter)
            {
                CharacterStats stats = GameManager.Instance?.ActiveCharacter?.GetComponent<CharacterStats>();
                float bonus = stats != null ? stats.CraftingBonus : 2f;
                required = Mathf.Max(1, Mathf.CeilToInt(required / bonus));
            }

            if (!inventory.HasItem(ingredient.item, required))
            {
                return false;
            }
        }

        // Kiểm tra còn slot để chứa sản phẩm
        if (!inventory.HasItem(recipe.result) && inventory.IsFull)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Thực hiện chế tạo.
    /// Trừ nguyên liệu và thêm sản phẩm vào kho.
    /// </summary>
    public bool Craft(CraftingRecipe recipe)
    {
        if (!CanCraft(recipe))
        {
            Debug.LogWarning($"[CraftingSystem] Không thể chế tạo {recipe.recipeName}! Thiếu nguyên liệu.");
            return false;
        }

        Inventory inventory = GetSharedInventory();
        if (inventory == null) return false;

        bool isCrafter = IsActiveCrafter();

        // Trừ nguyên liệu
        foreach (var ingredient in recipe.ingredients)
        {
            int cost = ingredient.amount;

            // Linh → giảm 1 nguyên liệu
            if (isCrafter && cost > 1)
            {
                cost -= 1;
            }

            inventory.RemoveItem(ingredient.item, cost);
        }

        // Thêm sản phẩm
        int resultAmount = recipe.resultAmount;
        inventory.AddItem(recipe.result, resultAmount);

        // Log kết quả
        string crafterName = GameManager.Instance?.ActiveCharacter?.characterName ?? "???";
        Debug.Log($"[CraftingSystem] ✦ {crafterName} đã chế tạo {resultAmount}x {recipe.recipeName}!");

        if (isCrafter)
        {
            Debug.Log("[CraftingSystem] ★ Bonus Crafter: tiết kiệm nguyên liệu!");
        }

        // Phát event
        OnCraftSuccess?.Invoke(recipe);

        return true;
    }

    /// <summary>
    /// Kiểm tra nhân vật active có phải Linh (Crafter) không.
    /// </summary>
    private bool IsActiveCrafter()
    {
        if (GameManager.Instance == null) return false;

        PlayerController active = GameManager.Instance.ActiveCharacter;
        if (active == null) return false;

        CharacterStats stats = active.GetComponent<CharacterStats>();
        return stats != null && stats.Type == CharacterStats.CharacterType.Linh;
    }

    /// <summary>
    /// Lấy shared inventory từ GameManager.
    /// </summary>
    private Inventory GetSharedInventory()
    {
        if (GameManager.Instance == null) return null;
        return GameManager.Instance.GetComponent<Inventory>();
    }

    private void CreateFallbackRecipesIfNeeded()
    {
        if (recipes.Count > 0) return;

        ItemData wood = ItemData.CreateRuntimeResource(ResourceNode.ResourceType.Wood);
        ItemData stone = ItemData.CreateRuntimeResource(ResourceNode.ResourceType.Stone);
        ItemData food = ItemData.CreateRuntimeResource(ResourceNode.ResourceType.Food);
        ItemData campfire = ItemData.CreateRuntimeItemByName("Campfire");
        ItemData cookedFood = ItemData.CreateRuntimeItemByName("Cooked Food");
        cookedFood.itemType = ItemData.ItemType.Food;
        cookedFood.hungerRestore = 35f;

        CraftingRecipe campfireRecipe = CreateRuntimeRecipe("Campfire", "Build a safer camp point.", campfire, 1);
        AddRuntimeIngredient(campfireRecipe, wood, 3);
        AddRuntimeIngredient(campfireRecipe, stone, 2);
        recipes.Add(campfireRecipe);

        CraftingRecipe cookedFoodRecipe = CreateRuntimeRecipe("Cooked Food", "Cook wild food into a stronger meal.", cookedFood, 1);
        AddRuntimeIngredient(cookedFoodRecipe, food, 2);
        AddRuntimeIngredient(cookedFoodRecipe, wood, 1);
        recipes.Add(cookedFoodRecipe);
    }

    private CraftingRecipe CreateRuntimeRecipe(string recipeName, string description,
        ItemData result, int resultAmount)
    {
        CraftingRecipe recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
        recipe.name = $"RuntimeRecipe_{recipeName}";
        recipe.recipeName = recipeName;
        recipe.description = description;
        recipe.result = result;
        recipe.resultAmount = resultAmount;

        return recipe;
    }

    private void AddRuntimeIngredient(CraftingRecipe recipe, ItemData item, int amount)
    {
        recipe.ingredients.Add(new CraftingRecipe.Ingredient
        {
            item = item,
            amount = amount
        });
    }

    /// <summary>
    /// Lấy danh sách recipe có thể craft được ngay.
    /// </summary>
    public List<CraftingRecipe> GetCraftableRecipes()
    {
        List<CraftingRecipe> craftable = new List<CraftingRecipe>();

        foreach (var recipe in recipes)
        {
            if (CanCraft(recipe))
            {
                craftable.Add(recipe);
            }
        }

        return craftable;
    }

    /// <summary>
    /// Lấy mô tả chi tiết 1 recipe (cho UI tooltip).
    /// </summary>
    public string GetRecipeDescription(CraftingRecipe recipe)
    {
        if (recipe == null) return "";

        Inventory inventory = GetSharedInventory();
        bool isCrafter = IsActiveCrafter();

        string desc = $"<b>{recipe.recipeName}</b>\n";
        desc += $"{recipe.description}\n\n";
        desc += "Nguyên liệu cần:\n";

        foreach (var ingredient in recipe.ingredients)
        {
            int required = ingredient.amount;
            if (isCrafter && required > 1)
                required -= 1;

            int have = inventory != null ? inventory.GetItemCount(ingredient.item) : 0;
            string status = have >= required ? "✓" : "✗";

            desc += $"  {status} {ingredient.item.itemName}: {have}/{required}\n";
        }

        desc += $"\n→ Kết quả: {recipe.resultAmount}x {recipe.result.itemName}";

        return desc;
    }
}

/// <summary>
/// CraftingRecipe - ScriptableObject định nghĩa 1 công thức chế tạo.
/// Tạo mới: Assets > Create > Vực Thẳm > Crafting Recipe
/// </summary>
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Vực Thẳm/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Thông tin công thức")]
    [Tooltip("Tên công thức")]
    public string recipeName = "Công thức mới";

    [TextArea(2, 3)]
    [Tooltip("Mô tả cách chế tạo")]
    public string description = "";

    [Header("Nguyên liệu đầu vào")]
    public List<Ingredient> ingredients = new List<Ingredient>();

    [Header("Sản phẩm đầu ra")]
    [Tooltip("Item kết quả sau khi chế tạo")]
    public ItemData result;

    [Tooltip("Số lượng sản phẩm")]
    public int resultAmount = 1;

    /// <summary>
    /// 1 nguyên liệu trong công thức.
    /// </summary>
    [Serializable]
    public class Ingredient
    {
        [Tooltip("Item nguyên liệu")]
        public ItemData item;

        [Tooltip("Số lượng cần")]
        public int amount = 1;
    }
}
