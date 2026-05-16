using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// GeneratePlaceholderSprites - Tạo placeholder pixel art 16x16
/// cho toàn bộ game CAVE RIFT khi chưa có art thật.
/// Menu: Tools > CAVE RIFT > Tạo Placeholder Sprites
/// </summary>
public class GeneratePlaceholderSprites : EditorWindow
{
    [MenuItem("Tools/CAVE RIFT/Tạo Placeholder Sprites")]
    static void Generate()
    {
        string basePath = "Assets/Sprites";
        int total = 0;

        // === NHÂN VẬT ===
        // Minh (Tank) - xanh dương đậm
        total += CreateCharSprite($"{basePath}/Characters/Minh/minh_idle.png",
            new Color(0.2f,0.4f,0.8f), new Color(0.8f,0.7f,0.6f), true);
        total += CreateCharSprite($"{basePath}/Characters/Minh/minh_run_sheet.png",
            new Color(0.2f,0.4f,0.8f), new Color(0.8f,0.7f,0.6f), true, 4);
        total += CreateCharSprite($"{basePath}/Characters/Minh/minh_attack_sheet.png",
            new Color(0.2f,0.4f,0.8f), new Color(0.8f,0.7f,0.6f), true, 3);
        total += CreateCharSprite($"{basePath}/Characters/Minh/minh_hurt.png",
            new Color(0.2f,0.4f,0.8f), new Color(0.9f,0.3f,0.3f), true);
        total += CreateCharSprite($"{basePath}/Characters/Minh/minh_die.png",
            new Color(0.15f,0.3f,0.6f), new Color(0.5f,0.5f,0.5f), true);

        // Linh (Crafter) - cam/vàng
        total += CreateCharSprite($"{basePath}/Characters/Linh/linh_idle.png",
            new Color(0.9f,0.6f,0.2f), new Color(0.85f,0.75f,0.65f), false);
        total += CreateCharSprite($"{basePath}/Characters/Linh/linh_run_sheet.png",
            new Color(0.9f,0.6f,0.2f), new Color(0.85f,0.75f,0.65f), false, 4);
        total += CreateCharSprite($"{basePath}/Characters/Linh/linh_attack_sheet.png",
            new Color(0.9f,0.6f,0.2f), new Color(0.85f,0.75f,0.65f), false, 3);
        total += CreateCharSprite($"{basePath}/Characters/Linh/linh_hurt.png",
            new Color(0.9f,0.6f,0.2f), new Color(0.9f,0.3f,0.3f), false);
        total += CreateCharSprite($"{basePath}/Characters/Linh/linh_die.png",
            new Color(0.7f,0.45f,0.15f), new Color(0.5f,0.5f,0.5f), false);

        // Khoa (Scout) - xanh lá
        total += CreateCharSprite($"{basePath}/Characters/Khoa/khoa_idle.png",
            new Color(0.2f,0.7f,0.3f), new Color(0.8f,0.7f,0.6f), true);
        total += CreateCharSprite($"{basePath}/Characters/Khoa/khoa_run_sheet.png",
            new Color(0.2f,0.7f,0.3f), new Color(0.8f,0.7f,0.6f), true, 4);
        total += CreateCharSprite($"{basePath}/Characters/Khoa/khoa_attack_sheet.png",
            new Color(0.2f,0.7f,0.3f), new Color(0.8f,0.7f,0.6f), true, 3);
        total += CreateCharSprite($"{basePath}/Characters/Khoa/khoa_hurt.png",
            new Color(0.2f,0.7f,0.3f), new Color(0.9f,0.3f,0.3f), true);
        total += CreateCharSprite($"{basePath}/Characters/Khoa/khoa_die.png",
            new Color(0.15f,0.5f,0.2f), new Color(0.5f,0.5f,0.5f), true);

        // Trang (Healer) - hồng/tím
        total += CreateCharSprite($"{basePath}/Characters/Trang/trang_idle.png",
            new Color(0.8f,0.4f,0.7f), new Color(0.85f,0.75f,0.65f), false);
        total += CreateCharSprite($"{basePath}/Characters/Trang/trang_run_sheet.png",
            new Color(0.8f,0.4f,0.7f), new Color(0.85f,0.75f,0.65f), false, 4);
        total += CreateCharSprite($"{basePath}/Characters/Trang/trang_attack_sheet.png",
            new Color(0.8f,0.4f,0.7f), new Color(0.85f,0.75f,0.65f), false, 3);
        total += CreateCharSprite($"{basePath}/Characters/Trang/trang_hurt.png",
            new Color(0.8f,0.4f,0.7f), new Color(0.9f,0.3f,0.3f), false);
        total += CreateCharSprite($"{basePath}/Characters/Trang/trang_die.png",
            new Color(0.6f,0.3f,0.5f), new Color(0.5f,0.5f,0.5f), false);

        // Shared
        total += CreateSimple($"{basePath}/Characters/Shared/shadow.png", 16,16,
            new Color(0,0,0,0.3f), DrawCircle);

        // === KẺ THÙ ===
        total += CreateEnemySprite($"{basePath}/Enemies/Gremvak/gremvak_idle.png",
            new Color(0.5f,0.7f,0.2f), 16);
        total += CreateEnemySprite($"{basePath}/Enemies/Gremvak/gremvak_walk_sheet.png",
            new Color(0.5f,0.7f,0.2f), 16, 4);
        total += CreateEnemySprite($"{basePath}/Enemies/Gremvak/gremvak_attack_sheet.png",
            new Color(0.6f,0.8f,0.2f), 16, 3);
        total += CreateEnemySprite($"{basePath}/Enemies/Gremvak/gremvak_hurt.png",
            new Color(0.7f,0.3f,0.2f), 16);
        total += CreateEnemySprite($"{basePath}/Enemies/BongDem/bongdem_idle.png",
            new Color(0.15f,0.1f,0.3f), 16);
        total += CreateEnemySprite($"{basePath}/Enemies/BongDem/bongdem_walk_sheet.png",
            new Color(0.15f,0.1f,0.3f), 16, 4);
        total += CreateEnemySprite($"{basePath}/Enemies/BongDem/bongdem_attack_sheet.png",
            new Color(0.3f,0.1f,0.4f), 16, 3);

        // === ENVIRONMENT ===
        total += CreateTile($"{basePath}/Environment/Tiles/grass_tile.png",
            new Color(0.3f,0.55f,0.2f), new Color(0.35f,0.6f,0.25f));
        total += CreateTile($"{basePath}/Environment/Tiles/dirt_tile.png",
            new Color(0.5f,0.35f,0.2f), new Color(0.55f,0.4f,0.25f));
        total += CreateTile($"{basePath}/Environment/Tiles/stone_tile.png",
            new Color(0.4f,0.4f,0.42f), new Color(0.45f,0.45f,0.47f));
        total += CreateTile($"{basePath}/Environment/Tiles/cave_floor.png",
            new Color(0.25f,0.22f,0.2f), new Color(0.3f,0.27f,0.25f));
        total += CreateTile($"{basePath}/Environment/Tiles/cave_wall.png",
            new Color(0.2f,0.18f,0.17f), new Color(0.15f,0.13f,0.12f));

        total += CreateTreeSprite($"{basePath}/Environment/Trees/tree_01.png",
            new Color(0.2f,0.5f,0.15f), new Color(0.4f,0.28f,0.15f));
        total += CreateTreeSprite($"{basePath}/Environment/Trees/tree_02.png",
            new Color(0.15f,0.45f,0.2f), new Color(0.35f,0.25f,0.12f));
        total += CreateTreeSprite($"{basePath}/Environment/Trees/bush.png",
            new Color(0.25f,0.55f,0.2f), new Color(0.3f,0.6f,0.25f));

        total += CreateSimple($"{basePath}/Environment/Rocks/rock_01.png",16,16,
            new Color(0.45f,0.43f,0.4f), DrawRock);
        total += CreateSimple($"{basePath}/Environment/Rocks/rock_02.png",16,16,
            new Color(0.5f,0.48f,0.45f), DrawRock);

        total += CreateSimple($"{basePath}/Environment/Props/campfire.png",16,16,
            new Color(0.9f,0.5f,0.1f), DrawCampfire);
        total += CreateSimple($"{basePath}/Environment/Props/chest.png",16,16,
            new Color(0.6f,0.4f,0.15f), DrawChest);
        total += CreateSimple($"{basePath}/Environment/Props/ruins.png",16,16,
            new Color(0.4f,0.38f,0.35f), DrawRuins);

        // === UI ICONS ===
        string iconP = $"{basePath}/UI/Icons";
        total += CreateIcon($"{iconP}/icon_wood.png", new Color(0.55f,0.35f,0.15f));
        total += CreateIcon($"{iconP}/icon_stone.png", new Color(0.5f,0.5f,0.52f));
        total += CreateIcon($"{iconP}/icon_food_raw.png", new Color(0.6f,0.75f,0.3f));
        total += CreateIcon($"{iconP}/icon_herb.png", new Color(0.3f,0.7f,0.4f));
        total += CreateIcon($"{iconP}/icon_crystal.png", new Color(0.5f,0.3f,0.9f));
        total += CreateIcon($"{iconP}/icon_tent.png", new Color(0.7f,0.6f,0.4f));
        total += CreateIcon($"{iconP}/icon_stick.png", new Color(0.5f,0.35f,0.2f));
        total += CreateIcon($"{iconP}/icon_food_cooked.png", new Color(0.8f,0.55f,0.2f));
        total += CreateIcon($"{iconP}/icon_medicine.png", new Color(0.3f,0.8f,0.5f));
        total += CreateIcon($"{iconP}/icon_portal_key.png", new Color(0.6f,0.3f,0.9f));
        total += CreateIcon($"{iconP}/icon_sword.png", new Color(0.7f,0.7f,0.75f));

        // HUD
        total += CreateRect($"{basePath}/UI/HUD/bar_bg.png", 64,8, new Color(0.15f,0.15f,0.15f));
        total += CreateRect($"{basePath}/UI/HUD/bar_hp.png", 64,8, new Color(0.3f,0.8f,0.3f));
        total += CreateRect($"{basePath}/UI/HUD/bar_hunger.png", 64,8, new Color(0.9f,0.6f,0.2f));
        total += CreateRect($"{basePath}/UI/HUD/bar_thirst.png", 64,8, new Color(0.3f,0.6f,0.9f));
        total += CreateRect($"{basePath}/UI/HUD/bar_energy.png", 64,8, new Color(0.7f,0.5f,0.9f));

        // Buttons
        total += CreateRect($"{basePath}/UI/Buttons/btn_normal.png", 48,16, new Color(0.25f,0.4f,0.6f));
        total += CreateRect($"{basePath}/UI/Buttons/btn_hover.png", 48,16, new Color(0.35f,0.5f,0.7f));
        total += CreateRect($"{basePath}/UI/Buttons/btn_pressed.png", 48,16, new Color(0.2f,0.3f,0.5f));
        total += CreateRect($"{basePath}/UI/Buttons/panel_bg.png", 64,64, new Color(0.1f,0.1f,0.15f,0.92f));

        // Effects
        total += CreateSimple($"{basePath}/Effects/Particles/particle_dot.png",4,4,
            Color.white, null);
        total += CreateSimple($"{basePath}/Effects/Particles/particle_star.png",8,8,
            new Color(1f,0.9f,0.4f), DrawStar);
        total += CreateSimple($"{basePath}/Effects/Lighting/glow_circle.png",32,32,
            new Color(1f,1f,0.8f,0.5f), DrawGlow);

        AssetDatabase.Refresh();
        Debug.Log($"✅ Đã tạo {total} placeholder sprites cho CAVE RIFT!");
        EditorUtility.DisplayDialog("Hoàn thành", $"Đã tạo {total} placeholder sprites!", "OK");
    }

