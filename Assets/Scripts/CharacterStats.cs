using UnityEngine;

/// <summary>
/// CharacterStats - Chỉ số riêng biệt cho mỗi nhân vật.
/// Tự động apply vào PlayerController khi Awake.
/// Gắn vào mỗi nhân vật cùng với PlayerController.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class CharacterStats : MonoBehaviour
{
    // === LOẠI NHÂN VẬT ===
    public enum CharacterType
    {
        Minh,   // Tank - HP cao, phòng thủ mạnh
        Linh,   // Crafter - chế tạo giỏi
        Khoa,   // Scout - nhanh, tầm nhìn xa
        Trang   // Healer - hồi máu cho đồng đội
    }

    [Header("Loại nhân vật")]
    [SerializeField] [Tooltip("Chọn nhân vật để tự động áp dụng chỉ số")]
    private CharacterType characterType = CharacterType.Minh;

    /// <summary>Loại nhân vật hiện tại</summary>
    public CharacterType Type => characterType;

    // === CHỈ SỐ CƠ BẢN ===
    [Header("Chỉ số (tự động set theo CharacterType)")]
    [SerializeField] private float baseHP = 100f;
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float defense = 1f;
    [SerializeField] private float attackPower = 5f;

    // === CHỈ SỐ ĐẶC BIỆT ===
    [Header("Chỉ số đặc biệt (chỉ một số nhân vật có)")]
    [SerializeField] [Tooltip("Linh: hệ số chế tạo (2x = chế nhanh gấp đôi)")]
    private float craftingBonus = 1f;

    [SerializeField] [Tooltip("Khoa: tầm nhìn xa (đơn vị unit)")]
    private float visionRange = 10f;

    [SerializeField] [Tooltip("Trang: sức mạnh hồi máu")]
    private float healPower = 0f;

    // === PROPERTIES ===
    public float BaseHP => baseHP;
    public float BaseSpeed => baseSpeed;
    public float Defense => defense;
    public float AttackPower => attackPower;
    public float CraftingBonus => craftingBonus;
    public float VisionRange => visionRange;
    public float HealPower => healPower;

    // === REFERENCES ===
    private PlayerController playerController;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();

        // Khởi tạo chỉ số theo loại nhân vật
        InitializeStats();

        // Áp dụng chỉ số vào PlayerController
        ApplyStatsToController();
    }

    public void SetCharacterType(CharacterType type)
    {
        characterType = type;
        InitializeStats();
        ApplyStatsToController();
    }

    /// <summary>
    /// Khởi tạo chỉ số riêng cho từng nhân vật dựa trên CharacterType.
    /// </summary>
    private void InitializeStats()
    {
        switch (characterType)
        {
            case CharacterType.Minh:
                // Tank: HP cao, phòng thủ vững, tấn công mạnh, di chuyển chậm
                baseHP = 150f;
                baseSpeed = 4f;
                defense = 3f;
                attackPower = 12f;
                craftingBonus = 1f;
                visionRange = 10f;
                healPower = 0f;
                break;

            case CharacterType.Linh:
                // Crafter: HP thấp, chế tạo gấp đôi, tấn công yếu
                baseHP = 90f;
                baseSpeed = 5f;
                defense = 1f;
                attackPower = 6f;
                craftingBonus = 2f;  // Chế tạo nhanh gấp đôi
                visionRange = 10f;
                healPower = 0f;
                break;

            case CharacterType.Khoa:
                // Scout: nhanh nhất, tầm nhìn xa, HP trung bình
                baseHP = 100f;
                baseSpeed = 8f;
                defense = 1f;
                attackPower = 8f;
                craftingBonus = 1f;
                visionRange = 15f;  // Nhìn xa hơn
                healPower = 0f;
                break;

            case CharacterType.Trang:
                // Healer: cân bằng, có khả năng hồi máu
                baseHP = 100f;
                baseSpeed = 5f;
                defense = 2f;
                attackPower = 5f;
                craftingBonus = 1f;
                visionRange = 10f;
                healPower = 20f;  // Hồi 20 HP mỗi lần
                break;
        }
    }

    /// <summary>
    /// Áp dụng chỉ số từ CharacterStats vào PlayerController.
    /// Gọi trong Awake để đảm bảo chỉ số được set trước Start.
    /// </summary>
    private void ApplyStatsToController()
    {
        if (playerController == null) return;

        // Set tên và vai trò
        playerController.characterName = GetCharacterName();
        playerController.characterRole = GetRoleName();

        // Set chỉ số cơ bản
        playerController.maxHealth = baseHP;
        playerController.health = baseHP;
        playerController.speed = baseSpeed;

        Debug.Log($"[CharacterStats] Đã áp dụng chỉ số cho {playerController.characterName} ({playerController.characterRole})");
    }

    /// <summary>
    /// Lấy tên nhân vật theo CharacterType.
    /// </summary>
    public string GetCharacterName()
    {
        return characterType switch
        {
            CharacterType.Minh => "Minh",
            CharacterType.Linh => "Linh",
            CharacterType.Khoa => "Khoa",
            CharacterType.Trang => "Trang",
            _ => "Không rõ"
        };
    }

    /// <summary>
    /// Lấy tên vai trò theo CharacterType.
    /// </summary>
    public string GetRoleName()
    {
        return characterType switch
        {
            CharacterType.Minh => "Tank",
            CharacterType.Linh => "Crafter",
            CharacterType.Khoa => "Scout",
            CharacterType.Trang => "Healer",
            _ => "None"
        };
    }

    /// <summary>
    /// Tính sát thương thực nhận (sau khi trừ defense).
    /// Tối thiểu nhận 1 damage.
    /// </summary>
    public float CalculateDamageTaken(float rawDamage)
    {
        float reduced = rawDamage - defense;
        return Mathf.Max(reduced, 1f); // Tối thiểu 1 damage
    }

    /// <summary>
    /// Trả về chuỗi mô tả đầy đủ chỉ số nhân vật.
    /// Dùng cho UI thông tin nhân vật hoặc debug.
    /// </summary>
    public string GetStatDescription()
    {
        string desc = $"═══ {GetCharacterName()} - {GetRoleName()} ═══\n";
        desc += $"  ♥ HP: {baseHP}\n";
        desc += $"  ► Tốc độ: {baseSpeed}\n";
        desc += $"  ◆ Phòng thủ: {defense}\n";
        desc += $"  ✦ Sức tấn công: {attackPower}\n";

        // Hiển thị chỉ số đặc biệt (chỉ khi có giá trị đặc biệt)
        switch (characterType)
        {
            case CharacterType.Linh:
                desc += $"  ★ Chế tạo: x{craftingBonus} (nhanh gấp đôi!)\n";
                break;
            case CharacterType.Khoa:
                desc += $"  ★ Tầm nhìn: {visionRange} unit (trinh sát xa)\n";
                break;
            case CharacterType.Trang:
                desc += $"  ★ Sức hồi: {healPower} HP/lần\n";
                break;
        }

        return desc;
    }
}
