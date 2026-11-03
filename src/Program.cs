// Program.cs - window, main loop, input, screenshot mode
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Granny13
{
    public static class Input
    {
        static bool[] down = new bool[256];
        static bool[] hit = new bool[256];
        public static double MouseDX, MouseDY;
        public static double MouseSensitivity = 1.0;
        public static bool MouseLocked = true;

        public static bool Key(Keys k) { return down[(int)k & 255]; }
        public static bool Hit(Keys k) { return hit[(int)k & 255]; }

        public static void Set(Keys k, bool v)
        {
            int i = (int)k & 255;
            if (v && !down[i]) hit[i] = true;
            down[i] = v;
        }

        public static void Tap(Keys k)
        {
            int i = (int)k & 255;
            down[i] = true; hit[i] = true;
        }

        public static void EndFrame()
        {
            for (int i = 0; i < 256; i++) hit[i] = false;
            MouseDX = 0; MouseDY = 0;
        }

        public static bool AnyKey()
        {
            for (int i = 0; i < 256; i++) if (down[i]) return true;
            return false;
        }
    }

    public class GameForm : Form
    {
        Game game;
        Stopwatch clock = Stopwatch.StartNew();
        System.Windows.Forms.Timer timer;
        double lastT;
        bool locked;
        Point center;
        public bool Fullscreen;

        public GameForm(int seed, bool nosound, bool shots)
        {
            Text = "ГРЕННИ 13 — побег от бабушки";
            ClientSize = new Size(Game.OW, Game.OH);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.Black;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.UserPaint, true);
            DoubleBuffered = false;
            KeyPreview = true;

            game = new Game(seed);
            if (Assets.Icon != null) { try { Icon = Icon.FromHandle(Assets.Icon.GetHicon()); } catch { } }
            if (!nosound) Audio.Init();

            center = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
            lastT = clock.Elapsed.TotalSeconds;

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1;
            timer.Tick += new EventHandler(OnTick);
            timer.Start();

            KeyDown += new KeyEventHandler(OnKeyDown);
            KeyUp += new KeyEventHandler(OnKeyUp);
            MouseDown += new MouseEventHandler(OnMouseDown);
            FormClosing += new FormClosingEventHandler(OnClosing);
            Activated += new EventHandler(delegate(object s, EventArgs e) { LockMouse(true); });
            Deactivate += new EventHandler(delegate(object s, EventArgs e) { LockMouse(false); });
        }

        void LockMouse(bool on)
        {
            locked = on;
            if (on)
            {
                try { Cursor.Hide(); } catch { }
                try { refPos = PointToScreen(center); Cursor.Position = refPos; refPos = Cursor.Position; } catch { }
            }
            else
            {
                try { Cursor.Show(); } catch { }
            }
        }

        Point refPos;
        bool refReady;

        void ReadMouse()
        {
            try
            {
                Point c = new Point(ClientSize.Width / 2, ClientSize.Height / 2);
                if (!refReady) { refPos = PointToScreen(c); Cursor.Position = refPos; refPos = Cursor.Position; refReady = true; }
                Point p = Cursor.Position;
                double dx = p.X - refPos.X;
                double dy = p.Y - refPos.Y;
                if (dx > 240) dx = 240; if (dx < -240) dx = -240;
                if (dy > 240) dy = 240; if (dy < -240) dy = -240;
                Input.MouseDX += dx;
                Input.MouseDY += dy;
                Point cp = PointToScreen(c);
                if (p.X != cp.X || p.Y != cp.Y)
                {
                    Cursor.Position = cp;
                    refPos = Cursor.Position;   // remember where the pointer really landed
                }
                else refPos = p;
            }
            catch { }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                Fullscreen = !Fullscreen;
                FormBorderStyle = Fullscreen ? FormBorderStyle.None : FormBorderStyle.FixedSingle;
                WindowState = Fullscreen ? FormWindowState.Maximized : FormWindowState.Normal;
                return;
            }
            if (e.KeyCode == Keys.F1) game.ShowDebug = !game.ShowDebug;
            Input.Set(e.KeyCode, true);
            game.OnKey(e.KeyCode, true);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            Input.Set(e.KeyCode, false);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) Input.Tap(Keys.E);
            game.OnKey(Keys.E, true);
        }

        void OnClosing(object sender, FormClosingEventArgs e)
        {
            timer.Stop();
            Audio.Shutdown();
            Log.Close();
        }

        void OnTick(object sender, EventArgs e)
        {
            double now = clock.Elapsed.TotalSeconds;
            double dt = now - lastT;
            lastT = now;
            if (dt > 0.1) dt = 0.1;
            if (dt <= 0) dt = 0.001;

            if (locked && game.Screen == GScreen.Play)
            {
                ReadMouse();
            }

            game.Update(dt);
            game.Render(dt);
            Input.EndFrame();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (game.Back == null) return;
            if (Fullscreen)
            {
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                e.Graphics.DrawImage(game.Back, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            }
            else
            {
                e.Graphics.DrawImageUnscaled(game.Back, 0, 0);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent) { }

        public Game Game { get { return game; } }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool shots = false, nosound = false, autotest = false, audiotest = false, soak = false, dumpmap = false;
            int seed = 20241113;
            string shotDir = "shots";
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--shots") shots = true;
                else if (args[i] == "--autotest") autotest = true;
                else if (args[i] == "--soak") soak = true;
                else if (args[i] == "--dumpmap") dumpmap = true;
                else if (args[i] == "--audiotest") audiotest = true;
                else if (args[i] == "--nosound") nosound = true;
                else if (args[i] == "--seed" && i + 1 < args.Length) int.TryParse(args[i + 1], out seed);
                else if (args[i] == "--out" && i + 1 < args.Length) shotDir = args[i + 1];
            }
            Log.Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "granny13.log");
            try
            {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }
            catch { }

            if (dumpmap)
            {
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "map.txt"), Tests.DumpMap(), System.Text.Encoding.UTF8);
                Log.Close();
                return;
            }

            if (soak)
            {
                try
                {
                    string rep = Tests.Soak(seed, 9000);
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "soak.txt"), rep, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    File.WriteAllText("soak_crash.txt", ex.ToString(), System.Text.Encoding.UTF8);
                }
                Log.Close();
                return;
            }

            if (audiotest)            {
                bool ok = Audio.Init();
                System.Threading.Thread.Sleep(120);
                Audio.Play(Sfx.Scream, 0.5, 1.0, false);
                Audio.Play(Sfx.Meow, 0.6, 1.0, false);
                Audio.Play(Sfx.Step, 0.5, 1.0, false);
                System.Threading.Thread.Sleep(1200);
                string rep = "audio enabled=" + ok + " buffersFilled=" + Audio.BuffersFilled +
                    " screamLen=" + (Sfx.Scream == null ? 0 : Sfx.Scream.Length) +
                    " meowLen=" + (Sfx.Meow == null ? 0 : Sfx.Meow.Length);
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audiotest.txt"), rep, System.Text.Encoding.UTF8);
                Log.W(rep);
                Audio.Shutdown();
                Log.Close();
                return;
            }

            if (autotest)
            {
                try
                {
                    string report = Tests.Run(seed);
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, shotDir));
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, shotDir, "autotest.txt"), report, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    File.WriteAllText("autotest_crash.txt", ex.ToString(), System.Text.Encoding.UTF8);
                    Log.W("autotest crashed: " + ex);
                }
                Log.Close();
                return;
            }

            if (shots)
            {
                try { MakeShots(seed, shotDir); }
                catch (Exception e) { Log.W("shots failed: " + e); Console.WriteLine(e.ToString()); }
                Log.Close();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GameForm(seed, nosound, false));
            Log.Close();
        }

        static void Sh(int w, int h) { }

        static void MakeShots(int seed, string dir)
        {
            Directory.CreateDirectory(dir);
            Assets.Load();
            TexLib.Build();
            WorldBuilder.Build(seed);
            Game g = new Game(seed);
            // 1 title
            g.ResetToTitle();
            for (int i = 0; i < 30; i++) g.Update(0.033);
            g.Render(0.033);
            g.SaveShot(Path.Combine(dir, "01_title.png"));

            // 2 difficulty
            g.Screen = GScreen.Diff;
            g.DiffIdxMenu = 1;
            g.Render(0.033);
            g.SaveShot(Path.Combine(dir, "02_diff.png"));

            // 3 intro
            g.Screen = GScreen.Intro;
            g.Render(0.033);
            g.SaveShot(Path.Combine(dir, "03_intro.png"));

            // 4 gameplay in hall
            g.StartGame();
            g.Screen = GScreen.Play;
            g.P.X = 15.5; g.P.Y = 27.0; g.P.Angle = -M.PI / 2;
            g.Cam.FlashBattery = 1; g.Cam.Flashlight = true;
            g.Inv.Add(Items.KEY); g.Inv.Add(Items.MILK);
            g.Locks[0] = true;
            for (int i = 0; i < 3; i++) g.Update(0.016);
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "04_hall.png"));

            // 5 living room with granny
            g.P.X = 7.5; g.P.Y = 22.4; g.P.Angle = -M.PI / 2; g.P.Level = 0; g.Cam.Pitch = -6;
            g.G.Level = 0; g.G.X = 7.5; g.G.Y = 19.0; g.G.Angle = M.PI / 2; g.G.State = GState.Chase;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "05_granny.png"));

            // 6 granny close
            g.G.X = 7.5; g.G.Y = 20.4; g.G.State = GState.Chase;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "06_granny_close.png"));

            // 7 attic + cat
            g.P.Level = 1; g.P.X = 9.5; g.P.Y = 8.6; g.P.Angle = -M.PI / 2; g.Cam.Pitch = 4;
            g.Marusya.X = 9.5; g.Marusya.Y = 5.2;
            g.G.Level = 0;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "07_attic.png"));

            // 8 inventory / hud
            g.Inv.Add(Items.CROWBAR); g.Inv.Add(Items.NOTE); g.Inv.Add(Items.BATTERY);
            g.P.Level = 0; g.P.X = 24.5; g.P.Y = 9.0; g.P.Angle = 3.1;
            g.G.Level = 0; g.G.X = 24.5; g.G.Y = 5.0;
            g.Locks[1] = true;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "08_hud.png"));

            // 9 caught
            g.Screen = GScreen.Caught; g.CaughtT = 0.35; g.P.Dead = true;
            for (int i = 0; i < 6; i++) { g.Update(0.05); }
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "09_caught.png"));

            // 10 win
            g.Screen = GScreen.Win;
            g.Locks[0] = g.Locks[1] = true; g.Locks[2] = true;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "10_win.png"));

            // 11 pause
            g.Screen = GScreen.Play; g.P.Dead = false;
            g.Render(0.016);
            g.Screen = GScreen.Pause;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "11_pause.png"));

            // 12 help
            g.Screen = GScreen.Help;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "12_help.png"));

            // 13 trap in front
            g.Screen = GScreen.Play; g.P.Level = 0; g.P.X = 15.5; g.P.Y = 15.0; g.P.Angle = M.PI / 2;
            g.Cam.Pitch = 22;
            g.Levels[0].Traps.Add(new Trap(15.5, 16.6));
            g.Levels[0].RebuildTrapGrid();
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "13_trap.png"));

            // 14 hiding in closet
            HideSpot hs2 = g.Levels[0].Hides[0];
            g.P.Hidden = true; g.P.HideSpot = hs2;
            g.P.X = hs2.EnterX + 0.5; g.P.Y = hs2.EnterY + 0.5;
            g.P.Angle = -M.PI / 2;
            g.Cam.Hiding = 1;
            g.Render(0.016);
            g.SaveShot(Path.Combine(dir, "14_hide.png"));
            g.P.Hidden = false; g.P.HideSpot = null; g.Cam.Hiding = 0;

            Log.W("shots complete");
        }
    }
}
