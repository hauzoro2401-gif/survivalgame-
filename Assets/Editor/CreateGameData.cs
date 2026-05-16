using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// CreateGameData - Tạo toàn bộ ScriptableObject data cho CAVE RIFT.
/// Menu: Tools > CAVE RIFT > Create All Game Data
/// </summary>
public static class CreateGameData
{
    [MenuItem("Tools/CAVE RIFT/Create All Game Data")]
    public static void CreateAll()
    {
        int created = 0;
        created += CreateItems();
        created += CreateRecipes();
        created += CreateLoreFragments();
        created += CreateDialogues();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CreateGameData] ✅ Đã tạo {created} ScriptableObject!");
        EditorUtility.DisplayDialog(
            "CAVE RIFT — Game Data",
            $"Đã tạo {created} asset thành công!\n\n" +
            "• 10 ItemData → Assets/Data/Items/\n" +
            "• 4 CraftingRecipe → Assets/Data/Recipes/\n" +
            "• 10 LoreData → Assets/Data/Lore/\n" +
            "• 6+ DialogueData → Assets/Data/Dialogues/",
            "OK");
    }

    // ══════════════════════════════════════
    // ITEMS
    // ══════════════════════════════════════
    private static int CreateItems()
    {
        string dir = "Assets/Data/Items";
        EnsureDirectory(dir);
        int count = 0;

        count += MakeItem(dir, "Go",          "Gỗ",            ItemData.ItemType.Resource,  0f, 0f, 0f, 0f,
            "Gỗ thu thập từ cây trong hang. Nguyên liệu chế tạo cơ bản.");

        count += MakeItem(dir, "Da",          "Đá",            ItemData.ItemType.Resource,  0f, 0f, 0f, 0f,
            "Đá từ vách hang. Dùng để xây dựng và chế tạo.");

        count += MakeItem(dir, "ThucAnTho",   "Thức ăn thô",   ItemData.ItemType.Food,      10f, 3f, 0f, 0f,
            "Thức ăn hoang dã chưa chế biến. Hồi đói ít, có thể gây khó chịu.");

        count += MakeItem(dir, "CayThuoc",    "Cây thuốc",     ItemData.ItemType.Resource,  0f, 0f, 0f, 0f,
            "Thảo mộc dược liệu mọc trong hang tối. Nguyên liệu làm thuốc.");

        count += MakeItem(dir, "TinhThe",     "Tinh thể",      ItemData.ItemType.Resource,  0f, 0f, 0f, 0f,
            "Tinh thể phát quang xanh lam. Nguồn năng lượng bí ẩn của Vực Thẳm.");

        count += MakeItem(dir, "LeuVai",      "Lều vải",       ItemData.ItemType.Structure, 0f, 0f, 0f, 0f,
            "Nơi trú ẩn an toàn. Nghỉ ngơi trong lều hồi phục energy nhanh hơn.");

        count += MakeItem(dir, "GayGo",       "Gậy gỗ",        ItemData.ItemType.Weapon,    0f, 0f, 0f, 5f,
            "Gậy gỗ thô. Tăng sức tấn công +5. Vũ khí cơ bản đầu game.");

        count += MakeItem(dir, "ThucAnChin",  "Thức ăn chín",  ItemData.ItemType.Food,      30f, 10f, 0f, 0f,
            "Thức ăn đã nấu chín. Hồi đói và khát tốt hơn nhiều.");

        count += MakeItem(dir, "ThuocCoBan",  "Thuốc cơ bản",  ItemData.ItemType.Medicine,  0f, 0f, 30f, 0f,
            "Thuốc chữa thương từ cây dược liệu. Hồi 30 HP.");

        count += MakeItem(dir, "ChiaKhoaCong","Chìa khóa Cổng",ItemData.ItemType.Tool,      0f, 0f, 0f, 0f,
            "Chìa khóa rơi từ Kẻ Gác Cổng. Dùng để mở cổng dịch chuyển về nhà.",
            isStackable: false, maxStack: 1);

        return count;
    }

    private static int MakeItem(string dir, string fileName, string itemName,
        ItemData.ItemType type, float hunger, float thirst, float health, float damage,
        string desc = "", bool isStackable = true, int maxStack = 99)
    {
        string path = $"{dir}/{fileName}.asset";
        if (File.Exists(path)) return 0;

        ItemData item       = ScriptableObject.CreateInstance<ItemData>();
        item.itemName       = itemName;
        item.description    = string.IsNullOrEmpty(desc) ? $"Nguyên liệu: {itemName}." : desc;
        item.itemType       = type;
        item.hungerRestore  = hunger;
        item.thirstRestore  = thirst;
        item.healthRestore  = health;
        item.damageBonus    = damage;
        item.isStackable    = isStackable;
        item.maxStack       = maxStack;

        AssetDatabase.CreateAsset(item, path);
        Debug.Log($"[Items] ✦ Created: {itemName}");
        return 1;
    }

    // ══════════════════════════════════════
    // CRAFTING RECIPES
    // ══════════════════════════════════════
    private static int CreateRecipes()
    {
        string dir = "Assets/Data/Recipes";
        EnsureDirectory(dir);
        int count = 0;

        // Lấy các ItemData đã tạo
        ItemData go          = Load<ItemData>("Assets/Data/Items/Go.asset");
        ItemData da          = Load<ItemData>("Assets/Data/Items/Da.asset");
        ItemData thucAnTho   = Load<ItemData>("Assets/Data/Items/ThucAnTho.asset");
        ItemData cayThuoc    = Load<ItemData>("Assets/Data/Items/CayThuoc.asset");
        ItemData tinhThe     = Load<ItemData>("Assets/Data/Items/TinhThe.asset");
        ItemData leuVai      = Load<ItemData>("Assets/Data/Items/LeuVai.asset");
        ItemData gayGo       = Load<ItemData>("Assets/Data/Items/GayGo.asset");
        ItemData thucAnChin  = Load<ItemData>("Assets/Data/Items/ThucAnChin.asset");
        ItemData thuocCoBan  = Load<ItemData>("Assets/Data/Items/ThuocCoBan.asset");

        // Lều vải: Gỗ x5 + Đá x2
        if (leuVai != null && go != null && da != null)
        {
            count += MakeRecipe(dir, "Recipe_LeuVai", "Lều vải",
                "Dựng lều trú ẩn tạm thời. Cần gỗ và đá để ổn định.",
                leuVai, 1, new[] { (go, 5), (da, 2) });
        }

        // Gậy gỗ: Gỗ x3
        if (gayGo != null && go != null)
        {
            count += MakeRecipe(dir, "Recipe_GayGo", "Gậy gỗ",
                "Vũ khí cơ bản từ gỗ hang động. Nhẹ, dễ làm.",
                gayGo, 1, new[] { (go, 3) });
        }

        // Thức ăn chín: Thức ăn thô x2
        if (thucAnChin != null && thucAnTho != null)
        {
            count += MakeRecipe(dir, "Recipe_ThucAnChin", "Thức ăn chín",
                "Nấu chín thức ăn hoang dã. Ngon hơn, an toàn hơn.",
                thucAnChin, 2, new[] { (thucAnTho, 2) });
        }

        // Thuốc cơ bản: Cây thuốc x2 + Tinh thể x1
        if (thuocCoBan != null && cayThuoc != null && tinhThe != null)
        {
            count += MakeRecipe(dir, "Recipe_ThuocCoBan", "Thuốc cơ bản",
                "Kết hợp cây dược liệu với tinh thể. Hồi phục vết thương hiệu quả.",
                thuocCoBan, 1, new[] { (cayThuoc, 2), (tinhThe, 1) });
        }

        return count;
    }

    private static int MakeRecipe(string dir, string fileName, string recipeName,
        string desc, ItemData result, int resultAmount,
        (ItemData item, int amount)[] ingredients)
    {
        string path = $"{dir}/{fileName}.asset";
        if (File.Exists(path)) return 0;

        CraftingRecipe recipe   = ScriptableObject.CreateInstance<CraftingRecipe>();
        recipe.recipeName       = recipeName;
        recipe.description      = desc;
        recipe.result           = result;
        recipe.resultAmount     = resultAmount;

        foreach (var (item, amount) in ingredients)
        {
            if (item != null)
                recipe.ingredients.Add(new CraftingRecipe.Ingredient { item = item, amount = amount });
        }

        AssetDatabase.CreateAsset(recipe, path);
        Debug.Log($"[Recipes] ✦ Created: {recipeName}");
        return 1;
    }

    // ══════════════════════════════════════
    // LORE FRAGMENTS
    // ══════════════════════════════════════
    private static int CreateLoreFragments()
    {
        string dir = "Assets/Data/Lore";
        EnsureDirectory(dir);
        int count = 0;

        var lores = new (int num, string title, string content)[]
        {
            (1, "Nhật ký — Ngày đầu",
             "Ngày đầu tiên...\n\nTôi không biết mình đang ở đâu. Ánh sáng xanh vẫn còn in trong mắt. " +
             "Không khí lạnh và ngột ngạt.\n\n— Linh"),

            (2, "Nhật ký — Ngày thứ 3",
             "Đã 3 ngày. Tìm được nước nhưng không có lửa.\n\n" +
             "Sinh vật ban đêm rất đáng sợ. Chúng không giết ngay — chúng chờ đợi.\n\n— Khoa"),

            (3, "Tàn tích — Chữ khắc",
             "Tìm được tàn tích ở khu vực phía Bắc. Chữ viết kỳ lạ.\n\n" +
             "Linh đang cố dịch. Cô ấy nói... đây là lời cảnh báo.\n\n— Minh"),

            (4, "Về chiếc cổng",
             "Cổng... ai đó đã tạo ra nó có chủ ý.\n\n" +
             "Đây không phải tai nạn. Chúng ta được dẫn đến đây.\n\n— Linh"),

            (5, "Kẻ Gác Cổng",
             "Kẻ Gác Cổng từng là người như chúng ta.\n\n" +
             "Hắn mắc kẹt quá lâu cho đến khi hắn... quên mất mình là ai.\n\n— Trang"),

            (6, "Tinh thể kích hoạt",
             "Cần 3 tinh thể để kích hoạt cổng.\n\n" +
             "Chúng nằm sâu trong hang — ở những nơi ánh sáng không chạm tới được.\n\n— Linh"),

            (7, "Bóng Đêm là gì",
             "Bóng Đêm không phải sinh vật thật.\n\n" +
             "Chúng là ký ức của những người đã chết ở đây — đang tìm kiếm... ai đó để thay thế.\n\n— Trang"),

            (8, "Chu kỳ 7 ngày",
             "Thế giới này có chu kỳ 7 ngày.\n\n" +
             "Ngày thứ 7, cổng sẽ yếu nhất. Đó là cơ hội duy nhất.\n\n— Khoa"),

            (9, "Điểm yếu của Kẻ Gác",
             "Kẻ Gác Cổng sợ ánh sáng tinh thể.\n\n" +
             "Đó là điểm yếu duy nhất. Nhưng tiếp cận hắn không đơn giản chút nào.\n\n— Minh"),

            (10, "Sự thật về cổng",
             "Cổng không dẫn về nhà.\n\n" +
             "Nó dẫn đến... [phần này bị xóa]. Chúng ta phải chọn: biết sự thật hay trở về bình yên?\n\n" +
             "— Linh (ghi chú cuối)")
        };

        foreach (var (num, title, content) in lores)
        {
            string path = $"{dir}/Lore_{num:00}.asset";
            if (File.Exists(path)) continue;

            LoreData lore = ScriptableObject.CreateInstance<LoreData>();

            // Gán theo field names thật của LoreData trong LoreFragment.cs:
            // fragmentNumber, title, content, hiddenContent
            SerializedObject so = new SerializedObject(lore);
            var pNum     = so.FindProperty("fragmentNumber");
            var pTitle   = so.FindProperty("title");
            var pContent = so.FindProperty("content");

            if (pNum     != null) pNum.intValue      = num;
            if (pTitle   != null) pTitle.stringValue  = title;
            if (pContent != null) pContent.stringValue = content;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(lore, path);
            Debug.Log($"[Lore] ✦ Created: Mảnh {num} — {title}");
            count++;
        }

        return count;
    }

    // ══════════════════════════════════════
    // DIALOGUES
    // ══════════════════════════════════════
    private static int CreateDialogues()
    {
        string dir = "Assets/Data/Dialogues";
        EnsureDirectory(dir);
        int count = 0;

        // DialogueData thật dùng: dialogueID (string), lines (List<DialogueLine>)
        // DialogueLine có: speaker (enum Speaker), text (string)
        // → Map speaker bằng index: 0=Minh, 1=Linh, 2=Khoa, 3=Trang, 4=Narrator

        var dialogues = new (string fileName, string id, (int speaker, string text)[] lines)[]
        {
            ("Dialogue_Intro", "dialogue_intro", new[]
            {
                (0, "...Cậu ổn không? Trang, trả lời tôi!"),
                (3, "Tôi... ổn. Đau đầu một chút. Chúng ta đang ở đâu vậy?"),
                (2, "Không biết. Nhưng ánh sáng ở đây... không bình thường."),
                (1, "Cổng sáng đó — chúng ta bước qua nó rồi. Giờ không thể quay lại ngay."),
                (0, "Được rồi. Tập hợp lại. Chúng ta cần tìm nước và nơi trú ẩn trước.")
            }),

            ("Dialogue_Ngay1", "dialogue_day1", new[]
            {
                (1, "Tôi đã vẽ sơ đồ khu vực phía Đông. Có nhiều tài nguyên ở đó."),
                (2, "Tôi sẽ đi thám thính. Cần biết địch ở đâu."),
                (0, "Cẩn thận. Đêm nay chúng ta ở lại đây — xây trại trước."),
                (3, "Minh... cậu bị xước ở tay rồi. Để tôi băng bó cho.")
            }),

            ("Dialogue_Ngay3", "dialogue_day3", new[]
            {
                (1, "Tôi dịch được rồi! Chữ trên vách là — 'Ai vào đây tự nguyện hay bị kéo?'"),
                (2, "Câu hỏi kỳ lạ. Ý nó là sao?"),
                (1, "Có lẽ... có ai đó đã mở cổng đó từ phía bên kia."),
                (3, "Hoặc từ phía này. Để dẫn chúng ta vào."),
                (0, "Dù sao thì — mục tiêu không đổi. Chúng ta cần tìm đường ra.")
            }),

            ("Dialogue_Ngay5", "dialogue_day5", new[]
            {
                (2, "Nó to lắm. Tôi nhìn thấy bóng nó từ phía Bắc — to bằng cái nhà."),
                (3, "Kẻ Gác Cổng... Linh, mảnh lore cậu đọc về nó nói gì?"),
                (1, "Nói rằng nó từng là người. Rằng nó sợ ánh sáng tinh thể."),
                (0, "Thì chúng ta cần tinh thể. Tất cả sẵn sàng chưa?")
            }),

            ("Dialogue_Ngay7", "dialogue_day7", new[]
            {
                (1, "Đây là ngày thứ 7. Cổng yếu nhất lúc này."),
                (0, "Chúng ta đã làm được — sống sót 7 ngày ở đây."),
                (3, "Dù điều gì xảy ra sau cổng... tôi không hối hận đã ở đây với các cậu."),
                (2, "Đừng có ủy mị vậy. Đi thôi!"),
                (0, "Đúng rồi. Cùng nhau. Như từ đầu đến giờ.")
            }),

            ("Dialogue_BossDeath", "dialogue_boss_death", new[]
            {
                (4, "Cuối... cùng..."),
                (4, "Ta đã chờ rất lâu... ai đó đủ mạnh để thay ta."),
                (4, "Nhưng ta không thể... để các ngươi qua... nếu chưa hiểu..."),
                (4, "Thứ bên kia cổng... không phải thế giới của các ngươi..."),
                (4, "Hãy... hãy chọn... đúng đắn...")
            })
        };

        foreach (var (fileName, id, lines) in dialogues)
        {
            string path = $"{dir}/{fileName}.asset";
            if (File.Exists(path)) continue;

            DialogueData dialogue = ScriptableObject.CreateInstance<DialogueData>();

            SerializedObject so = new SerializedObject(dialogue);

            // dialogueID (field thật trong DialogueData)
            var pId = so.FindProperty("dialogueID");
            if (pId != null) pId.stringValue = id;

            // lines là List<DialogueLine> với sub-fields: speaker (int enum), text (string)
            var pLines = so.FindProperty("lines");
            if (pLines != null)
            {
                pLines.arraySize = lines.Length;
                for (int i = 0; i < lines.Length; i++)
                {
                    var elem    = pLines.GetArrayElementAtIndex(i);
                    var spkProp = elem.FindPropertyRelative("speaker");
                    var txtProp = elem.FindPropertyRelative("text");

                    if (spkProp != null) spkProp.enumValueIndex = lines[i].speaker;
                    if (txtProp != null) txtProp.stringValue     = lines[i].text;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(dialogue, path);
            Debug.Log($"[Dialogues] ✦ Created: {id}");
            count++;
        }

        return count;
    }

    // ══════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════
    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
// ⚠️ LoreData và DialogueData đã được định nghĩa trong:
//    Assets/Scripts/LoreFragment.cs  (LoreData)
//    Assets/Scripts/DialogueSystem.cs (DialogueData)
// KHÔNG khai báo lại ở đây để tránh compile conflict.

