// Renderer.cs - textured raycaster with flashlight, sprites, post-processing
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Granny13
{
    public class Camera
    {
        public double X, Y, Angle;
        public double Pitch;
        public double EyeH = 0.5;
        public double Fov = 66.0 * M.PI / 180.0;
        public bool Flashlight = true;
        public double FlashBattery = 1.0;
        public double Shake;
        public double ShakePhase;
        public double Hiding;      // 0..1 slats
        public double HideLook;    // peek offset when hiding
        public double Breathe;
        public double Flicker = 1.0;
        public double Hp = 1.0;
        public double GrannyProximity;  // 0..1 for red pulsing
        public double HeadBob;

        public void AddShake(double amount)
        {
            Shake = Math.Min(1.6, Shake + amount);
        }
    }

    public class SpriteInst
    {
        public double X, Y;
        public double Height = 1.0;
        public double Elev;
        public Sprite Img;
        public float Tint = 1f;
        public int TintColor = 0;
        public float TintAmount;
        public float Alpha = 1f;
        public bool Glow;
        public bool Flat;    // decal on floor
        public bool FaceCam = true;
        public double Shear;
    }

    public unsafe class Renderer
    {
        public Frame F;
        public int W, H;
        public double[] Vignette;
        public float[] Grain;
        public int GrainW = 256, GrainH = 256;
        public double Ambient = 0.31;
        public double LightMul = 1.0;
        public double NoiseSeed;
        float[] beamLut;
        Tex[] floorHouse = new Tex[32], floorAttic = new Tex[32];
        Tex ceilHouse, ceilAttic;
        bool mapsReady;

        public const int FOG_R = 10, FOG_G = 10, FOG_B = 16;

        public Renderer(int w, int h)
        {
            W = w; H = h;
            F = new Frame(w, h);
            BuildVignette();
            BuildGrain();
        }

        void BuildVignette()
        {
            Vignette = new double[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    double dx = (x - W * 0.5) / (W * 0.5);
                    double dy = (y - H * 0.5) / (H * 0.5);
                    double d = Math.Sqrt(dx * dx * 0.85 + dy * dy * 1.0);
                    double v = 1.0 - 0.62 * Math.Pow(M.Clamp01(d * 0.92), 2.1);
                    Vignette[y * W + x] = v;
                }
        }

        void BuildGrain()
        {
            Grain = new float[GrainW * GrainH];
            Rng r = new Rng(4242);
            for (int i = 0; i < Grain.Length; i++) Grain[i] = (float)(r.Next() - 0.5) * 2f;
        }

        public void Resize(int w, int h)
        {
            W = w; H = h;
            F = new Frame(w, h);
            BuildVignette();
        }

        float Beam(float camX)
        {
            float a = (float)Math.Abs(camX);
            float v = 1f - (a * a) / 1.15f;
            if (v < 0f) v = 0f;
            return 0.52f * v + 0.48f * v * v;
        }

        int ShadeWall(int c, double dist, float beam, float tileVar, Camera cam)
        {
            float amb = (float)(Ambient * (0.8 + 0.2 * cam.Flicker));
            float flash = 0f;
            if (cam.Flashlight)
            {
                float reach = (float)(1.0 / (1.0 + dist * 0.30 + dist * dist * 0.045));
                flash = (float)(1.55 * cam.FlashBattery) * beam * reach;
            }
            float light = amb + flash;
            if (light > 1.6f) light = 1.6f;
            float fog = (float)M.Clamp01((dist - 3.0) / 26.0);
            float r = ((c >> 16) & 0xFF) * light * tileVar;
            float g = ((c >> 8) & 0xFF) * light * tileVar;
            float b = (c & 0xFF) * light * tileVar;
            r = r * (1 - fog) + FOG_R * fog;
            g = g * (1 - fog) + FOG_G * fog;
            b = b * (1 - fog) + FOG_B * fog;
            if (r > 255) r = 255; if (g > 255) g = 255; if (b > 255) b = 255;
            return (0xFF << 24) | ((int)r << 16) | ((int)g << 8) | (int)b;
        }

        static float TileVar(int x, int y)
        {
            int h = x * 73856093 ^ y * 19349663;
            h = (h ^ (h >> 13)) & 0xFFFF;
            return 0.93f + (h / 65535f) * 0.14f;
        }

        static int TrapPixel(double r, double ang, bool armed)
        {
            if (r > 0.30) return -1;
            if (r < 0.05) return 0x2A2226;
            if (r < 0.095) return armed ? 0x6A5A50 : 0x40383A;
            double teeth = Math.Abs(Math.Sin(ang * 9.0));
            if (r > 0.175 && r < 0.275) return teeth > 0.42 ? 0xA2A2AC : 0x5C5C66;
            if (r > 0.135 && r < 0.175) return 0x45414A;
            return -1;
        }

        void PrepareMaps()
        {
            beamLut = new float[W];
            for (int x = 0; x < W; x++) beamLut[x] = Beam(2f * x / W - 1f);
            for (int i = 0; i < 32; i++)
            {
                floorHouse[i] = TexLib.Floor[TexLib.TF_WOOD];
                floorAttic[i] = TexLib.Floor[TexLib.TF_ATTIC];
            }
            floorHouse[T.RUG] = TexLib.Floor[TexLib.TF_RUG];
            floorAttic[T.RUG] = TexLib.Floor[TexLib.TF_RUG];
            floorHouse[T.TILEW] = TexLib.Floor[TexLib.TF_TILE];
            floorAttic[T.TILEW] = TexLib.Floor[TexLib.TF_TILE];
            ceilHouse = TexLib.Ceil[0];
            ceilAttic = TexLib.Ceil[1];
            mapsReady = true;
        }

        public void Render(Level lv, Camera cam, List<SpriteInst> sprites, double time)
        {
            System.Diagnostics.Stopwatch tm = System.Diagnostics.Stopwatch.StartNew();
            if (!mapsReady || beamLut == null || beamLut.Length != this.W) PrepareMaps();
            int* px = null;
            fixed (int* p0 = F.Px) px = p0;
            int W = this.W, H = this.H;

            double shake = cam.Shake;
            cam.ShakePhase += 0.0;
            double shX = shake > 0.001 ? Math.Sin(cam.ShakePhase * 37.0) * shake * 9 : 0;
            double shY = shake > 0.001 ? Math.Cos(cam.ShakePhase * 29.0) * shake * 7 : 0;

            double dirX = Math.Cos(cam.Angle), dirY = Math.Sin(cam.Angle);
            double planeLen = Math.Tan(cam.Fov * 0.5) * (2.0 * 0.5);
            // plane is perpendicular, scaled so that screen edge = fov/2
            double planeX = -dirY * planeLen * ((double)W / H) * ((double)H / W);
            double planeY = dirX * planeLen;
            double horizon = H * 0.5 + cam.Pitch + shY;
            double eyeH = cam.EyeH;

            int* row = px;
            // ---------------------------------------------------------- ceiling & floor
            for (int y = 0; y < H; y++)
            {
                int screenY = y;
                if (screenY > horizon)
                {
                    // floor
                    double p = screenY - horizon;
                    double posZ = eyeH * H;
                    double rowDist = posZ / p;
                    if (rowDist > 60) rowDist = 60;
                    double stepX = rowDist * (2.0 * planeX) / W;
                    double stepY = rowDist * (2.0 * planeY) / W;
                    double fx = cam.X + rowDist * (dirX - planeX);
                    double fy = cam.Y + rowDist * (dirY - planeY);
                    float fog = (float)M.Clamp01((rowDist - 3.0) / 26.0);
                    float amb = (float)(Ambient * (0.75 + 0.25 * cam.Flicker));
                    float flashBase = 0f;
                    if (cam.Flashlight)
                    {
                        float reach = (float)(1.0 / (1.0 + rowDist * 0.24 + rowDist * rowDist * 0.032));
                        flashBase = (float)(1.5 * cam.FlashBattery) * reach;
                    }
                    float pool = (float)M.Clamp01(1.0 - rowDist / 3.2) * 0.30f;
                    float fogInv = 1f - fog;
                    float fogR = FOG_R * fog, fogG = FOG_G * fog, fogB = FOG_B * fog;
                    Tex[] fmap = (lv.Id == 1) ? floorAttic : floorHouse;
                    byte[] trapGrid = lv.TrapGrid;
                    int lw = lv.W, lh = lv.H;
                    byte[] lmap = lv.Map;
                    for (int x = 0; x < W; x++)
                    {
                        int tileX = (int)fx, tileY = (int)fy;
                        bool inb = (tileX >= 0 && tileY >= 0 && tileX < lw && tileY < lh);
                        byte t = inb ? lmap[tileY * lw + tileX] : T.BRICK;
                        Tex tex = fmap[t];
                        int tx = (int)(fx * 256); if (tx < 0) tx = -tx;
                        int ty = (int)(fy * 256); if (ty < 0) ty = -ty;
                        if (t == T.RUG)
                        {
                            tx = (int)(fx * 128); if (tx < 0) tx = -tx;
                            ty = (int)(fy * 128); if (ty < 0) ty = -ty;
                        }
                        int c = tex.Px[((ty & 127) << 7) + (tx & 127)];
                        if (trapGrid != null && inb && trapGrid[tileY * lw + tileX] != 0)
                        {
                            double fxx = fx - tileX - 0.5, fyy = fy - tileY - 0.5;
                            double rr = Math.Sqrt(fxx * fxx + fyy * fyy);
                            if (rr < 0.31)
                            {
                                int tc = TrapPixel(rr, Math.Atan2(fyy, fxx), trapGrid[tileY * lw + tileX] == 2);
                                if (tc >= 0) { c = (0xFF << 24) | tc; TrapHits++; }
                            }
                        }
                        float light = amb + flashBase * beamLut[x] + pool;
                        int r = (int)(((c >> 16) & 0xFF) * light * fogInv + fogR);
                        int g = (int)(((c >> 8) & 0xFF) * light * fogInv + fogG);
                        int b = (int)((c & 0xFF) * light * fogInv + fogB);
                        if (r > 255) r = 255;
                        if (g > 255) g = 255;
                        if (b > 255) b = 255;
                        row[x] = (0xFF << 24) | (r << 16) | (g << 8) | b;
                        fx += stepX; fy += stepY;
                    }
                }
                else
                {
                    // ceiling
                    double p = horizon - screenY;
                    if (p < 0.6) p = 0.6;
                    double rowDist = (1.0 - eyeH) * H / p;
                    if (rowDist > 60) rowDist = 60;
                    double stepX = rowDist * (2.0 * planeX) / W;
                    double stepY = rowDist * (2.0 * planeY) / W;
                    double fx = cam.X + rowDist * (dirX - planeX);
                    double fy = cam.Y + rowDist * (dirY - planeY);
                    Tex tex = (lv.Id == 1) ? ceilAttic : ceilHouse;
                    float fog = (float)M.Clamp01((rowDist - 4.0) / 26.0);
                    float amb = (float)(Ambient * 0.46 * (0.8 + 0.2 * cam.Flicker));
                    float flashBase = 0f;
                    if (cam.Flashlight)
                    {
                        float reach = (float)(1.0 / (1.0 + rowDist * 0.5 + rowDist * rowDist * 0.14));
                        flashBase = (float)(0.85 * cam.FlashBattery) * reach;
                    }
                    float fogInv = 1f - fog;
                    float fogR = FOG_R * fog, fogG = FOG_G * fog, fogB = FOG_B * fog;
                    for (int x = 0; x < W; x++)
                    {
                        int tx = (int)(fx * 256); if (tx < 0) tx = -tx;
                        int ty = (int)(fy * 256); if (ty < 0) ty = -ty;
                        int c = tex.Px[((ty & 127) << 7) + (tx & 127)];
                        float light = amb + flashBase * beamLut[x];
                        int r = (int)(((c >> 16) & 0xFF) * light * fogInv + fogR);
                        int g = (int)(((c >> 8) & 0xFF) * light * fogInv + fogG);
                        int b = (int)((c & 0xFF) * light * fogInv + fogB);
                        if (r > 255) r = 255;
                        if (g > 255) g = 255;
                        if (b > 255) b = 255;
                        row[x] = (0xFF << 24) | (r << 16) | (g << 8) | b;
                        fx += stepX; fy += stepY;
                    }
                }
                row += W;
            }

            // ---------------------------------------------------------- walls
            MsFloor = tm.Elapsed.TotalMilliseconds;
            tm.Restart();
            fixed (float* zcol = F.Z)
            {
                for (int x = 0; x < W; x++)
                {
                    double camX = 2.0 * x / W - 1.0;
                    double rx = dirX + planeX * camX;
                    double ry = dirY + planeY * camX;
                    int mapX = (int)Math.Floor(cam.X), mapY = (int)Math.Floor(cam.Y);
                    double ddx = Math.Abs(rx) < 1e-9 ? 1e9 : 1.0 / Math.Abs(rx);
                    double ddy = Math.Abs(ry) < 1e-9 ? 1e9 : 1.0 / Math.Abs(ry);
                    double sideX, sideY;
                    int stepX, stepY;
                    if (rx < 0) { stepX = -1; sideX = (cam.X - mapX) * ddx; }
                    else { stepX = 1; sideX = (mapX + 1.0 - cam.X) * ddx; }
                    if (ry < 0) { stepY = -1; sideY = (cam.Y - mapY) * ddy; }
                    else { stepY = 1; sideY = (mapY + 1.0 - cam.Y) * ddy; }
                    bool hit = false;
                    int side = 0;
                    byte ht = 0;
                    int guard = 0;
                    double dist = 0;
                    while (!hit && guard++ < 200)
                    {
                        if (sideX < sideY) { dist = sideX; sideX += ddx; mapX += stepX; side = 0; }
                        else { dist = sideY; sideY += ddy; mapY += stepY; side = 1; }
                        if (mapX < 0 || mapY < 0 || mapX >= lv.W || mapY >= lv.H) { hit = true; ht = T.BRICK; dist = 60; break; }
                        byte t = lv.Map[mapY * lv.W + mapX];
                        if (!T.Solid[t]) continue;
                        if (t == T.DOOR)
                        {
                            Door d = lv.DoorAt(mapX, mapY);
                            if (d != null && d.Open) continue;
                        }
                        hit = true; ht = t;
                    }
                    if (!hit) { zcol[x] = 60; continue; }
                    zcol[x] = (float)dist;
                    if (dist < 0.02) dist = 0.02;
                    int texIdx = T.WallTex[ht];
                    if (texIdx < 0 || texIdx >= TexLib.Wall.Length || TexLib.Wall[texIdx] == null) texIdx = TexLib.TW_BRICK;
                    Tex tex = TexLib.Wall[texIdx];
                    if (tex == null) { texIdx = TexLib.TW_BRICK; tex = TexLib.Wall[TexLib.TW_BRICK]; }
                    double wallX;
                    if (side == 0) wallX = cam.Y + dist * ry;
                    else wallX = cam.X + dist * rx;
                    wallX -= Math.Floor(wallX);
                    double lineHeight = H / dist;
                    double drawStart = horizon - lineHeight * 0.5;
                    double drawEnd = horizon + lineHeight * 0.5;
                    int texX = (int)(wallX * tex.W * TexLib.WallUv[texIdx]);
                    if ((side == 0 && rx > 0) || (side == 1 && ry < 0)) texX = -texX - 1;
                    texX = texX % tex.W; if (texX < 0) texX += tex.W;
                    if (texX < 0) texX = 0; if (texX >= tex.W) texX = tex.W - 1;
                    float beam = Beam((float)camX);
                    float tileVar = TileVar(mapX, mapY);
                    float fog = (float)M.Clamp01((dist - 3.0) / 26.0);
                    float amb = (float)(Ambient * (0.78 + 0.22 * cam.Flicker));
                    float flash = 0f;
                    if (cam.Flashlight)
                    {
                        float reach = (float)(1.0 / (1.0 + dist * 0.24 + dist * dist * 0.032));
                        flash = (float)(1.55 * cam.FlashBattery) * beam * reach;
                    }
                    float light = Math.Min(1.8f, amb + flash);
                    int y0 = (int)drawStart; if (y0 < 0) y0 = 0;
                    int y1 = (int)drawEnd; if (y1 >= H) y1 = H - 1;
                    double step = tex.H / lineHeight;
                    double texPos = (y0 - drawStart) * step;
                    for (int y = y0; y <= y1; y++)
                    {
                        int texY = (int)texPos; texPos += step;
                        if (texY < 0) texY = 0; if (texY >= tex.H) texY = tex.H - 1;
                        int c = tex.Px[texY * tex.W + texX];
                        float l = light;
                        // baseboard darkening
                        if (texY > tex.H - 14) l *= 0.45f + (tex.H - texY) * 0.02f;
                        else if (texY < 6) l *= 0.7f;
                        float r = ((c >> 16) & 0xFF) * l * tileVar;
                        float g = ((c >> 8) & 0xFF) * l * tileVar;
                        float b = (c & 0xFF) * l * tileVar;
                        r = r * (1 - fog) + FOG_R * fog;
                        g = g * (1 - fog) + FOG_G * fog;
                        b = b * (1 - fog) + FOG_B * fog;
                        if (r > 255) r = 255; if (g > 255) g = 255; if (b > 255) b = 255;
                        px[y * W + x] = (0xFF << 24) | ((int)r << 16) | ((int)g << 8) | (int)b;
                    }
                }
            }

            // ---------------------------------------------------------- sprites
            MsWalls = tm.Elapsed.TotalMilliseconds;
            tm.Restart();
            if (sprites != null && sprites.Count > 0)
            {
                List<SpriteInst> sorted = new List<SpriteInst>(sprites);
                sorted.Sort(delegate(SpriteInst a, SpriteInst b)
                {
                    double da = (a.X - cam.X) * (a.X - cam.X) + (a.Y - cam.Y) * (a.Y - cam.Y);
                    double db = (b.X - cam.X) * (b.X - cam.X) + (b.Y - cam.Y) * (b.Y - cam.Y);
                    return db.CompareTo(da);
                });
                double invDet = 1.0 / (planeX * dirY - dirX * planeY);
                for (int i = 0; i < sorted.Count; i++)
                {
                    SpriteInst s = sorted[i];
                    if (s.Img == null) continue;
                    double sx = s.X - cam.X, sy = s.Y - cam.Y;
                    double tX = invDet * (dirY * sx - dirX * sy);
                    double tY = invDet * (-planeY * sx + planeX * sy);
                    if (tY < 0.12) continue;
                    int screenX = (int)((W * 0.5) * (1 + tX / tY));
                    double hpx = (H / tY) * s.Height;
                    double wpx = hpx * s.Img.Aspect;
                    double floorY = horizon + (H / tY) * eyeH;
                    double bottom = floorY - (H / tY) * s.Elev;
                    double top = bottom - hpx;
                    if (s.Flat)
                    {
                        hpx = (H / tY) * s.Height;
                        top = floorY - hpx;
                        bottom = floorY;
                    }
                    int x0 = (int)(screenX - wpx * 0.5);
                    int x1 = (int)(screenX + wpx * 0.5);
                    if (x0 < 0) x0 = 0; if (x1 > W - 1) x1 = W - 1;
                    int y0 = (int)top; if (y0 < 0) y0 = 0;
                    int y1 = (int)bottom; if (y1 > H - 1) y1 = H - 1;
                    if (x1 < x0 || y1 < y0) continue;
                    float amb = (float)(Ambient * (0.8 + 0.2 * cam.Flicker));
                    float fog = (float)M.Clamp01((tY - 3.0) / 26.0);
                    int sw = s.Img.W, sh = s.Img.H;
                    int* spx = null;
                    fixed (int* psp = s.Img.Px) spx = psp;
                    for (int x = x0; x <= x1; x++)
                    {
                        if (tY >= F.Z[x]) continue;
                        double u = (x - (screenX - wpx * 0.5)) / wpx;
                        if (u < 0 || u >= 1) continue;
                        float beam = Beam((float)(2.0 * x / W - 1.0));
                        float flash = 0f;
                        if (cam.Flashlight)
                        {
                            float reach = (float)(1.0 / (1.0 + tY * 0.24 + tY * tY * 0.032));
                            flash = (float)(1.6 * cam.FlashBattery) * beam * reach;
                        }
                        float light = Math.Min(1.8f, amb + flash);
                        // shear gives a cheap "walking" lean
                        double shear = s.Shear * (1.0 - (u - 0.5) * 0.6);
                        int texX = (int)(u * sw);
                        if (texX < 0) texX = 0; if (texX >= sw) texX = sw - 1;
                        for (int y = y0; y <= y1; y++)
                        {
                            double v = (y - top) / hpx;
                            if (v < 0 || v >= 1) continue;
                            int texY = (int)(v * sh); if (texY < 0) texY = 0; if (texY >= sh) texY = sh - 1;
                            int c = spx[texY * sw + texX];
                            int a = (c >> 24) & 0xFF;
                            if (a < 8) continue;
                            float al = a / 255f * s.Alpha;
                            float l = s.Glow ? 1f : light;
                            float r = ((c >> 16) & 0xFF) * l;
                            float g = ((c >> 8) & 0xFF) * l;
                            float b = (c & 0xFF) * l;
                            if (!s.Glow)
                            {
                                r = r * (1 - fog) + FOG_R * fog;
                                g = g * (1 - fog) + FOG_G * fog;
                                b = b * (1 - fog) + FOG_B * fog;
                            }
                            float tr = s.TintColor >> 16 & 0xFF, tg = s.TintColor >> 8 & 0xFF, tb = s.TintColor & 0xFF;
                            if (s.TintAmount > 0)
                            {
                                r = r * (1 - s.TintAmount) + tr * s.TintAmount;
                                g = g * (1 - s.TintAmount) + tg * s.TintAmount;
                                b = b * (1 - s.TintAmount) + tb * s.TintAmount;
                            }
                            r *= s.Tint; g *= s.Tint; b *= s.Tint;
                            if (r > 255) r = 255; if (g > 255) g = 255; if (b > 255) b = 255;
                            int dst = px[y * W + x];
                            int dr = (dst >> 16) & 0xFF, dg = (dst >> 8) & 0xFF, db = dst & 0xFF;
                            int rr = (int)(dr + (r - dr) * al);
                            int gg = (int)(dg + (g - dg) * al);
                            int bb = (int)(db + (b - db) * al);
                            px[y * W + x] = (0xFF << 24) | (rr << 16) | (gg << 8) | bb;
                        }
                    }
                }
            }

            // ---------------------------------------------------------- post fx
            MsSprites = tm.Elapsed.TotalMilliseconds;
            tm.Restart();
            PostFx(cam, time);
            MsPost = tm.Elapsed.TotalMilliseconds;
        }

        public double MsFloor, MsWalls, MsSprites, MsPost;
        public static int TrapHits;

        void PostFx(Camera cam, double time)
        {
            int W = this.W, H = this.H;
            float gr = (float)cam.GrannyProximity;
            float pulse = 0f;
            if (gr > 0.01f)
                pulse = (float)((0.5 + 0.5 * Math.Sin(time * (6.0 + gr * 5.0))) * gr * 0.16);
            float hp = (float)cam.Hp;
            float red = Math.Max(pulse, (1f - hp) * 0.35f);
            float darkness = 1f;
            for (int y = 0; y < H; y++)
            {
                int gy = (int)((y + (int)(time * 60)) & (GrainH - 1));
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    int c = F.Px[i];
                    float v = (float)Vignette[i];
                    float g = Grain[gy * GrainW + ((x + (int)(time * 137)) & (GrainW - 1))] * 4.2f;
                    float r = ((c >> 16) & 0xFF) * v * darkness + g;
                    float gg = ((c >> 8) & 0xFF) * v * darkness + g;
                    float b = (c & 0xFF) * v * darkness + g;
                    // cold shadows, warm highlights (cheap colour grade)
                    float lum = (r + gg + b) * 0.3333f;
                    if (lum < 115f) b += (115f - lum) * 0.16f;
                    else { r += (lum - 115f) * 0.045f; }
                    if (red > 0)
                    {
                        r = r * (1 - red) + 150 * red;
                        gg = gg * (1 - red) * 0.85f;
                        b = b * (1 - red) * 0.85f;
                    }
                    if (r > 255) r = 255; if (gg > 255) gg = 255; if (b > 255) b = 255;
                    if (r < 0) r = 0; if (gg < 0) gg = 0; if (b < 0) b = 0;
                    F.Px[i] = (0xFF << 24) | ((int)r << 16) | ((int)gg << 8) | (int)b;
                }
            }
            // hiding slats overlay
            if (cam.Hiding > 0.01)
            {
                float k = (float)cam.Hiding;
                for (int y = 0; y < H; y++)
                {
                    int slat = (y % 26);
                    float mul = (slat < 14) ? 0.06f : 1f;
                    if (slat < 14) mul = 0.06f + 0.10f * (slat / 14f);
                    mul = 1f - (1f - mul) * k;
                    for (int x = 0; x < W; x++)
                    {
                        int i = y * W + x;
                        int c = F.Px[i];
                        int r = (int)(((c >> 16) & 0xFF) * mul);
                        int g = (int)(((c >> 8) & 0xFF) * mul);
                        int b = (int)((c & 0xFF) * mul);
                        F.Px[i] = (0xFF << 24) | (r << 16) | (g << 8) | b;
                    }
                }
            }
        }

        public void DrawMinimapOverlay(Level lv, Camera cam, double px, double py, bool showMap)
        {
            if (!showMap) return;
            int cell = 3;
            int mw = lv.W * cell, mh = lv.H * cell;
            int ox = W - mw - 6, oy = H - mh - 6;
            if (ox < 0 || oy < 0) return;
            for (int y = 0; y < mh; y++)
                for (int x = 0; x < mw; x++)
                {
                    int gx = x / cell, gy = y / cell;
                    byte t = lv.Get(gx, gy);
                    int c;
                    if (t == T.DOOR) c = 0x2A5A2A;
                    else if (T.Solid[t]) c = 0x6A6A78;
                    else c = 0x1A1A22;
                    int dx = ox + x, dy = oy + y;
                    if (dx >= 0 && dy >= 0 && dx < W && dy < H)
                        F.Px[dy * W + dx] = unchecked((int)0xFF000000) | c;
                }
            // player
            int pxp = ox + (int)(cam.X * cell), pyp = oy + (int)(cam.Y * cell);
            for (int y = -2; y <= 2; y++)
                for (int x = -2; x <= 2; x++)
                {
                    int dx = pxp + x, dy = pyp + y;
                    if (dx >= 0 && dy >= 0 && dx < W && dy < H) F.Px[dy * W + dx] = unchecked((int)0xFFFFD040);
                }
        }
    }
}


