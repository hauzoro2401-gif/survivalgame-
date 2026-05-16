using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// ProjectSetup - Tự động cấu hình project settings cho CAVE RIFT.
/// Menu: Tools > CAVE RIFT > Fix All Settings
/// </summary>
public static class ProjectSetup
{
    [MenuItem("Tools/CAVE RIFT/Fix All Settings")]
    public static void FixAllSettings()
    {
        int fixes = 0;

        fixes += SetupLayers();
        fixes += SetupTags();
        fixes += SetupPhysics2D();
        fixes += SetupBuildSettings();

        AssetDatabase.SaveAssets();
        Debug.Log($"[ProjectSetup] ✅ Đã fix {fixes} settings!");

        EditorUtility.DisplayDialog(
            "CAVE RIFT — Fix All Settings",
            $"Hoàn tất! Đã áp dụng {fixes} thay đổi.\n\n" +
            "✓ Layer 'Enemy' đã tạo\n" +
            "✓ Tag 'Player' và 'Enemy' đã tạo\n" +
            "✓ Physics2D Layer Matrix đã set\n" +
            "✓ Build Settings đã cập nhật\n\n" +
            "Hướng dẫn tiếp theo:\n" +
            "1. Tools > CAVE RIFT > Create All Game Data\n" +
            "2. Tools > CAVE RIFT > Map Sprites\n" +
            "3. Nhấn Play để test!",
            "OK");
    }

    // ══════════════════════════════════════
    // LAYERS
    // ══════════════════════════════════════
    private static int SetupLayers()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layers = tagManager.FindProperty("layers");
        int fixes = 0;

        string[] requiredLayers = { "Player", "Enemy", "Ground", "Resource", "Portal" };

        foreach (string layerName in requiredLayers)
        {
            if (LayerMask.NameToLayer(layerName) >= 0) continue; // Đã tồn tại

            // Tìm slot trống (8-31)
            for (int i = 8; i < 32; i++)
            {
                SerializedProperty sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[ProjectSetup] ✦ Tạo Layer '{layerName}' tại slot {i}");
                    fixes++;
                    break;
                }
            }
        }

        return fixes;
    }

    // ══════════════════════════════════════
    // TAGS
    // ══════════════════════════════════════
    private static int SetupTags()
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty tags = tagManager.FindProperty("tags");
        int fixes = 0;

        string[] requiredTags = { "Player", "Enemy", "Resource", "Portal", "Camp" };

        foreach (string tagName in requiredTags)
        {
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                { exists = true; break; }
            }

            if (!exists)
            {
                tags.arraySize++;
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
                tagManager.ApplyModifiedProperties();
                Debug.Log($"[ProjectSetup] ✦ Tạo Tag '{tagName}'");
                fixes++;
            }
        }

        return fixes;
    }

    // ══════════════════════════════════════
    // PHYSICS 2D LAYER MATRIX
    // ══════════════════════════════════════
    private static int SetupPhysics2D()
    {
        int playerLayer  = LayerMask.NameToLayer("Player");
        int enemyLayer   = LayerMask.NameToLayer("Enemy");
        int groundLayer  = LayerMask.NameToLayer("Ground");
        int defaultLayer = LayerMask.NameToLayer("Default");

        if (playerLayer < 0 || enemyLayer < 0)
        {
            Debug.LogWarning("[ProjectSetup] Layer chưa tạo xong, skip Physics2D setup. Chạy lại sau.");
            return 0;
        }

        int fixes = 0;

        // Player ↔ Enemy: ON (player bị enemy damage)
        if (Physics2D.GetIgnoreLayerCollision(playerLayer, enemyLayer))
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
            Debug.Log("[ProjectSetup] ✦ Player ↔ Enemy: collision BẬT");
            fixes++;
        }

        // Enemy ↔ Enemy: OFF (không đẩy nhau)
        if (!Physics2D.GetIgnoreLayerCollision(enemyLayer, enemyLayer))
        {
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            Debug.Log("[ProjectSetup] ✦ Enemy ↔ Enemy: collision TẮT");
            fixes++;
        }

        // Player ↔ Player: OFF (không đẩy nhau)
        if (!Physics2D.GetIgnoreLayerCollision(playerLayer, playerLayer))
        {
            Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);
            Debug.Log("[ProjectSetup] ✦ Player ↔ Player: collision TẮT");
            fixes++;
        }

        // Player ↔ Ground: ON
        if (groundLayer >= 0 && Physics2D.GetIgnoreLayerCollision(playerLayer, groundLayer))
        {
            Physics2D.IgnoreLayerCollision(playerLayer, groundLayer, false);
            Debug.Log("[ProjectSetup] ✦ Player ↔ Ground: collision BẬT");
            fixes++;
        }

        return fixes;
    }

    // ══════════════════════════════════════
    // BUILD SETTINGS
    // ══════════════════════════════════════
    private static int SetupBuildSettings()
    {
        int fixes = 0;

        // Tạo MainMenu scene nếu chưa có
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        if (!File.Exists(mainMenuPath))
        {
            CreateMainMenuScene(mainMenuPath);
            fixes++;
        }

        // Thêm vào Build Settings
        var scenes = new List<EditorBuildSettingsScene>();

        // Index 0: MainMenu
        bool mainMenuAdded = false;
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.path == mainMenuPath) mainMenuAdded = true;
        }

        if (!mainMenuAdded)
        {
            scenes.Add(new EditorBuildSettingsScene(mainMenuPath, true));
            Debug.Log("[ProjectSetup] ✦ Thêm MainMenu vào Build Settings (index 0)");
            fixes++;
        }
        else
        {
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path == mainMenuPath) scenes.Add(s);
        }

        // Thêm SampleScene
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";
        bool sampleAdded = false;
        foreach (var s in scenes)
            if (s.path == sampleScenePath) sampleAdded = true;

        if (!sampleAdded && File.Exists(sampleScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(sampleScenePath, true));
            Debug.Log("[ProjectSetup] ✦ Thêm SampleScene vào Build Settings (index 1)");
            fixes++;
        }

        // Thêm các scene còn lại từ build settings cũ (tránh mất)
        foreach (var s in EditorBuildSettings.scenes)
        {
            bool already = false;
            foreach (var existing in scenes)
                if (existing.path == s.path) { already = true; break; }
            if (!already) scenes.Add(s);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        return fixes;
    }

    private static void CreateMainMenuScene(string path)
    {
        // Tạo scene trống
        Scene mainMenu = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

        // Tạo Canvas + MainMenu component
        GameObject canvasObj = new GameObject("Canvas");
        var canvas      = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Thêm MainMenu script nếu có
        if (System.Type.GetType("MainMenu") != null)
        {
            canvasObj.AddComponent(System.Type.GetType("MainMenu"));
        }

        // EventSystem
        GameObject evtSys = new GameObject("EventSystem");
        evtSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evtSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        SceneManager.MoveGameObjectToScene(canvasObj, mainMenu);
        SceneManager.MoveGameObjectToScene(evtSys,    mainMenu);

        EditorSceneManager.SaveScene(mainMenu, path);
        EditorSceneManager.CloseScene(mainMenu, true);

        AssetDatabase.Refresh();
        Debug.Log($"[ProjectSetup] ✦ Tạo MainMenu scene tại {path}");
    }
}
