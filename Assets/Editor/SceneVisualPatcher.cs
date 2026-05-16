// SceneVisualPatcher.cs – CAVE RIFT | Unity 6 | Editor Only
// Tools > CAVE RIFT > Patch Scene Visuals (Ngay Lập Tức)
// Áp dụng theme hang động lên scene ĐANG MỞ mà không cần Play.
// Đổi màu nền, Global Light, thêm LightFlicker vào đèn có sẵn.

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CaveRift.Editor
{
    public static class SceneVisualPatcher
    {
        // ─── Màu hang động ────────────────────────────────────────
        private static readonly Color CAVE_BG      = new Color(0.07f, 0.05f, 0.13f);
        private static readonly Color CAVE_GROUND  = new Color(0.12f, 0.09f, 0.22f);
        private static readonly Color CAVE_ROCK    = new Color(0.20f, 0.15f, 0.35f);
        private static readonly Color GLOBAL_LIGHT = new Color(0.12f, 0.10f, 0.25f);

        // ─── Màu nhân vật ─────────────────────────────────────────
        private static readonly Color[] CHAR_COLORS = {
            new Color(0.40f, 0.65f, 1.00f),  // Minh – xanh lam
            new Color(1.00f, 0.65f, 0.20f),  // Linh – cam ấm
            new Color(0.30f, 1.00f, 0.55f),  // Khoa – xanh ngọc
            new Color(1.00f, 0.50f, 0.80f),  // Trang – hồng tím
        };
        private static readonly string[] CHAR_NAMES = { "Minh", "Linh", "Khoa", "Trang" };

        // ══════════════════════════════════════════════════════════
        //  MenuItem chính
        // ══════════════════════════════════════════════════════════
        [MenuItem("Tools/CAVE RIFT/⚡ Patch Scene Visuals (Ngay Lập Tức)")]
        public static void PatchSceneVisuals()
        {
            int changes = 0;

            // 1. Global Light 2D → tối hang động
            changes += PatchGlobalLight();

            // 2. Tất cả SpriteRenderer → đổi màu theo loại object
            changes += PatchAllSpriteRenderers();

            // 3. Thêm LightFlicker vào các Light2D point
            changes += AddLightEffects();

            // 4. Thêm EnemyVisualController vào tất cả enemy
            changes += AddEnemyVisuals();

            // 5. Xóa camera background xanh
            changes += PatchCamera();

            // Lưu scene
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log($"[SceneVisualPatcher] ✅ Đã patch {changes} thay đổi!");
            EditorUtility.DisplayDialog("CAVE RIFT – Visual Patch",
                $"✅ Đã áp dụng {changes} thay đổi visual!\n\n" +
                "• Nền tím đen hang động\n" +
                "• Global Light tối 10%\n" +
                "• Nhân vật màu nổi bật\n" +
                "• Enemy có LightFlicker/EnemyVisualController\n\n" +
                "Nhấn Play để xem kết quả!",
                "OK");
        }

        // ══════════════════════════════════════════════════════════
        //  1. Global Light
        // ══════════════════════════════════════════════════════════
        private static int PatchGlobalLight()
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            int count  = 0;
            foreach (var lt in lights)
            {
                if (lt.lightType != Light2D.LightType.Global) continue;
                lt.color     = GLOBAL_LIGHT;
                lt.intensity = 0.10f;
                EditorUtility.SetDirty(lt);
                count++;
                Debug.Log($"[Patcher] 💡 Global Light → tối hang động");
            }
            return count;
        }

        // ══════════════════════════════════════════════════════════
        //  2. SpriteRenderer – đổi màu theo tên object
        // ══════════════════════════════════════════════════════════
        private static int PatchAllSpriteRenderers()
        {
            var srs   = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var sr in srs)
            {
                string name  = sr.gameObject.name.ToLowerInvariant();
                string pName = sr.transform.parent != null
                    ? sr.transform.parent.name.ToLowerInvariant() : "";
                Color newColor = sr.color;
                bool  changed  = false;

                // Nhân vật theo tên
                for (int i = 0; i < CHAR_NAMES.Length; i++)
                {
                    if (name.Contains(CHAR_NAMES[i].ToLower()) ||
                        pName.Contains(CHAR_NAMES[i].ToLower()))
                    {
                        newColor = CHAR_COLORS[i];
                        changed  = true;
                        break;
                    }
                }

                // Nền / Ground
                if (!changed && (name.Contains("ground") || name.Contains("nền")
                    || name.Contains("bg_") || name.Contains("floor")))
                {
                    if (name.Contains("bg_deep"))
                        newColor = new Color(0.04f, 0.03f, 0.08f);
                    else if (name.Contains("bg_mid"))
                        newColor = new Color(0.07f, 0.05f, 0.14f);
                    else
                        newColor = CAVE_GROUND;
                    changed = true;
                }

                // Đá / Obstacles
                if (!changed && (name.Contains("đá") || name.Contains("rock")
                    || name.Contains("cây") || name.Contains("tree")
                    || name.Contains("wall")))
                {
                    newColor = CAVE_ROCK;
                    changed  = true;
                }

                // Bụi / Tinh thể
                if (!changed && (name.Contains("bụi") || name.Contains("bush")
                    || name.Contains("crystal")))
                {
                    newColor = new Color(0.35f, 0.70f, 1.00f, 0.85f);
                    changed  = true;
                }

                if (changed && newColor != sr.color)
                {
                    Undo.RecordObject(sr, "Patch Color");
                    sr.color = newColor;
                    EditorUtility.SetDirty(sr);
                    count++;
                }
            }

            Debug.Log($"[Patcher] 🎨 Đã đổi màu {count} SpriteRenderer");
            return count;
        }

        // ══════════════════════════════════════════════════════════
        //  3. Light2D Point → thêm LightFlicker
        // ══════════════════════════════════════════════════════════
        private static int AddLightEffects()
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            int count  = 0;

            foreach (var lt in lights)
            {
                if (lt.lightType != Light2D.LightType.Point) continue;

                string nm = lt.gameObject.name.ToLowerInvariant();
                bool isTorch = nm.Contains("torch") || nm.Contains("fire")
                            || nm.Contains("camp");

                if (isTorch && lt.GetComponent<LightFlicker>() == null)
                {
                    lt.gameObject.AddComponent<LightFlicker>();
                    EditorUtility.SetDirty(lt.gameObject);
                    count++;
                }
                else if (!isTorch && lt.GetComponent<GlowPulse>() == null)
                {
                    // Crystal lights → GlowPulse thay vì flicker
                    lt.gameObject.AddComponent<GlowPulse>();
                    EditorUtility.SetDirty(lt.gameObject);
                    count++;
                }
            }

            Debug.Log($"[Patcher] ✨ Thêm {count} light effect components");
            return count;
        }

        // ══════════════════════════════════════════════════════════
        //  4. Enemy → thêm EnemyVisualController
        // ══════════════════════════════════════════════════════════
        private static int AddEnemyVisuals()
        {
            // Tìm qua EnemyBase
            var enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            int count   = 0;

            foreach (var e in enemies)
            {
                if (e.GetComponent<EnemyVisualController>() != null) continue;
                Undo.AddComponent<EnemyVisualController>(e.gameObject);
                count++;
                Debug.Log($"[Patcher] 👹 Thêm EnemyVisualController → {e.name}");
            }

            // Tìm thêm theo tên
            string[] enemyKeywords = { "enemy", "bongdem", "gremvak", "boss", "kẻ thù" };
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allObjects)
            {
                if (go.GetComponent<EnemyVisualController>() != null) continue;
                if (go.GetComponent<SpriteRenderer>() == null) continue;

                string nm = go.name.ToLowerInvariant();
                bool isEnemy = false;
                foreach (var kw in enemyKeywords)
                    if (nm.Contains(kw)) { isEnemy = true; break; }

                if (isEnemy)
                {
                    Undo.AddComponent<EnemyVisualController>(go);
                    count++;
                }
            }

            Debug.Log($"[Patcher] 👹 Tổng EnemyVisualController đã thêm: {count}");
            return count;
        }

        // ══════════════════════════════════════════════════════════
        //  5. Camera → background đen tím
        // ══════════════════════════════════════════════════════════
        private static int PatchCamera()
        {
            var cams  = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var cam in cams)
            {
                if (cam.backgroundColor == CAVE_BG) continue;
                Undo.RecordObject(cam, "Patch Camera BG");
                cam.clearFlags       = CameraClearFlags.SolidColor;
                cam.backgroundColor  = CAVE_BG;
                EditorUtility.SetDirty(cam);
                count++;
                Debug.Log($"[Patcher] 📷 Camera '{cam.name}' → nền tím đen");
            }

            return count;
        }

        // ══════════════════════════════════════════════════════════
        //  MenuItem phụ: chỉ đổi màu nhân vật
        // ══════════════════════════════════════════════════════════
        [MenuItem("Tools/CAVE RIFT/Patch: Chỉ Đổi Màu Nhân Vật")]
        public static void PatchCharacterColorsOnly()
        {
            int count = 0;
            for (int i = 0; i < CHAR_NAMES.Length; i++)
            {
                // Tìm GameObject tên khớp
                var go = GameObject.Find(CHAR_NAMES[i]);
                if (go == null) continue;

                var sr = go.GetComponentInChildren<SpriteRenderer>();
                if (sr == null) continue;

                Undo.RecordObject(sr, "Patch Char Color");
                sr.color = CHAR_COLORS[i];
                EditorUtility.SetDirty(sr);
                count++;
                Debug.Log($"[Patcher] 👤 {CHAR_NAMES[i]} → {CHAR_COLORS[i]}");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[Patcher] Đã đổi màu {count} nhân vật.");
        }

        // ══════════════════════════════════════════════════════════
        //  MenuItem phụ: reset về màu gốc (Ctrl+Z cũng được)
        // ══════════════════════════════════════════════════════════
        [MenuItem("Tools/CAVE RIFT/Patch: Hoàn Tác Visual (Undo All)")]
        public static void UndoAllPatches()
        {
            Undo.PerformUndo();
            Debug.Log("[Patcher] Đã hoàn tác.");
        }
    }
}
#endif
