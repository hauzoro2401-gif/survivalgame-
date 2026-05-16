using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Công cụ thiết lập Scene tự động.
/// Gắn tất cả các Manager và hệ thống vào một Scene trống để có thể nhấn Play ngay lập tức.
/// Menu: Tools > CAVE RIFT > 5. Tự động Setup Scene Gameplay
/// </summary>
public class SetupGameScene : EditorWindow
{
    [MenuItem("Tools/CAVE RIFT/5. Tự động Setup Scene Gameplay")]
    public static void AutoSetupScene()
    {
        // Tạo một object GameManager chứa tất cả hệ thống cốt lõi
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
        {
            gmObj = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
        }

        // Add các Component cần thiết vào GameManager
        AddComponentIfMissing<GameManager>(gmObj);
        AddComponentIfMissing<Inventory>(gmObj);
        AddComponentIfMissing<DayNightCycle>(gmObj);
        AddComponentIfMissing<BondingSystem>(gmObj);
        AddComponentIfMissing<EnemySpawner>(gmObj);
        AddComponentIfMissing<AudioManager>(gmObj);
        AddComponentIfMissing<SaveSystem>(gmObj);
        AddComponentIfMissing<ParticleEffects>(gmObj);
        AddComponentIfMissing<EndingManager>(gmObj);
        AddComponentIfMissing<DialogueSystem>(gmObj);
        AddComponentIfMissing<PauseMenu>(gmObj);
        AddComponentIfMissing<PortalSystem>(gmObj);
        AddComponentIfMissing<InventoryUI>(gmObj);

        // Tạo SceneSetup (để sinh map và nhân vật)
        GameObject envObj = GameObject.Find("EnvironmentSetup");
        if (envObj == null)
        {
            envObj = new GameObject("EnvironmentSetup");
            Undo.RegisterCreatedObjectUndo(envObj, "Create EnvironmentSetup");
        }
        AddComponentIfMissing<SceneSetup_ForestGlow>(envObj);

        // Tạo Lore Journal UI
        GameObject loreObj = GameObject.Find("LoreJournalManager");
        if (loreObj == null)
        {
            loreObj = new GameObject("LoreJournalManager");
            Undo.RegisterCreatedObjectUndo(loreObj, "Create LoreJournalManager");
        }
        AddComponentIfMissing<LoreJournalUI>(loreObj);

        // Cấu hình Camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
            Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");
        }
        
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
        AddComponentIfMissing<CameraController>(cam.gameObject);

        DayNightCycle dayNight = gmObj.GetComponent<DayNightCycle>();
        if (dayNight != null)
        {
            var globalLight = Object.FindAnyObjectByType<UnityEngine.Rendering.Universal.Light2D>();
            if (globalLight != null)
                dayNight.globalLight = globalLight;
        }

        // Đánh dấu scene đã thay đổi để lưu
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("✅ [CAVE RIFT] Đã setup thành công tất cả hệ thống vào Scene hiện tại! Bạn có thể nhấn nút PLAY (▶) để chơi ngay.");
        EditorUtility.DisplayDialog("Hoàn thành", "Đã gán toàn bộ scripts cốt lõi vào Scene!\n\nHãy nhấn nút PLAY ở giữa màn hình phía trên để chơi thử ngay.", "OK");
    }

    private static void AddComponentIfMissing<T>(GameObject obj) where T : Component
    {
        if (obj.GetComponent<T>() == null)
        {
            Undo.AddComponent<T>(obj);
        }
    }
}
