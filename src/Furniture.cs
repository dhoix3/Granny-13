// Furniture.cs - billboard sprites for low furniture drawn with GDI+
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Granny13
{
    public static class Furniture
    {
        public static Sprite[] Build()
        {
            Sprite[] s = new Sprite[5];
            s[0] = ToSprite(DrawBed());
            s[1] = ToSprite(DrawTable());
            s[2] = ToSprite(DrawSofa());
            s[3] = ToSprite(DrawCrate());
            s[4] = ToSprite(DrawDesk());
            return s;
        }

        static Sprite ToSprite(Bitmap b)
        {
            Sprite sp = Sprite.FromBitmap(b);
            b.Dispose();
            return sp;
        }

        static Bitmap Canvas(int w, int h)
        {
            Bitmap b = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
            }
            return b;
        }

        static LinearGradientBrush Grad(Rectangle r, Color a, Color b, float angle)
        {
            LinearGradientBrush br = new LinearGradientBrush(r, a, b, angle);
            return br;
        }

        static void Shadow(Graphics g, int w, int h, int baseY)
        {
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddEllipse(w * 0.06f, baseY - h * 0.10f, w * 0.88f, h * 0.16f);
                using (PathGradientBrush pb = new PathGradientBrush(p))
                {
                    pb.CenterColor = Color.FromArgb(150, 0, 0, 0);
                    pb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(pb, p);
                }
            }
        }

        static Bitmap DrawBed()
        {
            int w = 640, h = 300;
            Bitmap b = Canvas(w, h);
            using (Graphics g = Graphics.FromImage(b))
            {
                Shadow(g, w, h, h - 18);
                // legs
                using (SolidBrush leg = new SolidBrush(Color.FromArgb(255, 58, 38, 22)))
                {
                    g.FillRectangle(leg, 40, h - 92, 26, 78);
                    g.FillRectangle(leg, w - 70, h - 92, 26, 78);
                }
                // frame
                Rectangle fr = new Rectangle(24, h - 130, w - 48, 54);
                using (LinearGradientBrush br = Grad(fr, Color.FromArgb(255, 122, 90, 52), Color.FromArgb(255, 66, 46, 26), 90f))
                    g.FillRectangle(br, fr);
                // mattress
                Rectangle mat = new Rectangle(28, h - 212, w - 56, 88);
                using (LinearGradientBrush br = Grad(mat, Color.FromArgb(255, 226, 220, 202), Color.FromArgb(255, 168, 160, 144), 90f))
                    g.FillRectangle(br, mat);
                using (Pen p = new Pen(Color.FromArgb(255, 110, 104, 92), 2))
                    g.DrawRectangle(p, mat);
                // blanket
                Rectangle bl = new Rectangle(w / 2 - 24, h - 216, w / 2 - 16, 96);
                using (LinearGradientBrush br = Grad(bl, Color.FromArgb(255, 118, 44, 62), Color.FromArgb(255, 72, 24, 40), 90f))
                    g.FillRectangle(br, bl);
                using (Pen p = new Pen(Color.FromArgb(255, 46, 14, 24), 2))
                    g.DrawRectangle(p, bl);
                using (Pen p = new Pen(Color.FromArgb(90, 220, 180, 190), 2))
                {
                    g.DrawLine(p, bl.Left + 10, bl.Top + 14, bl.Right - 10, bl.Top + 14);
                    g.DrawLine(p, bl.Left + 10, bl.Bottom - 22, bl.Right - 10, bl.Bottom - 22);
                }
                // pillow
                Rectangle pil = new Rectangle(60, h - 208, 130, 58);
                using (LinearGradientBrush br = Grad(pil, Color.FromArgb(255, 246, 242, 232), Color.FromArgb(255, 196, 190, 178), 90f))
                    g.FillEllipse(br, pil);
                using (Pen p = new Pen(Color.FromArgb(60, 90, 84, 74), 2))
                    g.DrawEllipse(p, pil);
                // headboard
                Rectangle hb = new Rectangle(18, h - 300, 70, 190);
                using (LinearGradientBrush br = Grad(hb, Color.FromArgb(255, 108, 76, 44), Color.FromArgb(255, 56, 38, 22), 0f))
                    g.FillRectangle(br, hb);
            }
            return b;
        }

        static Bitmap DrawTable()
        {
            int w = 640, h = 280;
            Bitmap b = Canvas(w, h);
            using (Graphics g = Graphics.FromImage(b))
            {
                Shadow(g, w, h, h - 14);
                using (SolidBrush leg = new SolidBrush(Color.FromArgb(255, 74, 50, 28)))
                {
                    g.FillRectangle(leg, 90, 120, 30, h - 140);
                    g.FillRectangle(leg, w - 120, 120, 30, h - 140);
                    g.FillRectangle(leg, w / 2 - 15, 130, 26, h - 150);
                }
                Rectangle top = new Rectangle(20, 70, w - 40, 60);
                using (LinearGradientBrush br = Grad(top, Color.FromArgb(255, 168, 128, 76), Color.FromArgb(255, 84, 56, 30), 90f))
                    g.FillRectangle(br, top);
                using (Pen p = new Pen(Color.FromArgb(255, 44, 28, 16), 3))
                    g.DrawRectangle(p, top);
                // cloth
                using (SolidBrush c = new SolidBrush(Color.FromArgb(210, 140, 30, 40)))
                    g.FillRectangle(c, w / 2 - 70, top.Bottom - 4, 150, 40);
            }
            return b;
        }

        static Bitmap DrawSofa()
        {
            int w = 640, h = 300;
            Bitmap b = Canvas(w, h);
            using (Graphics g = Graphics.FromImage(b))
            {
                Shadow(g, w, h, h - 16);
                Rectangle back = new Rectangle(20, 60, w - 40, 120);
                using (LinearGradientBrush br = Grad(back, Color.FromArgb(255, 106, 52, 46), Color.FromArgb(255, 60, 26, 26), 90f))
                {
                    using (GraphicsPath p = new GraphicsPath())
                    {
                        p.AddArc(back.Left, back.Top, 40, 40, 180, 90);
                        p.AddArc(back.Right - 40, back.Top, 40, 40, 270, 90);
                        p.AddRectangle(new Rectangle(back.Left + 20, back.Top + 20, back.Width - 40, back.Height - 20));
                        g.FillPath(br, p);
                    }
                }
                Rectangle seat = new Rectangle(40, 168, w - 80, 70);
                using (LinearGradientBrush br = Grad(seat, Color.FromArgb(255, 134, 68, 58), Color.FromArgb(255, 72, 32, 30), 90f))
                    g.FillRectangle(br, seat);
                using (SolidBrush arm = new SolidBrush(Color.FromArgb(255, 84, 40, 36)))
                {
                    g.FillRectangle(arm, 24, 140, 44, 110);
                    g.FillRectangle(arm, w - 68, 140, 44, 110);
                }
                using (Pen p = new Pen(Color.FromArgb(90, 220, 160, 140), 2))
                {
                    g.DrawLine(p, 52, 186, 160, 186);
                    g.DrawLine(p, w - 160, 186, w - 52, 186);
                }
                using (SolidBrush leg = new SolidBrush(Color.FromArgb(255, 40, 26, 18)))
                {
                    g.FillRectangle(leg, 40, 250, 22, 26);
                    g.FillRectangle(leg, w - 62, 250, 22, 26);
                }
            }
            return b;
        }

        static Bitmap DrawCrate()
        {
            int w = 320, h = 300;
            Bitmap b = Canvas(w, h);
            using (Graphics g = Graphics.FromImage(b))
            {
                Shadow(g, w, h, h - 14);
                Rectangle r = new Rectangle(24, 70, w - 48, h - 96);
                using (LinearGradientBrush br = Grad(r, Color.FromArgb(255, 148, 110, 66), Color.FromArgb(255, 76, 52, 30), 90f))
                    g.FillRectangle(br, r);
                using (Pen p = new Pen(Color.FromArgb(255, 40, 26, 14), 4))
                    g.DrawRectangle(p, r);
                using (Pen p = new Pen(Color.FromArgb(255, 118, 86, 50), 8))
                {
                    g.DrawLine(p, r.Left, r.Top, r.Right, r.Bottom);
                    g.DrawLine(p, r.Right, r.Top, r.Left, r.Bottom);
                    g.DrawRectangle(p, new Rectangle(r.Left + 6, r.Top + 6, r.Width - 12, r.Height - 12));
                }
            }
            return b;
        }

        static Bitmap DrawDesk()
        {
            int w = 640, h = 260;
            Bitmap b = Canvas(w, h);
            using (Graphics g = Graphics.FromImage(b))
            {
                Shadow(g, w, h, h - 12);
                using (SolidBrush leg = new SolidBrush(Color.FromArgb(255, 66, 44, 26)))
                {
                    g.FillRectangle(leg, 60, 110, 34, h - 124);
                    g.FillRectangle(leg, w - 100, 110, 34, h - 124);
                }
                Rectangle top = new Rectangle(20, 66, w - 40, 52);
                using (LinearGradientBrush br = Grad(top, Color.FromArgb(255, 138, 102, 62), Color.FromArgb(255, 70, 46, 26), 90f))
                    g.FillRectangle(br, top);
                Rectangle drawers = new Rectangle(w - 190, 118, 150, 90);
                using (SolidBrush db = new SolidBrush(Color.FromArgb(255, 96, 68, 38)))
                    g.FillRectangle(db, drawers);
                using (Pen p = new Pen(Color.FromArgb(255, 40, 26, 14), 2))
                {
                    g.DrawRectangle(p, drawers);
                    g.DrawLine(p, drawers.Left, drawers.Top + 44, drawers.Right, drawers.Top + 44);
                    g.DrawLine(p, drawers.Left + 60, drawers.Top + 16, drawers.Left + 90, drawers.Top + 16);
                    g.DrawLine(p, drawers.Left + 60, drawers.Top + 60, drawers.Left + 90, drawers.Top + 60);
                }
                // papers on top
                using (SolidBrush paper = new SolidBrush(Color.FromArgb(230, 220, 212, 190)))
                {
                    g.FillRectangle(paper, 90, 54, 90, 20);
                    g.FillRectangle(paper, 200, 58, 70, 14);
                }
            }
            return b;
        }
    }
}
