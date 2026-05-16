using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// MainMenu - Màn hình chính của CAVE RIFT.
/// Scene riêng "MainMenu". Nút: Bắt đầu, Hướng dẫn, Thoát.
/// Hiệu ứng fade in/out khi chuyển scene.
/// </summary>
public class MainMenu : MonoBehaviour
{
    // === CẤU HÌNH ===
    [Header("Cấu hình")]
    [SerializeField] [Tooltip("Tên scene gameplay")]
    private string gameSceneName = "SampleScene";

    [SerializeField] [Tooltip("Thời gian fade (giây)")]
    private float fadeDuration = 1.5f;

    // === UI REFERENCES ===
    [Header("UI (tự tạo nếu null)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private Image fadeOverlay;

    // === TRẠNG THÁI ===
    private bool isTransitioning = false;

    private void Start()
    {
        // Đảm bảo timeScale bình thường
        Time.timeScale = 1f;

        // Tạo UI nếu chưa có
        if (mainPanel == null) CreateMainMenuUI();

        // Fade in từ đen
        StartCoroutine(FadeIn());
    }

    // ===================================
    // NÚT CHỨC NĂNG
    // ===================================

    /// <summary>Bắt đầu game mới</summary>
    public void OnStartGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // Reset static data
        EndingManager.EnemiesKilled = 0;
        LoreFragment.ResetAll();
        PlayerPrefs.DeleteKey("CaveRift_ShouldLoadSave");
        PlayerPrefs.Save();

        StartCoroutine(FadeOutAndLoadScene(gameSceneName));
    }

    /// <summary>Tiếp tục game đã lưu</summary>
    public void OnContinueGame()
    {
        if (isTransitioning) return;

        // Kiểm tra có save không
        if (!PlayerPrefs.HasKey("CaveRift_SaveData"))
        {
            Debug.Log("[MainMenu] Không tìm thấy dữ liệu lưu!");
            return;
        }

        isTransitioning = true;
        PlayerPrefs.SetInt("CaveRift_ShouldLoadSave", 1);
        PlayerPrefs.Save();
        StartCoroutine(FadeOutAndLoadScene(gameSceneName));
        // SaveSystem sẽ tự load sau khi scene load xong
    }

    /// <summary>Mở panel hướng dẫn</summary>
    public void OnShowGuide()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (guidePanel != null) guidePanel.SetActive(true);
    }

