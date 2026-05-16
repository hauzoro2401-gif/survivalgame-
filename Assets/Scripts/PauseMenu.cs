using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// PauseMenu - Menu tạm dừng game.
/// Phím Esc: pause/resume. Panel: Resume, Save, Load, Settings, Quit.
/// Settings: volume sliders, độ sáng. Confirm dialog khi quit.
/// Thống kê nhanh: ngày, enemies killed, lore.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    // === UI REFERENCES ===
    [Header("UI (tự tạo nếu null)")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmPanel;

    // === SLIDERS ===
    [Header("Settings Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    // === TEXTS ===
    [SerializeField] private TextMeshProUGUI statsText;

    // === TRẠNG THÁI ===
    private bool isPaused = false;
    private bool isShowingConfirm = false;

    private void Start()
    {
        if (pausePanel == null) CreatePauseMenuUI();
        else
        {
            pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Chỉ xử lý trong scene gameplay (không phải MainMenu)
        if (GameManager.Instance == null) return;

        // Không toggle pause khi đang thoại
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsDialogueActive) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isShowingConfirm)
            {
                HideConfirm();
            }
            else if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    // ===================================
    // PAUSE / RESUME
    // ===================================

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Pause();
        }
        else
        {
            Resume();
        }
    }

    private void Pause()
    {
        isPaused = true;
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameManager.GameState.Paused);
        else
            Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        RefreshStats();
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameManager.GameState.Playing);
    }

    // ===================================
    // SAVE / LOAD
    // ===================================

    public void OnSave()
    {
        SaveSystem save = FindAnyObjectByType<SaveSystem>();
        if (save != null)
        {
            save.SaveGame();
            Debug.Log("[PauseMenu] Game đã lưu!");
        }
    }

    public void OnLoad()
    {
        SaveSystem save = FindAnyObjectByType<SaveSystem>();
        if (save != null)
        {
            Resume(); // Unpause trước
            save.LoadGame();
            Debug.Log("[PauseMenu] Game đã load!");
        }
    }

    // ===================================
    // SETTINGS
    // ===================================

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);

        // Sync sliders với AudioManager
        if (AudioManager.Instance != null)
        {
            if (bgmSlider != null) bgmSlider.value = AudioManager.Instance.BGMVolume;
            if (sfxSlider != null) sfxSlider.value = AudioManager.Instance.SFXVolume;
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnBGMVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.BGMVolume = value;
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SFXVolume = value;
    }

    // ===================================
    // QUIT
    // ===================================

    public void ShowConfirmQuit()
    {
        isShowingConfirm = true;
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    private void HideConfirm()
    {
        isShowingConfirm = false;
        if (confirmPanel != null) confirmPanel.SetActive(false);
    }

    public void ConfirmQuit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void CancelQuit()
    {
        HideConfirm();
    }

    // ===================================
    // THỐNG KÊ NHANH
    // ===================================

    private void RefreshStats()
    {
        if (statsText == null) return;

        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        int day = dayNight != null ? dayNight.CurrentDay : 0;
        string time = dayNight != null ? dayNight.GetTimeString() : "";
        int enemies = EndingManager.EnemiesKilled;
        int lore = LoreFragment.CollectedCount;

        int alive = 0;
        if (GameManager.Instance != null)
        {
            foreach (var c in GameManager.Instance.characters)
                if (c != null && !c.IsDead) alive++;
        }

        statsText.text =
            $"☀ {time}\n" +
            $"⚔ Kẻ thù: {enemies}\n" +
            $"📖 Lore: {lore}/10\n" +
            $"👤 Sống: {alive}/4";
    }

    // ===================================
    // TẠO UI TỰ ĐỘNG
    // ===================================

    private void CreatePauseMenuUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("UICanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = c.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            c.AddComponent<GraphicRaycaster>();
        }

        // === PAUSE PANEL ===
        pausePanel = CreatePanel(canvas.transform, "PausePanel",
            Vector2.zero, Vector2.one, new Color(0.02f, 0.02f, 0.05f, 0.9f));

        // Title
        CreateTMP(pausePanel.transform, "PauseTitle", "TẠM DỪNG",
            new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.92f), 36,
            new Color(0.7f, 0.7f, 0.8f), TextAlignmentOptions.Center);

        // Stats
        statsText = CreateTMP(pausePanel.transform, "Stats", "",
            new Vector2(0.35f, 0.68f), new Vector2(0.65f, 0.82f), 14,
            new Color(0.6f, 0.6f, 0.65f), TextAlignmentOptions.Center);

        // Buttons
        float y = 0.62f;
        float h = 0.055f;
        float gap = 0.015f;

        CreatePauseButton(pausePanel.transform, "Tiếp tục", "▶ TIẾP TỤC",
            y, new Color(0.2f, 0.5f, 0.3f), Resume);
        y -= h + gap;

        CreatePauseButton(pausePanel.transform, "Lưu", "💾 LƯU GAME",
            y, new Color(0.3f, 0.4f, 0.6f), OnSave);
        y -= h + gap;

        CreatePauseButton(pausePanel.transform, "Load", "↻ TẢI GAME",
            y, new Color(0.3f, 0.5f, 0.55f), OnLoad);
        y -= h + gap;

        CreatePauseButton(pausePanel.transform, "Settings", "⚙ CÀI ĐẶT",
            y, new Color(0.4f, 0.35f, 0.5f), OpenSettings);
        y -= h + gap;

        CreatePauseButton(pausePanel.transform, "Quit", "✕ THOÁT",
            y, new Color(0.5f, 0.2f, 0.2f), ShowConfirmQuit);

        // === SETTINGS PANEL ===
        settingsPanel = CreatePanel(pausePanel.transform, "SettingsPanel",
            new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.85f),
            new Color(0.08f, 0.06f, 0.12f, 0.95f));

        CreateTMP(settingsPanel.transform, "SetTitle", "CÀI ĐẶT",
            new Vector2(0.1f, 0.85f), new Vector2(0.9f, 0.98f), 28,
            new Color(0.7f, 0.5f, 0.9f), TextAlignmentOptions.Center);

        // BGM Volume
        CreateTMP(settingsPanel.transform, "BGMLabel", "🎵 Nhạc nền:",
            new Vector2(0.1f, 0.68f), new Vector2(0.4f, 0.78f), 16,
            Color.white, TextAlignmentOptions.MidlineLeft);
        bgmSlider = CreateSlider(settingsPanel.transform, "BGMSlider",
            new Vector2(0.42f, 0.68f), new Vector2(0.88f, 0.78f), 0.5f);
        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        // SFX Volume
        CreateTMP(settingsPanel.transform, "SFXLabel", "🔊 Hiệu ứng:",
            new Vector2(0.1f, 0.53f), new Vector2(0.4f, 0.63f), 16,
            Color.white, TextAlignmentOptions.MidlineLeft);
        sfxSlider = CreateSlider(settingsPanel.transform, "SFXSlider",
            new Vector2(0.42f, 0.53f), new Vector2(0.88f, 0.63f), 0.7f);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Close settings button
        CreateSettingsButton(settingsPanel.transform, "CloseSet", "← ĐÓNG",
            new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.18f),
            new Color(0.4f, 0.3f, 0.5f), CloseSettings);

        settingsPanel.SetActive(false);

        // === CONFIRM PANEL ===
        confirmPanel = CreatePanel(pausePanel.transform, "ConfirmPanel",
            new Vector2(0.25f, 0.35f), new Vector2(0.75f, 0.65f),
            new Color(0.1f, 0.05f, 0.05f, 0.97f));

        CreateTMP(confirmPanel.transform, "ConfirmText", "Bạn có muốn thoát không?\n(Tiến trình chưa lưu sẽ mất)",
            new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.9f), 18,
            Color.white, TextAlignmentOptions.Center);

        CreateSettingsButton(confirmPanel.transform, "ConfirmYes", "CÓ, THOÁT",
            new Vector2(0.05f, 0.08f), new Vector2(0.48f, 0.35f),
            new Color(0.6f, 0.2f, 0.2f), ConfirmQuit);

        CreateSettingsButton(confirmPanel.transform, "ConfirmNo", "KHÔNG",
            new Vector2(0.52f, 0.08f), new Vector2(0.95f, 0.35f),
            new Color(0.3f, 0.5f, 0.3f), CancelQuit);

        confirmPanel.SetActive(false);

        // Ẩn toàn bộ
        pausePanel.SetActive(false);
    }

    // ===================================
    // UI HELPERS
    // ===================================

    private GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image bg = obj.AddComponent<Image>();
        bg.color = color;
        return obj;
    }

    private TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize,
        Color color, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.color = color; tmp.alignment = align;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private void CreatePauseButton(Transform parent, string name, string label,
        float y, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        float h = 0.055f;
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, y);
        rect.anchorMax = new Vector2(0.7f, y + h);
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor = bgColor * 0.7f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        CreateTMP(btnObj.transform, "Label", label,
            Vector2.zero, Vector2.one, 18,
            Color.white, TextAlignmentOptions.Center);
    }

    private void CreateSettingsButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = bgColor;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        CreateTMP(btnObj.transform, "Label", label,
            Vector2.zero, Vector2.one, 16,
            Color.white, TextAlignmentOptions.Center);
    }

    private Slider CreateSlider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, float defaultValue)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.value = defaultValue;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.35f);
        bgRect.anchorMax = new Vector2(1, 0.65f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.2f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1, 0.65f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.4f, 0.3f, 0.7f);

        slider.fillRect = fillRect;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.7f, 0.6f, 0.9f);

        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;

        return slider;
    }
}
