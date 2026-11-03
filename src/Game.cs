// Game.cs - game rules, player, interaction, screens state machine
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Granny13
{
    public static class GScreen
    {
        public const int Title = 0, Diff = 1, Intro = 2, Play = 3, Pause = 4, Caught = 5, Win = 6, Over = 7, Help = 8, Settings = 9;
    }

    public class Difficulty
    {
        public string Name, Desc;
        public double Walk, Run, Sight, HearMul, HideCheck, CatRange, Battery;
        public int Traps, Days;
        public Difficulty(string n, string d, double walk, double run, double sight, double hear,
            double hideCheck, double catRange, int traps, int days, double battery)
        {
            Name = n; Desc = d; Walk = walk; Run = run; Sight = sight; HearMul = hear;
            HideCheck = hideCheck; CatRange = catRange; Traps = traps; Days = days; Battery = battery;
        }
        public static Difficulty[] All = new Difficulty[] {
            new Difficulty("ЛЕГКО", "Бабушка сегодня добрая: медленная, слепая и почти не ставит капканы.", 1.35, 1.95, 7.0, 0.75, 0.12, 2.2, 1, 7, 240),
            new Difficulty("НОРМАЛЬНО", "Классика: она слышит шаги, проверяет шкафы и ставит капканы.", 1.62, 2.30, 9.0, 1.0, 0.35, 3.0, 3, 5, 170),
            new Difficulty("СЛОЖНО", "Она быстрее тебя и знает дом лучше. Молись, чтобы не скрипнула половица.", 1.85, 2.55, 11.0, 1.28, 0.7, 3.6, 5, 4, 120),
            new Difficulty("КОШМАР", "Ты уже мёртв — просто ещё не знаешь. Один день, ноль шансов на ошибку.", 2.08, 2.85, 13.0, 1.55, 1.1, 4.4, 7, 2, 95)
        };
    }

    public class Player
    {
        public double X, Y, Angle, Pitch;
        public int Level;
        public bool Crouch, Hidden, Dead;
        public HideSpot HideSpot;
        public double Stamina = 1, Health = 1, Battery = 1;
        public double Bob, StepAcc, TrapStuck, TrapMash;
        public double MoveSpeed;
        public double FlashOn = 1;
    }

    public class Msg
    {
        public string Text;
        public double T, Dur;
        public bool Granny;
        public Msg(string t, double dur, bool granny) { Text = t; Dur = dur; T = 0; Granny = granny; }
    }

    public class Game
    {
        public const int OW = 1280, OH = 720;
        public int IW = 640, IH = 360;

        public Renderer R;
        public Bitmap Back;
        public Bitmap FbBmp;
        public Graphics Bg;
        public Camera Cam = new Camera();
        public Player P = new Player();
        public Granny G = new Granny();
        public Cat Marusya = new Cat();
        public Level[] Levels;
        public Level Lv { get { return Levels[P.Level]; } }

        public int Screen = GScreen.Title;
        public int DiffIdx = 1;
        public Difficulty Diff;
        public int Day = 1;
        public int MaxDays = 5;
        public double Time, PlayTime;
        public List<int> Inv = new List<int>();
        public bool[] Locks = new bool[3];
        public List<Noise> Noises = new List<Noise>();
        public List<Msg> Subs = new List<Msg>();
        public double NoiseSeen;
        public Rng Rand = new Rng(99);
        public bool Minimap = true;
        public double AtticAlertT;
        public bool NoiseCrossLevel = true;

        public int TargetKind;           // 0 none 1 door 2 container 3 hide 4 hole 5 front 6 cat
        public object TargetObj;
        public string Prompt = "";
        public double InteractProg;
        public string InteractLabel = "";
        public double InteractFlash;
        public string Toast = "";
        public double ToastT;
        public int MenuIdx;
        public int DiffIdxMenu;
        public int PauseIdx;
        public int SettingsIdx;
        public double CaughtT, WinT, FadeT;
        public double AmbientT;
        public double HeartT;
        public double CatMeowT = 12;
        public double TitleT;
        public int Seed;
        public bool ShowDebug;
        public double CamHeight = 0.5;
        public double GrabT;
        public int Deaths;
        public double RunBeat;

        public List<SpriteInst> Sprites = new List<SpriteInst>();
        public string[] MenuItems = { "НОВАЯ ИГРА", "СЛОЖНОСТЬ", "КАК ИГРАТЬ", "ВЫХОД" };
        public string[] PauseItems = { "ПРОДОЛЖИТЬ", "СЛОЖНОСТЬ", "В ГЛАВНОЕ МЕНЮ", "ВЫХОД ИЗ ИГРЫ" };

        public Game(int seed)
        {
            Seed = seed;
            Assets.Load();
            TexLib.Build();
            WorldBuilder.Build(seed);
            Levels = new Level[] { WorldBuilder.House, WorldBuilder.Attic };
            SetupFramebuffers();
            Diff = Difficulty.All[DiffIdx];
            ResetToTitle();
        }

        void SetupFramebuffers()
        {
            R = new Renderer(IW, IH);
            FbBmp = new Bitmap(IW, IH, PixelFormat.Format32bppArgb);
            Back = new Bitmap(OW, OH, PixelFormat.Format32bppArgb);
            Bg = Graphics.FromImage(Back);
            Bg.SmoothingMode = SmoothingMode.AntiAlias;
            Bg.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        }

        // ------------------------------------------------------------------ setup
        public void ResetToTitle()
        {
            Screen = GScreen.Title;
            TitleT = 0;
            P.Level = 0;
            P.X = 15.5; P.Y = 27.2; P.Angle = -M.PI / 2 + 0.06; P.Pitch = -6;
            Cam.Flashlight = false; Cam.FlashBattery = 0;
            G.Spawn(Levels[0], 15.5, 21.5, M.PI / 2);
            G.Angle = M.PI / 2;
            Cam.X = P.X; Cam.Y = P.Y; Cam.Angle = P.Angle; Cam.Pitch = P.Pitch;
            MenuIdx = 0;
            Subs.Clear();
        }

        public void StartGame()
        {
            Diff = Difficulty.All[DiffIdx];
            MaxDays = Diff.Days;
            Day = 1;
            Inv.Clear();
            Locks = new bool[3];
            Noises.Clear();
            Subs.Clear();
            PlayTime = 0;
            Deaths = 0;
            TargetKind = 0;
            InteractProg = 0;
            Screen = GScreen.Intro;
            FadeT = 0;
            // reset world objects
            for (int i = 0; i < Levels.Length; i++)
            {
                Level l = Levels[i];
                l.Traps.Clear();
                l.RebuildTrapGrid();
                for (int k = 0; k < l.Containers.Count; k++) l.Containers[k].Searched = false;
                for (int k = 0; k < l.Doors.Count; k++) l.Doors[k].Open = false;
            }
            // granny starts placing traps per difficulty
            PlaceStartingTraps();
            RespawnPlayer();
            Marusya.Reset(this);
            Log.W("game start diff=" + Diff.Name + " traps=" + Levels[0].Traps.Count);
        }

        void PlaceStartingTraps()
        {
            Level l = Levels[0];
            double[][] spots = new double[][] {
                new double[]{ 7.5, 15.5 }, new double[]{ 23.5, 15.5 }, new double[]{ 15.5, 12.5 },
                new double[]{ 15.5, 24.5 }, new double[]{ 9.5, 21.5 }, new double[]{ 25.5, 21.5 },
                new double[]{ 4.5, 15.5 }, new double[]{ 27.5, 15.5 }, new double[]{ 6.5, 9.5 },
                new double[]{ 22.5, 9.5 }
            };
            int n = Math.Min(Diff.Traps, spots.Length);
            for (int i = 0; i < n; i++)
            {
                int k = Rand.Range(0, spots.Length);
                l.Traps.Add(new Trap(spots[k][0], spots[k][1]));
            }
            l.RebuildTrapGrid();
        }

        public void RespawnPlayer()
        {
            P.Level = 0;
            P.X = 15.5; P.Y = 27.5; P.Angle = -M.PI / 2;
            P.Pitch = 0;
            P.Health = 1;
            P.Stamina = 1;
            P.Battery = 1;
            P.Dead = false;
            P.Hidden = false;
            P.HideSpot = null;
            P.TrapStuck = 0;
            P.Crouch = false;
            Cam.Flashlight = true;
            Cam.FlashBattery = 1;
            Cam.Hiding = 0;
            Cam.Shake = 0;
            Cam.GrannyProximity = 0;
            Cam.Hp = 1;
            Cam.Pitch = 0;
            G.Spawn(Levels[0], 24.5, 7.5, M.PI * 0.5);
            G.TrapT = 10 + Rand.Next() * 8;
            TargetKind = 0;
            InteractProg = 0;
            AtticAlertT = 0;
        }

        // ------------------------------------------------------------------ input
        public void OnKey(Keys k, bool down)
        {
            if (!down) return;
            switch (Screen)
            {
                case GScreen.Title:
                    if (k == Keys.Up || k == Keys.W) MenuIdx = (MenuIdx + MenuItems.Length - 1) % MenuItems.Length;
                    else if (k == Keys.Down || k == Keys.S) MenuIdx = (MenuIdx + 1) % MenuItems.Length;
                    else if (k == Keys.Enter || k == Keys.Space)
                    {
                        Audio.Play(Sfx.Lock, 0.4f, 0.7f, false);
                        if (MenuIdx == 0) StartGame();
                        else if (MenuIdx == 1) { Screen = GScreen.Diff; DiffIdxMenu = DiffIdx; }
                        else if (MenuIdx == 2) Screen = GScreen.Help;
                        else Application.Exit();
                    }
                    break;
                case GScreen.Diff:
                    if (k == Keys.Up || k == Keys.W) DiffIdxMenu = (DiffIdxMenu + 3) % 4;
                    else if (k == Keys.Down || k == Keys.S) DiffIdxMenu = (DiffIdxMenu + 1) % 4;
                    else if (k == Keys.Enter || k == Keys.Space) { DiffIdx = DiffIdxMenu; Diff = Difficulty.All[DiffIdx]; Screen = GScreen.Title; }
                    else if (k == Keys.Escape) Screen = GScreen.Title;
                    break;
                case GScreen.Help:
                    if (k == Keys.Escape || k == Keys.Enter || k == Keys.Space) Screen = GScreen.Title;
                    break;
                case GScreen.Intro:
                    if (k == Keys.Enter || k == Keys.Space || k == Keys.Escape) { Screen = GScreen.Play; FadeT = 1; PushSubtitle("Найди три ключа и открой входную дверь.", 4.0, false); }
                    break;
                case GScreen.Play:
                    if (k == Keys.Escape) { Screen = GScreen.Pause; PauseIdx = 0; }
                    else if (k == Keys.E)
                    {
                        if (P.Hidden && Input.Hit(Keys.E)) { }
                    }
                    else if (k == Keys.M) Minimap = !Minimap;
                    else if (k == Keys.F) ToggleFlash();
                    else if (k == Keys.Space)
                    {
                        if (P.TrapStuck > 0) { P.TrapStuck -= 0.22; P.TrapMash += 0.22; Audio.Play(Sfx.Step2, 0.5f, 1.6f, false); }
                    }
                    break;
                case GScreen.Pause:
                    if (k == Keys.Escape) Screen = GScreen.Play;
                    else if (k == Keys.Up || k == Keys.W) PauseIdx = (PauseIdx + 3) % 4;
                    else if (k == Keys.Down || k == Keys.S) PauseIdx = (PauseIdx + 1) % 4;
                    else if (k == Keys.Enter || k == Keys.Space)
                    {
                        if (PauseIdx == 0) Screen = GScreen.Play;
                        else if (PauseIdx == 1) { Screen = GScreen.Diff; DiffIdxMenu = DiffIdx; }
                        else if (PauseIdx == 2) ResetToTitle();
                        else Application.Exit();
                    }
                    break;
                case GScreen.Win:
                case GScreen.Over:
                    if (k == Keys.Enter || k == Keys.Space || k == Keys.Escape) ResetToTitle();
                    break;
                case GScreen.Caught:
                    break;
            }
        }

        public void ToggleFlash()
        {
            if (P.Battery <= 0.02) { Toast = "Фонарик сел. Нужна батарейка."; ToastT = 2.4; return; }
            Cam.Flashlight = !Cam.Flashlight;
            Audio.Play(Sfx.Lock, 0.3f, 1.3f, false);
        }

        public void PushSubtitle(string text, double dur, bool granny)
        {
            if (Subs.Count > 0 && Subs[Subs.Count - 1].Text == text) { Subs[Subs.Count - 1].T = 0; return; }
            Subs.Add(new Msg(text, dur, granny));
            if (Subs.Count > 4) Subs.RemoveAt(0);
        }

        public void EmitNoise(double x, double y, double r, string what)
        {
            Noises.Add(new Noise(x, y, r, what, Time));
            if (Levels[0].Id == 0 && P.Level == 1) AtticAlertT = Math.Max(AtticAlertT, 6.0);
            if (P.Level == 1) AtticAlertT = Math.Max(AtticAlertT, 6.0);
        }

        // ------------------------------------------------------------------ update
        public void Update(double dt)
        {
            if (dt > 0.08) dt = 0.08;
            Time += dt;
            switch (Screen)
            {
                case GScreen.Title:
                    UpdateTitle(dt);
                    break;
                case GScreen.Diff:
                    break;
                case GScreen.Intro:
                    break;
                case GScreen.Play:
                    UpdatePlay(dt);
                    break;
                case GScreen.Pause:
                    break;
                case GScreen.Caught:
                    UpdateCaught(dt);
                    break;
                case GScreen.Win:
                case GScreen.Over:
                    WinT += dt;
                    break;
            }
            // subtitles
            for (int i = Subs.Count - 1; i >= 0; i--)
            {
                Subs[i].T += dt;
                if (Subs[i].T > Subs[i].Dur) Subs.RemoveAt(i);
            }
            if (ToastT > 0) ToastT -= dt;
            FadeT = Math.Max(0, FadeT - dt * 1.6);
        }

        void UpdateTitle(double dt)
        {
            TitleT += dt;
            // slow camera drift in the hall
            P.X = 15.5 + Math.Sin(TitleT * 0.16) * 0.35;
            P.Y = 27.0;
            P.Angle = -M.PI / 2 - 0.14 + Math.Sin(TitleT * 0.11) * 0.03;
            P.Pitch = -10 + Math.Sin(TitleT * 0.23) * 2;
            Cam.Flashlight = true;
            Cam.FlashBattery = 0.9 + 0.1 * Math.Sin(TitleT * 1.7);
            Cam.Shake = 0.045 * Math.Max(0, Math.Sin(TitleT * 0.7));
            Cam.Hiding = 0;
            Cam.GrannyProximity = 0.25 + 0.1 * Math.Sin(TitleT * 1.2);
            G.AnimT += dt;
            G.X = 16.55 + Math.Sin(TitleT * 0.5) * 0.10;
            G.Y = 23.0;
            G.Angle = M.PI / 2;
            G.State = (Math.Sin(TitleT * 0.21) > 0.55) ? GState.Investigate : GState.Patrol;
            Cam.X = P.X; Cam.Y = P.Y; Cam.Angle = P.Angle; Cam.Pitch = P.Pitch;
        }

        void UpdatePlay(double dt)
        {
            if (P.Dead) return;
            PlayTime += dt;
            Cam.Shake = Math.Max(0, Cam.Shake - dt * 2.2);
            UpdateAtmosphere(dt);
            if (P.Hidden)
            {
                UpdateHidden(dt);
                UpdateSenseCamera(dt);
                G.Update(dt, this);
                return;
            }
            UpdatePlayerMove(dt);
            UpdateTargetScan();
            UpdateInteraction(dt);
            UpdateTraps(dt);
            UpdateCamera(dt);
            UpdateSenseCamera(dt);

            G.Update(dt, this);
            Marusya.Update(dt, this);

            // noise cleanup
            for (int i = Noises.Count - 1; i >= 0; i--)
                if (Time - Noises[i].Time > 2.5) Noises.RemoveAt(i);

            // decay attic alert
            if (AtticAlertT > 0) AtticAlertT -= dt;

            // sounds of fear
            double d = M.Dist(P.X, P.Y, G.X, G.Y);
            double prox = M.Clamp01(1.0 - d / 9.0);
            if (G.Level == P.Level) Cam.GrannyProximity = prox; else Cam.GrannyProximity = 0.15 * prox;
            HeartT -= dt;
            if (HeartT <= 0 && prox > 0.25)
            {
                Audio.Play(Sfx.Heart, 0.35f + prox * 0.55f, 1.0f + prox * 0.25f, false);
                HeartT = M.Lerp(1.25, 0.42, prox);
            }
            if (Cam.Flashlight) Cam.FlashBattery = P.Battery;
        }

        void UpdateAtmosphere(double dt)
        {
            AmbientT -= dt;
            Cam.Flicker = 0.9 + 0.1 * Math.Sin(Time * 13.7) * Math.Sin(Time * 3.1);
            if (AmbientT <= 0)
            {
                AmbientT = 14 + Rand.Next() * 26;
                if (Rand.Next() < 0.5) Audio.Play(Sfx.Creak, 0.22f, 0.55f + Rand.Next() * 0.4, false);
                else if (Rand.Next() < 0.4) Audio.Play(Sfx.Whisper, 0.25f, 0.8f, false);
                else if (Rand.Next() < 0.5) Audio.Play(Sfx.Thunder, 0.28f, 0.8f, false);
            }
        }

        void UpdatePlayerMove(double dt)
        {
            double fwd = 0, strafe = 0;
            if (Input.Key(Keys.W) || Input.Key(Keys.Up)) fwd += 1;
            if (Input.Key(Keys.S) || Input.Key(Keys.Down)) fwd -= 1;
            if (Input.Key(Keys.D) || Input.Key(Keys.Right)) strafe += 1;
            if (Input.Key(Keys.A) || Input.Key(Keys.Left)) strafe -= 1;
            bool wantRun = Input.Key(Keys.ShiftKey) && !P.Crouch;
            bool crouchKey = Input.Key(Keys.ControlKey) || Input.Key(Keys.C);

            if (P.TrapStuck > 0)
            {
                P.TrapStuck -= dt;
                Cam.Shake = Math.Max(Cam.Shake, 0.12);
                if (P.TrapStuck <= 0) { P.TrapStuck = 0; Toast = "Ты вырвался из капкана!"; ToastT = 2.0; }
                fwd = 0; strafe = 0;
            }

            P.Crouch = crouchKey;
            double baseSpeed = P.Crouch ? 1.05 : 2.05;
            bool running = wantRun && P.Stamina > 0.05 && (fwd != 0 || strafe != 0);
            if (running) baseSpeed = 3.45;
            if (P.Crouch) running = false;

            double len = Math.Sqrt(fwd * fwd + strafe * strafe);
            if (len > 0.001) { fwd /= len; strafe /= len; }
            double spd = baseSpeed;
            P.MoveSpeed = spd * len;

            double dirX = Math.Cos(P.Angle), dirY = Math.Sin(P.Angle);
            double dx = (dirX * fwd - dirY * strafe) * spd * dt;
            double dy = (dirY * fwd + dirX * strafe) * spd * dt;
            if (len > 0)
            {
                TryMove(dx, dy);
                P.Bob += dt * (running ? 11.5 : P.Crouch ? 4.2 : 7.4);
                P.StepAcc += dt * (running ? 3.4 : P.Crouch ? 1.2 : 2.15);
                if (P.StepAcc > 1.0)
                {
                    P.StepAcc = 0;
                    float v = running ? 0.5f : P.Crouch ? 0.12f : 0.28f;
                    Audio.Play(Rand.Next() < 0.5 ? Sfx.Step : Sfx.Step2, v, 0.85f + (float)Rand.Next() * 0.3f, false);
                    if (running) EmitNoise(P.X, P.Y, 6.2, "шаги");
                    else if (!P.Crouch) EmitNoise(P.X, P.Y, 2.6, "шаги");
                    else if (Rand.Next() < 0.35) EmitNoise(P.X, P.Y, 0.9, "скрип");
                }
            }
            // stamina
            if (running && len > 0) P.Stamina = Math.Max(0, P.Stamina - dt * 0.17);
            else P.Stamina = Math.Min(1, P.Stamina + dt * (P.Crouch ? 0.36 : 0.22));

            // flashlight battery
            if (Cam.Flashlight)
            {
                P.Battery = Math.Max(0, P.Battery - dt / Diff.Battery);
                if (P.Battery <= 0)
                {
                    Cam.Flashlight = false;
                    Toast = "Фонарик погас. Найди батарейку!";
                    ToastT = 3.0;
                }
            }
            Cam.FlashBattery = P.Battery;

            // mouse look
            double lookSpeed = Input.MouseSensitivity;
            P.Angle += Input.MouseDX * 0.0022 * lookSpeed;
            P.Pitch -= Input.MouseDY * 1.15 * lookSpeed;
            double kb = 2.1 * dt;
            if (Input.Key(Keys.Q)) P.Angle -= kb;
            if (Input.Key(Keys.R)) P.Angle += kb;
            P.Pitch = M.Clamp(P.Pitch, -140, 140);
            P.Angle = M.AngleNorm(P.Angle);
        }

        void TryMove(double dx, double dy)
        {
            double r = 0.24;
            double nx = P.X + dx;
            if (!Blocked(nx, P.Y, r)) P.X = nx;
            double ny = P.Y + dy;
            if (!Blocked(P.X, ny, r)) P.Y = ny;
        }

        bool Blocked(double x, double y, double r)
        {
            if (Lv.SolidAt(x - r, y - r) || Lv.SolidAt(x + r, y - r) ||
                Lv.SolidAt(x - r, y + r) || Lv.SolidAt(x + r, y + r)) return true;
            for (int i = 0; i < Lv.Decos.Count; i++)
            {
                Deco d = Lv.Decos[i];
                if (!d.Solid) continue;
                if (Math.Abs(x - d.X) < d.RadiusX + r && Math.Abs(y - d.Y) < d.RadiusY + r) return true;
            }
            return false;
        }

        void UpdateCamera(double dt)
        {
            Cam.X = P.X; Cam.Y = P.Y;
            Cam.Angle = P.Angle;
            double bob = Math.Sin(P.Bob * 2.0) * 0.012 * M.Clamp01(P.MoveSpeed / 2.0);
            Cam.Pitch = P.Pitch + Math.Sin(P.Bob) * 2.2 * M.Clamp01(P.MoveSpeed / 3.0);
            Cam.EyeH = 0.5 + bob - (P.Crouch ? 0.14 : 0.0);
            Cam.Hp = P.Health;
        }

        void UpdateSenseCamera(double dt)
        {
            if (P.Hidden) { Cam.Hiding = Math.Min(1, Cam.Hiding + dt * 4); }
            else Cam.Hiding = Math.Max(0, Cam.Hiding - dt * 6);
        }

        // ------------------------------------------------------------------ hidden
        void EnterHide(HideSpot h)
        {
            P.Hidden = true;
            P.HideSpot = h;
            P.Crouch = true;
            Cam.Shake = 0.2;
            Audio.Play(Sfx.Creak, 0.45f, 1.15f, false);
            EmitNoise(P.X, P.Y, 2.2, "шкаф");
            // if granny is watching -> she knows
            if (G.Level == P.Level && G.SawPlayer)
            {
                G.State = GState.Chase;
                G.TargetX = h.X; G.TargetY = h.Y;
                G.Say(this, "Я видела, куда ты залез, заинька!", 2.0);
            }
            Toast = "Сиди тихо. Не двигайся.";
            ToastT = 2.6;
        }

        void ExitHide()
        {
            P.Hidden = false;
            P.HideSpot = null;
            Audio.Play(Sfx.Creak, 0.45f, 0.9f, false);
            EmitNoise(P.X, P.Y, 2.4, "шкаф");
            if (P.HideSpot != null) { }
        }

        void UpdateHidden(double dt)
        {
            HideSpot h = P.HideSpot;
            if (h == null) { P.Hidden = false; return; }
            P.X = h.EnterX + 0.5;
            P.Y = h.EnterY + 0.5;
            P.Angle += Input.MouseDX * 0.0018 + (Input.Key(Keys.D) || Input.Key(Keys.Right) ? dt * 1.6 : 0) - (Input.Key(Keys.A) || Input.Key(Keys.Left) ? dt * 1.6 : 0);
            P.Angle = M.Clamp(M.AngleNorm(P.Angle), -1.1, 1.1);
            if (Input.Hit(Keys.E)) ExitHide();
            Cam.X = P.X; Cam.Y = P.Y;
            Cam.Angle = P.Angle;
            Cam.EyeH = h.IsBed ? 0.22 : 0.5;
            Cam.Pitch = P.Pitch * 0.4;
            if (Input.Key(Keys.Space)) { }
        }

        // ------------------------------------------------------------------ target
        void UpdateTargetScan()
        {
            TargetKind = 0; TargetObj = null; Prompt = ""; InteractLabel = "";
            double dirX = Math.Cos(P.Angle), dirY = Math.Sin(P.Angle);
            for (double d = 0.3; d <= 2.1; d += 0.1)
            {
                double tx = P.X + dirX * d, ty = P.Y + dirY * d;
                int ix = (int)Math.Floor(tx), iy = (int)Math.Floor(ty);
                byte t = Lv.Get(ix, iy);
                if (t == T.DOOR)
                {
                    Door dr = Lv.DoorAt(ix, iy);
                    TargetKind = 1; TargetObj = dr;
                    Prompt = (dr != null && dr.Open) ? "Закрыть дверь" : "Открыть дверь";
                    InteractLabel = "";
                    return;
                }
                if (t == T.FRONT)
                {
                    TargetKind = 5; TargetObj = Lv.DoorAt(ix, iy);
                    int n = 0; for (int i = 0; i < 3; i++) if (Locks[i]) n++;
                    Prompt = "Входная дверь  (" + n + "/3 замка)";
                    return;
                }
                if (t == T.HOLE)
                {
                    TargetKind = 4;
                    Prompt = P.Level == 0 ? "Залезть на чердак" : "Спуститься в дом";
                    return;
                }
                Container c = Lv.ContainerAt(ix, iy);
                if (c != null)
                {
                    TargetKind = 2; TargetObj = c;
                    Prompt = c.Searched ? (T.Name[c.Tile] + " — пусто") : ("Обыскать: " + c.Label);
                    if (c.Searched) TargetKind = 0;
                    return;
                }
                HideSpot hs = Lv.HideAt(ix, iy);
                if (hs != null)
                {
                    TargetKind = 3; TargetObj = hs;
                    Prompt = "Спрятаться: " + hs.Label;
                    return;
                }
                if (T.Solid[t]) return;
            }
            // cat (only when not aiming at furniture)
            if (P.Level == 1 && Marusya != null && !Marusya.Fed && M.Dist(P.X, P.Y, Marusya.X, Marusya.Y) < 1.7)
            {
                TargetKind = 6; TargetObj = Marusya;
                Prompt = HasItem(Items.MILK) ? "Дать Марусе молока" : "Маруся шипит на тебя";
                return;
            }
            // bed (deco) check
            for (int i = 0; i < Lv.Decos.Count; i++)
            {
                Deco dc = Lv.Decos[i];
                if (dc.Kind != 0) continue;
                if (M.Dist(P.X, P.Y, dc.X, dc.Y) < 1.9)
                {
                    HideSpot bed = null;
                    for (int k = 0; k < Lv.Hides.Count; k++) if (Lv.Hides[k].IsBed) bed = Lv.Hides[k];
                    if (bed != null)
                    {
                        TargetKind = 3; TargetObj = bed; Prompt = "Спрятаться под кроватью"; return;
                    }
                }
            }
        }

        void UpdateInteraction(double dt)
        {
            bool holdE = Input.Key(Keys.E);
            if (InteractFlash > 0) InteractFlash -= dt;
            switch (TargetKind)
            {
                case 1:
                    {
                        Door d = (Door)TargetObj;
                        if (Input.Hit(Keys.E))
                        {
                            d.Open = !d.Open;
                            Audio.Play(d.Open ? Sfx.Creak : Sfx.DoorSlam, 0.55f, 1.0f, false);
                            EmitNoise(d.X + 0.5, d.Y + 0.5, 7.5, "дверь");
                        }
                        break;
                    }
                case 5:
                    {
                        int n = 0; for (int i = 0; i < 3; i++) if (Locks[i]) n++;
                        if (n >= 3)
                        {
                            if (Input.Hit(Keys.E)) WinGame();
                        }
                        else
                        {
                            int nextLock = -1;
                            for (int i = 0; i < 3; i++) if (!Locks[i]) { nextLock = i; break; }
                            string need = nextLock == 0 ? "Ключ от замка" : nextLock == 1 ? "Монтировка" : "Записка с кодом";
                            int needItem = nextLock == 0 ? Items.KEY : nextLock == 1 ? Items.CROWBAR : Items.NOTE;
                            if (holdE && HasItem(needItem))
                            {
                                InteractProg += dt / 1.4;
                                InteractLabel = "Открываю замок...";
                                if (InteractProg >= 1)
                                {
                                    InteractProg = 0;
                                    Locks[nextLock] = true;
                                    Audio.Play(Sfx.Unlock, 0.7f, 1.0f, false);
                                    EmitNoise(P.X, P.Y, 7.0, "замок");
                                    Toast = "Замок открыт! (" + (CountLocks()) + "/3)";
                                    ToastT = 2.6;
                                }
                            }
                            else
                            {
                                InteractProg = Math.Max(0, InteractProg - dt * 1.5);
                                if (Input.Hit(Keys.E)) { Toast = "Нужно: " + need; ToastT = 2.2; }
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        Container c = (Container)TargetObj;
                        if (holdE && !c.Searched)
                        {
                            InteractProg += dt / 0.95;
                            InteractLabel = "Обыскиваю...";
                            if (InteractProg >= 1)
                            {
                                InteractProg = 0;
                                c.Searched = true;
                                Audio.Play(Sfx.Search, 0.6f, 1.0f, false);
                                EmitNoise(P.X, P.Y, 3.4, "обыск");
                                if (c.ItemId != Items.NONE)
                                {
                                    Inv.Add(c.ItemId);
                                    Audio.Play(Sfx.Pickup, 0.7f, 1.0f, false);
                                    Toast = "+ " + Items.Name(c.ItemId);
                                    ToastT = 3.0;
                                    PushSubtitle(Items.Hint(c.ItemId), 3.4, false);
                                    if (c.ItemId == Items.BATTERY) { P.Battery = Math.Min(1, P.Battery + 0.55); }
                                    if (c.ItemId == Items.BANDAGE) { P.Health = Math.Min(1, P.Health + 0.5); }
                                }
                                else
                                {
                                    Toast = "Пусто...";
                                    ToastT = 1.8;
                                }
                            }
                        }
                        else InteractProg = Math.Max(0, InteractProg - dt * 1.6);
                        break;
                    }
                case 3:
                    {
                        HideSpot h = (HideSpot)TargetObj;
                        if (Input.Hit(Keys.E)) EnterHide(h);
                        break;
                    }
                case 4:
                    {
                        if (holdE)
                        {
                            InteractProg += dt / 0.7;
                            InteractLabel = P.Level == 0 ? "Лезем на чердак..." : "Спускаемся...";
                            if (InteractProg >= 1) { InteractProg = 0; SwitchLevel(); }
                        }
                        else InteractProg = Math.Max(0, InteractProg - dt * 1.6);
                        break;
                    }
                case 6:
                    {
                        if (Input.Hit(Keys.E))
                        {
                            if (Marusya.Fed) { Marusya.PurrNow(this); }
                            else if (HasItem(Items.MILK))
                            {
                                RemoveItem(Items.MILK);
                                Marusya.Feed(this);
                                Toast = "Маруся мурчит и засыпает.";
                                ToastT = 3.0;
                            }
                            else
                            {
                                Marusya.Alert(this, "шипение");
                                Toast = "Маруся недовольна. Лучше принести ей молока.";
                                ToastT = 2.6;
                            }
                        }
                        break;
                    }
            }
            if (TargetKind != 2 && TargetKind != 4 && TargetKind != 5) InteractProg = Math.Max(0, InteractProg - dt * 3);
        }

        void SwitchLevel()
        {
            if (P.Level == 0)
            {
                P.Level = 1;
                P.X = 4.5; P.Y = 19.0; P.Angle = -M.PI / 2;
                Audio.Play(Sfx.Climb, 0.6f, 1.0f, false);
                EmitNoise(15.5, 3.5, 6.0, "лестница");
                PushSubtitle("Чердак. Пахнет пылью и кошкой.", 3.2, false);
                if (!Marusya.Fed && !Marusya.Angry) Marusya.Alert(this, "мяу");
            }
            else
            {
                P.Level = 0;
                P.X = 15.5; P.Y = 4.0; P.Angle = M.PI / 2;
                Audio.Play(Sfx.Climb, 0.6f, 0.9f, false);
                EmitNoise(15.5, 3.5, 6.0, "лестница");
            }
            P.Pitch = 0;
            InteractProg = 0;
            Cam.Shake = 0.15;
        }

        void UpdateTraps(double dt)
        {
            if (P.TrapStuck > 0) return;
            Trap t = Lv.TrapNear(P.X, P.Y, 0.3);
            if (t != null && t.Armed)
            {
                t.Armed = false;
                t.TriggeredT = Time;
                P.TrapStuck = 2.6;
                P.TrapMash = 0;
                P.Health = Math.Max(0.05, P.Health - 0.28);
                Audio.Play(Sfx.Snap, 0.9f, 1.0f, false);
                Audio.Play(Sfx.Scream, 0.25f, 1.9f, false);
                Cam.AddShake(1.2);
                EmitNoise(P.X, P.Y, 13.0, "капкан");
                Toast = "КАТКАН! Жми ПРОБЕЛ, чтобы вырваться!";
                ToastT = 3.0;
                Lv.RebuildTrapGrid();
            }
        }

        public int CountLocks() { int n = 0; for (int i = 0; i < 3; i++) if (Locks[i]) n++; return n; }
        public bool HasItem(int id) { return Inv.Contains(id); }
        public void RemoveItem(int id) { Inv.Remove(id); }

        // ------------------------------------------------------------------ events
        public void OnCaught()
        {
            if (Screen != GScreen.Play) return;
            Screen = GScreen.Caught;
            CaughtT = 0;
            P.Dead = true;
            Deaths++;
            Cam.AddShake(1.5);
            Audio.Play(Sfx.Scream, 1.0f, 0.95f, false);
            Audio.Play(Sfx.Snap, 0.6f, 0.7f, false);
            G.Say(this, "ПОПАЛСЯ, СОЛНЫШКО!", 2.0);
            Log.W("caught day " + Day);
        }

        public void OnCaughtInHide()
        {
            if (Screen != GScreen.Play) return;
            P.Hidden = false;
            P.HideSpot = null;
            G.X = P.X + Math.Cos(P.Angle) * 0.7;
            G.Y = P.Y + Math.Sin(P.Angle) * 0.7;
            G.Angle = P.Angle + M.PI;
            OnCaught();
            G.Say(this, "Я знала, что ты там!", 2.0);
        }

        public void WinGame()
        {
            if (Screen != GScreen.Play) return;
            Screen = GScreen.Win;
            WinT = 0;
            Audio.Play(Sfx.Win, 0.9f, 1.0f, false);
            for (int i = 0; i < Levels[0].Doors.Count; i++) if (Levels[0].Doors[i].Front) Levels[0].Doors[i].Open = true;
            Log.W("win day " + Day + " time " + PlayTime.ToString("F1"));
        }

        void UpdateCaught(double dt)
        {
            CaughtT += dt;
            Cam.Shake = Math.Max(0, Cam.Shake - dt * 1.2);
            // granny lunges at camera
            double d = Math.Max(0.42, 1.25 - CaughtT * 0.55);
            P.Angle = Math.Atan2(G.Y - P.Y, G.X - P.X);
            double dirX = Math.Cos(P.Angle), dirY = Math.Sin(P.Angle);
            G.X = P.X + dirX * d;
            G.Y = P.Y + dirY * d;
            G.Angle = P.Angle + M.PI;
            Cam.X = P.X; Cam.Y = P.Y; Cam.Angle = P.Angle;
            Cam.Pitch = Math.Sin(CaughtT * 9) * 4;
            Cam.Shake = 0.5 * Math.Max(0, 1.4 - CaughtT);
            if (CaughtT > 2.9)
            {
                Day++;
                if (Day > MaxDays)
                {
                    Screen = GScreen.Over;
                    WinT = 0;
                    Audio.Play(Sfx.Lose, 0.8f, 1.0f, false);
                }
                else
                {
                    RespawnPlayer();
                    Screen = GScreen.Play;
                    PushSubtitle("День " + Day + ". Она снова где-то в доме...", 4.0, false);
                    Audio.Play(Sfx.Thunder, 0.5f, 1.0f, false);
                }
            }
        }

        // ------------------------------------------------------------------ render
        public void Render(double dt)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            Cam.X = P.X; Cam.Y = P.Y; Cam.Angle = P.Angle;
            BuildSprites();
            R.Render(CurrentLevel(), Cam, Sprites, Time);
            R.F.Present2x(Back, true);
            MsWorld = sw.Elapsed.TotalMilliseconds;
            sw.Restart();
            UI.Draw(Bg, this, OW, OH);
            MsUi = sw.Elapsed.TotalMilliseconds;
        }

        public double MsWorld, MsUi;

        public Level CurrentLevel()
        {
            int li = M.ClampI(P.Level, 0, Levels.Length - 1);
            return Levels[li];
        }

        void BuildSprites()
        {
            Sprites.Clear();
            Level lv = CurrentLevel();
            for (int i = 0; i < lv.Decos.Count; i++)
            {
                Deco d = lv.Decos[i];
                SpriteInst s = new SpriteInst();
                s.X = d.X; s.Y = d.Y; s.Img = d.Img; s.Height = d.Height;
                s.Elev = 0;
                Sprites.Add(s);
            }
            // granny
            if (G.Level == P.Level && Screen != GScreen.Title)
            {
                SpriteInst s = new SpriteInst();
                s.X = G.X; s.Y = G.Y;
                s.Img = Assets.Granny;
                s.Height = 1.82;
                double bob = Math.Sin(G.Bob) * 0.012;
                s.Elev = -bob;
                if (G.State == GState.Chase) { s.Height = 1.86; }
                s.TintColor = 0x883333;
                s.TintAmount = G.State == GState.Chase ? 0.10f : 0.02f;
                Sprites.Add(s);
                if (TexLib.Glow != null)
                {
                    double dx = Cam.X - G.X, dy = Cam.Y - G.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 0.001 && len < 16)
                    {
                        double px = -dy / len, py = dx / len;
                        double hx = G.X + dx / len * 0.12, hy = G.Y + dy / len * 0.12;
                        float pulse = (float)(0.72 + 0.28 * Math.Sin(Time * 7.0));
                        for (int e = -1; e <= 1; e += 2)
                        {
                            SpriteInst gs = new SpriteInst();
                            gs.X = hx + px * 0.072 * e;
                            gs.Y = hy + py * 0.072 * e;
                            gs.Img = TexLib.Glow;
                            gs.Height = 0.085;
                            gs.Elev = 1.615;
                            gs.Glow = true;
                            gs.TintColor = 0xFF2A18;
                            gs.TintAmount = 1f;
                            gs.Alpha = (G.State == GState.Chase ? 0.95f : 0.5f) * pulse;
                            Sprites.Add(gs);
                        }
                    }
                }
            }
            if (Screen == GScreen.Title)
            {
                SpriteInst s = new SpriteInst();
                s.X = G.X; s.Y = G.Y; s.Img = Assets.Granny; s.Height = 1.82;
                s.TintColor = 0x552222; s.TintAmount = 0.18f;
                Sprites.Add(s);
            }
            if (P.Level == 1)
            {
                SpriteInst s = new SpriteInst();
                s.X = Marusya.X; s.Y = Marusya.Y;
                s.Img = Marusya.Angry ? Assets.CatAngry : Assets.Cat;
                s.Height = 0.56;
                s.Elev = 0.0;
                s.Flat = false;
                Sprites.Add(s);
            }
        }

        // screenshot helper
        public void SaveShot(string path)
        {
            using (Bitmap b = new Bitmap(Back))
                b.Save(path, ImageFormat.Png);
        }
    }
}