    /// <summary>Đóng panel hướng dẫn, quay lại menu</summary>
    public void OnCloseGuide()
    {
        if (guidePanel != null) guidePanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    /// <summary>Thoát game</summary>
    public void OnQuitGame()
    {
        Debug.Log("[MainMenu] Thoát game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ===================================
    // FADE EFFECT
    // ===================================

    private IEnumerator FadeIn()
    {
        if (fadeOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            fadeOverlay.color = new Color(0, 0, 0, 1f - t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.raycastTarget = false;
    }

    private IEnumerator FadeOutAndLoadScene(string sceneName)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.raycastTarget = true;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                float t = elapsed / fadeDuration;
                fadeOverlay.color = new Color(0, 0, 0, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            fadeOverlay.color = Color.black;
        }

        yield return new WaitForSecondsRealtime(0.3f);
        SceneManager.LoadScene(sceneName);
    }

    // ===================================
    // TẠO UI TỰ ĐỘNG
    // ===================================

    private void CreateMainMenuUI()
    {
        // Canvas
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("MenuCanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = c.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            c.AddComponent<GraphicRaycaster>();
        }

        // === BACKGROUND ===
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvas.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.03f, 0.02f, 0.06f);

        // Tạo silhouette decoration
        CreateSilhouetteDecoration(bgObj.transform);

        // === MAIN PANEL ===
        mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero; mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero; mainRect.offsetMax = Vector2.zero;

        // Game Title
        CreateTMPUI(mainPanel.transform, "Title", "CAVE RIFT",
            new Vector2(0.2f, 0.7f), new Vector2(0.8f, 0.92f), 72,
            new Color(0.6f, 0.3f, 0.9f), FontStyles.Bold, TextAlignmentOptions.Center);

        // Subtitle
        CreateTMPUI(mainPanel.transform, "Subtitle", "— Vực Thẳm —",
            new Vector2(0.3f, 0.64f), new Vector2(0.7f, 0.72f), 24,
            new Color(0.5f, 0.5f, 0.6f), FontStyles.Italic, TextAlignmentOptions.Center);

        // Tagline
        CreateTMPUI(mainPanel.transform, "Tagline",
            "4 học sinh. 1 thế giới lạ. Sinh tồn để tìm đường về nhà.",
            new Vector2(0.15f, 0.57f), new Vector2(0.85f, 0.64f), 16,
            new Color(0.6f, 0.6f, 0.65f), FontStyles.Normal, TextAlignmentOptions.Center);

        // Buttons
        float btnY = 0.46f;
        float btnH = 0.06f;
        float gap = 0.015f;

        CreateMenuButton(mainPanel.transform, "BắtĐầu", "▶  BẮT ĐẦU",
            new Vector2(0.32f, btnY), new Vector2(0.68f, btnY + btnH),
            new Color(0.25f, 0.55f, 0.35f), OnStartGame);
        btnY -= (btnH + gap);

        CreateMenuButton(mainPanel.transform, "TiếpTục", "↻  TIẾP TỤC",
            new Vector2(0.32f, btnY), new Vector2(0.68f, btnY + btnH),
            new Color(0.25f, 0.4f, 0.6f), OnContinueGame);
        btnY -= (btnH + gap);

        CreateMenuButton(mainPanel.transform, "HướngDẫn", "?  HƯỚNG DẪN",
            new Vector2(0.32f, btnY), new Vector2(0.68f, btnY + btnH),
            new Color(0.45f, 0.35f, 0.55f), OnShowGuide);
        btnY -= (btnH + gap);

        CreateMenuButton(mainPanel.transform, "Thoát", "✕  THOÁT",
            new Vector2(0.32f, btnY), new Vector2(0.68f, btnY + btnH),
            new Color(0.5f, 0.2f, 0.2f), OnQuitGame);

        // Version text
        CreateTMPUI(mainPanel.transform, "Version", "v1.0 — Unity 6 URP",
            new Vector2(0.7f, 0.01f), new Vector2(0.99f, 0.04f), 12,
            new Color(0.3f, 0.3f, 0.35f), FontStyles.Normal, TextAlignmentOptions.BottomRight);

        // === GUIDE PANEL ===
        guidePanel = new GameObject("GuidePanel");
        guidePanel.transform.SetParent(canvas.transform, false);
        RectTransform guideRect = guidePanel.AddComponent<RectTransform>();
        guideRect.anchorMin = Vector2.zero; guideRect.anchorMax = Vector2.one;
        guideRect.offsetMin = Vector2.zero; guideRect.offsetMax = Vector2.zero;

        // Guide background
        Image guideBg = guidePanel.AddComponent<Image>();
        guideBg.color = new Color(0.04f, 0.03f, 0.08f, 0.97f);

        CreateTMPUI(guidePanel.transform, "GuideTitle", "HƯỚNG DẪN ĐIỀU KHIỂN",
            new Vector2(0.2f, 0.85f), new Vector2(0.8f, 0.95f), 32,
            new Color(0.7f, 0.5f, 0.9f), FontStyles.Bold, TextAlignmentOptions.Center);

        string guideText =
            "╔══════════════════════════════════════╗\n" +
            "║   WASD / Arrows    Di chuyển                  ║\n" +
            "║   1 - 4                    Đổi nhân vật               ║\n" +
            "║   Space                  Tấn công                       ║\n" +
            "║   Shift                    Kỹ năng đặc biệt        ║\n" +
            "║   E                          Thu hoạch tài nguyên  ║\n" +
            "║   I                           Mở kho đồ                   ║\n" +
            "║   J                           Mở nhật ký lore          ║\n" +
            "║   Esc                       Tạm dừng                      ║\n" +
            "╚══════════════════════════════════════╝\n\n" +
            "★ Minh (Tank): HP cao, stun kẻ thù\n" +
            "★ Linh (Crafter): chế tạo giỏi, thu hoạch x2\n" +
            "★ Khoa (Scout): nhanh, dash\n" +
            "★ Trang (Healer): hồi máu đồng đội\n\n" +
            "Mục tiêu: Sinh tồn 7 ngày, tìm 10 mảnh lore,\n" +
            "đánh bại Kẻ Gác Cổng và tìm cổng về nhà!";

        CreateTMPUI(guidePanel.transform, "GuideText", guideText,
            new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.84f), 16,
            new Color(0.8f, 0.8f, 0.8f), FontStyles.Normal, TextAlignmentOptions.Center);

        CreateMenuButton(guidePanel.transform, "CloseGuide", "← QUAY LẠI",
            new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.12f),
            new Color(0.4f, 0.3f, 0.5f), OnCloseGuide);

        guidePanel.SetActive(false);

        // === FADE OVERLAY (trên cùng) ===
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvas.transform, false);
        RectTransform fadeRect = fadeObj.AddComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero; fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero; fadeRect.offsetMax = Vector2.zero;
        fadeOverlay = fadeObj.AddComponent<Image>();
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = true;
    }

