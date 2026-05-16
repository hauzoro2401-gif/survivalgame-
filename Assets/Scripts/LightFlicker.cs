// LightFlicker.cs – CAVE RIFT | Unity 6 | URP 2D
// Gắn vào GameObject có Light2D: giả lập lửa đuốc/đốm lửa.
// Dùng Perlin Noise để nhấp nháy tự nhiên (không bị giật).

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Hiệu ứng nhấp nháy đuốc lửa sử dụng Perlin Noise.
/// Gắn vào cùng object với Light2D (Point).
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    [Tooltip("Cường độ sáng tối thiểu")]
    public float minIntensity  = 1.2f;

    [Tooltip("Cường độ sáng tối đa")]
    public float maxIntensity  = 2.5f;

    [Tooltip("Tốc độ thay đổi (Perlin speed)")]
    public float flickerSpeed  = 3.5f;

    [Tooltip("Bán kính dao động nhỏ (giả lập gió)")]
    public float radiusJitter  = 0.3f;

    [Tooltip("Màu dao động từ cam sang vàng")]
    public bool  colorShift    = true;

    // ─── Trạng thái nội bộ ────────────────────────────────────
    private Light2D  lt;
    private float    baseRadius;
    private float    noiseOffsetX;   // offset khác nhau mỗi đèn
    private float    noiseOffsetY;
    private Color    baseColor;

    // Màu đuốc (cam–vàng)
    private static readonly Color TORCH_HOT  = new Color(1.0f, 0.8f, 0.3f);
    private static readonly Color TORCH_COOL = new Color(1.0f, 0.5f, 0.1f);

    private void Awake()
    {
        lt          = GetComponent<Light2D>();
        baseRadius  = lt.pointLightOuterRadius;
        baseColor   = lt.color;

        // Offset ngẫu nhiên để mỗi đèn nhấp nháy khác pha
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float time = Time.time * flickerSpeed;

        // Perlin noise → [0,1]
        float noiseI = Mathf.PerlinNoise(noiseOffsetX + time, 0.5f);
        float noiseR = Mathf.PerlinNoise(noiseOffsetY + time, 1.5f);

        // Cường độ
        lt.intensity = Mathf.Lerp(minIntensity, maxIntensity, noiseI);

        // Bán kính
        lt.pointLightOuterRadius = baseRadius + (noiseR - 0.5f) * radiusJitter * 2f;

        // Màu cam–vàng (tùy chọn)
        if (colorShift)
            lt.color = Color.Lerp(TORCH_COOL, TORCH_HOT, noiseI);
    }

    /// <summary>Tắt flicker, fix intensity cố định</summary>
    public void SetFixed(float intensity)
    {
        enabled      = false;
        lt.intensity = intensity;
    }
}