    // === TẠO SPRITE NHÂN VẬT 16x16 ===
    static int CreateCharSprite(string path, Color bodyColor, Color skinColor, bool isMale, int frames = 1)
    {
        int w = 16 * frames, h = 16;
        Texture2D tex = new Texture2D(w, h);
        ClearTex(tex);

        for (int f = 0; f < frames; f++)
        {
            int ox = f * 16;
            int legOffset = f % 2 == 1 && frames > 1 ? 1 : 0;

            // Chân (2 chân)
            SetPx(tex, ox+6, 0+legOffset, bodyColor*0.7f);
            SetPx(tex, ox+6, 1, bodyColor*0.7f);
            SetPx(tex, ox+9, 0+(frames>1?(1-legOffset):0), bodyColor*0.7f);
            SetPx(tex, ox+9, 1, bodyColor*0.7f);
            // Thân
            for(int x=5;x<=10;x++) for(int y=2;y<=7;y++) SetPx(tex,ox+x,y,bodyColor);
            // Tay
            SetPx(tex,ox+4,4,skinColor); SetPx(tex,ox+4,5,skinColor);
            SetPx(tex,ox+11,4,skinColor); SetPx(tex,ox+11,5,skinColor);
            // Đầu
            for(int x=5;x<=10;x++) for(int y=8;y<=12;y++) SetPx(tex,ox+x,y,skinColor);
            // Tóc
            Color hairColor = isMale ? new Color(0.2f,0.15f,0.1f) : bodyColor*0.8f;
            for(int x=5;x<=10;x++) for(int y=11;y<=13;y++) SetPx(tex,ox+x,y,hairColor);
            // Mắt
            SetPx(tex,ox+6,10,Color.white); SetPx(tex,ox+9,10,Color.white);
            SetPx(tex,ox+6,9,new Color(0.1f,0.1f,0.1f)); SetPx(tex,ox+9,9,new Color(0.1f,0.1f,0.1f));
        }

        tex.Apply();
        SavePNG(path, tex);
        return 1;
    }

