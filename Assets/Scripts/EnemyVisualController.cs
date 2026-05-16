// EnemyVisualController.cs – CAVE RIFT | Unity 6 | URP 2D
// Gắn vào mọi GameObject có EnemyBase:
//   - Bóng đổ dưới chân
//   - Exclamation "!" khi phát hiện nhân vật
//   - Vầng đỏ khi ban đêm (buff indicator)
//   - Particle trail khi đuổi (tốc độ cao)
//   - Flash trắng khi bị đánh mượt hơn (material swap)
//   - Death dissolve effect (alpha + scale)

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Visual controller cho tất cả enemy trong CAVE RIFT.
/// Tự động tìm EnemyBase và phản ứng với state thay đổi.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyVisualController : MonoBehaviour
{
    // ─── Cài đặt Inspector ────────────────────────────────────
    [Header("Bóng đổ")]
    public bool   showShadow       = true;
    public float  shadowAlpha      = 0.4f;
    public float  shadowScaleX     = 1.2f;

    [Header("Exclamation (!)")]
    public float  alertDuration    = 0.8f;
    public Color  alertColor       = new Color(1f, 0.9f, 0.2f);

    [Header("Night Buff Glow")]
    public Color  nightGlowColor   = new Color(0.8f, 0.15f, 0.15f, 0.5f);
    public float  nightGlowRadius  = 2.5f;

    [Header("Chase Trail")]
    public bool   showChaseTrail   = true;
    public int    trailLength      = 6;
    public float  trailSpawnRate   = 0.06f;

    [Header("Hit Flash")]
    public float  hitFlashDuration = 0.12f;

    [Header("Death Effect")]
    public float  deathDuration    = 1.0f;

    // ─── References (tự tìm) ──────────────────────────────────
    private SpriteRenderer  sr;
    private EnemyBase       enemy;
    private Light2D         nightGlow;
    private GameObject      shadowObj;
    private GameObject      alertObj;
    private SpriteRenderer  shadowSR;

    // ─── Trạng thái ───────────────────────────────────────────
    private EnemyBase.EnemyState lastState = (EnemyBase.EnemyState)(-1);
    private bool isNight = false;
    private Coroutine trailRoutine;
    private Color     originalColor;

    // ══════════════════════════════════════════════════════════
    //  Khởi tạo
    // ══════════════════════════════════════════════════════════
    private void Awake()
    {
        sr    = GetComponent<SpriteRenderer>();
        enemy = GetComponent<EnemyBase>();
        originalColor = sr.color;
    }

    private void Start()
    {
        if (showShadow)      CreateShadow();
        CreateNightGlow();

        // Đăng ký event ngày/đêm
        var dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnNightStart += OnNightStart;
            dayNight.OnDayStart   += OnDayStart;
            isNight = dayNight.IsNight;
            ApplyNightVisual(isNight);
        }
    }

    private void OnDestroy()
    {
        var dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnNightStart -= OnNightStart;
            dayNight.OnDayStart   -= OnDayStart;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  Update: theo dõi state thay đổi
    // ══════════════════════════════════════════════════════════
    private void Update()
    {
        if (enemy == null) return;

        var state = enemy.CurrentState;

        // Cập nhật bóng
        UpdateShadow();

        // Phản ứng khi state thay đổi
        if (state != lastState)
        {
            OnStateChanged(lastState, state);
            lastState = state;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  State Change Handler
    // ══════════════════════════════════════════════════════════
    private void OnStateChanged(EnemyBase.EnemyState from, EnemyBase.EnemyState to)
    {
        // Dừng trail nếu không phải Chase
        if (to != EnemyBase.EnemyState.Chase && trailRoutine != null)
        {
            StopCoroutine(trailRoutine);
            trailRoutine = null;
        }

        switch (to)
        {
            case EnemyBase.EnemyState.Chase:
                ShowAlert();
                if (showChaseTrail)
                    trailRoutine = StartCoroutine(ChaseTrailRoutine());
                break;

            case EnemyBase.EnemyState.Hurt:
                StartCoroutine(HitFlashRoutine());
                break;

            case EnemyBase.EnemyState.Dead:
                StartCoroutine(DeathRoutine());
                break;

            case EnemyBase.EnemyState.Patrol:
            case EnemyBase.EnemyState.Idle:
                // Đặt lại màu gốc
                sr.color = originalColor;
                break;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  Tạo bóng đổ dưới chân
    // ══════════════════════════════════════════════════════════
    private void CreateShadow()
    {
        shadowObj = new GameObject("Shadow");
        shadowObj.transform.SetParent(transform);
        shadowObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        shadowObj.transform.localScale    = new Vector3(shadowScaleX, 0.3f, 1f);

        shadowSR = shadowObj.AddComponent<SpriteRenderer>();
        shadowSR.sprite       = CreateEllipseSprite();
        shadowSR.color        = new Color(0f, 0f, 0f, shadowAlpha);
        shadowSR.sortingOrder = sr.sortingOrder - 1;
    }

    private void UpdateShadow()
    {
        if (shadowObj == null) return;

        // Scale bóng theo chiều ngang (di chuyển → bóng méo nhẹ)
        float speedFactor = 1f;
        if (enemy != null && enemy.CurrentState == EnemyBase.EnemyState.Chase)
            speedFactor = 1.2f;

        shadowObj.transform.localScale = new Vector3(
            shadowScaleX * speedFactor, 0.3f, 1f);
    }

    // ══════════════════════════════════════════════════════════
    //  Alert "!" khi phát hiện nhân vật
    // ══════════════════════════════════════════════════════════
    private void ShowAlert()
    {
        if (alertObj != null) return;  // đang hiện rồi

        alertObj = new GameObject("Alert!");
        alertObj.transform.SetParent(transform);
        alertObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        alertObj.transform.localScale    = Vector3.one * 0.5f;

        var alertSR = alertObj.AddComponent<SpriteRenderer>();
        alertSR.sprite       = CreateTextSprite("!");
        alertSR.color        = alertColor;
        alertSR.sortingOrder = sr.sortingOrder + 10;

        StartCoroutine(AlertRoutine(alertObj));
    }

    private IEnumerator AlertRoutine(GameObject obj)
    {
        float elapsed = 0f;

        // Pop up animation
        while (elapsed < alertDuration)
        {
            float t = elapsed / alertDuration;

            // Scale pop: 0→1.2→1
            float s = t < 0.3f
                ? Mathf.Lerp(0f, 1.2f, t / 0.3f)
                : Mathf.Lerp(1.2f, 1.0f, (t - 0.3f) / 0.7f);

            if (obj != null)
                obj.transform.localScale = Vector3.one * s * 0.5f;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (obj != null) Destroy(obj);
        alertObj = null;
    }

    // ══════════════════════════════════════════════════════════
    //  Chase Trail – vệt mờ khi đuổi
    // ══════════════════════════════════════════════════════════
    private IEnumerator ChaseTrailRoutine()
    {
        while (true)
        {
            // Tạo ghost sprite tại vị trí hiện tại
            var ghost = new GameObject("Trail");
            ghost.transform.position   = transform.position;
            ghost.transform.localScale = transform.localScale;
            ghost.transform.rotation   = transform.rotation;

            var ghostSR = ghost.AddComponent<SpriteRenderer>();
            ghostSR.sprite            = sr.sprite;
            ghostSR.flipX             = sr.flipX;
            ghostSR.sortingOrder      = sr.sortingOrder - 1;

            Color ghostColor = originalColor;
            ghostColor.a = 0.35f;
            ghostSR.color = ghostColor;

            // Fade và destroy
            StartCoroutine(FadeAndDestroy(ghost, ghostSR, ghostColor, 0.25f));

            yield return new WaitForSeconds(trailSpawnRate);
        }
    }

    private IEnumerator FadeAndDestroy(GameObject obj, SpriteRenderer objSR,
        Color startColor, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && obj != null)
        {
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            objSR.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (obj != null) Destroy(obj);
    }

    // ══════════════════════════════════════════════════════════
    //  Hit Flash – flash trắng khi bị đánh
    // ══════════════════════════════════════════════════════════
    private IEnumerator HitFlashRoutine()
    {
        // Spawn particle hit spark tại vị trí
        if (ParticleEffects.Instance != null)
            ParticleEffects.Instance.SpawnEffect("hit_spark", transform.position);

        // Flash trắng
        sr.color = Color.white;
        yield return new WaitForSeconds(hitFlashDuration);

        // Trở về màu gốc (nếu chưa chết)
        if (enemy != null && !enemy.IsDead)
            sr.color = originalColor;
    }

    // ══════════════════════════════════════════════════════════
    //  Death Routine – tan rã khi chết
    // ══════════════════════════════════════════════════════════
    private IEnumerator DeathRoutine()
    {
        // Particle vụ nổ
        if (ParticleEffects.Instance != null)
            ParticleEffects.Instance.SpawnEffect("hit_spark", transform.position);

        // Ẩn bóng
        if (shadowObj != null) shadowObj.SetActive(false);

        // Tắt night glow
        if (nightGlow != null) nightGlow.enabled = false;

        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Color   startColor = sr.color;

        while (elapsed < deathDuration)
        {
            float t = elapsed / deathDuration;

            // Fade alpha + thu nhỏ dần
            sr.color = new Color(startColor.r, startColor.g, startColor.b,
                Mathf.Lerp(1f, 0f, t));

            // Bay lên nhẹ khi tan rã
            transform.localScale = startScale * Mathf.Lerp(1f, 0.3f, t);
            transform.position  += Vector3.up * Time.deltaTime * 0.5f;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  Night Glow – vầng đỏ ban đêm
    // ══════════════════════════════════════════════════════════
    private void CreateNightGlow()
    {
        var glowObj = new GameObject("NightGlow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;

        nightGlow = glowObj.AddComponent<Light2D>();
        nightGlow.lightType             = Light2D.LightType.Point;
        nightGlow.color                 = nightGlowColor;
        nightGlow.intensity             = 0f;  // tắt ban ngày
        nightGlow.pointLightOuterRadius = nightGlowRadius;
        nightGlow.pointLightInnerRadius = nightGlowRadius * 0.2f;
        nightGlow.shadowsEnabled        = false;
    }

    private void ApplyNightVisual(bool night)
    {
        if (nightGlow == null) return;
        nightGlow.intensity = night ? 1.5f : 0f;
    }

    private void OnNightStart() { isNight = true;  ApplyNightVisual(true); }
    private void OnDayStart()   { isNight = false; ApplyNightVisual(false); }

    // ══════════════════════════════════════════════════════════
    //  Sprite helpers
    // ══════════════════════════════════════════════════════════
    private static Sprite CreateEllipseSprite()
    {
        int size = 16;
        var tex  = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var px   = new Color[size * size];
        float cx = size / 2f, cy = size / 2f;
        for (int i = 0; i < px.Length; i++)
        {
            float x = i % size, y = i / size;
            float dx = (x - cx) / (size / 2f), dy = (y - cy) / (size / 4f);
            px[i] = dx * dx + dy * dy <= 1f ? Color.white : Color.clear;
        }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateTextSprite(string text)
    {
        // Tạo sprite đơn giản 8x8 đại diện cho "!"
        int size = 8;
        var tex  = new Texture2D(size, size) { filterMode = FilterMode.Point };
        var px   = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        // Vẽ "!" thủ công: cột giữa
        int mid = size / 2;
        for (int y = 2; y < size; y++) px[y * size + mid] = Color.white;
        px[0 * size + mid] = Color.white;  // chấm

        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 8f);
    }
}
