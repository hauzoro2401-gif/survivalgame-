// GlowPulse.cs – CAVE RIFT | Unity 6 | URP 2D
// Gắn vào décor tinh thể: nhấp nháy màu sắc + scale nhẹ.
// Không cần tham số – tự lấy màu từ SpriteRenderer.

using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Hiệu ứng tinh thể phát sáng nhấp nháy (glow pulse).
/// Tự lấy màu SpriteRenderer, điều chỉnh Light2D nếu có.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GlowPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Tốc độ nhấp nháy (rad/s)")]
    public float pulseSpeed    = 1.8f;

    [Tooltip("Biên độ thay đổi alpha (0-0.5)")]
    public float alphaAmplitude = 0.25f;

    [Tooltip("Biên độ thay đổi scale")]
    public float scaleAmplitude = 0.06f;

    [Tooltip("Offset pha ngẫu nhiên để mỗi tinh thể khác nhau")]
    private float phaseOffset;

    // ─── References ──────────────────────────────────────────
    private SpriteRenderer sr;
    private Light2D         pointLight;   // nếu cùng GameObject có Light2D
    private Color           baseColor;
    private Vector3         baseScale;
    private float           baseLightIntensity;

    private void Awake()
    {
        sr         = GetComponent<SpriteRenderer>();
        pointLight = GetComponent<Light2D>();
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        baseColor  = sr.color;
        baseScale  = transform.localScale;

        if (pointLight != null)
            baseLightIntensity = pointLight.intensity;
    }

    private void Update()
    {
        float t = Mathf.Sin(Time.time * pulseSpeed + phaseOffset);  // -1 → 1

        // Alpha pulse
        Color c = baseColor;
        c.a = Mathf.Clamp01(baseColor.a + t * alphaAmplitude);
        sr.color = c;

        // Scale pulse (hô hấp nhẹ)
        float scaleMod = 1f + t * scaleAmplitude;
        transform.localScale = baseScale * scaleMod;

        // Light intensity pulse (nếu có Light2D)
        if (pointLight != null)
            pointLight.intensity = baseLightIntensity + t * baseLightIntensity * 0.4f;
    }

    /// <summary>Thay màu tinh thể và cập nhật base</summary>
    public void SetColor(Color newColor)
    {
        baseColor = newColor;
        sr.color  = newColor;
    }
}
