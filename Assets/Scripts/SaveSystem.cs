using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// SaveSystem - Lưu/Load game bằng PlayerPrefs + JsonUtility.
/// Auto-save khi bắt đầu ngày mới.
/// Lưu: ngày, inventory, bonds, lore, HP, trại, giờ.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    // === SINGLETON ===
    public static SaveSystem Instance { get; private set; }

    // === CẤU HÌNH ===
    [Header("Cấu hình")]
    [SerializeField] private string saveKey = "CaveRift_SaveData";
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    // === TRẠNG THÁI ===
    private bool hasRegisteredEvents = false;

    /// <summary>Có save data không</summary>
    public static bool HasSaveData => PlayerPrefs.HasKey("CaveRift_SaveData");

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        RegisterDayEvent();

        if (PlayerPrefs.GetInt("CaveRift_ShouldLoadSave", 0) == 1)
            StartCoroutine(LoadSaveAfterSceneSetup());
    }

    private IEnumerator LoadSaveAfterSceneSetup()
    {
        yield return null;
        yield return null;

        PlayerPrefs.DeleteKey("CaveRift_ShouldLoadSave");
        LoadGame();
    }

    /// <summary>Đăng ký auto-save khi ngày mới bắt đầu</summary>
    private void RegisterDayEvent()
    {
        if (hasRegisteredEvents) return;

        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart += AutoSave;
            hasRegisteredEvents = true;
        }
    }

    private void OnDestroy()
    {
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart -= AutoSave;
        }
    }

    // ===================================
    // SAVE
    // ===================================

    /// <summary>Auto-save khi ngày mới bắt đầu</summary>
    private void AutoSave()
    {
        SaveGame();
        Debug.Log("[Save] Auto-save khi bắt đầu ngày mới");
    }

    /// <summary>
    /// Lưu toàn bộ game state vào PlayerPrefs.
    /// </summary>
    public void SaveGame()
    {
        if (GameManager.Instance == null) return;

        SaveData data = new SaveData();

        // === Ngày/giờ ===
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            data.currentDay = dayNight.CurrentDay;
            data.currentHour = dayNight.CurrentHour;
        }

        // === Nhân vật ===
        data.characters = new List<CharacterSaveData>();
        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null) continue;

            CharacterSaveData charData = new CharacterSaveData();
            charData.name = character.characterName;
            charData.health = character.health;
            charData.maxHealth = character.maxHealth;
            charData.isDead = character.IsDead;
            charData.posX = character.transform.position.x;
            charData.posY = character.transform.position.y;

            // Survival stats
            SurvivalSystem survival = character.GetComponent<SurvivalSystem>();
            if (survival != null)
            {
                charData.hunger = survival.hunger;
                charData.thirst = survival.thirst;
                charData.energy = survival.energy;
            }

            data.characters.Add(charData);
        }

        // === Trại ===
        data.campPosX = GameManager.Instance.campPosition.x;
        data.campPosY = GameManager.Instance.campPosition.y;
        data.campBuilt = GameManager.Instance.CampBuilt;

        // === Active character ===
        data.activeCharacterIndex = GameManager.Instance.ActiveCharacterIndex;

        // === Inventory ===
        data.inventoryItems = new List<InventorySaveEntry>();
        Inventory inv = GameManager.Instance.GetComponent<Inventory>();
        if (inv != null)
        {
            var allItems = inv.GetAllItems();
            foreach (var pair in allItems)
            {
                if (pair.Key == null) continue;
                InventorySaveEntry entry = new InventorySaveEntry();
                entry.itemName = pair.Key.itemName;
                entry.amount = pair.Value;
                data.inventoryItems.Add(entry);
            }
        }

        // === Bond levels ===
        data.bondLevels = new List<BondSaveEntry>();
        BondingSystem bonding = GameManager.Instance.GetComponent<BondingSystem>();
        if (bonding != null)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    BondSaveEntry bond = new BondSaveEntry();
                    bond.charA = i;
                    bond.charB = j;
                    bond.level = bonding.GetBondLevel(i, j);
                    data.bondLevels.Add(bond);
                }
            }
        }

        // === Lore collected ===
        data.loreCollected = new List<int>();
        // FIX #3: Null-check trước khi duyệt CollectedLore
        if (LoreFragment.CollectedLore != null)
        {
            foreach (var lore in LoreFragment.CollectedLore)
            {
                if (lore != null)
                    data.loreCollected.Add(lore.fragmentNumber);
            }
        }

        // === Thống kê ===
        data.enemiesKilled = EndingManager.EnemiesKilled;

        // Serialize và lưu
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[Save] Game đã lưu! Ngày {data.currentDay}, {data.characters.Count} nhân vật");
    }

    // ===================================
    // LOAD
    // ===================================

    /// <summary>
    /// Load game từ PlayerPrefs.
    /// </summary>
    public void LoadGame()
    {
        if (!PlayerPrefs.HasKey(saveKey))
        {
            Debug.LogWarning("[Save] Không tìm thấy dữ liệu lưu!");
            return;
        }

        string json = PlayerPrefs.GetString(saveKey);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            Debug.LogError("[Save] Dữ liệu lưu bị hỏng!");
            return;
        }

        ApplySaveData(data);
    }

    /// <summary>
    /// Áp dụng dữ liệu đã load vào game state.
    /// </summary>
    private void ApplySaveData(SaveData data)
    {
        if (GameManager.Instance == null) return;

        // === Nhân vật ===
        for (int i = 0; i < data.characters.Count && i < GameManager.Instance.characters.Count; i++)
        {
            var charData = data.characters[i];
            var character = GameManager.Instance.characters[i];
            if (character == null) continue;

            character.RestoreState(charData.health, charData.maxHealth, charData.isDead);
            character.transform.position = new Vector3(charData.posX, charData.posY, 0f);

            // Restore survival
            SurvivalSystem survival = character.GetComponent<SurvivalSystem>();
            if (survival != null)
            {
                survival.hunger = charData.hunger;
                survival.thirst = charData.thirst;
                survival.energy = charData.energy;
            }

            // Revive nếu cần
            if (charData.isDead && character.health <= 0)
            {
                // Giữ trạng thái chết
            }
            else if (!charData.isDead && character.IsDead)
            {
                character.Revive(charData.health);
            }
        }

        // === Trại ===
        bool restoredCampBuilt = data.campBuilt || GameManager.Instance.CampBuilt;
        GameManager.Instance.RestoreCamp(new Vector2(data.campPosX, data.campPosY), restoredCampBuilt);

        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
            dayNight.RestoreTime(data.currentDay, data.currentHour);

        RestoreInventory(data.inventoryItems);
        RestoreBonds(data.bondLevels);
        LoreFragment.RestoreCollectedFragments(data.loreCollected);

        // === Active character ===
        int activeIndex = GetValidActiveCharacterIndex(data.activeCharacterIndex);
        if (activeIndex >= 0)
            GameManager.Instance.SwitchCharacter(activeIndex);
        else
            GameManager.Instance.CheckGameOver();

        // === Thống kê ===
        EndingManager.EnemiesKilled = data.enemiesKilled;

        Debug.Log($"[Save] Game đã load! Ngày {data.currentDay}");
    }

    /// <summary>
    /// Xóa dữ liệu lưu.
    /// </summary>
    private void RestoreInventory(List<InventorySaveEntry> entries)
    {
        Inventory inv = GameManager.Instance.GetComponent<Inventory>();
        if (inv == null) return;

        inv.ClearAll();
        if (entries == null) return;

        foreach (var entry in entries)
        {
            ItemData item = FindItemByName(entry.itemName);
            if (item != null)
                inv.AddItem(item, entry.amount);
        }
    }

    private void RestoreBonds(List<BondSaveEntry> entries)
    {
        BondingSystem bonding = GameManager.Instance.GetComponent<BondingSystem>();
        if (bonding == null || entries == null) return;

        foreach (var entry in entries)
            bonding.SetBondLevel(entry.charA, entry.charB, entry.level);
    }

    private ItemData FindItemByName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            return null;

        foreach (var item in itemDatabase)
        {
            if (item != null && item.itemName == itemName)
                return item;
        }

        foreach (var item in Resources.LoadAll<ItemData>(""))
        {
            if (item != null && item.itemName == itemName)
                return item;
        }

        foreach (var item in Resources.FindObjectsOfTypeAll<ItemData>())
        {
            if (item != null && item.itemName == itemName)
                return item;
        }

        return ItemData.CreateRuntimeItemByName(itemName);
    }

    private int GetValidActiveCharacterIndex(int requestedIndex)
    {
        var characters = GameManager.Instance.characters;
        if (requestedIndex >= 0 && requestedIndex < characters.Count)
        {
            PlayerController requested = characters[requestedIndex];
            if (requested != null && !requested.IsDead)
                return requestedIndex;
        }

        for (int i = 0; i < characters.Count; i++)
        {
            PlayerController character = characters[i];
            if (character != null && !character.IsDead)
                return i;
        }

        return -1;
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log("[Save] Đã xóa dữ liệu lưu");
    }

    // ===================================
    // DATA CLASSES (cho JsonUtility)
    // ===================================

    [Serializable]
    public class SaveData
    {
        public int currentDay = 1;
        public float currentHour = 6f;
        public List<CharacterSaveData> characters = new List<CharacterSaveData>();
        public float campPosX;
        public float campPosY;
        public bool campBuilt;
        public int activeCharacterIndex;
        public List<InventorySaveEntry> inventoryItems = new List<InventorySaveEntry>();
        public List<BondSaveEntry> bondLevels = new List<BondSaveEntry>();
        public List<int> loreCollected = new List<int>();
        public int enemiesKilled;
    }

    [Serializable]
    public class CharacterSaveData
    {
        public string name;
        public float health;
        public float maxHealth;
        public bool isDead;
        public float posX, posY;
        public float hunger = 100f;
        public float thirst = 100f;
        public float energy = 100f;
    }

    [Serializable]
    public class InventorySaveEntry
    {
        public string itemName;
        public int amount;
    }

    [Serializable]
    public class BondSaveEntry
    {
        public int charA, charB;
        public float level;
    }
}
