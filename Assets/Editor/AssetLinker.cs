using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Bước 5 - AssetLinker: Tự động gán Sprite và Animator vào Prefabs/ScriptableObjects.
/// Menu: Tools > CAVE RIFT > 3. Link Assets
/// </summary>
public class AssetLinker : EditorWindow
{
    [MenuItem("Tools/CAVE RIFT/3. Link Assets")]
    public static void LinkAssets()
    {
        int linkedCount = 0;

        // 1. Link Icons cho ItemData ScriptableObjects
        string[] itemGUIDs = AssetDatabase.FindAssets("t:ItemData");
        foreach (string guid in itemGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null)
            {
                string iconName = GetIconNameForType(item.itemType, item.itemName);
                Sprite icon = LoadSpriteByName(iconName);
                if (icon != null)
                {
                    // Lấy serialized object để gán private field nếu cần, nhưng icon thường public
                    // Giả sử field tên là 'icon' (như yêu cầu trong ItemData.cs)
                    SerializedObject so = new SerializedObject(item);
                    SerializedProperty prop = so.FindProperty("icon");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = icon;
                        so.ApplyModifiedProperties();
                        linkedCount++;
                    }
                }
            }
        }

        // 2. Link Sprites và Animators cho Prefabs (nếu có)
        string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool modified = false;

            // Kiểm tra Character
            PlayerController pc = prefab.GetComponent<PlayerController>();
            if (pc != null)
            {
                string charName = pc.characterName;
                if (string.IsNullOrEmpty(charName)) charName = prefab.name;

                Sprite sprite = LoadSpriteByName($"{charName.ToLower()}_idle");
                if (sprite != null)
                {
                    SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                    if (sr != null) { sr.sprite = sprite; modified = true; }
                }

                UnityEditor.Animations.AnimatorController anim = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>($"Assets/Animations/Characters/{charName}Animator.controller");
                if (anim != null)
                {
                    Animator animator = prefab.GetComponent<Animator>();
                    if (animator == null) animator = prefab.AddComponent<Animator>();
                    animator.runtimeAnimatorController = anim;
                    modified = true;
                }
            }

            // Kiểm tra Enemy
            EnemyBase enemy = prefab.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                string enemyName = prefab.name; // Ví dụ: Gremvak
                Sprite sprite = LoadSpriteByName($"{enemyName.ToLower()}_idle");
                if (sprite != null)
                {
                    SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                    if (sr != null) { sr.sprite = sprite; modified = true; }
                }

                UnityEditor.Animations.AnimatorController anim = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>($"Assets/Animations/Enemies/{enemyName}Animator.controller");
                if (anim != null)
                {
                    Animator animator = prefab.GetComponent<Animator>();
                    if (animator == null) animator = prefab.AddComponent<Animator>();
                    animator.runtimeAnimatorController = anim;
                    modified = true;
                }
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                linkedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Đã gán sprite/animator cho {linkedCount} objects!");
    }

    private static Sprite LoadSpriteByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"{name} t:Sprite");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        
        guids = AssetDatabase.FindAssets($"{name} t:Texture2D");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach(var asset in assets)
            {
                if (asset is Sprite) return (Sprite)asset;
            }
        }
        return null;
    }

    private static string GetIconNameForType(ItemData.ItemType type, string itemName)
    {
        // Phân tích tên để tìm icon phù hợp
        string lowerName = itemName.ToLower();
        if (lowerName.Contains("gỗ") || lowerName.Contains("wood")) return "icon_wood";
        if (lowerName.Contains("đá") || lowerName.Contains("stone")) return "icon_stone";
        if (lowerName.Contains("thô") || lowerName.Contains("raw")) return "icon_food_raw";
        if (lowerName.Contains("chín") || lowerName.Contains("cooked")) return "icon_food_cooked";
        if (lowerName.Contains("cây thuốc") || lowerName.Contains("herb")) return "icon_herb";
        if (lowerName.Contains("tinh thể") || lowerName.Contains("crystal")) return "icon_crystal";
        if (lowerName.Contains("lều") || lowerName.Contains("tent")) return "icon_tent";
        if (lowerName.Contains("gậy") || lowerName.Contains("stick") || lowerName.Contains("vũ khí")) return "icon_sword";
        if (lowerName.Contains("chìa khóa") || lowerName.Contains("key")) return "icon_portal_key";
        if (lowerName.Contains("thuốc") || lowerName.Contains("medicine")) return "icon_medicine";
        
        return "icon_wood"; // Mặc định
    }
}
