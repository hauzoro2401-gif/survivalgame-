using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using System.Linq;
using TMPro;

/// <summary>
/// DayNightCycle - Hệ thống ngày/đêm cho Vực Thẳm.
/// 1 ngày = 10 phút thực. Điều khiển Global Light 2D và spawn events.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    // === CẤU HÌNH THỜI GIAN ===
    [Header("Cấu hình thời gian")]
    [Tooltip("Thời gian 1 ngày tính bằng giây thực (600 = 10 phút)")]
    public float dayDurationInSeconds = 600f;

    [Tooltip("Giờ bắt đầu ban ngày (6 = 6:00 sáng)")]
    public float dayStartHour = 6f;

    [Tooltip("Giờ bắt đầu ban đêm (18 = 6:00 tối)")]
    public float nightStartHour = 18f;

    // === ÁNH SÁNG ===
    [Header("Global Light 2D")]
    [Tooltip("Kéo Light2D (Global) vào đây")]
    public Light2D globalLight;

    [Tooltip("Cường độ sáng ban ngày")]
    public float dayIntensity = 1f;

    [Tooltip("Cường độ sáng ban đêm")]
    public float nightIntensity = 0.15f;

    [Tooltip("Màu ánh sáng ban ngày")]
    public Color dayColor = new Color(1f, 0.98f, 0.88f); // Trắng ấm

    [Tooltip("Màu ánh sáng ban đêm")]
    public Color nightColor = new Color(0.2f, 0.2f, 0.45f); // Xanh tím tối

    // === UI ===
    [Header("UI Hiển thị thời gian")]
    [Tooltip("Text hiển thị ngày và giờ (VD: 'Ngày 1 - 06:00')")]
    public TextMeshProUGUI timeDisplayText;

    // === EVENTS ===
    /// <summary>Event phát ra khi ban ngày bắt đầu</summary>
    public event Action OnDayStart;

    /// <summary>Event phát ra khi ban đêm bắt đầu</summary>
    public event Action OnNightStart;

    // === TRẠNG THÁI ===
    /// <summary>Ngày hiện tại (bắt đầu từ 1)</summary>
    public int CurrentDay { get; private set; } = 1;

    /// <summary>Giờ hiện tại trong ngày (0-24)</summary>
    public float CurrentHour { get; private set; }

    /// <summary>Đang là ban đêm không</summary>
    public bool IsNight { get; private set; } = false;

    // === BIẾN NỘI BỘ ===
    // Thời gian đã trôi qua trong ngày hiện tại (giây thực)
    private float currentTimeOfDay;
    // Lưu trạng thái đêm trước đó để phát event đúng lúc
    private bool wasNight = false;

    private void Start()
    {
        // FIX #4 + #13: Tự tìm Global Light 2D nếu chưa được giao trong Inspector
        if (globalLight == null)
        {
            globalLight = FindObjectsByType<Light2D>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.lightType == Light2D.LightType.Global);

            if (globalLight == null)
                Debug.LogWarning("[DayNightCycle] Không tìm thấy Global Light 2D trong scene!");
        }

        // Bắt đầu từ 6:00 sáng ngày 1
        currentTimeOfDay = (dayStartHour / 24f) * dayDurationInSeconds;
        wasNight = false;

        // Phát event ban ngày đầu tiên
        OnDayStart?.Invoke();
    }

    private void Update()
    {
        // Không chạy khi game pause
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Tiến thời gian
        currentTimeOfDay += Time.deltaTime;

        // Hết 1 ngày → sang ngày mới
        if (currentTimeOfDay >= dayDurationInSeconds)
        {
            currentTimeOfDay -= dayDurationInSeconds;
            CurrentDay++;
            Debug.Log($"[DayNight] Ngày mới: Ngày {CurrentDay} trong Vực Thẳm");
        }

        // Tính giờ hiện tại (0-24)
        float normalizedTime = currentTimeOfDay / dayDurationInSeconds;
        CurrentHour = normalizedTime * 24f;

        // Xác định ban ngày/đêm
        IsNight = CurrentHour >= nightStartHour || CurrentHour < dayStartHour;

        // Phát events khi chuyển ngày/đêm
        CheckDayNightTransition();

        // Cập nhật ánh sáng
        UpdateLighting();

        // Cập nhật UI
        UpdateTimeDisplay();
    }

    /// <summary>
    /// Kiểm tra và phát event khi chuyển giữa ngày và đêm.
    /// </summary>
    private void CheckDayNightTransition()
    {
        if (IsNight && !wasNight)
        {
            // Vừa chuyển sang đêm
            Debug.Log($"[DayNight] Đêm buông xuống Vực Thẳm... (Ngày {CurrentDay})");
            OnNightStart?.Invoke();
        }
        else if (!IsNight && wasNight)
        {
            // Vừa chuyển sang ngày
            Debug.Log($"[DayNight] Bình minh ló dạng trong Vực Thẳm (Ngày {CurrentDay})");
            OnDayStart?.Invoke();
        }

        wasNight = IsNight;
    }

    /// <summary>
    /// Cập nhật Global Light 2D - sáng/tối dần mượt mà.
    /// </summary>
    private void UpdateLighting()
    {
        if (globalLight == null) return;

        // Tính hệ số chuyển tiếp mượt mà
        float targetIntensity;
        Color targetColor;

        if (IsNight)
        {
            // Ban đêm - tối dần
            targetIntensity = nightIntensity;
            targetColor = nightColor;
        }
        else
        {
            // Ban ngày - sáng dần
            targetIntensity = dayIntensity;
            targetColor = dayColor;
        }

        // Lerp mượt mà giữa các trạng thái ánh sáng
        float lerpSpeed = 0.5f * Time.deltaTime;
        globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, lerpSpeed);
        globalLight.color = Color.Lerp(globalLight.color, targetColor, lerpSpeed);
    }

    /// <summary>
    /// Cập nhật UI hiển thị thời gian.
    /// Format: "Ngày X - HH:MM"
    /// </summary>
    private void UpdateTimeDisplay()
    {
        if (timeDisplayText == null) return;

        int hours = Mathf.FloorToInt(CurrentHour);
        int minutes = Mathf.FloorToInt((CurrentHour - hours) * 60f);

        string period = IsNight ? "🌙" : "☀️";
        timeDisplayText.text = $"{period} Ngày {CurrentDay} - {hours:D2}:{minutes:D2}";
    }

    /// <summary>
    /// Lấy chuỗi thời gian hiện tại (cho debug hoặc UI khác).
    /// </summary>
    public string GetTimeString()
    {
        int hours = Mathf.FloorToInt(CurrentHour);
        int minutes = Mathf.FloorToInt((CurrentHour - hours) * 60f);
        return $"Ngày {CurrentDay} - {hours:D2}:{minutes:D2}";
    }

    public void RestoreTime(int day, float hour)
    {
        CurrentDay = Mathf.Max(1, day);
        CurrentHour = Mathf.Repeat(hour, 24f);
        currentTimeOfDay = (CurrentHour / 24f) * Mathf.Max(1f, dayDurationInSeconds);
        IsNight = CurrentHour >= nightStartHour || CurrentHour < dayStartHour;
        wasNight = IsNight;

        if (globalLight != null)
        {
            globalLight.intensity = IsNight ? nightIntensity : dayIntensity;
            globalLight.color = IsNight ? nightColor : dayColor;
        }

        UpdateTimeDisplay();
    }
}
