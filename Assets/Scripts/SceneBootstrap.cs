using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Ensures the gameplay scene has the required runtime managers even when the
/// editor setup menu has not been run yet.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SceneBootstrap : MonoBehaviour
{
    private void Awake()
    {
        SetupManagers();
        SetupEnvironment();
        SetupCamera();
    }

    private void SetupManagers()
    {
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
            gmObj = new GameObject("GameManager");

        AddIfMissing<GameManager>(gmObj);
        AddIfMissing<Inventory>(gmObj);
        DayNightCycle dayNight = AddIfMissing<DayNightCycle>(gmObj);
        AddIfMissing<BondingSystem>(gmObj);
        AddIfMissing<EnemySpawner>(gmObj);
        AddIfMissing<AudioManager>(gmObj);
        AddIfMissing<SaveSystem>(gmObj);
        AddIfMissing<ParticleEffects>(gmObj);
        AddIfMissing<EndingManager>(gmObj);
        AddIfMissing<DialogueSystem>(gmObj);
        AddIfMissing<PauseMenu>(gmObj);
        AddIfMissing<PortalSystem>(gmObj);
        AddIfMissing<InventoryUI>(gmObj);

        Light2D globalLight = FindAnyObjectByType<Light2D>();
        if (dayNight != null && dayNight.globalLight == null && globalLight != null)
            dayNight.globalLight = globalLight;
    }

    private void SetupEnvironment()
    {
        GameObject envObj = GameObject.Find("EnvironmentSetup");
        if (envObj == null)
            envObj = new GameObject("EnvironmentSetup");

        AddIfMissing<SceneSetup_ForestGlow>(envObj);

        GameObject loreObj = GameObject.Find("LoreJournalManager");
        if (loreObj == null)
            loreObj = new GameObject("LoreJournalManager");

        AddIfMissing<LoreJournalUI>(loreObj);
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        AddIfMissing<CameraController>(cam.gameObject);
    }

    private T AddIfMissing<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }
}
