using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

/// <summary>
/// EndingManager - Quản lý 3 ending và màn thống kê cuối game.
/// Ending A: Cả 4 sống → về nhà.
/// Ending B: 3 người → 1 ở lại giữ cổng.
/// Ending C: Thiếu lore → thứ gì đó bước qua theo.
/// </summary>
public class EndingManager : MonoBehaviour
{
    // === LOẠI ENDING ===
    public enum EndingType { EndingA, EndingB, EndingC }

    // === UI ===
    [Header("UI (tự tạo nếu null)")]
    [SerializeField] private GameObject endingPanel;
    [SerializeField] private TextMeshProUGUI endingTitleText;
    [SerializeField] private TextMeshProUGUI endingStoryText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject replayButton;

    // === CHỌN ENDING B ===
    [Header("Ending B - Chọn ai ở lại")]
    [SerializeField] private GameObject choicePanel;

    // === TRẠNG THÁI ===
    private bool isEnding = false;
    // FIX #15: Lưu nhân vật được chọn ở lại (Ending B)
    private int sacrificedCharacterIndex = -1;

    // === THỐNG KÊ ===
    /// <summary>Số kẻ thù đã giết (static để track xuyên game)</summary>
    public static int EnemiesKilled { get; set; } = 0;

    /// <summary>
    /// Kích hoạt ending.
    /// </summary>
    public void TriggerEnding(EndingType type)
    {
        if (isEnding) return;
        isEnding = true;

        // Pause game
        if (GameManager.Instance != null)
            GameManager.Instance.SetGameState(GameManager.GameState.Paused);

        // Tạo UI nếu chưa có
        if (endingPanel == null) CreateEndingUI();

        endingPanel.SetActive(true);

        // Hiện ending tương ứng
        switch (type)
        {
            case EndingType.EndingA:
                ShowEndingA();
                break;
            case EndingType.EndingB:
                // FIX #15: Hiện panel chọn ai ở lại trước
                ShowEndingBChoicePanel();
                break;
            case EndingType.EndingC:
                ShowEndingC();
                break;
        }

        Debug.Log($"[Ending] Kích hoạt {type}");
    }

    // ===================================
    // 3 ENDINGS
    // ===================================

    /// <summary>
    /// Ending A (Tốt): Cả 4 sống, về nhà.
    /// </summary>
    private void ShowEndingA()
    {
        if (endingTitleText != null)
        {
            endingTitleText.text = "ENDING A — ĐOÀN TỤ";
            endingTitleText.color = new Color(0.3f, 0.9f, 0.5f);
        }

        string story = "Ánh sáng rực rỡ tràn ngập khi cổng mở ra.\n\n";
        story += "Minh bước đi trước, vững chãi như mọi khi, tay nắm chặt gậy gỗ.\n";
        story += "Linh mỉm cười, tay cầm cuốn nhật ký đầy ắp ghi chép.\n";
        story += "Khoa nhìn lại Vực Thẳm lần cuối — nơi đã dạy cậu ý nghĩa của dũng cảm.\n";
        story += "Trang bước qua cổng, nước mắt lặng lẽ — nhưng là nước mắt hạnh phúc.\n\n";
        story += "Cả nhóm bước qua cổng...\n";
        story += "mang theo ký ức về một thế giới không ai biết đến.\n\n";
        story += "Và Kẻ Gác Cổng — người đã mở đường cho họ —\n";
        story += "cuối cùng cũng được yên nghỉ.";

        if (endingStoryText != null)
            StartCoroutine(TypeEndingText(endingStoryText, story));

        ShowStats();
    }

    /// <summary>
    /// Ending B (Trung): 3 người sống, 1 tự nguyện ở lại.
    /// </summary>
    private void ShowEndingB()
    {
        // Lấy tên nhân vật hy sinh
        string[] charNames = { "Minh", "Linh", "Khoa", "Trang" };
        string sacrificeName = (sacrificedCharacterIndex >= 0 && sacrificedCharacterIndex < charNames.Length)
            ? charNames[sacrificedCharacterIndex]
            : "Một người";

        if (endingTitleText != null)
        {
            endingTitleText.text = "ENDING B — HY SINH";
            endingTitleText.color = new Color(0.9f, 0.7f, 0.2f);
        }

        string story = "Cổng bắt đầu đóng lại... cần có ai đó giữ nó mở.\n\n";
        story += "\"Một người phải ở lại,\" giọng nói vang lên từ cổng.\n";
        story += "\"Nếu không ai giữ, cổng sẽ đóng mãi mãi.\"\n\n";
        story += "Bốn đôi mắt nhìn nhau. Không ai muốn nói lời chia tay.\n\n";
        story += $"\"Tớ sẽ ở lại,\" {sacrificeName} nói — bình tĩnh, dứt khoát.\n\n";
        story += $"{sacrificeName} mỉm cười, đẩy ba người bạn bước qua ánh sáng.\n\n";
        story += "Cổng đóng lại.\n";
        story += "Ba người về nhà.\n";
        story += $"{sacrificeName} trở thành Kẻ Gác Cổng mới.";

        if (endingStoryText != null)
            StartCoroutine(TypeEndingText(endingStoryText, story));

        ShowStats();
    }

