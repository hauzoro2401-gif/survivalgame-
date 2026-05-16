using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BondingSystem - Hệ thống gắn kết tình bạn giữa 4 nhân vật.
/// 6 cặp quan hệ: Minh-Linh, Minh-Khoa, Minh-Trang, Linh-Khoa, Linh-Trang, Khoa-Trang.
/// Gắn kết cao → buff, gắn kết thấp → debuff + cãi nhau.
/// Gắn vào GameObject GameManager.
/// </summary>
public class BondingSystem : MonoBehaviour
{
    // === CẤU HÌNH ===
    [Header("Cấu hình gắn kết")]
    [SerializeField] [Tooltip("Gắn kết ban đầu cho tất cả cặp")]
    private float initialBondLevel = 50f;

    [SerializeField] [Tooltip("Ngưỡng gắn kết cao → buff")]
    private float highBondThreshold = 70f;

    [SerializeField] [Tooltip("Ngưỡng gắn kết thấp → debuff")]
    private float lowBondThreshold = 30f;

    [SerializeField] [Tooltip("Khoảng cách tối đa để nhận buff gắn kết (unit)")]
    private float bondBuffRange = 10f;

    [SerializeField] [Tooltip("% tăng damage khi gắn kết cao")]
    private float highBondDamageBonus = 0.10f; // +10%

    [SerializeField] [Tooltip("% giảm damage khi gắn kết thấp")]
    private float lowBondDamagePenalty = 0.05f; // -5%

    // === GẮN KẾT GẦN NHAU ===
    [Header("Tăng gắn kết khi ở gần")]
    [SerializeField] [Tooltip("Khoảng cách gần để tăng gắn kết")]
    private float proximityRange = 5f;

    [SerializeField] [Tooltip("Lượng gắn kết tăng mỗi giây khi ở gần")]
    private float proximityBondRate = 0.1f;

    // === CÃI NHAU ===
    [Header("Thoại cãi nhau (gắn kết thấp)")]
    [SerializeField] [Tooltip("Thời gian giữa mỗi lần thoại cãi nhau (giây)")]
    private float argumentCooldown = 60f;

    // === DỮ LIỆU GẮN KẾT ===
    // Key: "charA_charB" (index nhỏ trước), Value: mức gắn kết 0-100
    private Dictionary<string, float> bondLevels = new Dictionary<string, float>();

    // Timer cho thoại cãi nhau
    private float argumentTimer = 0f;

    // Danh sách thoại cãi nhau random
    private readonly string[] argumentDialogues = new string[]
    {
        "Tại sao cậu lại làm thế?! Chúng ta sẽ chẳng bao giờ về nhà!",
        "Cậu chẳng giúp ích gì cả! Tự mình tớ còn tốt hơn!",
        "Đừng có chỉ huy tớ! Cậu không phải đội trưởng!",
        "Nếu không có tớ, cậu đã chết từ lâu rồi!",
        "Cậu ích kỷ quá! Chỉ nghĩ cho bản thân!",
        "Tớ mệt lắm rồi... Đừng ép tớ nữa...",
        "Vực Thẳm này sẽ nuốt chửng chúng ta nếu cứ cãi nhau!",
        "Cậu có biết tớ sợ đến mức nào không?!"
    };

    private void Start()
    {
        // Khởi tạo 6 cặp gắn kết với giá trị ban đầu
        InitializeBonds();
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Tăng gắn kết khi các nhân vật ở gần nhau
        UpdateProximityBonds();

        // Kiểm tra thoại cãi nhau khi gắn kết thấp
        UpdateArgumentTimer();
    }

    /// <summary>
    /// Khởi tạo 6 cặp quan hệ giữa 4 nhân vật.
    /// </summary>
    private void InitializeBonds()
    {
        // 4 nhân vật → 6 cặp: (0,1), (0,2), (0,3), (1,2), (1,3), (2,3)
        for (int i = 0; i < 4; i++)
        {
            for (int j = i + 1; j < 4; j++)
            {
                string key = GetBondKey(i, j);
                bondLevels[key] = initialBondLevel;
            }
        }

        Debug.Log("[BondingSystem] Đã khởi tạo 6 cặp gắn kết giữa các nhân vật");
    }

    /// <summary>
    /// Tạo key duy nhất cho cặp nhân vật (index nhỏ luôn đứng trước).
    /// </summary>
    private string GetBondKey(int charA, int charB)
    {
        int min = Mathf.Min(charA, charB);
        int max = Mathf.Max(charA, charB);
        return $"{min}_{max}";
    }

    /// <summary>
    /// Tăng gắn kết giữa 2 nhân vật.
    /// Gọi khi: chiến đấu cùng nhau, hồi máu cho nhau, hoàn thành nhiệm vụ chung.
    /// </summary>
    public void IncreaseBond(int charA, int charB, float amount)
    {
        if (charA == charB) return;

        string key = GetBondKey(charA, charB);
        if (!bondLevels.ContainsKey(key)) return;

        float oldLevel = bondLevels[key];
        bondLevels[key] = Mathf.Clamp(bondLevels[key] + amount, 0f, 100f);

        // Log khi vượt ngưỡng quan trọng
        float newLevel = bondLevels[key];
        string nameA = GetCharacterNameByIndex(charA);
        string nameB = GetCharacterNameByIndex(charB);

        if (oldLevel < highBondThreshold && newLevel >= highBondThreshold)
        {
            Debug.Log($"[BondingSystem] ★ {nameA} và {nameB} trở thành bạn thân! (Gắn kết: {newLevel:F0})");
        }
        else if (oldLevel >= lowBondThreshold && newLevel < lowBondThreshold)
        {
            Debug.Log($"[BondingSystem] ⚠ {nameA} và {nameB} đang mâu thuẫn! (Gắn kết: {newLevel:F0})");
        }
    }

