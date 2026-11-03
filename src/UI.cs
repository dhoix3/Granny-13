// UI.cs - HUD, menus and screens (GDI+ over the software-rendered frame)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace Granny13
{
    public static class UI
    {
        static Font F_Title, F_Big, F_Med, F_Small, F_Tiny, F_Mono, F_Micro, F_Story;
        static bool ready;
        static Dictionary<Sprite, Bitmap> cache = new Dictionary<Sprite, Bitmap>();
        static Random rnd = new Random(7);
        static double flick;

        static Font Make(string family, float size, FontStyle style)
        {
            try { return new Font(family, size, style, GraphicsUnit.Pixel); }
            catch { }
            try { return new Font("Arial", size, style, GraphicsUnit.Pixel); }
            catch { return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel); }
        }

        public static void Ensure()
        {
            if (ready) return;
            F_Title = Make("Arial Black", 92, FontStyle.Bold);
            F_Big = Make("Arial Black", 42, FontStyle.Bold);
            F_Med = Make("Georgia", 26, FontStyle.Bold);
            F_Small = Make("Segoe UI", 20, FontStyle.Regular);
            F_Tiny = Make("Segoe UI", 15, FontStyle.Regular);
            F_Micro = Make("Segoe UI", 12, FontStyle.Regular);
            F_Mono = Make("Consolas", 18, FontStyle.Bold);
            F_Story = Make("Georgia", 23, FontStyle.Italic);
            ready = true;
        }

        public static Bitmap SprBitmap(Sprite s)
        {
            if (s == null) return null;
            Bitmap b;
            if (cache.TryGetValue(s, out b)) return b;
            b = new Bitmap(s.W, s.H, PixelFormat.Format32bppArgb);
            BitmapData bd = b.LockBits(new Rectangle(0, 0, s.W, s.H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            for (int y = 0; y < s.H; y++)
                Marshal.Copy(s.Px, y * s.W, new IntPtr(bd.Scan0.ToInt64() + (long)y * bd.Stride), s.W);
            b.UnlockBits(bd);
            cache[s] = b;
            return b;
        }

        // ------------------------------------------------------------------ text
        public static void Txt(Graphics g, string s, Font f, Color c, float x, float y)
        {
            using (SolidBrush b = new SolidBrush(c)) g.DrawString(s, f, b, x, y);
        }

        public static void TxtC(Graphics g, string s, Font f, Color c, float cx, float y)
        {
            SizeF sz = g.MeasureString(s, f);
            Txt(g, s, f, c, cx - sz.Width / 2, y);
        }

        public static void Shadow(Graphics g, string s, Font f, Color c, float x, float y, float off, Color sc)
        {
            using (SolidBrush b = new SolidBrush(sc)) g.DrawString(s, f, b, x + off, y + off);
            using (SolidBrush b = new SolidBrush(c)) g.DrawString(s, f, b, x, y);
        }

        public static void ShadowC(Graphics g, string s, Font f, Color c, float cx, float y, float off, Color sc)
        {
            SizeF sz = g.MeasureString(s, f);
            Shadow(g, s, f, c, cx - sz.Width / 2, y, off, sc);
        }

        static void Panel(Graphics g, float x, float y, float w, float h, int alpha, Color border)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, 8, 8, 12)))
                g.FillRectangle(b, x, y, w, h);
            using (Pen p = new Pen(Color.FromArgb(160, border), 1.6f))
                g.DrawRectangle(p, x, y, w, h);
        }

        static Color Amber = Color.FromArgb(255, 216, 168, 72);
        static Color Blood = Color.FromArgb(255, 186, 38, 34);
        static Color Bone = Color.FromArgb(255, 226, 219, 204);
        static Color Dim = Color.FromArgb(255, 128, 122, 112);

        // ------------------------------------------------------------------ main
        public static void Draw(Graphics g, Game game, int W, int H)
        {
            Ensure();
            flick = 0.86 + 0.14 * Math.Sin(game.Time * 31.0) * Math.Sin(game.Time * 2.7);
            switch (game.Screen)
            {
                case GScreen.Title: DrawTitle(g, game, W, H); break;
                case GScreen.Diff: DrawDiff(g, game, W, H); break;
                case GScreen.Help: DrawHelp(g, game, W, H); break;
                case GScreen.Intro: DrawIntro(g, game, W, H); break;
                case GScreen.Play: DrawHud(g, game, W, H); break;
                case GScreen.Pause: DrawHud(g, game, W, H); DrawPause(g, game, W, H); break;
                case GScreen.Caught: DrawCaught(g, game, W, H); break;
                case GScreen.Win: DrawWin(g, game, W, H); break;
                case GScreen.Over: DrawOver(g, game, W, H); break;
            }
            if (game.FadeT > 0.01)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(game.FadeT * 255), 0, 0, 0)))
                    g.FillRectangle(b, 0, 0, W, H);
            }
        }

        // ------------------------------------------------------------------ title
        static void DrawTitle(Graphics g, Game game, int W, int H)
        {
            using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(210, 4, 4, 8), Color.FromArgb(30, 4, 4, 8), 0f))
            {
                bg.WrapMode = WrapMode.TileFlipX;
                ColorBlend cb = new ColorBlend();
                cb.Colors = new Color[] { Color.FromArgb(225, 3, 3, 6), Color.FromArgb(150, 6, 6, 12), Color.FromArgb(20, 0, 0, 0) };
                cb.Positions = new float[] { 0f, 0.45f, 1f };
                bg.InterpolationColors = cb;
                g.FillRectangle(bg, 0, 0, W, H);
            }
            // title
            float ty = 96;
            string t1 = "ГРЕННИ";
            string t2 = "13";
            using (Font ft = new Font(F_Title.FontFamily, 104, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                SizeF s1 = g.MeasureString(t1, ft);
                Shadow(g, t1, ft, Color.FromArgb(255, 214, 210, 198), 96, ty, 3, Color.FromArgb(200, 0, 0, 0));
                using (Font ft2 = new Font(F_Title.FontFamily, 150, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    SizeF s2 = g.MeasureString(t2, ft2);
                    Shadow(g, t2, ft2, Color.FromArgb((int)(255 * flick), 190, 30, 28), 96 + s1.Width + 18, ty - 44, 4, Color.FromArgb(200, 0, 0, 0));
                }
            }
            Txt(g, "ПОБЕГ ОТ БАБУШКИ", F_Med, Color.FromArgb(230, 176, 168, 150), 100, 214);
            using (Pen p = new Pen(Color.FromArgb(120, 186, 38, 34), 2))
                g.DrawLine(p, 100, 252, 520, 252);
            Txt(g, "пародия на Granny · 2 этажа · 4 сложности", F_Tiny, Color.FromArgb(150, 160, 152, 140), 100, 262);

            // menu
            float my = 330;
            for (int i = 0; i < game.MenuItems.Length; i++)
            {
                bool sel = i == game.MenuIdx;
                float x = 104;
                if (sel)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 150, 30, 26)))
                        g.FillRectangle(b, x - 22, my - 6, 380, 46);
                    using (Pen p = new Pen(Color.FromArgb(200, 186, 38, 34), 2))
                        g.DrawRectangle(p, x - 22, my - 6, 380, 46);
                    Txt(g, "►", F_Med, Blood, x - 12, my + 2);
                }
                Txt(g, game.MenuItems[i], sel ? F_Big : F_Med,
                    sel ? Color.FromArgb(255, 246, 236, 220) : Color.FromArgb(190, 170, 162, 148), x + 8, my - (sel ? 6 : 0));
                my += 62;
            }

            // current difficulty
            Panel(g, 104, 600, 420, 52, 110, Color.FromArgb(120, 120, 110, 96));
            Txt(g, "СЛОЖНОСТЬ:", F_Micro, Dim, 116, 610);
            Txt(g, Difficulty.All[game.DiffIdx].Name, F_Mono, Amber, 232, 606);

            TxtC(g, "↑ ↓ — выбор    Enter — подтвердить    F11 — полный экран", F_Tiny,
                Color.FromArgb(150, 150, 142, 128), W / 2f + 120, H - 46);

            // "photo" of granny holds the right side, already in the 3d scene
            DrawFog(g, game, W, H, 0.25);
            if (game.MenuIdx == 0 && (int)(game.Time * 1.6) % 2 == 0)
            {
                // breathing red tint behind title
            }
        }

        static void DrawFog(Graphics g, Game game, int W, int H, double amount)
        {
            for (int i = 0; i < 7; i++)
            {
                double t = game.Time * 0.05 + i * 1.7;
                float x = (float)((Math.Sin(t) * 0.5 + 0.5) * W);
                float y = (float)((Math.Cos(t * 0.7 + i) * 0.5 + 0.5) * H);
                float r = 180 + i * 40;
                using (GraphicsPath p = new GraphicsPath())
                {
                    p.AddEllipse(x - r, y - r * 0.5f, r * 2, r);
                    using (PathGradientBrush pb = new PathGradientBrush(p))
                    {
                        pb.CenterColor = Color.FromArgb((int)(22 * amount), 120, 120, 140);
                        pb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                        g.FillPath(pb, p);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ difficulty
        static void DrawDiff(Graphics g, Game game, int W, int H)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(225, 5, 5, 9))) g.FillRectangle(b, 0, 0, W, H);
            ShadowC(g, "ВЫБЕРИ СЛОЖНОСТЬ", F_Big, Color.FromArgb(240, 232, 222, 206), W / 2, 58, 3, Color.Black);
            TxtC(g, "чем сложнее — тем быстрее бабушка и тем меньше у тебя дней", F_Tiny, Dim, W / 2, 118);

            int n = 4;
            float cw = 268, gap = 22;
            float total = n * cw + (n - 1) * gap;
            float x0 = (W - total) / 2;
            for (int i = 0; i < n; i++)
            {
                Difficulty d = Difficulty.All[i];
                float x = x0 + i * (cw + gap);
                float y = 170;
                bool sel = i == game.DiffIdxMenu;
                bool cur = i == game.DiffIdx;
                using (SolidBrush b = new SolidBrush(sel ? Color.FromArgb(235, 22, 12, 14) : Color.FromArgb(210, 12, 12, 16)))
                    g.FillRectangle(b, x, y, cw, 430);
                using (Pen p = new Pen(sel ? Blood : Color.FromArgb(90, 110, 104, 96), sel ? 3f : 1.4f))
                    g.DrawRectangle(p, x, y, cw, 430);
                if (cur)
                {
                    Txt(g, "ТЕКУЩАЯ", F_Micro, Amber, x + 14, y + 10);
                }
                float cy = y + 40;
                TxtC(g, d.Name, i == 3 ? F_Med : F_Med, sel ? Bone : Color.FromArgb(210, 196, 188, 172), x + cw / 2, cy);
                cy += 52;
                Bar2(g, x + 24, cy, cw - 48, "СКОРОСТЬ", (float)((d.Run - 1.6) / 1.6), Blood);
                cy += 46;
                Bar2(g, x + 24, cy, cw - 48, "СЛУХ", (float)((d.HearMul - 0.6) / 1.1), Amber);
                cy += 46;
                Bar2(g, x + 24, cy, cw - 48, "ЗРЕНИЕ", (float)((d.Sight - 6) / 10), Color.FromArgb(120, 150, 210));
                cy += 46;
                Bar2(g, x + 24, cy, cw - 48, "КАПКАНЫ", d.Traps / 8f, Color.FromArgb(170, 110, 60));
                cy += 46;
                Txt(g, "ДНЕЙ: " + d.Days, F_Mono, Bone, x + 24, cy);
                cy += 44;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(200, 176, 168, 154)))
                using (Font f = new Font("Segoe UI", 15, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    RectangleF rf = new RectangleF(x + 20, cy, cw - 40, 160);
                    g.DrawString(d.Desc, f, b, rf);
                }
                if (sel)
                {
                    using (Pen p = new Pen(Color.FromArgb(255, 186, 38, 34), 2))
                        g.DrawRectangle(p, x - 4, y - 4, cw + 8, 438);
                }
            }
            TxtC(g, "← → выбрать      Enter — играть      Esc — назад", F_Tiny, Color.FromArgb(170, 160, 152, 140), W / 2, H - 56);
            // navigation with left/right too
        }

        static void Bar2(Graphics g, float x, float y, float w, string label, float v, Color c)
        {
            Txt(g, label, F_Micro, Color.FromArgb(170, 160, 152, 140), x, y - 4);
            v = M.Clamp01(v);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(120, 30, 30, 34)))
                g.FillRectangle(b, x, y + 14, w, 9);
            using (SolidBrush b = new SolidBrush(c))
                g.FillRectangle(b, x, y + 14, w * v, 9);
            for (int i = 1; i < 10; i++)
                using (Pen p = new Pen(Color.FromArgb(90, 0, 0, 0), 1))
                    g.DrawLine(p, x + w * i / 10f, y + 14, x + w * i / 10f, y + 23);
        }

        // ------------------------------------------------------------------ help
        static void DrawHelp(Graphics g, Game game, int W, int H)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(240, 4, 4, 8))) g.FillRectangle(b, 0, 0, W, H);
            ShadowC(g, "КАК ИГРАТЬ", F_Big, Bone, W / 2, 60, 3, Color.Black);
            string[] keys = {
                "W A S D / стрелки — ходить",
                "Мышь — осматриваться,  Q / R — поворот",
                "SHIFT — бежать (тратит выносливость и ОЧЕНЬ шумит)",
                "CTRL / C — красться (тихо, но медленно)",
                "E — открыть дверь, обыскать шкаф, спрятаться, лезть на чердак",
                "F — фонарик (свет видит Гренни), M — карта",
                "ПРОБЕЛ — вырваться из капкана",
                "ESC — пауза, F11 — полный экран"
            };
            float y = 170;
            for (int i = 0; i < keys.Length; i++)
            {
                Txt(g, "•", F_Med, Blood, 150, y - 4);
                Txt(g, keys[i], F_Small, Color.FromArgb(235, 214, 208, 196), 180, y);
                y += 44;
            }
            y += 16;
            Panel(g, 130, y, W - 260, 168, 120, Color.FromArgb(120, 150, 40, 36));
            Txt(g, "ЦЕЛЬ: открыть три замка на входной двери", F_Med, Amber, 156, y + 18);
            Txt(g, "1. Висячий замок   — ключ от замка", F_Small, Bone, 160, y + 62);
            Txt(g, "2. Ржавый засов    — монтировка", F_Small, Bone, 160, y + 92);
            Txt(g, "3. Кодовый замок   — записка с кодом (она на чердаке)", F_Small, Bone, 160, y + 122);
            TxtC(g, "Enter / Esc — назад", F_Tiny, Dim, W / 2, H - 54);
        }

        // ------------------------------------------------------------------ intro
        static void DrawIntro(Graphics g, Game game, int W, int H)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(245, 3, 3, 6))) g.FillRectangle(b, 0, 0, W, H);
            DrawFog(g, game, W, H, 0.35);
            ShadowC(g, "ДЕНЬ 1", F_Big, Color.FromArgb(240, 200, 40, 34), W / 2, 84, 3, Color.Black);
            string s1 = "Ты приехал к бабушке на выходные. Остался ночевать.";
            string s2 = "Утром дверь оказалась заперта снаружи, а на кухне кто-то точил нож.";
            string s3 = "У тебя " + Difficulty.All[game.DiffIdx].Days + " дней, чтобы найти ключи, открыть дверь и унести ноги.";
            float y = 200;
            TxtC(g, s1, F_Story, Color.FromArgb(235, 218, 210, 196), W / 2, y);
            TxtC(g, s2, F_Story, Color.FromArgb(235, 218, 210, 196), W / 2, y + 52);
            TxtC(g, s3, F_Story, Color.FromArgb(240, 230, 150, 60), W / 2, y + 118);
            TxtC(g, "На чердаке живёт её кошка Маруся. Она очень громкая.", F_Small,
                Color.FromArgb(200, 170, 164, 152), W / 2, y + 190);

            Panel(g, W / 2 - 300, 470, 600, 130, 140, Color.FromArgb(120, 140, 36, 32));
            TxtC(g, "НЕ БЕГИ БЕЗ ПРИЧИНЫ.", F_Med, Blood, W / 2, 486);
            TxtC(g, "Она слышит каждый шаг.", F_Small, Bone, W / 2, 526);
            TxtC(g, "Она ставит капканы.", F_Small, Bone, W / 2, 554);

            TxtC(g, "Нажми Enter, чтобы начать", F_Small, Color.FromArgb((int)(200 + 55 * Math.Sin(game.Time * 3)), 230, 200, 120), W / 2, 650);
        }

        // ------------------------------------------------------------------ hud
        static void DrawHud(Graphics g, Game game, int W, int H)
        {
            Player p = game.P;
            // crosshair
            using (Pen pen = new Pen(Color.FromArgb(110, 255, 255, 255), 1.4f))
            {
                g.DrawEllipse(pen, W / 2f - 3, H / 2f - 3, 6, 6);
            }

            // ---- top left: locks
            Panel(g, 24, 20, 300, 66, 130, Color.FromArgb(90, 96, 88, 80));
            Txt(g, "ЗАМКИ", F_Micro, Dim, 38, 28);
            for (int i = 0; i < 3; i++)
            {
                int icon = 7;
                Sprite s = (TexLib.Icons != null && TexLib.Icons.Length > icon) ? TexLib.Icons[icon] : null;
                Bitmap bm = SprBitmap(s);
                float x = 40 + i * 58, y = 42;
                if (bm != null)
                {
                    float sc = 36f / bm.Height;
                    using (ImageAttributes ia = new ImageAttributes())
                    {
                        ColorMatrix cm;
                        if (game.Locks[i])
                            cm = new ColorMatrix(new float[][] {
                                new float[]{0.45f,0,0,0,0}, new float[]{0,1.35f,0.35f,0,0}, new float[]{0,0.35f,0.45f,0,0},
                                new float[]{0,0,0,1,0}, new float[]{0,0,0,0,1} });
                        else
                            cm = new ColorMatrix(new float[][] {
                                new float[]{0.45f,0,0,0,0}, new float[]{0,0.45f,0,0,0}, new float[]{0,0,0.45f,0,0},
                                new float[]{0,0,0,0.75f,0}, new float[]{0,0,0,0,1} });
                        ia.SetColorMatrix(cm);
                        g.DrawImage(bm, new Rectangle((int)x, (int)y, (int)(bm.Width * sc), 36), 0, 0, bm.Width, bm.Height, GraphicsUnit.Pixel, ia);
                    }
                }
                using (Font fnum = new Font("Consolas", 13, FontStyle.Bold, GraphicsUnit.Pixel))
                    Txt(g, (i + 1).ToString(), fnum, game.Locks[i] ? Color.FromArgb(150, 220, 130) : Color.FromArgb(120, 118, 112, 106), x + 2, y + 38);
            }

            // ---- top right: day
            Panel(g, W - 264, 20, 240, 66, 130, Color.FromArgb(90, 96, 88, 80));
            Txt(g, "ДЕНЬ", F_Micro, Dim, W - 250, 28);
            using (Font f = new Font("Consolas", 30, FontStyle.Bold, GraphicsUnit.Pixel))
                Txt(g, game.Day + " / " + game.MaxDays, f, game.Day >= game.MaxDays ? Blood : Amber, W - 250, 46);
            string lvlName = game.P.Level == 1 ? "ЧЕРДАК" : "ДОМ";
            Txt(g, lvlName, F_Micro, Dim, W - 96, 28);

            // ---- inventory bottom left
            int invN = game.Inv.Count;
            float panelW = 100 + Math.Max(1, invN) * 66;
            Panel(g, 24, H - 118, panelW, 94, 130, Color.FromArgb(80, 86, 78, 70));
            Txt(g, "КАРМАНЫ", F_Micro, Dim, 36, H - 112);
            if (invN == 0) Txt(g, "пусто", F_Tiny, Color.FromArgb(120, 150, 144, 132), 40, H - 78);
            for (int i = 0; i < invN; i++)
            {
                int id = game.Inv[i];
                Sprite s = TexLib.Icons != null ? TexLib.Icons[Items.Icon(id)] : null;
                Bitmap bm = SprBitmap(s);
                float x = 34 + i * 66, y = H - 84;
                if (bm != null)
                {
                    float sc = 44f / bm.Height;
                    g.DrawImage(bm, x, y, bm.Width * sc, 44);
                }
                using (Font fl = new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel))
                    Txt(g, Items.Short(id), fl, Color.FromArgb(200, 205, 198, 184), x - 1, y + 46);
            }

            // ---- stamina + battery bottom right
            float bx = W - 250, by = H - 96;
            Txt(g, "ВЫНОСЛИВОСТЬ", F_Micro, Dim, bx, by - 6);
            Bar3(g, bx, by + 10, 210, (float)p.Stamina, Color.FromArgb(200, 200, 180, 80));
            Txt(g, "ФОНАРИК", F_Micro, Dim, bx, by + 34);
            Bar3(g, bx, by + 50, 210, (float)p.Battery, p.Battery < 0.2 ? Blood : Color.FromArgb(200, 200, 210, 120));

            // ---- prompt
            if (!string.IsNullOrEmpty(game.Prompt))
            {
                float pw = 520, px = W / 2f - pw / 2, py = H - 190;
                Panel(g, px, py, pw, 46, 150, Color.FromArgb(120, 150, 40, 36));
                TxtC(g, game.Prompt, F_Small, Bone, W / 2f, py + 10);
                Txt(g, "[E]", F_Mono, Amber, px + 12, py + 12);
            }
            if (game.InteractProg > 0.001)
            {
                float pw = 420, px = W / 2f - pw / 2, py = H - 236;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(160, 20, 20, 24)))
                    g.FillRectangle(b, px, py, pw, 22);
                using (SolidBrush b = new SolidBrush(Amber))
                    g.FillRectangle(b, px + 2, py + 2, (pw - 4) * (float)M.Clamp01(game.InteractProg), 18);
                TxtC(g, game.InteractLabel, F_Tiny, Color.Black, W / 2f, py + 1);
            }

            // ---- subtitles
            float sy = H - 258;
            for (int i = game.Subs.Count - 1; i >= 0; i--)
            {
                Msg m = game.Subs[i];
                float a = (float)M.Clamp01(Math.Min(m.T * 6, (m.Dur - m.T) * 3));
                Color c = m.Granny ? Color.FromArgb((int)(240 * a), 226, 70, 60) : Color.FromArgb((int)(230 * a), 224, 216, 200);
                ShadowC(g, m.Text, m.Granny ? F_Med : F_Small, c, W / 2f, sy, 2, Color.FromArgb((int)(200 * a), 0, 0, 0));
                sy -= m.Granny ? 46 : 34;
                if (i < game.Subs.Count - 3) break;
            }

            // ---- toast
            if (game.ToastT > 0)
            {
                float a = (float)M.Clamp01(game.ToastT * 2);
                float tw = g.MeasureString(game.Toast, F_Small).Width + 48;
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(170 * a), 10, 10, 14)))
                    g.FillRectangle(b, W / 2f - tw / 2, 100, tw, 40);
                using (Pen pen2 = new Pen(Color.FromArgb((int)(160 * a), 186, 38, 34), 1.4f))
                    g.DrawRectangle(pen2, W / 2f - tw / 2, 100, tw, 40);
                TxtC(g, game.Toast, F_Small, Color.FromArgb((int)(240 * a), 236, 228, 210), W / 2f, 109);
            }

            // ---- minimap
            if (game.Minimap) DrawMinimap(g, game, W, H);

            // ---- red edge when granny near
            float prox = (float)game.Cam.GrannyProximity;
            if (game.P.Level == game.G.Level && prox > 0.15f)
            {
                using (Pen pen3 = new Pen(Color.FromArgb((int)(140 * prox), 170, 20, 20), 8f))
                    g.DrawRectangle(pen3, 4, 4, W - 8, H - 8);
            }
            if (game.P.Hidden)
            {
                TxtC(g, "СИДИ ТИХО", F_Med, Color.FromArgb(220, 210, 200, 180), W / 2f, 130);
                TxtC(g, "[E] — выйти", F_Tiny, Dim, W / 2f, 168);
            }
            if (game.P.TrapStuck > 0)
            {
                TxtC(g, "ЖМИ ПРОБЕЛ!", F_Big, Color.FromArgb((int)(180 + 70 * Math.Sin(game.Time * 14)), 200, 30, 24), W / 2f, H / 2f + 90);
                TxtC(g, "капкан на ноге", F_Small, Bone, W / 2f, H / 2f + 140);
            }
        }

        static void Bar3(Graphics g, float x, float y, float w, float v, Color c)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(140, 26, 26, 30)))
                g.FillRectangle(b, x, y, w, 12);
            using (SolidBrush b = new SolidBrush(c))
                g.FillRectangle(b, x, y, w * M.Clamp01(v), 12);
            using (Pen p = new Pen(Color.FromArgb(90, 120, 116, 108), 1))
                g.DrawRectangle(p, x, y, w, 12);
        }

        static Bitmap mapBmp;
        static int mapKey = -1;

        static void BuildMapBitmap(Level lv)
        {
            int cell = 4;
            if (mapBmp != null) mapBmp.Dispose();
            mapBmp = new Bitmap(lv.W * cell, lv.H * cell, PixelFormat.Format32bppArgb);
            using (Graphics mg = Graphics.FromImage(mapBmp))
            {
                mg.Clear(Color.FromArgb(150, 6, 6, 9));
                for (int y = 0; y < lv.H; y++)
                    for (int x = 0; x < lv.W; x++)
                    {
                        byte t = lv.Get(x, y);
                        Color c;
                        if (t == T.DOOR) c = Color.FromArgb(170, 60, 120, 60);
                        else if (t == T.FRONT) c = Color.FromArgb(220, 190, 60, 40);
                        else if (t == T.HOLE) c = Color.FromArgb(200, 120, 90, 40);
                        else if (T.Solid[t]) c = Color.FromArgb(160, 96, 92, 84);
                        else if (t == T.RUG) c = Color.FromArgb(95, 110, 40, 40);
                        else c = Color.FromArgb(85, 24, 24, 30);
                        using (SolidBrush b = new SolidBrush(c))
                            mg.FillRectangle(b, x * cell, y * cell, cell - 1, cell - 1);
                    }
            }
        }

        static void DrawMinimap(Graphics g, Game game, int W, int H)
        {
            Level lv = game.CurrentLevel();
            float cell = 4f;
            float mw = lv.W * cell, mh = lv.H * cell;
            float ox = W - mw - 26, oy = 100;
            if (mapBmp == null || mapKey != lv.Id)
            {
                BuildMapBitmap(lv);
                mapKey = lv.Id;
            }
            using (SolidBrush b = new SolidBrush(Color.FromArgb(150, 6, 6, 9)))
                g.FillRectangle(b, ox - 6, oy - 6, mw + 12, mh + 12);
            using (Pen p = new Pen(Color.FromArgb(100, 120, 112, 96), 1))
                g.DrawRectangle(p, ox - 6, oy - 6, mw + 12, mh + 12);
            g.DrawImageUnscaled(mapBmp, (int)ox, (int)oy);
            // containers
            for (int i = 0; i < lv.Containers.Count; i++)
            {
                Container c = lv.Containers[i];
                if (c.Searched) continue;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(210, 210, 180, 90)))
                    g.FillEllipse(b, ox + c.X * cell + 0.6f, oy + c.Y * cell + 0.6f, 3f, 3f);
            }
            // granny if close
            if (game.G.Level == game.P.Level && M.Dist(game.P.X, game.P.Y, game.G.X, game.G.Y) < 7.5)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(160 + 90 * Math.Sin(game.Time * 8)), 220, 30, 26)))
                    g.FillEllipse(b, ox + (float)game.G.X * cell - 3, oy + (float)game.G.Y * cell - 3, 7, 7);
            }
            // player arrow
            float px = ox + (float)game.P.X * cell, py = oy + (float)game.P.Y * cell;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 255, 216, 90)))
            {
                double a = game.P.Angle;
                PointF[] tri = new PointF[] {
                    new PointF(px + (float)Math.Cos(a) * 7, py + (float)Math.Sin(a) * 7),
                    new PointF(px + (float)Math.Cos(a + 2.5) * 5, py + (float)Math.Sin(a + 2.5) * 5),
                    new PointF(px + (float)Math.Cos(a - 2.5) * 5, py + (float)Math.Sin(a - 2.5) * 5)
                };
                g.FillPolygon(b, tri);
            }
        }

        // ------------------------------------------------------------------ pause
        static void DrawPause(Graphics g, Game game, int W, int H)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(190, 3, 3, 6))) g.FillRectangle(b, 0, 0, W, H);
            ShadowC(g, "ПАУЗА", F_Big, Bone, W / 2, 170, 3, Color.Black);
            float y = 290;
            for (int i = 0; i < game.PauseItems.Length; i++)
            {
                bool sel = i == game.PauseIdx;
                if (sel)
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(90, 150, 30, 26)))
                        g.FillRectangle(b, W / 2f - 240, y - 8, 480, 52);
                    using (Pen p = new Pen(Blood, 2))
                        g.DrawRectangle(p, W / 2f - 240, y - 8, 480, 52);
                }
                TxtC(g, game.PauseItems[i], sel ? F_Big : F_Med, sel ? Bone : Color.FromArgb(180, 170, 162, 150), W / 2f, y);
                y += 70;
            }
            TxtC(g, "День " + game.Day + " из " + game.MaxDays + "   ·   замки: " + game.CountLocks() + "/3", F_Tiny, Dim, W / 2, H - 90);
        }

        // ------------------------------------------------------------------ caught
        static void DrawCaught(Graphics g, Game game, int W, int H)
        {
            double t = game.CaughtT;
            using (SolidBrush b0 = new SolidBrush(Color.FromArgb(255, 10, 2, 3))) g.FillRectangle(b0, 0, 0, W, H);

            Bitmap face = SprBitmap(Assets.GrannyFace);
            if (face != null)
            {
                float sc = 1.32f + (float)t * 0.30f;
                float fh = H * sc, fw = fh * face.Width / face.Height;
                float jx = (float)(Math.Sin(t * 47) * 16 * Math.Max(0, 1 - t * 0.7));
                float jy = (float)(Math.Cos(t * 39) * 13 * Math.Max(0, 1 - t * 0.7));
                float fx = W / 2f - fw / 2 + jx;
                float fy = H * 0.46f - fh * 0.30f + jy;
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ColorMatrix cm = new ColorMatrix(new float[][] {
                        new float[]{1.10f,0,0,0,0.04f}, new float[]{0,0.50f,0,0,0}, new float[]{0,0,0.46f,0,0},
                        new float[]{0,0,0,1,0}, new float[]{0,0,0,0,1} });
                    ia.SetColorMatrix(cm);
                    g.DrawImage(face, new Rectangle((int)fx, (int)fy, (int)fw, (int)fh), 0, 0, face.Width, face.Height, GraphicsUnit.Pixel, ia);
                }
            }
            // red edge vignette
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddRectangle(new Rectangle(-W / 3, -H / 3, W * 5 / 3, H * 5 / 3));
                using (PathGradientBrush pb = new PathGradientBrush(path))
                {
                    pb.CenterColor = Color.FromArgb(0, 0, 0, 0);
                    pb.SurroundColors = new Color[] { Color.FromArgb(248, 12, 0, 0) };
                    pb.FocusScales = new PointF(0.52f, 0.5f);
                    g.FillPath(pb, path);
                }
            }
            // flash
            float flash = (float)Math.Max(0, 1.0 - t * 3.0);
            if (flash > 0.01f)
                using (SolidBrush b = new SolidBrush(Color.FromArgb((int)(170 * flash), 170, 14, 10)))
                    g.FillRectangle(b, 0, 0, W, H);

            // dark band so the text stays readable over the face
            int bands = 72;
            for (int bi = 0; bi < bands; bi++)
            {
                float bt = (float)bi / (bands - 1);
                int alpha = (int)(232 * bt * bt);
                int y0 = H - 260 + bi * 260 / bands;
                using (SolidBrush bb = new SolidBrush(Color.FromArgb(alpha, 8, 0, 0)))
                    g.FillRectangle(bb, 0, y0, W, 260 / bands + 2);
            }

            ShadowC(g, "ОНА ТЕБЯ ПОЙМАЛА", F_Big, Color.FromArgb(255, 240, 232, 220), W / 2, H - 190, 4, Color.Black);
            TxtC(g, "День " + game.Day + " закончился", F_Med, Color.FromArgb(235, 214, 66, 54), W / 2, H - 140);
            if (game.Day < game.MaxDays)
                TxtC(g, "Осталось дней: " + Math.Max(0, game.MaxDays - game.Day), F_Small, Color.FromArgb(210, 190, 182, 168), W / 2, H - 98);
        }

        // ------------------------------------------------------------------ win
        static void DrawWin(Graphics g, Game game, int W, int H)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(238, 6, 8, 10))) g.FillRectangle(b, 0, 0, W, H);
            using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(120, 20, 60, 30), Color.FromArgb(0, 0, 0, 0), 90f))
                g.FillRectangle(bg, 0, 0, W, H);
            ShadowC(g, "ТЫ СБЕЖАЛ!", F_Big, Bone, W / 2, 110, 4, Color.Black);
            TxtC(g, "Дверь хлопнула. Морозный воздух. Свобода.", F_Story, Color.FromArgb(235, 220, 214, 200), W / 2, 196);
            Panel(g, W / 2 - 300, 250, 600, 230, 140, Color.FromArgb(120, 120, 160, 120));
            Txt(g, "СЛОЖНОСТЬ", F_Tiny, Dim, W / 2 - 250, 272);
            Txt(g, game.Diff.Name, F_Mono, Amber, W / 2 + 40, 268);
            Txt(g, "ДНЕЙ ПОТРАЧЕНО", F_Tiny, Dim, W / 2 - 250, 312);
            Txt(g, game.Day + " из " + game.MaxDays, F_Mono, Bone, W / 2 + 40, 308);
            Txt(g, "ВРЕМЯ", F_Tiny, Dim, W / 2 - 250, 352);
            Txt(g, ((int)game.PlayTime / 60) + ":" + ((int)game.PlayTime % 60).ToString("00"), F_Mono, Bone, W / 2 + 40, 348);
            Txt(g, "СМЕРТЕЙ", F_Tiny, Dim, W / 2 - 250, 392);
            Txt(g, game.Deaths.ToString(), F_Mono, Bone, W / 2 + 40, 388);
            TxtC(g, "Гренни осталась одна. С кошкой. И с капканами.", F_Small, Color.FromArgb(200, 170, 164, 150), W / 2, 540);
            TxtC(g, "Enter — в главное меню", F_Small, Color.FromArgb((int)(180 + 70 * Math.Sin(game.Time * 3)), 230, 200, 120), W / 2, 630);
        }

        // ------------------------------------------------------------------ over
        static void DrawOver(Graphics g, Game game, int W, int H)
        {
            using (SolidBrush b = new SolidBrush(Color.FromArgb(235, 8, 2, 4))) g.FillRectangle(b, 0, 0, W, H);
            Bitmap face = SprBitmap(Assets.GrannyFace);
            if (face != null)
            {
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ColorMatrix cm = new ColorMatrix(new float[][] {
                        new float[]{0.5f,0,0,0,0.02f}, new float[]{0,0.2f,0,0,0}, new float[]{0,0,0.18f,0,0},
                        new float[]{0,0,0,0.5f,0}, new float[]{0,0,0,0,1} });
                    ia.SetColorMatrix(cm);
                    float fw = W * 0.5f, fh = fw * face.Height / face.Width;
                    g.DrawImage(face, new Rectangle((int)(W * 0.62f - fw / 2), (int)(H * 0.5f - fh / 2), (int)fw, (int)fh),
                        0, 0, face.Width, face.Height, GraphicsUnit.Pixel, ia);
                }
            }
            Shadow(g, "ДНЕЙ БОЛЬШЕ НЕТ", F_Big, Color.FromArgb(255, 200, 36, 30), 90, 200, 4, Color.Black);
            Txt(g, "Она дождалась. Ты остался у неё на ужин.", F_Story, Color.FromArgb(230, 200, 192, 180), 94, 290);
            Txt(g, "Сложность: " + game.Diff.Name, F_Small, Dim, 94, 350);
            Txt(g, "Продержался: " + game.Day + " дн. · " + ((int)game.PlayTime / 60) + ":" + ((int)game.PlayTime % 60).ToString("00"), F_Small, Dim, 94, 382);
            Txt(g, "Enter — в главное меню", F_Small, Color.FromArgb((int)(180 + 70 * Math.Sin(game.Time * 3)), 230, 200, 120), 94, 470);
        }
    }
}


