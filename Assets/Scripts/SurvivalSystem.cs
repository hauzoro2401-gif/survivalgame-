using UnityEngine;

/// <summary>
/// SurvivalSystem - Hệ thống sinh tồn gắn vào mỗi nhân vật.
/// Quản lý: Đói (hunger), Khát (thirst), Năng lượng (energy).
/// Khi chỉ số về 0 sẽ có hình phạt tương ứng.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class SurvivalSystem : MonoBehaviour
{
    // === CHỈ SỐ SINH TỒN (0 - 100) ===
    [Header("Chỉ số sinh tồn (0 - 100)")]
    [Tooltip("Độ đói - giảm dần, về 0 sẽ trừ máu")]
    [Range(0f, 100f)]
    public float hunger = 100f;

    [Tooltip("Độ khát - giảm dần, về 0 sẽ trừ máu")]
    [Range(0f, 100f)]
    public float thirst = 100f;

    [Tooltip("Năng lượng - giảm dần, về 0 sẽ chậm 50%")]
    [Range(0f, 100f)]
    public float energy = 100f;

    // === TỐC ĐỘ GIẢM (đơn vị/giây) ===
    [Header("Tốc độ giảm chỉ số (đơn vị/giây)")]
    public float hungerDecayRate = 0.8f;
    public float thirstDecayRate = 1.0f;
    public float energyDecayRate = 0.5f;

    // === HÌNH PHẠT ===
    [Header("Hình phạt khi chỉ số về 0")]
    public float hungerDamagePerSecond = 2f;
    public float thirstDamagePerSecond = 3f;
    public float exhaustedSpeedMultiplier = 0.5f;

    // === REFERENCES ===
    private PlayerController playerController;

    /// <summary>Kiệt sức (energy = 0)</summary>
    public bool IsExhausted => energy <= 0f;
    /// <summary>Đang đói (hunger = 0)</summary>
    public bool IsStarving => hunger <= 0f;
    /// <summary>Đang khát (thirst = 0)</summary>
    public bool IsDehydrated => thirst <= 0f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (playerController.IsDead) return;

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        float dt = Time.deltaTime;

        // FIX #11: Nhân vật không được điều khiển decay chậm hơn 70%
        // Điều này cho phép người chơi quản lý nhóm mà không bị phạt quá nặng
        float decayMultiplier = 1f;
        if (!playerController.IsControlled)
            decayMultiplier = 0.3f; // Nhân vật đang "tự nghỉ" khi không điều khiển

        // Giảm dần các chỉ số
        hunger = Mathf.Clamp(hunger - hungerDecayRate * decayMultiplier * dt, 0f, 100f);
        thirst = Mathf.Clamp(thirst - thirstDecayRate * decayMultiplier * dt, 0f, 100f);
        energy = Mathf.Clamp(energy - energyDecayRate * decayMultiplier * dt, 0f, 100f);

        // Hình phạt khi đói/khát = 0 → trừ máu
        if (IsStarving)
            playerController.TakeDamage(hungerDamagePerSecond * dt);

        if (IsDehydrated)
            playerController.TakeDamage(thirstDamagePerSecond * dt);

        // Kiệt sức → chậm 50%
        playerController.speedMultiplier = IsExhausted
            ? exhaustedSpeedMultiplier
            : 1f;
    }

    /// <summary>Ăn → hồi hunger</summary>
    public void Eat(float amount)
    {
        if (playerController.IsDead) return;
        hunger = Mathf.Clamp(hunger + amount, 0f, 100f);
        Debug.Log($"[{playerController.characterName}] Ăn → Hunger: {hunger:F1}");
    }

    /// <summary>Uống → hồi thirst</summary>
    public void Drink(float amount)
    {
        if (playerController.IsDead) return;
        thirst = Mathf.Clamp(thirst + amount, 0f, 100f);
        Debug.Log($"[{playerController.characterName}] Uống → Thirst: {thirst:F1}");
    }

    /// <summary>Nghỉ ngơi → hồi energy</summary>
    public void Sleep(float amount)
    {
        if (playerController.IsDead) return;
        energy = Mathf.Clamp(energy + amount, 0f, 100f);
        Debug.Log($"[{playerController.characterName}] Nghỉ → Energy: {energy:F1}");
    }

    /// <summary>Chuỗi trạng thái cho debug/UI</summary>
    public string GetStatusText()
    {
        string s = $"Đói:{hunger:F0} Khát:{thirst:F0} NL:{energy:F0}";
        if (IsStarving) s += " [ĐÓI!]";
        if (IsDehydrated) s += " [KHÁT!]";
        if (IsExhausted) s += " [KIỆT SỨC!]";
        return s;
    }
}