    // === TẠO SPRITE KẺ THÙ ===
    static int CreateEnemySprite(string path, Color color, int size, int frames = 1)
    {
        Texture2D tex = new Texture2D(size*frames, size);
        ClearTex(tex);

        for(int f=0;f<frames;f++)
        {
            int ox=f*size;
            int bounce = f%2==1?1:0;
            // Thân tròn
            for(int x=3;x<=12;x++) for(int y=1+bounce;y<=9+bounce;y++)
            {
                float dx=x-7.5f, dy=y-5.5f-bounce;
                if(dx*dx+dy*dy < 25) SetPx(tex,ox+x,y,color);
            }
            // Mắt đỏ
            SetPx(tex,ox+5,7+bounce,new Color(0.9f,0.2f,0.1f));
            SetPx(tex,ox+10,7+bounce,new Color(0.9f,0.2f,0.1f));
            // Miệng
            for(int x=6;x<=9;x++) SetPx(tex,ox+x,4+bounce,color*0.5f);
        }
        tex.Apply();
        SavePNG(path, tex);
        return 1;
    }

    // === TẠO TILE 16x16 ===
    static int CreateTile(string path, Color c1, Color c2)
    {
        Texture2D tex = new Texture2D(16,16);
        for(int x=0;x<16;x++) for(int y=0;y<16;y++)
        {
            float n = Mathf.PerlinNoise(x*0.3f+0.1f, y*0.3f+0.1f);
            tex.SetPixel(x,y, Color.Lerp(c1,c2,n));
        }
        tex.Apply();
        SavePNG(path, tex);
        return 1;
    }