    /// <summary>Tạo silhouette 4 nhân vật và hang núi nền</summary>
    private void CreateSilhouetteDecoration(Transform parent)
    {
        // Hang núi (hình tam giác tối)
        for (int i = 0; i < 3; i++)
        {
            GameObject mtn = new GameObject($"Mountain_{i}");
            mtn.transform.SetParent(parent, false);
            RectTransform r = mtn.AddComponent<RectTransform>();
            float xCenter = 0.2f + i * 0.3f;
            float width = 0.25f + i * 0.05f;
            float height = 0.3f + (2 - i) * 0.1f;
            r.anchorMin = new Vector2(xCenter - width / 2f, 0);
            r.anchorMax = new Vector2(xCenter + width / 2f, height);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            Image img = mtn.AddComponent<Image>();
            float shade = 0.04f + i * 0.015f;
            img.color = new Color(shade, shade, shade + 0.02f);
        }

        // 4 silhouette nhân vật (thanh dọc đơn giản)
        Color silColor = new Color(0.06f, 0.04f, 0.08f);
        string[] names = { "Minh", "Linh", "Khoa", "Trang" };
        float[] heights = { 0.22f, 0.18f, 0.20f, 0.19f };

        for (int i = 0; i < 4; i++)
        {
            GameObject sil = new GameObject($"Silhouette_{names[i]}");
            sil.transform.SetParent(parent, false);
            RectTransform r = sil.AddComponent<RectTransform>();
            float x = 0.3f + i * 0.1f;
            r.anchorMin = new Vector2(x, 0.05f);
            r.anchorMax = new Vector2(x + 0.04f, 0.05f + heights[i]);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            Image img = sil.AddComponent<Image>();
            img.color = silColor;
        }
    }

    // ===================================
    // UI HELPERS
    // ===================================

    private TextMeshProUGUI CreateTMPUI(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize,
        Color color, FontStyles style, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.color = color; tmp.fontStyle = style;
        tmp.alignment = align; tmp.enableWordWrapping = true;
        return tmp;
    }

    private void CreateMenuButton(Transform parent, string name, string label,
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
        ColorBlock colors = btn.colors;
        colors.highlightedColor = bgColor * 1.3f;
        colors.pressedColor = bgColor * 0.7f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        CreateTMPUI(btnObj.transform, "Label", label,
            Vector2.zero, Vector2.one, 20,
            Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
    }
}
