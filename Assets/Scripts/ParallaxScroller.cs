// ParallaxScroller.cs – CAVE RIFT | Unity 6 | URP 2D
// Gắn vào root container của Parallax BG.
// Các child layer tự cuộn theo camera với hệ số khác nhau.

using UnityEngine;

/// <summary>
/// Hệ thống Parallax Scrolling cho background hang động.
/// Child[0] = xa nhất (hệ số nhỏ), Child[n] = gần nhất (hệ số lớn).
/// Tự tính hệ số dựa theo thứ tự layer (không cần kéo thả).
/// </summary>
public class ParallaxScroller : MonoBehaviour
{
    [Header("Cài đặt Parallax")]
    [Tooltip("Hệ số cuộn thủ công cho từng child (bỏ trống = tự tính)")]
    public float[] layerFactors = { 0.05f, 0.15f, 0.30f };

    [Tooltip("Đặt camera theo dõi (bỏ trống = tự tìm Camera.main)")]
    public Camera targetCamera;

    // ─── Trạng thái nội bộ ────────────────────────────────────
    private Vector3   lastCamPos;
    private Transform[] layers;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning("[Parallax] Không tìm thấy camera. Vô hiệu hóa.");
            enabled = false;
            return;
        }

        lastCamPos = targetCamera.transform.position;

        // Lấy tất cả child layer theo thứ tự
        layers = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            layers[i] = transform.GetChild(i);

        // Nếu layerFactors chưa đủ → tự tính đều
        if (layerFactors == null || layerFactors.Length < layers.Length)
        {
            layerFactors = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                layerFactors[i] = (i + 1) * 0.1f;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 delta = targetCamera.transform.position - lastCamPos;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;

            float factor = i < layerFactors.Length ? layerFactors[i] : 0.1f;

            // Chỉ cuộn ngang (X), không cuộn Y để tránh rối
            layers[i].position += new Vector3(delta.x * factor, delta.y * factor * 0.3f, 0f);
        }

        lastCamPos = targetCamera.transform.position;
    }
}