    /// <summary>
    /// FIX #15: Hiện panel chọn nhân vật ở lại (Ending B).
    /// </summary>
    private void ShowEndingBChoicePanel()
    {
        // Nếu chỉ còn 1 nhân vật sống → tự động chọn người chết ở lại
        if (GameManager.Instance != null)
        {
            int deadIdx = -1;
            int aliveCount = 0;
            for (int i = 0; i < GameManager.Instance.characters.Count; i++)
            {
                var c = GameManager.Instance.characters[i];
                if (c == null || c.IsDead) deadIdx = i;
                else aliveCount++;
            }
            if (aliveCount <= 3 && deadIdx >= 0)
            {
                SelectSacrifice(deadIdx);
                return;
            }
        }

        // Tạo choice panel nếu chưa có
        if (choicePanel != null) { choicePanel.SetActive(true); return; }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) { SelectSacrifice(0); return; }

        choicePanel = new GameObject("EndingB_ChoicePanel");
        choicePanel.transform.SetParent(canvas.transform, false);
        RectTransform pr = choicePanel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.15f, 0.20f);
        pr.anchorMax = new Vector2(0.85f, 0.80f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;
        choicePanel.AddComponent<Image>().color = new Color(0.04f, 0.02f, 0.09f, 0.96f);

        CreateTMPText(choicePanel.transform, "ChoiceTitle",
            "Cổng sắp đóng...\nAi sẽ ở lại giữ cổng?",
            new Vector2(0f, 0.72f), new Vector2(1f, 1f), 22,
            new Color(1f, 0.85f, 0.3f), TextAlignmentOptions.Center);

        string[] cNames  = { "Minh", "Linh", "Khoa", "Trang" };
        string[] cRoles  = { "Tank · Bảo vệ đến cùng", "Crafter · Để lại tri thức",
                             "Scout · Canh gác mặt trận", "Healer · Chăm sóc linh hồn" };
        Color[]  cColors = {
            new Color(0.25f,0.40f,0.80f), new Color(0.80f,0.45f,0.10f),
            new Color(0.15f,0.65f,0.35f), new Color(0.70f,0.25f,0.55f) };

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            float yMax = 0.68f - i * 0.17f;
            float yMin = yMax - 0.15f;

            var btnObj = new GameObject($"SacrificeBtn_{cNames[i]}");
            btnObj.transform.SetParent(choicePanel.transform, false);
            var br = btnObj.AddComponent<RectTransform>();
            br.anchorMin = new Vector2(0.05f, yMin);
            br.anchorMax = new Vector2(0.95f, yMax);
            br.offsetMin = br.offsetMax = Vector2.zero;
            btnObj.AddComponent<Image>().color = cColors[i];

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectSacrifice(idx));
            ColorBlock cb = btn.colors;
            cb.highlightedColor = Color.white;
            cb.pressedColor     = new Color(0.15f,0.15f,0.15f);
            btn.colors = cb;

            CreateTMPText(btnObj.transform, $"Lbl_{cNames[i]}",
                $"{cNames[i]}  ·  {cRoles[i]}",
                Vector2.zero, Vector2.one, 15, Color.white,
                TextAlignmentOptions.Center, true);
        }
    }

    /// <summary>FIX #15: Callback khi chọn nhân vật hy sinh.</summary>
    public void SelectSacrifice(int characterIndex)
    {
        sacrificedCharacterIndex = characterIndex;
        if (choicePanel != null) choicePanel.SetActive(false);
        ShowEndingB();
    }

    /// <summary>
    /// Ending C (Xấu): Mở cổng khi chưa đủ hiểu biết.
    /// </summary>
    private void ShowEndingC()
    {
        if (endingTitleText != null)
        {
            endingTitleText.text = "ENDING C — BÓNG TỐI";
            endingTitleText.color = new Color(0.9f, 0.2f, 0.2f);
        }

        string story = "Cổng mở ra... nhưng ánh sáng bên kia không ấm áp.\n\n";
        story += "Có gì đó sai. Rất sai.\n\n";
        story += "Những bóng tối bắt đầu tràn qua cổng,\n";
        story += "không phải từ Vực Thẳm — mà từ thế giới bên kia.\n\n";
        story += "\"Các em không hiểu gì cả,\" giọng Kẻ Gác Cổng vang vọng.\n";
        story += "\"Ta không gác cổng để nhốt ai...\"\n";
        story += "\"Ta gác cổng để ngăn THỨ ĐÓ bước sang.\"\n\n";
        story += "Cổng mở... nhưng thứ gì đó đã bước qua theo các em.\n\n";
        story += "Và Vực Thẳm không còn là nơi duy nhất đáng sợ nữa.";

        if (endingStoryText != null)
            StartCoroutine(TypeEndingText(endingStoryText, story));

        ShowStats();
    }

    // ===================================
    // THỐNG KÊ
    // ===================================

    /// <summary>
    /// Hiện màn thống kê cuối game.
    /// </summary>
    private void ShowStats()
    {
        if (statsText == null) return;

        // Lấy thông tin
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        int days = dayNight != null ? dayNight.CurrentDay : 0;

        int loreCount = LoreFragment.CollectedCount;

        // Tính bond cao nhất
        float highestBond = 0f;
        string bondPair = "";
        BondingSystem bonding = GameManager.Instance?.GetComponent<BondingSystem>();
        if (bonding != null)
        {
            string[] names = { "Minh", "Linh", "Khoa", "Trang" };
            for (int i = 0; i < 4; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    float bond = bonding.GetBondLevel(i, j);
                    if (bond > highestBond)
                    {
                        highestBond = bond;
                        bondPair = $"{names[i]} & {names[j]}";
                    }
                }
            }
        }

        // Đếm người sống
        int alive = 0;
        if (GameManager.Instance != null)
        {
            foreach (var c in GameManager.Instance.characters)
            {
                if (c != null && !c.IsDead) alive++;
            }
        }

        string stats = "\n═══════════════════════════\n";
        stats += "         THỐNG KÊ GAME\n";
        stats += "═══════════════════════════\n\n";
        stats += $"  ☀ Số ngày sống sót: {days}\n";
        stats += $"  ⚔ Kẻ thù đã tiêu diệt: {EnemiesKilled}\n";
        stats += $"  📖 Mảnh lore thu thập: {loreCount}/10\n";
        stats += $"  ❤ Bond cao nhất: {bondPair} ({highestBond:F0})\n";
        stats += $"  👤 Nhân vật còn sống: {alive}/4\n";
        stats += "\n═══════════════════════════";

        // Delay hiện stats (sau khi story text chạy xong)
        StartCoroutine(DelayedStats(stats, 3f));
    }

    private IEnumerator DelayedStats(string stats, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (statsText != null)
        {
            statsText.text = stats;
        }

        // Hiện nút chơi lại
        yield return new WaitForSecondsRealtime(2f);
        if (replayButton != null)
            replayButton.SetActive(true);
    }

    // ===================================
    // TYPEWRITER CHO ENDING
    // ===================================

    private IEnumerator TypeEndingText(TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = "";
        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSecondsRealtime(0.03f);
        }
    }

    // ===================================
    // CHƠI LẠI
    // ===================================

    /// <summary>
    /// Reload scene để chơi lại từ đầu.
    /// </summary>
    public void ReplayGame()
    {
        // FIX #2: Destroy GameManager trước khi reload để tránh duplicate Singleton
        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        // Reset static data
        EnemiesKilled = 0;
        LoreFragment.ResetAll();

        // Reset time scale
        Time.timeScale = 1f;

        // Reload scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        Debug.Log("[Ending] Chơi lại game!");
    }

    // ===================================
    // TẠO UI TỰ ĐỘNG
    // ===================================

    private void CreateEndingUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("UICanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        // Panel toàn màn hình
        endingPanel = new GameObject("EndingPanel");
        endingPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = endingPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = endingPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.05f, 0.97f);

        // Game title
        CreateTMPText(endingPanel.transform, "GameTitle", "CAVE RIFT",
            new Vector2(0, 0.92f), new Vector2(1, 1), 16,
            new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.Center);

        // Ending title
        endingTitleText = CreateTMPText(endingPanel.transform, "EndingTitle", "",
            new Vector2(0, 0.82f), new Vector2(1, 0.92f), 32,
            Color.white, TextAlignmentOptions.Center);

        // Story text
        endingStoryText = CreateTMPText(endingPanel.transform, "StoryText", "",
            new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.82f), 16,
            new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Center);

        // Stats text
        statsText = CreateTMPText(endingPanel.transform, "StatsText", "",
            new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.35f), 14,
            new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);

        // Replay button
        replayButton = new GameObject("ReplayButton");
        replayButton.transform.SetParent(endingPanel.transform, false);
        RectTransform btnRect = replayButton.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.02f);
        btnRect.anchorMax = new Vector2(0.65f, 0.08f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        Image btnBg = replayButton.AddComponent<Image>();
        btnBg.color = new Color(0.3f, 0.5f, 0.7f, 0.9f);

        Button btn = replayButton.AddComponent<Button>();
        btn.onClick.AddListener(ReplayGame);
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f);
        colors.pressedColor = new Color(0.2f, 0.4f, 0.6f);
        btn.colors = colors;

        CreateTMPText(replayButton.transform, "BtnLabel", "▶ CHƠI LẠI",
            Vector2.zero, Vector2.one, 18,
            Color.white, TextAlignmentOptions.Center, true);

        replayButton.SetActive(false);
        endingPanel.SetActive(false);
    }

    private TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize,
        Color color, TextAlignmentOptions alignment, bool stretch = false)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;

        if (stretch)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, 0);
        }

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;

        return tmp;
    }
}
