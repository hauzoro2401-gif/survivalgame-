# Art Setup Report — CAVE RIFT

## 1. Sprites đã import (Generated Placeholders)

Do không thể tải trực tiếp từ repo qua terminal, hệ thống đã dùng script `GeneratePlaceholderSprites.cs` để tạo toàn bộ asset pixel art 16x16:

*   **Characters/ (21 Sprites)**
    *   Minh (Tank - Xanh dương): Idle, Run, Attack, Hurt, Die
    *   Linh (Crafter - Vàng cam): Idle, Run, Attack, Hurt, Die
    *   Khoa (Scout - Xanh lá): Idle, Run, Attack, Hurt, Die
    *   Trang (Healer - Hồng tím): Idle, Run, Attack, Hurt, Die
    *   Shared: Shadow
*   **Enemies/ (7 Sprites)**
    *   Gremvak (Quái nhỏ - Xanh lục): Idle, Walk, Attack, Hurt
    *   BongDem (Bóng tối - Tím đen): Idle, Walk, Attack
*   **Environment/ (11 Sprites)**
    *   Tiles: grass_tile, dirt_tile, stone_tile, cave_floor, cave_wall
    *   Trees: tree_01, tree_02, bush
    *   Rocks: rock_01, rock_02
    *   Props: campfire, chest, ruins
*   **UI/ (16 Sprites)**
    *   Icons: wood, stone, food_raw, herb, crystal, tent, stick, food_cooked, medicine, portal_key, sword
    *   HUD: bar_bg, bar_hp, bar_hunger, bar_thirst, bar_energy
    *   Buttons: btn_normal, btn_hover, btn_pressed, panel_bg
*   **Effects/ (3 Sprites)**
    *   Particles: dot, star, glow_circle

## 2. Animators đã tạo

Đã tạo sẵn thông qua `AnimatorSetup.cs` (Tools > CAVE RIFT > 2. Setup Animators):

*   **Characters (4 Controllers):** `MinhAnimator`, `LinhAnimator`, `KhoaAnimator`, `TrangAnimator`
    *   States: Idle, Run, Attack, Hurt, Die
    *   Parameters: Speed (Float), Attack (Trigger), Hurt (Trigger), Die (Trigger)
*   **Enemies (2 Controllers):** `GremvakAnimator`, `BongDemAnimator`
    *   States: Idle, Walk, Attack, Hurt, Die
    *   Parameters: Speed (Float), Attack (Trigger), Hurt (Trigger), Die (Trigger)

## 3. Assets chưa tìm được / Cần làm thủ công

*   **Animation Clips:** Các `AnimatorController` đã có sơ đồ (State Machine) và chuyển cảnh (Transitions), nhưng chưa được gán `AnimationClip` (do sprite tạo động nên chưa tự động gen clip được). Bạn cần tạo clip và kéo các frame sprite vào tương ứng.
*   **Prefabs:** AssetLinker sẽ tự động link sprite nếu bạn đã tạo prefab có tên tương ứng (Minh, Linh, Khoa, Trang, Gremvak, BongDem). Nếu chưa tạo, hãy kéo các sprite vào scene để tạo prefab trước, sau đó chạy `Tools > CAVE RIFT > 3. Link Assets`.

## 4. Bước tiếp theo (Hướng dẫn hoàn thiện)

Để game có hình ảnh hoàn chỉnh, hãy làm theo các bước sau trong Unity Editor:

1.  **Chạy Generator:** Vào menu `Tools > CAVE RIFT > Tạo Placeholder Sprites`. Đợi vài giây để ảnh được tạo vào `Assets/Sprites`.
2.  **Cấu hình Pixel Art:** Vào menu `Tools > CAVE RIFT > 1. Setup Sprites` để chuyển toàn bộ ảnh sang chuẩn Pixel Art (Point filter, 16 PPU, cắt spritesheet).
3.  **Tạo Animator:** Vào menu `Tools > CAVE RIFT > 2. Setup Animators`.
4.  **Tạo Map:** Mở scene `ForestGlow` (hoặc `SampleScene`), vào menu `Tools > CAVE RIFT > 4. Generate Map` để tạo bản đồ 50x30 tự động bằng Tilemap.
5.  **Tạo Prefab (Thủ công):** Kéo sprite idle của các nhân vật và quái vào Hierarchy, đổi tên thành `Minh`, `Linh`, `Gremvak`..., thêm script tương ứng (`PlayerController`, `Enemy_Gremvak`...) và lưu thành Prefab trong thư mục `Assets/Prefabs`.
6.  **Gán tự động:** Cuối cùng, vào menu `Tools > CAVE RIFT > 3. Link Assets` để gán icon vào `ItemData` và gán Controller vào Prefab.
