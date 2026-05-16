using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// CameraController - Camera follow nhân vật đang được điều khiển.
/// Smooth follow, boundary clamp, zoom bằng scroll chuột.
/// Gắn vào Main Camera.
/// </summary>
public class CameraController : MonoBehaviour
{
    // === SMOOTH FOLLOW ===
    [Header("Smooth Follow")]
    [SerializeField] [Tooltip("Tốc độ camera bám theo nhân vật")]
    private float smoothSpeed = 5f;

    [SerializeField] [Tooltip("Offset camera so với nhân vật (VD: nhìn lên 1 chút)")]
    private Vector3 offset = new Vector3(0f, 0f, -10f);

    // === BOUNDARY (giới hạn camera trong map) ===
    [Header("Giới hạn Camera trong Map")]
    [SerializeField] private float minX = -25f;
    [SerializeField] private float maxX = 25f;
    [SerializeField] private float minY = -15f;
    [SerializeField] private float maxY = 15f;

    // === ZOOM ===
    [Header("Zoom (Scroll chuột)")]
    [SerializeField] [Tooltip("Orthographic size nhỏ nhất (zoom gần)")]
    private float minZoom = 3f;

    [SerializeField] [Tooltip("Orthographic size lớn nhất (zoom xa)")]
    private float maxZoom = 8f;

    [SerializeField] [Tooltip("Tốc độ zoom mỗi lần scroll")]
    private float zoomSpeed = 2f;

    [SerializeField] [Tooltip("Tốc độ lerp zoom cho mượt")]
    private float zoomSmoothSpeed = 5f;

    // === REFERENCES ===
    private Camera cam;
    private Transform target; // Nhân vật đang theo dõi
    private float targetZoom;  // Zoom đích (để lerp mượt)
    // FIX #17: Cache index để tránh UpdateTarget() mọi frame
    private int cachedCharacterIndex = -1;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
            cam = Camera.main;

        targetZoom = cam.orthographicSize;
    }

    private void Start()
    {
        // Tìm nhân vật đang được điều khiển từ GameManager
        UpdateTarget();
    }

    private void LateUpdate()
    {
        // FIX #17: Chỉ update target khi active character index thay đổi
        int currentIndex = GameManager.Instance?.ActiveCharacterIndex ?? -1;
        if (currentIndex != cachedCharacterIndex)
        {
            UpdateTarget();
            cachedCharacterIndex = currentIndex;
        }

        if (target == null) return;

        // === SMOOTH FOLLOW ===
        Vector3 desiredPosition = target.position + offset;

        // Lerp mượt mà đến vị trí mục tiêu
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // Clamp trong boundary của map
        smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
        smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        smoothedPosition.z = offset.z; // Giữ nguyên Z của camera

        transform.position = smoothedPosition;

        // === ZOOM ===
        HandleZoom();
    }

    /// <summary>
    /// Cập nhật target camera theo nhân vật đang được điều khiển.
    /// Khi SwitchCharacter, camera sẽ tự chuyển mượt sang nhân vật mới
    /// nhờ Lerp (không snap ngay lập tức).
    /// </summary>
    private void UpdateTarget()
    {
        if (GameManager.Instance == null) return;

        PlayerController activeChar = GameManager.Instance.ActiveCharacter;
        if (activeChar != null)
        {
            target = activeChar.transform;
        }
    }

    /// <summary>
    /// Xử lý zoom bằng scroll chuột.
    /// Scroll lên = zoom gần (size nhỏ), scroll xuống = zoom xa (size lớn).
    /// </summary>
    private void HandleZoom()
    {
        // Đọc scroll input
        float scrollInput = Mouse.current != null ? Mouse.current.scroll.y.ReadValue() * 0.001f : 0f;

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            // Scroll lên → zoom gần (giảm size), scroll xuống → zoom xa (tăng size)
            targetZoom -= scrollInput * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // Lerp zoom mượt mà
        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            zoomSmoothSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// Đặt boundary mới cho camera (gọi khi load map mới).
    /// </summary>
    public void SetBoundary(float newMinX, float newMaxX, float newMinY, float newMaxY)
    {
        minX = newMinX;
        maxX = newMaxX;
        minY = newMinY;
        maxY = newMaxY;
    }

    /// <summary>
    /// Snap camera ngay lập tức đến nhân vật (không lerp).
    /// Dùng khi load scene hoặc teleport.
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        Vector3 snapPos = target.position + offset;
        snapPos.x = Mathf.Clamp(snapPos.x, minX, maxX);
        snapPos.y = Mathf.Clamp(snapPos.y, minY, maxY);
        snapPos.z = offset.z;

        transform.position = snapPos;
    }
}