    // === TẠO CÂY ===
    static int CreateTreeSprite(string path, Color leafColor, Color trunkColor)
    {
        Texture2D tex = new Texture2D(16,16);
        ClearTex(tex);
        // Thân cây
        for(int y=0;y<7;y++){SetPx(tex,7,y,trunkColor);SetPx(tex,8,y,trunkColor);}
        // Tán lá (tam giác)
        for(int y=6;y<15;y++){
            int r = (y-5)/2+2;
            for(int x=8-r;x<=7+r;x++){
                if(x>=0&&x<16) SetPx(tex,x,y,leafColor*Random.Range(0.85f,1.1f));
            }
        }
        tex.Apply();
        SavePNG(path, tex);
        return 1;
    }

    // === TẠO ICON 16x16 ===
    static int CreateIcon(string path, Color color)
    {
        Texture2D tex = new Texture2D(16,16);
        // Nền bo góc
        for(int x=0;x<16;x++) for(int y=0;y<16;y++)
        {
            bool corner = (x<2&&y<2)||(x>13&&y<2)||(x<2&&y>13)||(x>13&&y>13);
            tex.SetPixel(x,y, corner ? Color.clear : new Color(0.15f,0.15f,0.2f,0.8f));
        }
        // Hình item ở giữa
        for(int x=4;x<=11;x++) for(int y=4;y<=11;y++)
        {
            float dx=x-7.5f,dy=y-7.5f;
            if(dx*dx+dy*dy<16) tex.SetPixel(x,y,color);
        }
        tex.Apply();
        SavePNG(path, tex);
        return 1;
    }

