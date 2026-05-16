using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AudioManager - Singleton quản lý âm thanh toàn game.
/// DontDestroyOnLoad. AudioSource pool (5 sources) cho SFX.
/// Tự chuyển BGM theo ngày/đêm. Fade between tracks.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // === SINGLETON ===
    public static AudioManager Instance { get; private set; }

    // === BGM ===
    [Header("Nhạc nền (BGM)")]
    [SerializeField] [Tooltip("Nhạc ban ngày - nhẹ nhàng")]
    private AudioClip bgm_day;

    [SerializeField] [Tooltip("Nhạc ban đêm - căng thẳng")]
    private AudioClip bgm_night;

    [SerializeField] [Tooltip("Nhạc boss fight")]
    private AudioClip bgm_boss;

    [SerializeField] [Tooltip("Nhạc menu")]
    private AudioClip bgm_menu;

    // === SFX ===
    [Header("Hiệu ứng âm thanh (SFX)")]
    [SerializeField] private AudioClip sfx_attack;
    [SerializeField] private AudioClip sfx_hurt;
    [SerializeField] private AudioClip sfx_die;
    [SerializeField] private AudioClip sfx_harvest;
    [SerializeField] private AudioClip sfx_craft;
    [SerializeField] private AudioClip sfx_portal_hum;
    [SerializeField] private AudioClip sfx_boss_roar;
    [SerializeField] private AudioClip sfx_dialogue_blip;
    [SerializeField] private AudioClip sfx_pickup;
    [SerializeField] private AudioClip sfx_ui_click;

    // === CẤU HÌNH ===
    [Header("Cấu hình")]
    [SerializeField] [Tooltip("Số AudioSource cho SFX pool")]
    private int sfxPoolSize = 5;

    [SerializeField] [Tooltip("Tốc độ fade giữa BGM tracks")]
    private float bgmFadeSpeed = 1f;

    // === VOLUME ===
    [Header("Âm lượng")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 0.7f;

    // === PROPERTIES ===
    public float BGMVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);
            if (bgmSourceA != null) bgmSourceA.volume = bgmVolume * bgmSourceATarget;
            if (bgmSourceB != null) bgmSourceB.volume = bgmVolume * bgmSourceBTarget;
            PlayerPrefs.SetFloat("CaveRift_BGMVolume", bgmVolume);
        }
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("CaveRift_SFXVolume", sfxVolume);
        }
    }

    // === AUDIO SOURCES ===
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private float bgmSourceATarget = 1f;
    private float bgmSourceBTarget = 0f;
    private bool usingSourceA = true;

    private List<AudioSource> sfxPool = new List<AudioSource>();
    private int currentSFXIndex = 0;

    // === SFX DICTIONARY ===
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // Singleton + DontDestroyOnLoad
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Tạo BGM sources (2 sources cho crossfade)
        bgmSourceA = gameObject.AddComponent<AudioSource>();
        bgmSourceA.loop = true;
        bgmSourceA.playOnAwake = false;
        bgmSourceA.volume = bgmVolume;

        bgmSourceB = gameObject.AddComponent<AudioSource>();
        bgmSourceB.loop = true;
        bgmSourceB.playOnAwake = false;
        bgmSourceB.volume = 0f;

        // Tạo SFX pool
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            sfxPool.Add(src);
        }

        // Build SFX dictionary
        BuildSFXDictionary();

        // Load volume settings
        bgmVolume = PlayerPrefs.GetFloat("CaveRift_BGMVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("CaveRift_SFXVolume", 0.7f);
    }

    private void Start()
    {
        // Đăng ký event ngày/đêm
        DayNightCycle dayNight = FindAnyObjectByType<DayNightCycle>();
        if (dayNight != null)
        {
            dayNight.OnDayStart += () => PlayBGM("bgm_day");
            dayNight.OnNightStart += () => PlayBGM("bgm_night");

            // Play nhạc phù hợp với thời điểm hiện tại
            if (dayNight.IsNight)
                PlayBGM("bgm_night");
            else
                PlayBGM("bgm_day");
        }
    }

    private void Update()
    {
        // Crossfade giữa 2 BGM sources
        UpdateBGMCrossfade();
    }

    /// <summary>
    /// Build dictionary ánh xạ tên → AudioClip.
    /// </summary>
    private void BuildSFXDictionary()
    {
        sfxDictionary.Clear();

        if (sfx_attack != null) sfxDictionary["attack"] = sfx_attack;
        if (sfx_hurt != null) sfxDictionary["hurt"] = sfx_hurt;
        if (sfx_die != null) sfxDictionary["die"] = sfx_die;
        if (sfx_harvest != null) sfxDictionary["harvest"] = sfx_harvest;
        if (sfx_craft != null) sfxDictionary["craft"] = sfx_craft;
        if (sfx_portal_hum != null) sfxDictionary["portal_hum"] = sfx_portal_hum;
        if (sfx_boss_roar != null) sfxDictionary["boss_roar"] = sfx_boss_roar;
        if (sfx_dialogue_blip != null) sfxDictionary["dialogue_blip"] = sfx_dialogue_blip;
        if (sfx_pickup != null) sfxDictionary["pickup"] = sfx_pickup;
        if (sfx_ui_click != null) sfxDictionary["ui_click"] = sfx_ui_click;
    }

    // ===================================
    // PLAY BGM (với crossfade)
    // ===================================

    /// <summary>
    /// Play nhạc nền theo tên. Tự fade giữa track cũ và mới.
    /// Tên hợp lệ: "bgm_day", "bgm_night", "bgm_boss", "bgm_menu"
    /// </summary>
    public void PlayBGM(string bgmName)
    {
        AudioClip clip = GetBGMClip(bgmName);
        if (clip == null)
        {
            Debug.LogWarning($"[Audio] Không tìm thấy BGM: {bgmName}");
            return;
        }

        // Nếu đang play clip này rồi → bỏ qua
        AudioSource currentSource = usingSourceA ? bgmSourceA : bgmSourceB;
        if (currentSource.clip == clip && currentSource.isPlaying) return;

        // Crossfade sang source còn lại
        if (usingSourceA)
        {
            bgmSourceB.clip = clip;
            bgmSourceB.Play();
            bgmSourceATarget = 0f; // Fade out A
            bgmSourceBTarget = 1f; // Fade in B
        }
        else
        {
            bgmSourceA.clip = clip;
            bgmSourceA.Play();
            bgmSourceATarget = 1f; // Fade in A
            bgmSourceBTarget = 0f; // Fade out B
        }

        usingSourceA = !usingSourceA;
        Debug.Log($"[Audio] BGM: {bgmName}");
    }

    /// <summary>
    /// Dừng nhạc nền (fade out).
    /// </summary>
    public void StopBGM()
    {
        bgmSourceATarget = 0f;
        bgmSourceBTarget = 0f;
    }

    /// <summary>
    /// Cập nhật crossfade giữa 2 BGM sources.
    /// </summary>
    private void UpdateBGMCrossfade()
    {
        if (bgmSourceA != null)
        {
            bgmSourceA.volume = Mathf.Lerp(bgmSourceA.volume,
                bgmVolume * bgmSourceATarget, bgmFadeSpeed * Time.unscaledDeltaTime);

            if (bgmSourceATarget == 0f && bgmSourceA.volume < 0.01f)
                bgmSourceA.Stop();
        }

        if (bgmSourceB != null)
        {
            bgmSourceB.volume = Mathf.Lerp(bgmSourceB.volume,
                bgmVolume * bgmSourceBTarget, bgmFadeSpeed * Time.unscaledDeltaTime);

            if (bgmSourceBTarget == 0f && bgmSourceB.volume < 0.01f)
                bgmSourceB.Stop();
        }
    }

    private AudioClip GetBGMClip(string name)
    {
        return name switch
        {
            "bgm_day" => bgm_day,
            "bgm_night" => bgm_night,
            "bgm_boss" => bgm_boss,
            "bgm_menu" => bgm_menu,
            _ => null
        };
    }

    // ===================================
    // PLAY SFX (từ pool)
    // ===================================

    /// <summary>
    /// Play hiệu ứng âm thanh theo tên.
    /// Dùng AudioSource pool để play nhiều SFX cùng lúc.
    /// Tên: "attack", "hurt", "die", "harvest", "craft",
    /// "portal_hum", "boss_roar", "dialogue_blip", "pickup", "ui_click"
    /// </summary>
    public void PlaySFX(string sfxName)
    {
        if (!sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
        {
            // Không có clip → bỏ qua im lặng (nhiều asset chưa có sẵn)
            return;
        }

        PlaySFX(clip);
    }

    /// <summary>
    /// Play SFX từ AudioClip trực tiếp.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxPool.Count == 0) return;

        // Round-robin qua pool
        AudioSource source = sfxPool[currentSFXIndex];
        currentSFXIndex = (currentSFXIndex + 1) % sfxPool.Count;

        source.clip = clip;
        source.volume = sfxVolume;
        source.pitch = Random.Range(0.95f, 1.05f); // Pitch nhẹ cho tự nhiên
        source.Play();
    }

    /// <summary>
    /// Play SFX với pitch tùy chỉnh.
    /// </summary>
    public void PlaySFX(string sfxName, float pitch)
    {
        if (!sfxDictionary.TryGetValue(sfxName, out AudioClip clip)) return;

        AudioSource source = sfxPool[currentSFXIndex];
        currentSFXIndex = (currentSFXIndex + 1) % sfxPool.Count;

        source.clip = clip;
        source.volume = sfxVolume;
        source.pitch = pitch;
        source.Play();
    }

    /// <summary>
    /// Play SFX tại vị trí 3D (spatial blend nhẹ).
    /// </summary>
    public void PlaySFXAtPosition(string sfxName, Vector3 position)
    {
        if (!sfxDictionary.TryGetValue(sfxName, out AudioClip clip)) return;

        // Tạo temporary AudioSource tại vị trí
        GameObject tempObj = new GameObject("TempSFX");
        tempObj.transform.position = position;
        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = sfxVolume;
        tempSource.spatialBlend = 0.5f;
        tempSource.Play();

        Destroy(tempObj, clip.length + 0.1f);
    }
}
