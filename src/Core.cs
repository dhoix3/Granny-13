// Core.cs - math, framebuffer, embedded assets, sprites
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Granny13
{
    public static class M
    {
        public const double PI = 3.14159265358979323846;
        public const double TAU = 6.28318530717958647692;

        public static double Clamp(double v, double a, double b) { return v < a ? a : (v > b ? b : v); }
        public static float Clamp01(float v) { return v < 0f ? 0f : (v > 1f ? 1f : v); }
        public static double Clamp01(double v) { return v < 0.0 ? 0.0 : (v > 1.0 ? 1.0 : v); }
        public static int ClampI(int v, int a, int b) { return v < a ? a : (v > b ? b : v); }
        public static double Lerp(double a, double b, double t) { return a + (b - a) * t; }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
        public static int LerpI(int a, int b, int t) { return a + (b - a) * t; }
        public static float SmoothStep(float t) { t = Clamp01(t); return t * t * (3f - 2f * t); }
        public static double Dist(double x1, double y1, double x2, double y2) { double dx = x2 - x1, dy = y2 - y1; return Math.Sqrt(dx * dx + dy * dy); }
        public static double AngleNorm(double a) { a = a % TAU; if (a > PI) a -= TAU; if (a < -PI) a += TAU; return a; }
        public static double AngleDiff(double a, double b) { return AngleNorm(a - b); }
        public static double Approach(double cur, double target, double speed) { return cur < target ? Math.Min(cur + speed, target) : Math.Max(cur - speed, target); }
        public static double Frac(double v) { return v - Math.Floor(v); }

        // smooth 0..1 ramp
        public static float Ramp(float v, float a, float b) { if (b <= a) return v >= b ? 1f : 0f; return Clamp01((v - a) / (b - a)); }

        public static int Pack(int col, float mul)
        {
            int r = (int)(((col >> 16) & 0xFF) * mul); if (r > 255) r = 255;
            int g = (int)(((col >> 8) & 0xFF) * mul); if (g > 255) g = 255;
            int b = (int)((col & 0xFF) * mul); if (b > 255) b = 255;
            return (0xFF << 24) | (r << 16) | (g << 8) | b;
        }

        public static int Mix(int c1, int c2, float t)
        {
            int r = (int)(((c1 >> 16) & 0xFF) + ((((c2 >> 16) & 0xFF) - ((c1 >> 16) & 0xFF)) * t));
            int g = (int)(((c1 >> 8) & 0xFF) + ((((c2 >> 8) & 0xFF) - ((c1 >> 8) & 0xFF)) * t));
            int b = (int)((c1 & 0xFF) + (((c2 & 0xFF) - (c1 & 0xFF)) * t));
            if (r < 0) r = 0; if (r > 255) r = 255;
            if (g < 0) g = 0; if (g > 255) g = 255;
            if (b < 0) b = 0; if (b > 255) b = 255;
            return (0xFF << 24) | (r << 16) | (g << 8) | b;
        }

        public static int ScaleLum(int c, float mul, float add)
        {
            int r = (int)(((c >> 16) & 0xFF) * mul + add);
            int g = (int)(((c >> 8) & 0xFF) * mul + add);
            int b = (int)((c & 0xFF) * mul + add);
            if (r < 0) r = 0; if (r > 255) r = 255;
            if (g < 0) g = 0; if (g > 255) g = 255;
            if (b < 0) b = 0; if (b > 255) b = 255;
            return (0xFF << 24) | (r << 16) | (g << 8) | b;
        }
    }

    public class Rng
    {
        uint s;
        public Rng(int seed) { s = (uint)(seed * 2654435761u + 1234567u); if (s == 0) s = 88172645u; }
        public uint NextU() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public double Next() { return (NextU() & 0xFFFFFF) / 16777216.0; }
        public double Range(double a, double b) { return a + (b - a) * Next(); }
        public int Range(int a, int b) { return a + (int)(Next() * (b - a)); }
        public bool Chance(double p) { return Next() < p; }
        public T Pick<T>(System.Collections.Generic.List<T> list) { return list[(int)(Next() * list.Count) % list.Count]; }
        public void Shuffle<T>(System.Collections.Generic.List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = (int)(Next() * (i + 1)); if (j > i) j = i;
                T t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }
    }

    public class Sprite
    {
        public int W, H;
        public int[] Px;   // 0xAARRGGBB
        public float Aspect { get { return (float)W / (float)H; } }

        public Sprite(int w, int h)
        {
            W = w; H = h; Px = new int[w * h];
        }

        public static Sprite FromFile(string path)
        {
            using (Bitmap b = new Bitmap(path))
                return FromBitmap(b);
        }

        public static Sprite FromBitmap(Bitmap src)
        {
            Sprite s = new Sprite(src.Width, src.Height);
            using (Bitmap b = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height));
                }
                BitmapData bd = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < b.Height; y++)
                    {
                        IntPtr row = new IntPtr(bd.Scan0.ToInt64() + (long)y * bd.Stride);
                        Marshal.Copy(row, s.Px, y * s.W, s.W);
                    }
                }
                finally { b.UnlockBits(bd); }
            }
            return s;
        }

        public Sprite Resized(int newH)
        {
            if (newH <= 0) newH = 1;
            int newW = Math.Max(1, (int)Math.Round((double)W * newH / H));
            Sprite o = new Sprite(newW, newH);
            for (int y = 0; y < newH; y++)
            {
                int sy = (int)((long)y * H / newH); if (sy >= H) sy = H - 1;
                for (int x = 0; x < newW; x++)
                {
                    int sx = (int)((long)x * W / newW); if (sx >= W) sx = W - 1;
                    o.Px[y * newW + x] = Px[sy * W + sx];
                }
            }
            return o;
        }

        public Sprite Cut(int x0, int y0, int w, int h)
        {
            Sprite o = new Sprite(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int sx = x0 + x, sy = y0 + y;
                    o.Px[y * w + x] = (sx >= 0 && sy >= 0 && sx < W && sy < H) ? Px[sy * W + sx] : 0;
                }
            return o;
        }

        public Sprite Tinted(int rgb, float amount)
        {
            Sprite o = new Sprite(W, H);
            for (int i = 0; i < Px.Length; i++)
            {
                int c = Px[i];
                int a = (c >> 24) & 0xFF;
                if (a == 0) { o.Px[i] = 0; continue; }
                int r = (c >> 16) & 0xFF, g = (c >> 8) & 0xFF, b = c & 0xFF;
                r = (int)(r * (1 - amount) + ((rgb >> 16) & 0xFF) * amount);
                g = (int)(g * (1 - amount) + ((rgb >> 8) & 0xFF) * amount);
                b = (int)(b * (1 - amount) + (rgb & 0xFF) * amount);
                o.Px[i] = (a << 24) | (r << 16) | (g << 8) | b;
            }
            return o;
        }

        // returns copy with alpha multiplied
        public Sprite Alphaed(float mul)
        {
            Sprite o = new Sprite(W, H);
            for (int i = 0; i < Px.Length; i++)
            {
                int c = Px[i];
                int a = (c >> 24) & 0xFF;
                a = (int)(a * mul); if (a > 255) a = 255; if (a < 0) a = 0;
                o.Px[i] = (a << 24) | (c & 0xFFFFFF);
            }
            return o;
        }
    }

    public static class Assets
    {
        public static Sprite Granny, GrannySmall, GrannyFace, Cat, CatAngry, Wallpaper;
        public static Bitmap Icon;
        public static bool Loaded;

        static Stream Find(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream s = asm.GetManifestResourceStream(name);
            if (s != null) return s;
            string[] names = asm.GetManifestResourceNames();
            for (int i = 0; i < names.Length; i++)
                if (names[i].EndsWith(name, StringComparison.OrdinalIgnoreCase)) return asm.GetManifestResourceStream(names[i]);
            return null;
        }

        public static Sprite LoadSprite(string name)
        {
            Stream s = Find(name);
            if (s == null) { Log.W("asset missing: " + name); return null; }
            try
            {
                using (Bitmap b = new Bitmap(s)) return Sprite.FromBitmap(b);
            }
            catch (Exception e) { Log.W("asset fail " + name + ": " + e.Message); return null; }
        }

        public static void Load()
        {
            if (Loaded) return;
            Sprite big = LoadSprite("granny.png");
            if (big != null) { Granny = big.Resized(760); GrannySmall = big.Resized(300); }
            GrannyFace = LoadSprite("granny_face.png");
            Cat = LoadSprite("marusya.png");
            CatAngry = LoadSprite("marusya_angry.png");
            Wallpaper = LoadSprite("wallpaper.png");
            try
            {
                Stream s = Find("icon.png");
                if (s != null) Icon = new Bitmap(s);
            }
            catch { }
            Loaded = true;
            Log.W("assets loaded: granny=" + (Granny != null) + " face=" + (GrannyFace != null) + " cat=" + (Cat != null) + " wp=" + (Wallpaper != null));
        }
    }

    public static class Log
    {
        static StreamWriter w;
        public static string Path = "granny13.log";
        static void Open()
        {
            if (w != null) return;
            try { w = new StreamWriter(Path, false, System.Text.Encoding.UTF8); w.AutoFlush = true; } catch { }
        }
        public static void W(string msg)
        {
            Open();
            if (w != null) { try { w.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg); } catch { } }
        }
        public static void Close() { if (w != null) { try { w.Close(); } catch { } w = null; } }
    }

    public unsafe class Frame
    {
        public readonly int W, H;
        public readonly int[] Px;
        public readonly float[] Z;      // depth per column
        public Frame(int w, int h)
        {
            W = w; H = h;
            Px = new int[w * h];
            Z = new float[w];
        }

        public void BlitTo(Bitmap bmp)
        {
            BitmapData bd = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                for (int y = 0; y < H; y++)
                {
                    IntPtr row = new IntPtr(bd.Scan0.ToInt64() + (long)y * bd.Stride);
                    Marshal.Copy(Px, y * W, row, W);
                }
            }
            finally { bmp.UnlockBits(bd); }
        }

        public void DrawTo(Bitmap bmp) { BlitTo(bmp); }

        static int AvgC(int a, int b)
        {
            int r = (((a >> 16) & 0xFF) + ((b >> 16) & 0xFF)) >> 1;
            int g = (((a >> 8) & 0xFF) + ((b >> 8) & 0xFF)) >> 1;
            int bl = ((a & 0xFF) + (b & 0xFF)) >> 1;
            return (0xFF << 24) | (r << 16) | (g << 8) | bl;
        }

        static int Avg4(int a, int b, int c, int d)
        {
            int r = (((a >> 16) & 0xFF) + ((b >> 16) & 0xFF) + ((c >> 16) & 0xFF) + ((d >> 16) & 0xFF)) >> 2;
            int g = (((a >> 8) & 0xFF) + ((b >> 8) & 0xFF) + ((c >> 8) & 0xFF) + ((d >> 8) & 0xFF)) >> 2;
            int bl = ((a & 0xFF) + (b & 0xFF) + (c & 0xFF) + (d & 0xFF)) >> 2;
            return (0xFF << 24) | (r << 16) | (g << 8) | bl;
        }

        // 2x upscale with a light bilinear-ish smoothing, straight into the window bitmap
        public unsafe void Present2x(Bitmap dst, bool smooth)
        {
            int dw = W * 2, dh = H * 2;
            BitmapData bd = dst.LockBits(new Rectangle(0, 0, dw, dh), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                byte* basePtr = (byte*)bd.Scan0;
                int stride = bd.Stride;
                for (int y = 0; y < H; y++)
                {
                    int* r0 = (int*)(basePtr + (long)(y * 2) * stride);
                    int* r1 = (int*)(basePtr + (long)(y * 2 + 1) * stride);
                    int sy = y * W;
                    int sy2 = ((y + 1 < H) ? y + 1 : y) * W;
                    if (!smooth)
                    {
                        for (int x = 0; x < W; x++)
                        {
                            int c = Px[sy + x];
                            r0[x * 2] = c; r0[x * 2 + 1] = c;
                            r1[x * 2] = c; r1[x * 2 + 1] = c;
                        }
                    }
                    else
                    {
                        for (int x = 0; x < W; x++)
                        {
                            int c = Px[sy + x];
                            int cr = Px[sy + ((x + 1 < W) ? x + 1 : x)];
                            int cd = Px[sy2 + x];
                            int cdr = Px[sy2 + ((x + 1 < W) ? x + 1 : x)];
                            r0[x * 2] = c;
                            r0[x * 2 + 1] = AvgC(c, cr);
                            r1[x * 2] = AvgC(c, cd);
                            r1[x * 2 + 1] = Avg4(c, cr, cd, cdr);
                        }
                    }
                }
            }
            finally { dst.UnlockBits(bd); }
        }

        public void Resize(int w, int h)
        {
        }
    }
}
