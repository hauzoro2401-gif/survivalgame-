using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ParticleEffects - Quản lý hiệu ứng particle trong CAVE RIFT.
/// Tạo particle bằng code (không cần prefab).
/// Gọi SpawnEffect(tên, vị_trí) từ bất cứ đâu.
/// </summary>
public class ParticleEffects : MonoBehaviour
{
    // === SINGLETON ===
    public static ParticleEffects Instance { get; private set; }

    // === CẤU HÌNH ===
    [Header("Cấu hình particle")]
    [SerializeField] [Tooltip("Số lượng hạt mỗi hiệu ứng")]
    private int defaultParticleCount = 15;

    [SerializeField] [Tooltip("Thời gian tồn tại mặc định (giây)")]
    private float defaultLifetime = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ===================================
    // API CHÍNH
    // ===================================

    /// <summary>
    /// Tạo hiệu ứng particle tại vị trí.
    /// Tên hợp lệ: "wood_dust", "hit_spark", "heal_glow",
    /// "portal_pulse", "boss_explosion", "harvest", "craft_sparkle"
    /// </summary>
    public void SpawnEffect(string effectName, Vector2 position)
    {
        switch (effectName)
        {
            case "wood_dust":
                SpawnWoodDust(position);
                break;
            case "hit_spark":
                SpawnHitSpark(position);
                break;
            case "heal_glow":
                SpawnHealGlow(position);
                break;
            case "portal_pulse":
                SpawnPortalPulse(position);
                break;
            case "boss_explosion":
                SpawnBossExplosion(position);
                break;
            case "harvest":
                SpawnHarvestEffect(position);
                break;
            case "craft_sparkle":
                SpawnCraftSparkle(position);
                break;
            default:
                SpawnGenericBurst(position, Color.white);
                break;
        }
    }

    // ===================================
    // HIỆU ỨNG CỤ THỂ
    // ===================================

    /// <summary>Bụi gỗ khi chặt cây</summary>
    private void SpawnWoodDust(Vector2 position)
    {
        StartCoroutine(BurstParticles(position, 10,
            new Color(0.6f, 0.45f, 0.2f),
            new Color(0.8f, 0.65f, 0.3f),
            0.8f, 1.5f, 0.3f));

        // Play sfx
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("harvest");
    }

    /// <summary>Tia lửa đỏ khi đánh trúng</summary>
    private void SpawnHitSpark(Vector2 position)
    {
        StartCoroutine(BurstParticles(position, 12,
            new Color(1f, 0.3f, 0.1f),
            new Color(1f, 0.8f, 0.2f),
            0.5f, 3f, 0.15f));
    }

    /// <summary>Hạt xanh lá bay lên khi Trang hồi máu</summary>
    private void SpawnHealGlow(Vector2 position)
    {
        StartCoroutine(RisingParticles(position, 15,
            new Color(0.3f, 0.9f, 0.4f, 0.8f),
            new Color(0.5f, 1f, 0.6f, 0.4f),
            1.5f, 1.5f));
    }

    /// <summary>Ánh sáng tím pulse khi cổng xuất hiện</summary>
    private void SpawnPortalPulse(Vector2 position)
    {
        StartCoroutine(PulseRing(position,
            new Color(0.5f, 0.2f, 0.9f, 0.7f),
            3f, 1f));
    }

    /// <summary>Vụ nổ sáng lớn khi boss chết</summary>
    private void SpawnBossExplosion(Vector2 position)
    {
        // Nổ lớn - nhiều hạt
        StartCoroutine(BurstParticles(position, 40,
            Color.white,
            new Color(1f, 0.6f, 0.2f),
            2f, 5f, 0.2f));

        // Vòng sáng mở rộng
        StartCoroutine(PulseRing(position,
            new Color(1f, 0.8f, 0.3f, 0.9f),
            6f, 1.5f));

        // Play sfx
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("boss_roar");
    }

    /// <summary>Hiệu ứng thu hoạch tài nguyên</summary>
    private void SpawnHarvestEffect(Vector2 position)
    {
        StartCoroutine(BurstParticles(position, 8,
            new Color(0.5f, 0.7f, 0.3f),
            new Color(0.7f, 0.9f, 0.4f),
            0.6f, 2f, 0.2f));
    }

    /// <summary>Lấp lánh khi chế tạo thành công</summary>
    private void SpawnCraftSparkle(Vector2 position)
    {
        StartCoroutine(SparkleEffect(position, 12,
            new Color(0.9f, 0.8f, 0.3f, 0.9f),
            1.2f));

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX("craft");
    }

    /// <summary>Burst chung (fallback)</summary>
    private void SpawnGenericBurst(Vector2 position, Color color)
    {
        StartCoroutine(BurstParticles(position, 8,
            color, color * 0.7f, 0.8f, 2f, 0.2f));
    }

    // ===================================
    // PARTICLE COROUTINES
    // ===================================

    /// <summary>
    /// Bùng nổ hạt ra mọi hướng.
    /// </summary>
    private IEnumerator BurstParticles(Vector2 center, int count,
        Color colorStart, Color colorEnd, float lifetime, float speed, float size)
    {
        List<ParticleData> particles = new List<ParticleData>();
        Sprite pixelSprite = CreatePixelSprite();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject("Particle");
            obj.transform.position = (Vector3)center;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = pixelSprite;
            sr.color = Color.Lerp(colorStart, colorEnd, Random.value);
            sr.sortingOrder = 150;

            obj.transform.localScale = Vector3.one * size * Random.Range(0.5f, 1.5f);

            Vector2 dir = Random.insideUnitCircle.normalized;
            float spd = speed * Random.Range(0.5f, 1.2f);

            particles.Add(new ParticleData
            {
                obj = obj,
                sr = sr,
                velocity = dir * spd,
                startColor = sr.color,
                lifetime = lifetime * Random.Range(0.7f, 1.3f)
            });
        }

