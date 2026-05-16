using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// GameManager - Singleton quản lý toàn bộ trạng thái game.
/// Quản lý 4 nhân vật học sinh bị dịch chuyển vào "Vực Thẳm".
/// </summary>
public class GameManager : MonoBehaviour
{
    // === SINGLETON ===
    public static GameManager Instance { get; private set; }

    // === KHO ĐỒ CHUNG (Shared Inventory) ===
    /// <summary>Kho đồ dùng chung cho cả nhóm (lấy từ component Inventory trên GameManager)</summary>
    private Inventory _sharedInventory;
    public Inventory SharedInventory
    {
        get
        {
            if (_sharedInventory == null)
                _sharedInventory = GetComponent<Inventory>();
            return _sharedInventory;
        }
    }

    // === GAME STATE ===
    public enum GameState { Playing, Paused, GameOver }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    /// <summary>Event phát ra khi game state thay đổi</summary>
    public event Action<GameState> OnGameStateChanged;

    // === NHÂN VẬT ===
    [Header("Danh sách 4 nhân vật")]
    [Tooltip("Thứ tự: 0-Minh(tank), 1-Linh(crafter), 2-Khoa(scout), 3-Trang(healer)")]
    public List<PlayerController> characters = new List<PlayerController>();

    /// <summary>Chỉ số nhân vật đang được điều khiển (0-3)</summary>
    public int ActiveCharacterIndex { get; private set; } = 0;

    /// <summary>Nhân vật đang được người chơi điều khiển</summary>
    public PlayerController ActiveCharacter => characters.Count > ActiveCharacterIndex
        ? characters[ActiveCharacterIndex]
        : null;

    // === TRẠI ===
    [Header("Vị trí trại base camp")]
    [Tooltip("Vị trí đặt trại của nhóm trong Vực Thẳm")]
    public Vector2 campPosition;

    public bool CampBuilt { get; private set; } = false;

    // === PHÍM TẮT ĐỔI NHÂN VẬT ===
    [Header("Phím tắt đổi nhân vật")]
    [Tooltip("Phím 1-4 để đổi nhân vật nhanh")]
    public Key[] switchKeys = new Key[]
    {
        Key.Digit1, // Minh
        Key.Digit2, // Linh
        Key.Digit3, // Khoa
        Key.Digit4  // Trang
    };

    private void Awake()
    {
        // Fix for corrupted Inspector values after switching from KeyCode to Key
        switchKeys = new Key[] { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

        // Thiết lập Singleton - chỉ tồn tại 1 GameManager duy nhất
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Mặc định điều khiển nhân vật đầu tiên (Minh - tank)
        if (characters.Count > 0)
        {
            SwitchCharacter(0);
        }
    }

    private void Update()
    {
        // Không xử lý input khi game over
        if (CurrentState == GameState.GameOver) return;

        // Pause/Unpause giờ do PauseMenu.cs xử lý (tránh xung đột)

        // Xử lý phím đổi nhân vật (chỉ khi đang chơi)
        if (CurrentState == GameState.Playing && Keyboard.current != null)
        {
            for (int i = 0; i < switchKeys.Length && i < characters.Count; i++)
            {
                if (Keyboard.current[switchKeys[i]].wasPressedThisFrame)
                {
                    SwitchCharacter(i);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Đổi nhân vật đang điều khiển.
    /// index: 0-Minh, 1-Linh, 2-Khoa, 3-Trang
    /// </summary>
    public void SwitchCharacter(int index)
    {
        // Kiểm tra index hợp lệ
        if (index < 0 || index >= characters.Count)
        {
            Debug.LogWarning($"[GameManager] Index nhân vật không hợp lệ: {index}");
            return;
        }

        // Kiểm tra nhân vật có còn sống không
        PlayerController target = characters[index];
        if (target == null || target.IsDead)
        {
            Debug.LogWarning($"[GameManager] Nhân vật {index} đã chết, không thể chuyển!");
            return;
        }

        // Tắt điều khiển nhân vật cũ
        if (ActiveCharacter != null)
        {
            ActiveCharacter.SetControlled(false);
        }

        // Bật điều khiển nhân vật mới
        ActiveCharacterIndex = index;
        target.SetControlled(true);

        Debug.Log($"[GameManager] Đổi sang nhân vật: {target.characterName}");
    }

    /// <summary>
    /// Thay đổi trạng thái game (Playing, Paused, GameOver).
    /// </summary>
    public void SetGameState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;

        // Điều chỉnh timeScale theo trạng thái
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                Debug.Log("[GameManager] Game đang chạy");
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                Debug.Log("[GameManager] Game tạm dừng");
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                Debug.Log("[GameManager] Game Over - Nhóm đã thất bại trong Vực Thẳm!");
                break;
        }

        // Thông báo cho các script khác
        OnGameStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Đặt vị trí trại mới cho nhóm.
    /// </summary>
    public void SetCampPosition(Vector2 position)
    {
        campPosition = position;
        CampBuilt = true;
        Debug.Log($"[GameManager] Đặt trại tại vị trí: {position}");
    }

    public void RestoreCamp(Vector2 position, bool built)
    {
        campPosition = position;
        CampBuilt = built;
    }

    /// <summary>
    /// Kiểm tra xem tất cả nhân vật đã chết chưa → Game Over.
    /// </summary>
    public void CheckGameOver()
    {
        bool allDead = true;
        foreach (var character in characters)
        {
            if (character != null && !character.IsDead)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            SetGameState(GameState.GameOver);
        }
    }

    /// <summary>
    /// Lấy khoảng cách từ một vị trí đến trại.
    /// </summary>
    public float GetDistanceToCamp(Vector2 fromPosition)
    {
        return Vector2.Distance(fromPosition, campPosition);
    }
}
