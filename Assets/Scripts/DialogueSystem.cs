using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// DialogueData - ScriptableObject chứa 1 đoạn hội thoại.
/// Tạo mới: Assets > Create > Vực Thẳm > Dialogue Data
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Vực Thẳm/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    /// <summary>Người nói có thể là nhân vật hoặc Narrator</summary>
    public enum Speaker { Minh, Linh, Khoa, Trang, Narrator }

    [Header("Thông tin hội thoại")]
    public string dialogueID = "dialogue_01";
    [TextArea(1, 2)]
    public string description = "Mô tả ngắn về đoạn thoại";

    [Header("Nội dung")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Event khi kết thúc")]
    public UnityEvent onDialogueEnd;

    /// <summary>1 dòng thoại</summary>
    [Serializable]
    public class DialogueLine
    {
        public Speaker speaker = Speaker.Narrator;
        [TextArea(2, 4)]
        public string text = "";
        [Tooltip("Biểu cảm (để mở rộng sau)")]
        public string emotion = "normal";
    }
}

/// <summary>
/// DialogueSystem - Hệ thống hội thoại kiểu visual novel.
/// Typewriter effect, ảnh nhân vật trái/phải.
/// Tự động trigger thoại theo tình huống game.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    // === SINGLETON ===
    public static DialogueSystem Instance { get; private set; }

    // === CẤU HÌNH ===
    [Header("Cấu hình")]
    [SerializeField] [Tooltip("Tốc độ gõ chữ (ký tự/giây)")]
    private float typeSpeed = 30f;

    [SerializeField] [Tooltip("Âm thanh gõ chữ (tùy chọn)")]
    private AudioClip typeSFX;

    // === THOẠI TỰ ĐỘNG ===
    [Header("Thoại tự động theo sự kiện")]
    [SerializeField] private DialogueData introDialogue;     // Lần đầu vào game
    [SerializeField] private DialogueData ruinsDialogue;     // Tìm tàn tích
    [SerializeField] private DialogueData lowHPDialogue;     // HP < 20%
    [SerializeField] private List<DialogueData> dayDialogues = new List<DialogueData>(); // Ngày 1,3,5,7

    [Header("Thoại bond (khi đạt 50, 80)")]
    [SerializeField] private List<DialogueData> bond50Dialogues = new List<DialogueData>();
    [SerializeField] private List<DialogueData> bond80Dialogues = new List<DialogueData>();

    // === UI REFERENCES ===
    [Header("UI (tự tạo nếu null)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private TextMeshProUGUI continueHint;

    // === TRẠNG THÁI ===
    private bool isDialogueActive = false;
    private bool isTyping = false;
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private Coroutine typeCoroutine;

    // Tracking thoại đã hiện (tránh lặp)
    private HashSet<string> shownDialogues = new HashSet<string>();
    private HashSet<string> shownBondDialogues = new HashSet<string>();
    private bool hasShownLowHP = false;
    private float lowHPCheckTimer = 0f;
    private float bondCheckTimer = 0f;

    // === EVENTS ===
    /// <summary>Event khi bắt đầu hội thoại</summary>
    public event Action OnDialogueStarted;
    /// <summary>Event khi kết thúc hội thoại</summary>
    public event Action OnDialogueEnded;

    /// <summary>Đang hiển thị hội thoại không</summary>
    public bool IsDialogueActive => isDialogueActive;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Tạo UI nếu chưa có
        if (dialoguePanel == null) CreateDialogueUI();
        else dialoguePanel.SetActive(false);

        // Đăng ký event ngày mới
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart += OnNewDay;
        }

        // Hiện thoại mở đầu sau 2 giây
        if (introDialogue != null && !shownDialogues.Contains("intro"))
        {
            StartCoroutine(DelayedDialogue(introDialogue, 2f, "intro"));
        }
    }

    private void Update()
    {
        // Xử lý input khi đang hiện thoại
        if (isDialogueActive)
        {
            // Space/Enter = next hoặc skip typing
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                if (isTyping)
                    SkipTyping();
                else
                    NextLine();
            }

            // Esc = skip toàn bộ
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                EndDialogue();
            }
            return;
        }

        // Kiểm tra trigger thoại tự động (không khi đang thoại)
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Kiểm tra HP thấp mỗi 5 giây
        lowHPCheckTimer -= Time.deltaTime;
        if (lowHPCheckTimer <= 0f)
        {
            CheckLowHPDialogue();
            lowHPCheckTimer = 5f;
        }

        // Kiểm tra bond mỗi 10 giây
        bondCheckTimer -= Time.deltaTime;
        if (bondCheckTimer <= 0f)
        {
            CheckBondDialogues();
            bondCheckTimer = 10f;
        }
    }

    // ===================================
    // HIỂN THỊ THOẠI
    // ===================================

    /// <summary>
    /// Bắt đầu hiển thị 1 đoạn hội thoại.
    /// </summary>
    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines.Count == 0) return;
        if (isDialogueActive) return;

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        OnDialogueStarted?.Invoke();

        // Pause game nhẹ (không dừng hẳn)
        // Chỉ chặn input nhân vật qua isDialogueActive

        ShowCurrentLine();

        Debug.Log($"[Dialogue] Bắt đầu: {dialogue.dialogueID}");
    }

    /// <summary>
    /// Hiển thị dòng thoại hiện tại với typewriter effect.
    /// </summary>
    private void ShowCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueData.DialogueLine line = currentDialogue.lines[currentLineIndex];

        // Cập nhật tên người nói
        if (speakerNameText != null)
        {
            speakerNameText.text = GetSpeakerName(line.speaker);
            speakerNameText.color = GetSpeakerColor(line.speaker);
        }

        // Cập nhật portrait (trái cho Minh/Linh, phải cho Khoa/Trang)
        UpdatePortraits(line.speaker);

        // Hint tiếp tục
        if (continueHint != null) continueHint.text = "";

        // Bắt đầu typewriter effect
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);
        typeCoroutine = StartCoroutine(TypewriterEffect(line.text));
    }

    /// <summary>
    /// Typewriter effect - gõ từng ký tự.
    /// </summary>
    private IEnumerator TypewriterEffect(string fullText)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(1f / typeSpeed);
        }

        isTyping = false;

        // Hiện hint tiếp tục
        if (continueHint != null)
            continueHint.text = "▼ Space để tiếp tục...";
    }

    /// <summary>
    /// Skip typing → hiện hết text ngay lập tức.
    /// </summary>
    private void SkipTyping()
    {
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);

        if (currentDialogue != null && currentLineIndex < currentDialogue.lines.Count)
        {
            dialogueText.text = currentDialogue.lines[currentLineIndex].text;
        }

        isTyping = false;
        if (continueHint != null)
            continueHint.text = "▼ Space để tiếp tục...";
    }

    /// <summary>
    /// Chuyển sang dòng thoại tiếp theo.
    /// </summary>
    private void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentLine();
        }
    }

    /// <summary>
    /// Kết thúc hội thoại.
    /// </summary>
    public void EndDialogue()
    {
        if (typeCoroutine != null) StopCoroutine(typeCoroutine);

        isDialogueActive = false;
        isTyping = false;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        // Trigger UnityEvent khi kết thúc
        currentDialogue?.onDialogueEnd?.Invoke();

        OnDialogueEnded?.Invoke();

        Debug.Log("[Dialogue] Kết thúc hội thoại");
    }

    // ===================================
    // TRIGGER TỰ ĐỘNG
    // ===================================

    /// <summary>Khi ngày mới bắt đầu (1, 3, 5, 7)</summary>
    private void OnNewDay()
    {
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight == null) return;

        int day = dayNight.CurrentDay;
        int[] triggerDays = { 1, 3, 5, 7 };

        for (int i = 0; i < triggerDays.Length; i++)
        {
            if (day == triggerDays[i] && i < dayDialogues.Count)
            {
                string id = $"day_{day}";
                if (!shownDialogues.Contains(id) && dayDialogues[i] != null)
                {
                    StartCoroutine(DelayedDialogue(dayDialogues[i], 3f, id));
                }
                break;
            }
        }
    }

    /// <summary>Kiểm tra HP nhân vật < 20%</summary>
    private void CheckLowHPDialogue()
    {
        if (hasShownLowHP || lowHPDialogue == null) return;
        if (isDialogueActive) return;

        foreach (var character in GameManager.Instance.characters)
        {
            if (character == null || character.IsDead) continue;

            if (character.health / character.maxHealth < 0.2f)
            {
                hasShownLowHP = true;
                StartDialogue(lowHPDialogue);
                // Reset sau 2 phút (để có thể trigger lại)
                StartCoroutine(ResetFlagAfterDelay(() => hasShownLowHP = false, 120f));
                break;
            }
        }
    }

    /// <summary>Kiểm tra bond đạt mốc 50, 80</summary>
    private void CheckBondDialogues()
    {
        if (isDialogueActive) return;

        BondingSystem bonding = GameManager.Instance.GetComponent<BondingSystem>();
        if (bonding == null) return;

        int[] thresholds = { 50, 80 };
        List<DialogueData>[] dialogueLists = { bond50Dialogues, bond80Dialogues };

        for (int t = 0; t < thresholds.Length; t++)
        {
            int pairIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    float bond = bonding.GetBondLevel(i, j);
                    string key = $"bond_{thresholds[t]}_{i}_{j}";

                    if (bond >= thresholds[t] && !shownBondDialogues.Contains(key))
                    {
                        if (pairIndex < dialogueLists[t].Count && dialogueLists[t][pairIndex] != null)
                        {
                            shownBondDialogues.Add(key);
                            StartDialogue(dialogueLists[t][pairIndex]);
                            return;
                        }
                    }
                    pairIndex++;
                }
            }
        }
    }

    /// <summary>Trigger thoại tàn tích (gọi từ LoreFragment)</summary>
    public void TriggerRuinsDialogue()
    {
        if (ruinsDialogue != null && !shownDialogues.Contains("ruins"))
        {
            shownDialogues.Add("ruins");
            StartDialogue(ruinsDialogue);
        }
    }

    /// <summary>Trigger thoại tùy chỉnh từ bên ngoài</summary>
    public void TriggerDialogue(DialogueData dialogue, string uniqueID = "")
    {
        if (!string.IsNullOrEmpty(uniqueID) && shownDialogues.Contains(uniqueID)) return;
        if (!string.IsNullOrEmpty(uniqueID)) shownDialogues.Add(uniqueID);
        StartDialogue(dialogue);
    }

    // ===================================
    // HELPER
    // ===================================

    private IEnumerator DelayedDialogue(DialogueData dialogue, float delay, string id)
    {
        yield return new WaitForSeconds(delay);
        if (!shownDialogues.Contains(id))
        {
            shownDialogues.Add(id);
            StartDialogue(dialogue);
        }
    }

    private IEnumerator ResetFlagAfterDelay(Action resetAction, float delay)
    {
        yield return new WaitForSeconds(delay);
        resetAction?.Invoke();
    }

    private string GetSpeakerName(DialogueData.Speaker speaker)
    {
        return speaker switch
        {
            DialogueData.Speaker.Minh => "Minh",
            DialogueData.Speaker.Linh => "Linh",
            DialogueData.Speaker.Khoa => "Khoa",
            DialogueData.Speaker.Trang => "Trang",
            DialogueData.Speaker.Narrator => "...",
            _ => "???"
        };
    }

    private Color GetSpeakerColor(DialogueData.Speaker speaker)
    {
        return speaker switch
        {
            DialogueData.Speaker.Minh => new Color(0.3f, 0.5f, 0.9f),
            DialogueData.Speaker.Linh => new Color(0.9f, 0.6f, 0.2f),
            DialogueData.Speaker.Khoa => new Color(0.2f, 0.8f, 0.4f),
            DialogueData.Speaker.Trang => new Color(0.9f, 0.4f, 0.7f),
            DialogueData.Speaker.Narrator => new Color(0.7f, 0.7f, 0.7f),
            _ => Color.white
        };
    }

    private void UpdatePortraits(DialogueData.Speaker speaker)
    {
        // Bật portrait trái hoặc phải tùy nhân vật
        if (leftPortrait != null)
            leftPortrait.gameObject.SetActive(
                speaker == DialogueData.Speaker.Minh || speaker == DialogueData.Speaker.Linh);
        if (rightPortrait != null)
            rightPortrait.gameObject.SetActive(
                speaker == DialogueData.Speaker.Khoa || speaker == DialogueData.Speaker.Trang);

        // Đổi màu portrait placeholder
        if (leftPortrait != null && leftPortrait.gameObject.activeSelf)
            leftPortrait.color = GetSpeakerColor(speaker);
        if (rightPortrait != null && rightPortrait.gameObject.activeSelf)
            rightPortrait.color = GetSpeakerColor(speaker);
    }

    // ===================================
    // TẠO UI TỰ ĐỘNG
    // ===================================

    private void CreateDialogueUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject c = new GameObject("UICanvas");
            canvas = c.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            c.AddComponent<CanvasScaler>();
            c.AddComponent<GraphicRaycaster>();
        }

        // Panel chính (dưới màn hình)
        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = dialoguePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0.3f);
        panelRect.offsetMin = new Vector2(20, 10);
        panelRect.offsetMax = new Vector2(-20, -5);

        Image panelBg = dialoguePanel.AddComponent<Image>();
        panelBg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

        // Portrait trái
        leftPortrait = CreatePortrait(dialoguePanel.transform, "LeftPortrait",
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(80, 80), new Vector2(50, 10));
        leftPortrait.gameObject.SetActive(false);

        // Portrait phải
        rightPortrait = CreatePortrait(dialoguePanel.transform, "RightPortrait",
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(80, 80), new Vector2(-50, 10));
        rightPortrait.gameObject.SetActive(false);

        // Tên người nói
        GameObject nameObj = new GameObject("SpeakerName");
        nameObj.transform.SetParent(dialoguePanel.transform, false);
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 1);
        nameRect.anchorMax = new Vector2(0.3f, 1);
        nameRect.offsetMin = new Vector2(100, -35);
        nameRect.offsetMax = new Vector2(0, -5);
        speakerNameText = nameObj.AddComponent<TextMeshProUGUI>();
        speakerNameText.fontSize = 20;
        speakerNameText.fontStyle = FontStyles.Bold;

        // Nội dung thoại
        GameObject textObj = new GameObject("DialogueText");
        textObj.transform.SetParent(dialoguePanel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.75f);
        textRect.offsetMin = new Vector2(100, 10);
        textRect.offsetMax = new Vector2(-100, -5);
        dialogueText = textObj.AddComponent<TextMeshProUGUI>();
        dialogueText.fontSize = 16;
        dialogueText.color = Color.white;

        // Hint tiếp tục
        GameObject hintObj = new GameObject("ContinueHint");
        hintObj.transform.SetParent(dialoguePanel.transform, false);
        RectTransform hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.7f, 0);
        hintRect.anchorMax = new Vector2(1, 0);
        hintRect.offsetMin = new Vector2(0, 5);
        hintRect.offsetMax = new Vector2(-10, 25);
        continueHint = hintObj.AddComponent<TextMeshProUGUI>();
        continueHint.fontSize = 12;
        continueHint.color = new Color(0.7f, 0.7f, 0.7f);
        continueHint.alignment = TextAlignmentOptions.BottomRight;

        dialoguePanel.SetActive(false);
    }

    private Image CreatePortrait(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        Image img = obj.AddComponent<Image>();
        img.color = Color.gray;
        return img;
    }
}