        // Animate
        float maxTime = lifetime * 1.3f;
        float elapsed = 0f;

        while (elapsed < maxTime)
        {
            bool allDone = true;

            foreach (var p in particles)
            {
                if (p.obj == null) continue;

                p.elapsed += Time.deltaTime;
                float t = p.elapsed / p.lifetime;

                if (t >= 1f)
                {
                    Destroy(p.obj);
                    continue;
                }

                allDone = false;

                // Di chuyển + giảm tốc
                p.obj.transform.position += (Vector3)(p.velocity * Time.deltaTime * (1f - t));

                // Fade out
                Color c = p.startColor;
                c.a = 1f - t;
                p.sr.color = c;

                // Thu nhỏ
                float scale = (1f - t * 0.5f) * p.obj.transform.localScale.x;
                p.obj.transform.localScale = Vector3.one * Mathf.Max(scale, 0.01f);
            }

            if (allDone) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Cleanup
        foreach (var p in particles)
        {
            if (p.obj != null) Destroy(p.obj);
        }
    }

    /// <summary>
    /// Hạt bay lên (heal effect).
    /// </summary>
    private IEnumerator RisingParticles(Vector2 center, int count,
        Color colorStart, Color colorEnd, float lifetime, float riseSpeed)
    {
        List<ParticleData> particles = new List<ParticleData>();
        Sprite pixelSprite = CreatePixelSprite();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject("HealParticle");
            Vector2 offset = Random.insideUnitCircle * 0.5f;
            obj.transform.position = (Vector3)(center + offset);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = pixelSprite;
            sr.color = Color.Lerp(colorStart, colorEnd, Random.value);
            sr.sortingOrder = 150;
            obj.transform.localScale = Vector3.one * 0.15f;

            float xWobble = Random.Range(-0.5f, 0.5f);
            float ySpeed = riseSpeed * Random.Range(0.8f, 1.2f);

            particles.Add(new ParticleData
            {
                obj = obj, sr = sr,
                velocity = new Vector2(xWobble, ySpeed),
                startColor = sr.color,
                lifetime = lifetime * Random.Range(0.8f, 1.2f)
            });
        }

        float maxTime = lifetime * 1.3f;
        float elapsed = 0f;

        while (elapsed < maxTime)
        {
            bool allDone = true;
            foreach (var p in particles)
            {
                if (p.obj == null) continue;
                p.elapsed += Time.deltaTime;
                float t = p.elapsed / p.lifetime;
                if (t >= 1f) { Destroy(p.obj); continue; }
                allDone = false;

                // Bay lên + lắc ngang
                float wobble = Mathf.Sin(p.elapsed * 5f) * 0.3f;
                p.obj.transform.position += new Vector3(wobble * Time.deltaTime, p.velocity.y * Time.deltaTime, 0);

                Color c = p.startColor;
                c.a = 1f - t;
                p.sr.color = c;
            }
            if (allDone) break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var p in particles)
            if (p.obj != null) Destroy(p.obj);
    }

    /// <summary>
    /// Vòng sáng mở rộng (portal/boss).
    /// </summary>
    private IEnumerator PulseRing(Vector2 center, Color color, float maxRadius, float duration)
    {
        GameObject ring = new GameObject("PulseRing");
        ring.transform.position = center;

        SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(16, 16);
        Color[] px = new Color[256];
        for (int i = 0; i < px.Length; i++)
        {
            int x = i % 16, y = i / 16;
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
            px[i] = (dist > 5f && dist < 7.5f) ? Color.white : new Color(1, 1, 1, 0);
        }
        tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 8f);
        sr.color = color;
        sr.sortingOrder = 140;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            ring.transform.localScale = Vector3.one * (t * maxRadius);
            sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(ring);
    }

    /// <summary>
    /// Lấp lánh (craft sparkle).
    /// </summary>
    private IEnumerator SparkleEffect(Vector2 center, int count, Color color, float duration)
    {
        List<GameObject> sparkles = new List<GameObject>();
        Sprite pixelSprite = CreatePixelSprite();

        for (int i = 0; i < count; i++)
        {
            GameObject obj = new GameObject("Sparkle");
            Vector2 offset = Random.insideUnitCircle * 1f;
            obj.transform.position = (Vector3)(center + offset);
            obj.transform.localScale = Vector3.one * Random.Range(0.1f, 0.25f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = pixelSprite;
            sr.color = color;
            sr.sortingOrder = 150;

            sparkles.Add(obj);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            foreach (var s in sparkles)
            {
                if (s == null) continue;
                SpriteRenderer sr = s.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                // Nhấp nháy
                float flash = Mathf.Sin((elapsed + s.GetInstanceID()) * 10f);
                float alpha = flash > 0 ? color.a : 0f;
                alpha *= (1f - t);
                sr.color = new Color(color.r, color.g, color.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var s in sparkles)
            if (s != null) Destroy(s);
    }

    // ===================================
    // HELPER
    // ===================================

    private Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(2, 2);
        Color[] px = new Color[4];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply(); tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
    }

    /// <summary>Data cho 1 hạt particle</summary>
    private class ParticleData
    {
        public GameObject obj;
        public SpriteRenderer sr;
        public Vector2 velocity;
        public Color startColor;
        public float lifetime;
        public float elapsed;
    }
}
