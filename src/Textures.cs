// Textures.cs - procedural wall / floor textures
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Granny13
{
    public class Tex
    {
        public int W, H;
        public int[] Px;

        public Tex(int w, int h)
        {
            W = w; H = h; Px = new int[w * h];
        }

        public void Set(int x, int y, int c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Px[y * W + x] = c;
        }

        public int Get(int x, int y)
        {
            x = x % W; if (x < 0) x += W;
            y = y % H; if (y < 0) y += H;
            return Px[y * W + x];
        }

        public void Fill(int c) { for (int i = 0; i < Px.Length; i++) Px[i] = c; }

        public void Rect(int x0, int y0, int x1, int y1, int c)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++) Set(x, y, c);
        }

        public void Line(int x0, int y0, int x1, int y1, int c)
        {
            int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        public void Ellipse(int cx, int cy, double rx, double ry, int c)
        {
            for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
                {
                    double dx = (x - cx) / rx, dy = (y - cy) / ry;
                    if (dx * dx + dy * dy <= 1.0) Set(x, y, c);
                }
        }

        public static int RGB(int r, int g, int b)
        {
            if (r < 0) r = 0; if (r > 255) r = 255;
            if (g < 0) g = 0; if (g > 255) g = 255;
            if (b < 0) b = 0; if (b > 255) b = 255;
            return (0xFF << 24) | (r << 16) | (g << 8) | b;
        }

        public static int Shade(int c, float f)
        {
            int r = (int)(((c >> 16) & 0xFF) * f);
            int g = (int)(((c >> 8) & 0xFF) * f);
            int b = (int)((c & 0xFF) * f);
            return RGB(r, g, b);
        }

        public void Grain(Rng r, float amount)
        {
            for (int i = 0; i < Px.Length; i++)
            {
                float n = (float)(r.Next() - 0.5) * amount;
                int c = Px[i];
                int rr = (int)((c >> 16 & 0xFF) + n * 255);
                int gg = (int)((c >> 8 & 0xFF) + n * 255);
                int bb = (int)((c & 0xFF) + n * 255);
                Px[i] = RGB(rr, gg, bb);
            }
        }

        public void VignetteEdge(float amount)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x - W * 0.5f) / W, dy = (y - H * 0.5f) / H;
                    float d = (float)Math.Sqrt(dx * dx * 1.5 + dy * dy);
                    float f = 1f - amount * M.Clamp01(d * 1.6f);
                    int i = y * W + x;
                    Px[i] = Shade(Px[i], f);
                }
        }
    }

    public static class TexLib
    {
        public const int S = 128;
        public static Tex[] Wall;
        public static float[] WallUv;
        public static Tex[] Floor;
        public static Tex[] Ceil;
        public static Tex Portrait;
        public static Sprite[] Icons;
        public static Sprite[] Furn;
        public static Sprite Glow;
        public static Sprite Bed, TrapSprite, ChestSprite;
        public static bool Built;

        // wall texture indices
        public const int TW_WP = 0, TW_PANEL = 1, TW_BRICK = 2, TW_DOOR = 3, TW_FRONT = 4,
            TW_WARDROBE = 5, TW_CABINET = 6, TW_SHELF = 7, TW_FRIDGE = 8, TW_STOVE = 9,
            TW_CRATE = 10, TW_WINDOW = 11, TW_ATTIC = 12, TW_ATTICWOOD = 13, TW_PORTRAIT = 14,
            TW_DOOROPEN = 15, TW_MIRROR = 16, TW_TILE = 17, TW_PANEL2 = 18, TW_BOOKS = 19,
            TW_TABLE = 20, TW_TOWELS = 21, TW_ATTCAGE = 22, TW_ATTICUP = 23;

        // floor texture indices
        public const int TF_WOOD = 0, TF_RUG = 1, TF_ATTIC = 2, TF_TILE = 3, TF_CONCRETE = 4;

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695040;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        static float VNoise(float x, float y, int seed)
        {
            int x0 = (int)Math.Floor(x), y0 = (int)Math.Floor(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed);
            float c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
            return M.Lerp(M.Lerp(a, b, fx), M.Lerp(c, d, fx), fy);
        }

        static float FBM(float x, float y, int seed, int oct)
        {
            float s = 0, amp = 1, tot = 0, f = 1;
            for (int i = 0; i < oct; i++)
            {
                s += VNoise(x * f, y * f, seed + i * 77) * amp;
                tot += amp; amp *= 0.5f; f *= 2.03f;
            }
            return s / tot;
        }

        public static void Build()
        {
            if (Built) return;
            Wall = new Tex[32];
            WallUv = new float[32];
            for (int i = 0; i < WallUv.Length; i++) WallUv[i] = 1f;
            Floor = new Tex[8];
            Ceil = new Tex[8];
            Rng r = new Rng(1337);

            BuildWallpaper(r);
            BuildPanel(r, TW_PANEL, 96, 66, 42, 128, 128);
            BuildPanel(r, TW_PANEL2, 74, 54, 40, 64, 128);
            BuildBrick(r);
            BuildDoor(r);
            BuildFrontDoor(r);
            BuildWardrobe(r);
            BuildCabinet(r);
            BuildShelf(r);
            BuildFridge(r);
            BuildStove(r);
            BuildCrate(r);
            BuildWindow(r);
            BuildAtticWall(r);
            BuildAtticWood(r);
            BuildPortrait();
            BuildTileWall(r);
            BuildTowels(r);
            BuildAtticCage(r);
            BuildAtticUp(r);
            BuildTableWall(r);

            Floor[TF_WOOD] = MakeWoodFloor(r, 108, 74, 44, false);
            Floor[TF_RUG] = MakeRug(r);
            Floor[TF_ATTIC] = MakeWoodFloor(r, 92, 74, 52, true);
            Floor[TF_TILE] = MakeTiles(r);
            Floor[TF_CONCRETE] = MakeConcrete(r);

            Ceil[0] = MakeCeil(r, false);
            Ceil[1] = MakeCeil(r, true);

            LoadIcons();
            BuildItemSprites();
            Furn = Furniture.Build();
            Glow = new Sprite(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    double dx = (x - 31.5) / 31.5, dy = (y - 31.5) / 31.5;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    double a = 1.0 - d;
                    a = a <= 0 ? 0 : a * a * a;
                    int ai = (int)(a * 255);
                    Glow.Px[y * 64 + x] = (ai << 24) | 0xFFFFFF;
                }
            WallUv[TW_WP] = 2.0f;
            WallUv[TW_TILE] = 2.0f;
            WallUv[TW_ATTIC] = 1.35f;
            Built = true;
            Log.W("textures built");
        }

        static void LoadIcons()
        {
            Icons = new Sprite[8];
            Sprite atlas = Assets.LoadSprite("items.png");
            if (atlas == null) return;
            int t = atlas.H / 2;
            for (int i = 0; i < 8; i++)
                Icons[i] = atlas.Cut((i % 4) * t, (i / 4) * t, t, t);
        }

        static void BuildItemSprites()
        {
            TrapSprite = Icons != null ? Icons[6].Resized(96) : null;
            if (TrapSprite != null) TrapSprite = TrapSprite.Tinted(0x995522, 0.15f);
        }

        // ------------------------------------------------------------------ walls
        static void BuildWallpaper(Rng r)
        {
            Tex t = new Tex(S, S);
            if (Assets.Wallpaper != null)
            {
                Sprite w = Assets.Wallpaper;
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        int c = w.Px[(y * w.H / S) * w.W + (x * w.W / S)];
                        float sh = 1.12f + 0.16f * FBM(x / 22f, y / 22f, 5, 3);
                        t.Px[y * S + x] = Tex.Shade(c, sh);
                    }
            }
            else
            {
                t.Fill(Tex.RGB(40, 52, 88));
                for (int i = 0; i < 60; i++)
                    t.Ellipse((int)r.Range(0, S), (int)r.Range(0, S), 10, 8, Tex.RGB(150, 40, 60));
            }
            // vertical shadow gradient like old wallpaper
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float edge = 1f - 0.18f * (float)Math.Pow(Math.Abs(x - S / 2f) / (S / 2f), 3);
                    t.Px[y * S + x] = Tex.Shade(t.Px[y * S + x], edge);
                }
            Wall[TW_WP] = t;
        }

        static void BuildPanel(Rng r, int idx, int cr, int cg, int cb, int w, int h)
        {
            Tex t = new Tex(S, S);
            int plankW = S / 4;
            for (int p = 0; p < 4; p++)
            {
                float v = 0.82f + 0.18f * (float)r.Next();
                int baseC = Tex.RGB((int)(cr * v), (int)(cg * v), (int)(cb * v));
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < plankW; x++)
                    {
                        float grain = FBM(x / 3.5f, (y + p * 40) / 34f, 9 + p, 3);
                        float f = 0.86f + 0.20f * grain;
                        t.Set(p * plankW + x, y, Tex.Shade(baseC, f));
                    }
                // groove
                for (int y = 0; y < S; y++)
                {
                    t.Set(p * plankW, y, Tex.Shade(baseC, 0.42f));
                    t.Set(p * plankW + 1, y, Tex.Shade(baseC, 0.62f));
                    t.Set(p * plankW + 2, y, Tex.Shade(baseC, 0.9f));
                }
            }
            // horizontal beams
            for (int y = 0; y < 7; y++)
            {
                float f = 0.55f + 0.06f * y;
                for (int x = 0; x < S; x++) t.Set(x, y, Tex.Shade(t.Get(x, y), f));
                for (int x = 0; x < S; x++) t.Set(x, S - 7 + y, Tex.Shade(t.Get(x, S - 7 + y), f));
            }
            // nails
            for (int x = 6; x < S; x += 22)
            {
                t.Set(x, 12, Tex.RGB(30, 24, 18)); t.Set(x + 1, 12, Tex.RGB(50, 42, 34));
                t.Set(x, S - 14, Tex.RGB(30, 24, 18));
            }
            t.Grain(r, 0.05f);
            Wall[idx] = t;
        }

        static void BuildBrick(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(58, 52, 48));
            int bh = 16, bw = 34;
            for (int row = 0; row < S / bh + 1; row++)
            {
                int off = (row % 2 == 0) ? 0 : -bw / 2;
                for (int col = -1; col < S / bw + 2; col++)
                {
                    int bx = off + col * bw, by = row * bh;
                    float v = 0.72f + 0.38f * (float)r.Next();
                    int baseC = Tex.RGB((int)(96 * v), (int)(66 * v), (int)(56 * v));
                    for (int y = 0; y < bh - 2; y++)
                        for (int x = 0; x < bw - 2; x++)
                        {
                            float g = FBM((bx + x) / 9f, (by + y) / 9f, 21, 3);
                            int c = Tex.Shade(baseC, 0.75f + 0.5f * g);
                            int px = bx + x + 1, py = by + y + 1;
                            if (px >= 0 && px < S && py >= 0 && py < S) t.Set(px, py, c);
                        }
                }
            }
            // mortar grime
            for (int i = 0; i < 40; i++)
            {
                int x = (int)r.Range(0, S), y = (int)r.Range(0, S);
                t.Ellipse(x, y, r.Range(2, 7), r.Range(3, 9), Tex.RGB(40, 36, 34));
            }
            t.Grain(r, 0.09f);
            Wall[TW_BRICK] = t;
        }

        static void BuildDoor(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(24, 18, 14));
            // frame
            t.Rect(6, 4, S - 7, S - 1, Tex.RGB(92, 62, 34));
            // door leaf
            int x0 = 13, x1 = S - 14;
            for (int y = 0; y < S; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float g = FBM(x / 6f, y / 46f, 33, 3);
                    t.Set(x, y, Tex.Shade(Tex.RGB(112, 76, 40), 0.78f + 0.34f * g));
                }
            // panels
            int c1 = Tex.RGB(78, 50, 26), c2 = Tex.RGB(140, 100, 58);
            t.Rect(x0 + 12, 18, x1 - 12, 52, c1);
            t.Rect(x0 + 14, 20, x1 - 14, 50, c2);
            t.Rect(x0 + 12, 68, x1 - 12, 112, c1);
            t.Rect(x0 + 14, 70, x1 - 14, 110, c2);
            // handle
            t.Ellipse(S - 30, 66, 5, 5, Tex.RGB(206, 176, 96));
            t.Ellipse(S - 31, 65, 3, 3, Tex.RGB(246, 220, 150));
            t.Rect(S - 33, 66, S - 27, 72, Tex.RGB(160, 130, 70));
            t.Grain(r, 0.05f);
            Wall[TW_DOOR] = t;
        }

        static void BuildFrontDoor(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(20, 16, 14));
            t.Rect(2, 2, S - 3, S - 1, Tex.RGB(64, 44, 26));
            for (int y = 0; y < S; y++)
                for (int x = 8; x < S - 8; x++)
                {
                    float g = FBM(x / 7f, y / 40f, 44, 3);
                    t.Set(x, y, Tex.Shade(Tex.RGB(74, 48, 28), 0.8f + 0.36f * g));
                }
            // iron bands
            for (int k = 0; k < 3; k++)
            {
                int y = 18 + k * 40;
                t.Rect(8, y, S - 9, y + 7, Tex.RGB(46, 44, 48));
                t.Rect(8, y + 1, S - 9, y + 3, Tex.RGB(86, 84, 92));
                for (int x = 14; x < S - 10; x += 12) { t.Set(x, y + 4, Tex.RGB(20, 18, 20)); t.Set(x + 1, y + 4, Tex.RGB(70, 68, 76)); }
            }
            // lock plate
            t.Rect(S / 2 - 9, 54, S / 2 + 9, 76, Tex.RGB(58, 56, 62));
            t.Rect(S / 2 - 7, 56, S / 2 + 7, 74, Tex.RGB(96, 94, 102));
            t.Ellipse(S / 2, 63, 3, 3, Tex.RGB(24, 22, 24));
            t.Grain(r, 0.06f);
            Wall[TW_FRONT] = t;
        }

        static void BuildWardrobe(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(38, 26, 16));
            t.Rect(4, 0, S - 5, S - 1, Tex.RGB(78, 52, 30));
            for (int y = 0; y < S; y++)
                for (int x = 8; x < S - 8; x++)
                {
                    float g = FBM(x / 5f, y / 30f, 55, 3);
                    t.Set(x, y, Tex.Shade(Tex.RGB(96, 64, 36), 0.82f + 0.3f * g));
                }
            // two doors
            t.Rect(10, 8, S / 2 - 3, S - 8, Tex.RGB(72, 48, 28));
            t.Rect(10, 8, S / 2 - 3, 12, Tex.RGB(122, 88, 50));
            t.Rect(S / 2 + 3, 8, S - 11, S - 8, Tex.RGB(72, 48, 28));
            t.Rect(S / 2 + 3, 8, S - 11, 12, Tex.RGB(122, 88, 50));
            // inset panels
            t.Rect(18, 22, S / 2 - 10, 60, Tex.RGB(94, 64, 36));
            t.Rect(18, 68, S / 2 - 10, 112, Tex.RGB(94, 64, 36));
            t.Rect(S / 2 + 10, 22, S - 18, 60, Tex.RGB(94, 64, 36));
            t.Rect(S / 2 + 10, 68, S - 18, 112, Tex.RGB(94, 64, 36));
            // handles
            t.Ellipse(S / 2 - 10, 64, 4, 6, Tex.RGB(206, 180, 100));
            t.Ellipse(S / 2 + 10, 64, 4, 6, Tex.RGB(206, 180, 100));
            t.Grain(r, 0.05f);
            Wall[TW_WARDROBE] = t;
        }

        static void BuildCabinet(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(58, 40, 24));
            t.Rect(2, 2, S - 3, S - 2, Tex.RGB(104, 72, 42));
            for (int i = 0; i < 3; i++)
            {
                int y0 = 10 + i * 38;
                t.Rect(8, y0, S - 9, y0 + 30, Tex.RGB(84, 56, 32));
                t.Rect(10, y0 + 2, S - 11, y0 + 28, Tex.RGB(126, 92, 54));
                t.Rect(52, y0 + 12, 76, y0 + 17, Tex.RGB(196, 172, 104));
                t.Rect(52, y0 + 12, 76, y0 + 13, Tex.RGB(232, 214, 150));
            }
            t.Grain(r, 0.06f);
            Wall[TW_CABINET] = t;
        }

        static void BuildShelf(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(30, 22, 14));
            t.Rect(2, 0, S - 3, S - 1, Tex.RGB(62, 44, 26));
            for (int s = 0; s < 4; s++)
            {
                int y = 8 + s * 30;
                t.Rect(6, y + 22, S - 7, y + 26, Tex.RGB(84, 62, 38));
                int x = 10;
                while (x < S - 14)
                {
                    int bw = (int)r.Range(5, 12);
                    int bh = (int)r.Range(16, 24);
                    int col = Tex.RGB((int)r.Range(60, 160), (int)r.Range(30, 90), (int)r.Range(30, 90));
                    t.Rect(x, y + 22 - bh, x + bw, y + 21, col);
                    t.Rect(x, y + 22 - bh, x + 1, y + 21, Tex.Shade(col, 1.25f));
                    x += bw + 1;
                }
            }
            t.Grain(r, 0.04f);
            Wall[TW_SHELF] = t;
        }

        static void BuildFridge(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(30, 32, 34));
            t.Rect(4, 2, S - 5, S - 2, Tex.RGB(196, 198, 194));
            for (int y = 0; y < S; y++)
                for (int x = 6; x < S - 6; x++)
                {
                    float g = FBM(x / 8f, y / 8f, 66, 2);
                    float f = 0.9f + 0.12f * g - 0.05f * (float)Math.Abs(x - S / 2f) / S;
                    t.Set(x, y, Tex.Shade(Tex.RGB(198, 200, 196), f));
                }
            t.Rect(6, 44, S - 7, 47, Tex.RGB(150, 152, 150));
            t.Rect(S - 26, 14, S - 19, 34, Tex.RGB(150, 152, 150));
            t.Rect(S - 26, 56, S - 19, 88, Tex.RGB(150, 152, 150));
            // magnets
            t.Rect(24, 24, 38, 36, Tex.RGB(180, 60, 60));
            t.Rect(42, 30, 54, 40, Tex.RGB(220, 200, 80));
            t.Grain(r, 0.03f);
            Wall[TW_FRIDGE] = t;
        }

        static void BuildStove(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Rect(0, 0, S - 1, S - 1, Tex.RGB(58, 56, 58));
            t.Rect(6, 6, S - 7, 62, Tex.RGB(30, 30, 32));
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                {
                    int cx = 36 + i * 56, cy = 22 + j * 22;
                    t.Ellipse(cx, cy, 13, 9, Tex.RGB(72, 70, 74));
                    t.Ellipse(cx, cy, 10, 7, Tex.RGB(24, 24, 26));
                }
            t.Rect(8, 70, S - 9, 120, Tex.RGB(44, 42, 44));
            t.Rect(14, 76, S - 15, 114, Tex.RGB(28, 28, 30));
            t.Rect(20, 80, S - 21, 96, Tex.RGB(60, 40, 30));
            t.Rect(S - 40, 100, S - 18, 106, Tex.RGB(160, 160, 164));
            t.Grain(r, 0.04f);
            Wall[TW_STOVE] = t;
        }

        static void BuildCrate(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(70, 50, 30));
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int plank = y / 26;
                    float g = FBM(x / 24f, (y + plank * 13) / 6f, 71 + plank, 2);
                    t.Set(x, y, Tex.Shade(Tex.RGB(108, 80, 48), 0.8f + 0.35f * g));
                }
            for (int y = 0; y < S; y += 26) { t.Rect(0, y, S - 1, y + 1, Tex.RGB(48, 34, 20)); }
            t.Rect(0, 0, 8, S - 1, Tex.RGB(84, 60, 34));
            t.Rect(S - 9, 0, S - 1, S - 1, Tex.RGB(84, 60, 34));
            // X brace
            t.Line(4, 4, S - 6, S - 6, Tex.RGB(88, 64, 38));
            t.Line(S - 6, 4, 4, S - 6, Tex.RGB(88, 64, 38));
            t.Grain(r, 0.05f);
            Wall[TW_CRATE] = t;
        }

        static void BuildWindow(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(26, 22, 20));
            t.Rect(10, 8, S - 11, S - 9, Tex.RGB(56, 40, 26));
            // night sky
            for (int y = 12; y < S - 13; y++)
                for (int x = 14; x < S - 15; x++)
                {
                    float f = 0.5f + 0.5f * (float)y / S;
                    t.Set(x, y, Tex.RGB((int)(16 + 14 * f), (int)(22 + 20 * f), (int)(40 + 34 * f)));
                }
            // moon
            t.Ellipse(44, 40, 11, 11, Tex.RGB(210, 216, 200));
            t.Ellipse(46, 38, 8, 8, Tex.RGB(232, 236, 220));
            // stars
            for (int i = 0; i < 26; i++)
                t.Set((int)r.Range(16, S - 18), (int)r.Range(14, S - 16), Tex.RGB(180, 190, 200));
            // bars
            t.Rect(S / 2 - 2, 12, S / 2 + 2, S - 13, Tex.RGB(60, 44, 28));
            t.Rect(14, S / 2 - 2, S - 15, S / 2 + 2, Tex.RGB(60, 44, 28));
            Wall[TW_WINDOW] = t;
        }

        static void BuildAtticWall(Rng r)
        {
            BuildBrick(r);
            Tex b = Wall[TW_BRICK];
            Tex t = new Tex(S, S);
            for (int i = 0; i < t.Px.Length; i++) t.Px[i] = Tex.Shade(b.Px[i], 0.62f);
            // cobwebs
            for (int i = 0; i < 6; i++)
            {
                int cx = (int)r.Range(0, S), cy = (int)r.Range(0, S);
                for (int k = 0; k < 7; k++)
                {
                    double a = r.Range(0, M.TAU);
                    t.Line(cx, cy, cx + (int)(Math.Cos(a) * 22), cy + (int)(Math.Sin(a) * 22), Tex.RGB(120, 120, 110));
                }
            }
            Wall[TW_ATTIC] = t;
        }

        static void BuildAtticWood(Rng r)
        {
            Tex t = new Tex(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float g = FBM(x / 40f, y / 7f, 88, 3);
                    t.Set(x, y, Tex.Shade(Tex.RGB(84, 68, 48), 0.7f + 0.4f * g));
                }
            for (int x = 0; x < S; x += 24)
            {
                t.Rect(x, 0, x + 1, S - 1, Tex.RGB(38, 30, 22));
                t.Rect(x + 2, 0, x + 3, S - 1, Tex.RGB(110, 92, 68));
            }
            t.Grain(r, 0.08f);
            Wall[TW_ATTICWOOD] = t;
        }

        static void BuildPortrait()
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(28, 22, 16));
            t.Rect(6, 6, S - 7, S - 7, Tex.RGB(150, 116, 54));
            t.Rect(9, 9, S - 10, S - 10, Tex.RGB(196, 162, 88));
            t.Rect(13, 13, S - 14, S - 14, Tex.RGB(18, 16, 20));
            Sprite f = Assets.GrannyFace;
            if (f != null)
            {
                int bw = S - 26, bh = S - 26;
                for (int y = 0; y < bh; y++)
                    for (int x = 0; x < bw; x++)
                    {
                        int sx = x * f.W / bw, sy = (int)((y + 20) * f.H / (bh + 40));
                        if (sx >= f.W) sx = f.W - 1; if (sy >= f.H) sy = f.H - 1;
                        int c = f.Px[sy * f.W + sx];
                        t.Set(13 + x, 13 + y, Tex.Shade(c, 0.72f));
                    }
            }
            Wall[TW_PORTRAIT] = t;
        }

        static void BuildTileWall(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(40, 52, 52));
            for (int y = 0; y < S; y += 16)
                for (int x = 0; x < S; x += 16)
                {
                    float v = 0.85f + 0.25f * (float)r.Next();
                    t.Rect(x + 1, y + 1, x + 14, y + 14, Tex.Shade(Tex.RGB(120, 150, 148), v));
                }
            t.Grain(r, 0.03f);
            Wall[TW_TILE] = t;
        }

        static void BuildTowels(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(120, 132, 136));
            t.Rect(0, 0, S - 1, 10, Tex.RGB(80, 88, 92));
            t.Rect(0, 60, S - 1, 66, Tex.RGB(90, 96, 100));
            int[] cols = { 0x8FA0B0, 0xB09090, 0x90B0A0, 0xA0A0C0 };
            for (int i = 0; i < 4; i++)
            {
                int x0 = 8 + i * 28;
                t.Rect(x0, 12, x0 + 20, 58, cols[i]);
                t.Rect(x0, 12, x0 + 3, 58, Tex.Shade(cols[i], 1.2f));
                for (int y = 18; y < 56; y += 4) t.Rect(x0 + 4, y, x0 + 19, y, Tex.Shade(cols[i], 0.9f));
            }
            t.Grain(r, 0.03f);
            Wall[TW_TOWELS] = t;
        }

        static void BuildAtticCage(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(24, 20, 16));
            t.Rect(4, 4, S - 5, S - 5, Tex.RGB(46, 38, 28));
            for (int x = 8; x < S - 8; x += 8)
                t.Rect(x, 8, x + 2, S - 9, Tex.RGB(96, 84, 64));
            for (int y = 8; y < S - 8; y += 32)
                t.Rect(6, y, S - 7, y + 2, Tex.RGB(86, 74, 56));
            // something inside
            t.Ellipse(64, 78, 18, 13, Tex.RGB(70, 62, 50));
            t.Ellipse(58, 72, 4, 4, Tex.RGB(150, 130, 90));
            t.Grain(r, 0.06f);
            Wall[TW_ATTCAGE] = t;
        }

        static void BuildAtticUp(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(30, 24, 18));
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float g = FBM(x / 30f, y / 10f, 99, 3);
                    float f = (x / (float)S < 0.5f) ? 1f : 0.82f;
                    t.Set(x, y, Tex.Shade(Tex.RGB(92, 72, 48), (0.7f + 0.35f * g) * f));
                }
            t.Rect(6, 60, S - 7, 68, Tex.RGB(40, 32, 22));
            t.Rect(6, 60, S - 7, 61, Tex.RGB(120, 96, 62));
            t.Grain(r, 0.07f);
            Wall[TW_ATTICUP] = t;
        }

        static void BuildTableWall(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(50, 36, 22));
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float g = FBM(x / 20f, y / 24f, 123, 3);
                    t.Set(x, y, Tex.Shade(Tex.RGB(116, 84, 50), 0.78f + 0.4f * g));
                }
            t.Rect(0, 0, S - 1, 6, Tex.RGB(146, 112, 68));
            t.Rect(0, S - 7, S - 1, S - 1, Tex.RGB(58, 40, 24));
            t.Grain(r, 0.05f);
            Wall[TW_TABLE] = t;
        }

        // ------------------------------------------------------------------ floors
        static Tex MakeWoodFloor(Rng r, int cr, int cg, int cb, bool rough)
        {
            Tex t = new Tex(S, S);
            int ph = 16;
            for (int row = 0; row < S / ph; row++)
            {
                float v = 0.82f + 0.3f * (float)r.Next();
                int baseC = Tex.RGB((int)(cr * v), (int)(cg * v), (int)(cb * v));
                for (int y = 0; y < ph; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float g = FBM(x / 26f, (y + row * 9) / 3.5f, 140 + row, 3);
                        int c = Tex.Shade(baseC, 0.8f + 0.4f * g);
                        if (y == 0) c = Tex.Shade(baseC, 0.42f);
                        if (rough && r.Next() < 0.01) c = Tex.Shade(c, 0.6f);
                        t.Set(x, row * ph + y, c);
                    }
                // plank ends
                for (int k = 0; k < 2; k++)
                {
                    int ex = (row * 53 + k * 71) % S;
                    for (int y = 0; y < ph; y++) t.Set(ex, row * ph + y, Tex.Shade(baseC, 0.5f));
                }
            }
            if (!rough)
            {
                // sheen
                for (int y = 0; y < S; y++)
                    for (int x = 0; x < S; x++)
                    {
                        float s = 0.06f * (float)Math.Sin((x + y) * 0.08);
                        t.Px[y * S + x] = Tex.Shade(t.Px[y * S + x], 1f + s);
                    }
            }
            t.Grain(r, 0.05f);
            return t;
        }

        static Tex MakeRug(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(76, 28, 30));
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float g = FBM(x / 5f, y / 5f, 155, 2);
                    t.Set(x, y, Tex.Shade(Tex.RGB(86, 30, 32), 0.8f + 0.35f * g));
                }
            // border
            t.Rect(4, 4, S - 5, 5, Tex.RGB(150, 124, 70));
            t.Rect(4, S - 6, S - 5, S - 5, Tex.RGB(150, 124, 70));
            t.Rect(4, 4, 5, S - 5, Tex.RGB(150, 124, 70));
            t.Rect(S - 6, 4, S - 5, S - 5, Tex.RGB(150, 124, 70));
            // medallion
            t.Ellipse(64, 64, 30, 26, Tex.RGB(112, 88, 52));
            t.Ellipse(64, 64, 26, 22, Tex.RGB(66, 24, 28));
            t.Ellipse(64, 64, 16, 13, Tex.RGB(120, 96, 56));
            t.Ellipse(64, 64, 12, 9, Tex.RGB(78, 30, 32));
            t.Grain(r, 0.06f);
            return t;
        }

        static Tex MakeTiles(Rng r)
        {
            Tex t = new Tex(S, S);
            t.Fill(Tex.RGB(60, 62, 60));
            for (int y = 0; y < S; y += 32)
                for (int x = 0; x < S; x += 32)
                {
                    float v = 0.86f + 0.2f * (float)r.Next();
                    for (int yy = 0; yy < 32; yy++)
                        for (int xx = 0; xx < 32; xx++)
                        {
                            float g = FBM((x + xx) / 6f, (y + yy) / 6f, 166, 2);
                            t.Set(x + xx, y + yy, Tex.Shade(Tex.RGB(146, 146, 140), v * (0.9f + 0.15f * g)));
                        }
                    for (int k = 0; k < 32; k++) { t.Set(x + k, y, Tex.RGB(84, 84, 80)); t.Set(x, y + k, Tex.RGB(84, 84, 80)); }
                }
            t.Grain(r, 0.04f);
            return t;
        }

        static Tex MakeConcrete(Rng r)
        {
            Tex t = new Tex(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float g = FBM(x / 14f, y / 14f, 177, 3);
                    t.Set(x, y, Tex.Shade(Tex.RGB(84, 82, 78), 0.72f + 0.5f * g));
                }
            for (int i = 0; i < 8; i++)
            {
                int cx = (int)r.Range(0, S), cy = (int)r.Range(0, S);
                t.Ellipse(cx, cy, r.Range(3, 12), r.Range(3, 10), Tex.Shade(Tex.RGB(50, 48, 46), 0.8f));
            }
            t.Grain(r, 0.08f);
            return t;
        }

        static Tex MakeCeil(Rng r, bool attic)
        {
            Tex t = new Tex(S, S);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float g = FBM(x / 18f, y / 18f, attic ? 190 : 188, 3);
                    int c = attic ? Tex.RGB(64, 54, 42) : Tex.RGB(112, 104, 92);
                    t.Set(x, y, Tex.Shade(c, 0.75f + 0.35f * g));
                }
            if (attic)
            {
                for (int y = 0; y < S; y += 32)
                    for (int yy = 0; yy < 6; yy++)
                        for (int x = 0; x < S; x++) t.Set(x, y + yy, Tex.RGB(46, 38, 28));
            }
            else
            {
                // water stains
                for (int i = 0; i < 5; i++)
                {
                    int cx = (int)r.Range(0, S), cy = (int)r.Range(0, S);
                    t.Ellipse(cx, cy, r.Range(6, 18), r.Range(6, 16), Tex.RGB(96, 88, 76));
                }
            }
            t.Grain(r, 0.05f);
            return t;
        }
    }
}