    // === HÀM HELPER ===
    static int CreateSimple(string path, int w, int h, Color c, System.Action<Texture2D,Color> draw)
    {
        Texture2D tex = new Texture2D(w,h);
        ClearTex(tex);
        if(draw!=null) draw(tex,c);
        else for(int x=0;x<w;x++) for(int y=0;y<h;y++) tex.SetPixel(x,y,c);
        tex.Apply();
        SavePNG(path, tex);
        return 1;
    }

    static int CreateRect(string path, int w, int h, Color c)
    {
        Texture2D tex = new Texture2D(w,h);
        for(int x=0;x<w;x++) for(int y=0;y<h;y++) tex.SetPixel(x,y,c);
        tex.Apply();
        SavePNG(path, tex);
        return 1;
    }

    static void DrawCircle(Texture2D t, Color c){
        int cx=t.width/2,cy=t.height/2; float r=t.width/2f;
        for(int x=0;x<t.width;x++) for(int y=0;y<t.height;y++){
            float d=Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy));
            if(d<r) t.SetPixel(x,y,new Color(c.r,c.g,c.b,c.a*(1f-d/r)));
        }
    }
    static void DrawRock(Texture2D t, Color c){
        for(int x=2;x<14;x++) for(int y=0;y<10;y++){
            float n=Mathf.PerlinNoise(x*0.4f,y*0.4f);
            if(y<8-Mathf.Abs(x-8)*0.5f) t.SetPixel(x,y,c*Random.Range(0.8f,1.1f));
        }
    }
    static void DrawCampfire(Texture2D t, Color c){
        // Gỗ
        for(int x=3;x<13;x++) for(int y=0;y<3;y++) SetPx(t,x,y,new Color(0.4f,0.25f,0.1f));
        // Lửa
        for(int x=5;x<11;x++) for(int y=3;y<10;y++){
            float d=Mathf.Abs(x-8)+(10-y)*0.3f;
            if(d<4) t.SetPixel(x,y,Color.Lerp(c,Color.yellow,d/4f));
        }
    }
    static void DrawChest(Texture2D t, Color c){
        for(int x=2;x<14;x++) for(int y=0;y<8;y++) t.SetPixel(x,y,c);
        for(int x=2;x<14;x++) for(int y=8;y<10;y++) t.SetPixel(x,y,c*0.8f);
        SetPx(t,7,6,Color.yellow); SetPx(t,8,6,Color.yellow);
    }
    static void DrawRuins(Texture2D t, Color c){
        for(int x=1;x<5;x++) for(int y=0;y<12;y++) t.SetPixel(x,y,c);
        for(int x=8;x<12;x++) for(int y=0;y<9;y++) t.SetPixel(x,y,c*0.9f);
        for(int x=3;x<10;x++) for(int y=0;y<3;y++) t.SetPixel(x,y,c*0.85f);
    }
    static void DrawStar(Texture2D t, Color c){
        int cx=t.width/2; SetPx(t,cx,0,c);SetPx(t,cx,t.height-1,c);
        SetPx(t,0,cx,c);SetPx(t,t.width-1,cx,c);
        for(int i=1;i<cx;i++){SetPx(t,cx,i,c);SetPx(t,cx,t.height-1-i,c);SetPx(t,i,cx,c);SetPx(t,t.width-1-i,cx,c);}
        SetPx(t,cx-1,cx-1,c);SetPx(t,cx+1,cx-1,c);SetPx(t,cx-1,cx+1,c);SetPx(t,cx+1,cx+1,c);
    }
    static void DrawGlow(Texture2D t, Color c){ DrawCircle(t,c); }

    static void SetPx(Texture2D t, int x, int y, Color c){
        if(x>=0&&x<t.width&&y>=0&&y<t.height) t.SetPixel(x,y,c);
    }
    static void ClearTex(Texture2D t){
        Color[] px=new Color[t.width*t.height];
        for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        t.SetPixels(px);
    }
    static void SavePNG(string path, Texture2D tex){
        string dir = Path.GetDirectoryName(path);
        if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
