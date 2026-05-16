// UIThemeManager.cs – CAVE RIFT | Unity 6 | URP 2D
// Singleton quản lý toàn bộ visual của UI:
//   - Màu sắc hang động (tím/xanh/cam)
//   - Hiệu ứng hover button (scale + glow)
//   - Damage number popup
//   - Healthbar gradient đẹp
//   - Screen flash khi bị đánh / heal
//   - Vignette tối khi máu thấp

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý UI visual toàn game: màu sắc, hiệu ứng, popup damage.
/// Singleton – gọi UIThemeManager.Instance từ bất kỳ đâu.
/// </summary>
public class UIThemeManager : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════
    //  Singleton
    // ══════════════════════════════════════════════════════════
    public static UIThemeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ══════════════════════════════════════════════════════════
    //  Bảng màu CAVE RIFT (hang động tối)
    // ══════════════════════════════════════════════════════════
    [Header("Bảng màu chủ đạo")]
    public Color colorPrimary    = new Color(0.55f, 0.30f, 0.90f);  // tím huyền
    public Color colorSecondary  = new Color(0.25f, 0.70f, 1.00f);  // xanh pha lê
    public Color colorAccent     = new Color(1.00f, 0.65f, 0.20f);  // cam đuốc
    public Color colorDanger     = new Color(0.95f, 0.20f, 0.20f);  // đỏ nguy hiểm
    public Color colorHeal       = new Color(0.30f, 0.90f, 0.45f);  // xanh heal
    public Color colorBG         = new Color(0.06f, 0.04f, 0.12f);  // nền tối

    // ══════════════════════════════════════════════════════════
    //  Screen Effects
    // ══════════════════════════════════════════════════════════
    [Header("Screen Flash")]
    [Tooltip("Image full-screen để flash khi bị đánh/heal")]
    public Image screenFlashImage;

    [Header("Vignette (máu thấp)")]
    [Tooltip("Image vòng tối viền màn hình")]
    public Image vignetteImage;

    [Tooltip("Ngưỡng HP % để hiện vignette")]
    [Range(0f, 0.5f)]
    public float vignetteThreshold = 0.30f;

    // ══════════════════════════════════════════════════════════
    //  Damage Number Pool
    // ══════════════════════════════════════════════════════════
    [Header("Damage Number")]
    [Tooltip("Prefab TextMeshPro cho số damage")]
    public TextMeshProUGUI damageNumberPrefab;

    [Tooltip("Canvas World Space để spawn damage number")]
    public Canvas worldCanvas;

    // Pool
    private Queue<TextMeshProUGUI> dmgPool = new Queue<TextMeshProUGUI>();
    private const int POOL_SIZE = 20;

    // ══════════════════════════════════════════════════════════
    //  Healthbar Settings
    // ══════════════════════════════════════════════════════════
    [Header("Healthbar Gradient")]
    public Gradient hpGradient;   // full→low: xanh lá → vàng → đỏ

    // ══════════════════════════════════════════════════════════
    //  Khởi tạo
    // ══════════════════════════════════════════════════════════
    private void Start()
    {
        InitHpGradient();
        InitDamagePool();

        // Đặt alpha ban đầu
        if (screenFlashImage != null) screenFlashImage.color = Color.clear;
        if (vignetteImage    != null) vignetteImage.color    = Color.clear;
    }

    // ─── Gradient HP mặc định (xanh → vàng → đỏ) ─────────────
    private void InitHpGradient()
    {
        if (hpGradient != null && hpGradient.colorKeys.Length > 0) return;

        hpGradient = new Gradient();
        hpGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(colorHeal,                  0.0f),
                new GradientColorKey(new Color(0.9f,0.9f,0.2f), 0.5f),
                new GradientColorKey(colorDanger,                1.0f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
    }

    // ─── Pre-pool damage numbers ───────────────────────────────
    private void InitDamagePool()
    {
        if (damageNumberPrefab == null || worldCanvas == null) return;

        for (int i = 0; i < POOL_SIZE; i++)
        {
            var txt = Instantiate(damageNumberPrefab, worldCanvas.transform);
            txt.gameObject.SetActive(false);
            dmgPool.Enqueue(txt);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  API: Hiệu ứng màn hình
    // ══════════════════════════════════════════════════════════

    /// <summary>Flash đỏ khi nhân vật bị đánh</summary>
    public void FlashDamage() =>
        StartCoroutine(FlashRoutine(new Color(colorDanger.r, colorDanger.g, colorDanger.b, 0.35f), 0.15f));

    /// <summary>Flash xanh lá khi heal</summary>
    public void FlashHeal() =>
        StartCoroutine(FlashRoutine(new Color(colorHeal.r, colorHeal.g, colorHeal.b, 0.25f), 0.20f));

    /// <summary>Flash trắng khi nhận item quan trọng</summary>
    public void FlashPickup() =>
        StartCoroutine(FlashRoutine(new Color(1f, 1f, 1f, 0.2f), 0.10f));

    private IEnumerator FlashRoutine(Color peakColor, float duration)
    {
        if (screenFlashImage == null) yield break;

        float half = duration / 2f;
        float elapsed = 0f;

        // Fade in
        while (elapsed < half)
        {
            screenFlashImage.color = Color.Lerp(Color.clear, peakColor, elapsed / half);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        // Fade out
        while (elapsed < half)
        {
            screenFlashImage.color = Color.Lerp(peakColor, Color.clear, elapsed / half);
            elapsed += Time.deltaTime;
            yield return null;
        }

        screenFlashImage.color = Color.clear;
    }

    // ──────────────────────────────────────────────────────────
    /// <summary>
    /// Cập nhật vignette theo % HP (gọi mỗi khi HP thay đổi).
    /// hpPercent: 0=chết, 1=đầy máu
    /// </summary>
    public void UpdateVignette(float hpPercent)
    {
        if (vignetteImage == null) return;

        if (hpPercent > vignetteThreshold)
        {
            vignetteImage.color = Color.clear;
            return;
        }

        // Càng ít máu → vignette càng đậm và đỏ hơn
        float t     = 1f - (hpPercent / vignetteThreshold);
        float alpha = Mathf.Lerp(0f, 0.7f, t);
        // Nhấp nháy nguy hiểm khi HP < 10%
        if (hpPercent < 0.1f)
            alpha *= (Mathf.Sin(Time.time * 4f) * 0.3f + 0.7f);

        vignetteImage.color = new Color(colorDanger.r, 0.05f, 0.05f, alpha);
    }

    // ══════════════════════════════════════════════════════════
    //  API: Damage Number Popup
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Hiện số damage bay lên tại worldPos.
    /// isHeal=true → màu xanh, isCrit=true → to hơn.
    /// </summary>
    public void ShowDamageNumber(float amount, Vector2 worldPos,
        bool isHeal = false, bool isCrit = false)
    {
        if (worldCanvas == null) return;

        TextMeshProUGUI txt = GetFromPool();
        txt.gameObject.SetActive(true);

        // Nội dung
        string prefix = isHeal ? "+" : "-";
        txt.text = $"{prefix}{Mathf.RoundToInt(amount)}";

        // Màu sắc
        txt.color = isHeal ? colorHeal : (isCrit ? colorAccent : colorDanger);
        txt.fontSize = isCrit ? 28f : 20f;
        txt.fontStyle = isCrit ? FontStyles.Bold : FontStyles.Normal;

        // Chuyển world → canvas position
        if (worldCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            txt.transform.position = screenPos;
        }
        else
        {
            txt.transform.position = new Vector3(worldPos.x, worldPos.y + 0.5f, 0f);
        }

        StartCoroutine(AnimateDamageNumber(txt, isCrit));
    }

    private IEnumerator AnimateDamageNumber(TextMeshProUGUI txt, bool isCrit)
    {
        float duration = isCrit ? 1.2f : 0.9f;
        float elapsed  = 0f;
        Vector3 startPos = txt.transform.position;
        Color   startCol = txt.color;

        // Pop scale cho crit
        if (isCrit)
        {
            txt.transform.localScale = Vector3.one * 1.4f;
            yield return new WaitForSeconds(0.08f);
            txt.transform.localScale = Vector3.one;
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Bay lên
            txt.transform.position = startPos + Vector3.up * (t * 1.5f);

            // Fade out nửa sau
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                txt.color = new Color(startCol.r, startCol.g, startCol.b,
                    Mathf.Lerp(1f, 0f, fadeT));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ReturnToPool(txt);
    }

    // ══════════════════════════════════════════════════════════
    //  API: Healthbar Color
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Trả về màu HP bar phù hợp với % máu hiện tại.
    /// hpPercent: 0=chết, 1=đầy
    /// </summary>
    public Color GetHealthBarColor(float hpPercent)
    {
        // Invert: 0→đỏ, 1→xanh lá
        return hpGradient.Evaluate(1f - Mathf.Clamp01(hpPercent));
    }

    // ══════════════════════════════════════════════════════════
    //  API: Button Hover Effects
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Gọi khi con trỏ hover vào button – scale lên nhẹ.
    /// Thêm EventTrigger và gọi hàm này từ OnPointerEnter.
    /// </summary>
    public void OnButtonHoverEnter(RectTransform btn)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleButton(btn, btn.localScale, Vector3.one * 1.08f, 0.12f));
    }

    /// <summary>Gọi khi con trỏ rời button – scale về bình thường.</summary>
    public void OnButtonHoverExit(RectTransform btn)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleButton(btn, btn.localScale, Vector3.one, 0.10f));
    }

    private IEnumerator ScaleButton(RectTransform btn, Vector3 from, Vector3 to, float dur)
    {
        float elapsed = 0f;
        while (elapsed < dur && btn != null)
        {
            btn.localScale = Vector3.Lerp(from, to, elapsed / dur);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (btn != null) btn.localScale = to;
    }

    // ══════════════════════════════════════════════════════════
    //  API: Damage Number Pool
    // ══════════════════════════════════════════════════════════
    private TextMeshProUGUI GetFromPool()
    {
        if (dmgPool.Count > 0)
        {
            var t = dmgPool.Dequeue();
            t.transform.localScale = Vector3.one;
            return t;
        }

        // Pool rỗng → tạo mới
        if (damageNumberPrefab == null)
        {
            var go = new GameObject("DmgNum");
            go.transform.SetParent(worldCanvas != null ? worldCanvas.transform : transform);
            return go.AddComponent<TextMeshProUGUI>();
        }

        return Instantiate(damageNumberPrefab,
            worldCanvas != null ? worldCanvas.transform : transform);
    }

    private void ReturnToPool(TextMeshProUGUI txt)
    {
        txt.gameObject.SetActive(false);
        dmgPool.Enqueue(txt);
    }

    // ══════════════════════════════════════════════════════════
    //  API: Apply theme lên một panel ngay lập tức
    // ══════════════════════════════════════════════════════════

    /// <summary>
    /// Áp dụng màu sắc CAVE RIFT lên tất cả Image/Text trong panel.
    /// </summary>
    public void ApplyThemeToPanel(GameObject panel)
    {
        if (panel == null) return;

        foreach (var img in panel.GetComponentsInChildren<Image>())
        {
            // Background panel → nền tối
            if (img.gameObject == panel)
                img.color = new Color(colorBG.r, colorBG.g, colorBG.b, 0.9f);
        }

        foreach (var txt in panel.GetComponentsInChildren<TextMeshProUGUI>())
        {
            txt.color = Color.white;
        }
    }
}