    /// <summary>
    /// Giảm gắn kết giữa 2 nhân vật.
    /// Gọi khi: tranh giành tài nguyên, để đồng đội bị thương.
    /// </summary>
    public void DecreaseBond(int charA, int charB, float amount)
    {
        IncreaseBond(charA, charB, -amount);
    }

    /// <summary>
    /// Lấy mức gắn kết giữa 2 nhân vật (0-100).
    /// </summary>
    public float GetBondLevel(int charA, int charB)
    {
        if (charA == charB) return 100f; // Tự gắn kết với bản thân = max

        string key = GetBondKey(charA, charB);
        return bondLevels.TryGetValue(key, out float level) ? level : initialBondLevel;
    }

    public void SetBondLevel(int charA, int charB, float level)
    {
        if (charA == charB) return;

        string key = GetBondKey(charA, charB);
        bondLevels[key] = Mathf.Clamp(level, 0f, 100f);
    }

    /// <summary>
    /// Lấy hệ số damage bonus/penalty dựa trên gắn kết.
    /// Trả về hệ số nhân (VD: 1.1 = +10%, 0.95 = -5%).
    /// charA là người tấn công, charB là đồng đội gần nhất.
    /// </summary>
    public float GetDamageMultiplier(int charA, int charB)
    {
        float bond = GetBondLevel(charA, charB);

        // Kiểm tra khoảng cách giữa 2 nhân vật
        if (!AreCharactersInRange(charA, charB, bondBuffRange))
            return 1f; // Quá xa → không buff/debuff

        if (bond >= highBondThreshold)
        {
            // Gắn kết cao → buff damage +10%
            return 1f + highBondDamageBonus;
        }
        else if (bond < lowBondThreshold)
        {
            // Gắn kết thấp → debuff damage -5%
            return 1f - lowBondDamagePenalty;
        }

        return 1f; // Trung bình → không bonus
    }

    /// <summary>
    /// Kiểm tra 2 nhân vật có đứng trong khoảng cách cho phép không.
    /// </summary>
    private bool AreCharactersInRange(int charA, int charB, float range)
    {
        var characters = GameManager.Instance.characters;

        if (charA < 0 || charA >= characters.Count) return false;
        if (charB < 0 || charB >= characters.Count) return false;

        var a = characters[charA];
        var b = characters[charB];

        if (a == null || b == null) return false;
        if (a.IsDead || b.IsDead) return false;

        return Vector2.Distance(a.transform.position, b.transform.position) <= range;
    }

    /// <summary>
    /// Cập nhật gắn kết khi nhân vật ở gần nhau (mỗi frame).
    /// Ở gần = tăng tình bạn tự nhiên.
    /// </summary>
    private void UpdateProximityBonds()
    {
        var characters = GameManager.Instance.characters;

        for (int i = 0; i < characters.Count; i++)
        {
            for (int j = i + 1; j < characters.Count; j++)
            {
                if (AreCharactersInRange(i, j, proximityRange))
                {
                    // Ở gần nhau → tăng gắn kết từ từ
                    IncreaseBond(i, j, proximityBondRate * Time.deltaTime);
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra và hiển thị thoại cãi nhau khi gắn kết thấp.
    /// </summary>
    private void UpdateArgumentTimer()
    {
        argumentTimer -= Time.deltaTime;
        if (argumentTimer > 0f) return;

        // Tìm cặp có gắn kết thấp
        var characters = GameManager.Instance.characters;
        for (int i = 0; i < characters.Count; i++)
        {
            for (int j = i + 1; j < characters.Count; j++)
            {
                float bond = GetBondLevel(i, j);
                if (bond < lowBondThreshold && AreCharactersInRange(i, j, proximityRange))
                {
                    // Cãi nhau!
                    string nameA = GetCharacterNameByIndex(i);
                    string nameB = GetCharacterNameByIndex(j);
                    string dialogue = argumentDialogues[Random.Range(0, argumentDialogues.Length)];

                    Debug.Log($"[BondingSystem] 💢 {nameA} nói với {nameB}: \"{dialogue}\"");

                    argumentTimer = argumentCooldown;
                    return; // Chỉ 1 cuộc cãi nhau mỗi lần
                }
            }
        }
    }

    /// <summary>
    /// Lấy tên nhân vật theo index.
    /// </summary>
    private string GetCharacterNameByIndex(int index)
    {
        return index switch
        {
            0 => "Minh",
            1 => "Linh",
            2 => "Khoa",
            3 => "Trang",
            _ => $"Nhân vật {index}"
        };
    }

    /// <summary>
    /// Lấy mô tả trạng thái gắn kết cho tất cả cặp (debug/UI).
    /// </summary>
    public string GetAllBondDescriptions()
    {
        string desc = "═══ TRẠNG THÁI GẮN KẾT ═══\n";

        var characters = GameManager.Instance.characters;
        for (int i = 0; i < characters.Count; i++)
        {
            for (int j = i + 1; j < characters.Count; j++)
            {
                float bond = GetBondLevel(i, j);
                string nameA = GetCharacterNameByIndex(i);
                string nameB = GetCharacterNameByIndex(j);

                string emoji;
                if (bond >= highBondThreshold) emoji = "❤️";
                else if (bond < lowBondThreshold) emoji = "💔";
                else emoji = "💛";

                desc += $"  {emoji} {nameA} ↔ {nameB}: {bond:F0}/100\n";
            }
        }

        return desc;
    }
}
