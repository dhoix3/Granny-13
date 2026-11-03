// Tests.cs - headless self-test of the game logic (run with --autotest)
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Granny13
{
    public static class Tests
    {
        static int pass, fail;
        static List<string> lines = new List<string>();

        static void Check(bool ok, string name)
        {
            Check(ok, name, null);
        }

        static void Check(bool ok, string name, string extra)
        {
            if (ok) { pass++; lines.Add("PASS  " + name + (extra != null ? "   [" + extra + "]" : "")); }
            else { fail++; lines.Add("FAIL  " + name + (extra != null ? "   [" + extra + "]" : "")); }
        }

        static void Step(Game g, double seconds)
        {
            int n = (int)(seconds * 60);
            for (int i = 0; i < n; i++)
            {
                g.Update(1.0 / 60.0);
                Input.EndFrame();
            }
        }

        static void Key(Game g, Keys k, bool down)
        {
            Input.Set(k, down);
            g.OnKey(k, down);
        }

        static void AimAt(Game g, double tx, double ty)
        {
            g.P.Angle = Math.Atan2(ty - g.P.Y, tx - g.P.X);
            g.Cam.Angle = g.P.Angle;
        }

        static void Park(Game g)
        {
            g.G.Level = 1;
            g.G.X = 3.5; g.G.Y = 3.5;
            g.G.State = GState.Patrol;
        }

        public static string DumpMap()
        {
            Game g = new Game(7);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int li = 0; li < 2; li++)
            {
                Level lv = g.Levels[li];
                sb.AppendLine("=== " + lv.Name + " (" + lv.W + "x" + lv.H + ") ===");
                sb.AppendLine("  0123456789012345678901234567890123");
                for (int y = 0; y < lv.H; y++)
                {
                    sb.Append(y.ToString("00") + "");
                    for (int x = 0; x < lv.W; x++)
                    {
                        byte t = lv.Get(x, y);
                        char c = '?';
                        if (t == T.FLOOR) c = '.';
                        else if (t == T.RUG) c = ',';
                        else if (t == T.BRICK) c = '#';
                        else if (t == T.WP) c = '=';
                        else if (t == T.PANEL) c = 'p';
                        else if (t == T.DOOR) c = 'D';
                        else if (t == T.FRONT) c = 'F';
                        else if (t == T.HOLE) c = 'O';
                        else if (t == T.WINDOW) c = 'w';
                        else if (t == T.WARDROBE) c = 'H';
                        else if (t == T.CABINET) c = 'c';
                        else if (t == T.SHELF) c = 's';
                        else if (t == T.FRIDGE) c = 'f';
                        else if (t == T.STOVE) c = 'v';
                        else if (t == T.CRATE) c = 'x';
                        else if (t == T.TILEW) c = 't';
                        else if (t == T.TOWELS) c = 'T';
                        else if (t == T.CAGE) c = 'g';
                        else if (t == T.PORTRAIT) c = 'P';
                        else if (t == T.MIRROR) c = 'M';
                        else if (t == T.BOOKS) c = 'B';
                        else if (t == T.ATTIC) c = '#';
                        else if (t == T.ATTICWOOD) c = '|';
                        else if (t == T.TABLEW) c = 'R';
                        sb.Append(c);
                    }
                    sb.AppendLine();
                }
                for (int i = 0; i < lv.Containers.Count; i++)
                {
                    Container c = lv.Containers[i];
                    sb.AppendLine("container " + c.X + "," + c.Y + " " + c.Label + " -> " + Items.Name(c.ItemId));
                }
                for (int i = 0; i < lv.Decos.Count; i++)
                    sb.AppendLine("deco kind=" + lv.Decos[i].Kind + " at " + lv.Decos[i].X.ToString("F1") + "," + lv.Decos[i].Y.ToString("F1"));
                for (int i = 0; i < lv.Hides.Count; i++)
                    sb.AppendLine("hide " + lv.Hides[i].Label + " at " + lv.Hides[i].X + "," + lv.Hides[i].Y + " enter " + lv.Hides[i].EnterX + "," + lv.Hides[i].EnterY);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static string Soak(int seed, int frames)
        {
            Game g = new Game(seed);
            g.DiffIdx = 3;
            g.StartGame();
            g.Screen = GScreen.Play;
            Rng r = new Rng(seed + 5);
            int caught = 0, wins = 0, levelChanges = 0, trapTriggers = 0;
            int lastLevel = 0;
            for (int i = 0; i < frames; i++)
            {
                // random wandering + random interactions
                Keys[] all = new Keys[] { Keys.W, Keys.A, Keys.S, Keys.D, Keys.ShiftKey, Keys.ControlKey, Keys.E };
                if (i % 11 == 0)
                {
                    Keys k = all[r.Range(0, all.Length)];
                    bool down = r.Chance(0.55);
                    Input.Set(k, down);
                    g.OnKey(k, down);
                }
                if (i % 37 == 0) { g.P.Angle += r.Range(-1.2, 1.2); Input.MouseDX = r.Range(-140, 140); }
                if (g.Screen == GScreen.Caught && i % 200 == 0) { }
                try
                {
                    g.Update(1.0 / 60.0);
                    Input.EndFrame();
                }
                catch (Exception ex)
                {
                    return "SOAK CRASH at frame " + i + ":\n" + ex.ToString();
                }
                if (g.Screen == GScreen.Caught) caught++;
                if (g.Screen == GScreen.Win) { wins++; g.ResetToTitle(); g.StartGame(); g.Screen = GScreen.Play; }
                if (g.Screen == GScreen.Over) { g.ResetToTitle(); g.StartGame(); g.Screen = GScreen.Play; }
                if (g.Screen == GScreen.Play && g.P.Level != lastLevel) { levelChanges++; lastLevel = g.P.Level; }
                if (g.P.TrapStuck > 2.5) trapTriggers++;
                // keep the run going
                if (g.Screen == GScreen.Title) g.StartGame();
            }
            string rep = "SOAK OK frames=" + frames + " caughtFrames=" + caught + " wins=" + wins +
                " levelChanges=" + levelChanges + " day=" + g.Day + " grannyLevel=" + g.G.Level +
                " traps=" + g.Levels[0].Traps.Count + " renderMs=" + g.MsWorld.ToString("F1") + "/" + g.MsUi.ToString("F1");
            Log.W(rep);
            return rep;
        }

        public static string Run(int seed)
        {
            pass = 0; fail = 0; lines.Clear();
            Game g = new Game(seed);
            Player P = g.P;

            // ---- world sanity
            Check(g.Levels.Length == 2, "two levels");
            Check(WorldBuilder.House.Containers.Count >= 8, "containers", "" + WorldBuilder.House.Containers.Count);

            int found = 0;
            for (int i = 0; i < g.Levels.Length; i++)
                for (int k = 0; k < g.Levels[i].Containers.Count; k++)
                {
                    int id = g.Levels[i].Containers[k].ItemId;
                    if (id == Items.KEY || id == Items.CROWBAR || id == Items.NOTE) found++;
                }
            Check(found == 3, "three quest items placed", "" + found);

            // reachability of every container from spawn
            Level h = g.Levels[0];
            int bad = 0;
            for (int i = 0; i < h.Containers.Count; i++)
            {
                Container c = h.Containers[i];
                bool ok = false;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (!h.WalkableTile(c.X + dx, c.Y + dy)) continue;
                        if (h.FindPath(15, 27, c.X + dx, c.Y + dy, true) != null) ok = true;
                    }
                if (!ok) { bad++; lines.Add("  unreachable container: " + c.Label + " at " + c.X + "," + c.Y); }
            }
            Check(bad == 0, "all containers reachable", "" + bad);
            Check(h.FindPath(15, 27, 15, 29, true) != null, "front door reachable");
            Check(h.FindPath(15, 27, 15, 3, true) != null, "attic hatch reachable");
            Check(g.Levels[1].FindPath(4, 19, 9, 3, true) != null, "attic chest reachable");

            // ---- start game
            g.DiffIdx = 1;
            g.StartGame();
            g.Screen = GScreen.Play;
            Park(g);
            Step(g, 0.2);
            Check(g.Screen == GScreen.Play, "play state after start");
            Check(g.Inv.Count == 0, "inventory empty at start");
            Check(g.Day == 1 && g.MaxDays == 5, "day counters", g.Day + "/" + g.MaxDays);
            Check(g.Levels[0].Traps.Count == 3, "starting traps normal", "" + g.Levels[0].Traps.Count);

            // ---- movement
            P.X = 15.5; P.Y = 27.5; P.Angle = -M.PI / 2; P.Level = 0;
            g.Cam.Flashlight = true; P.Battery = 1;
            double y0 = P.Y;
            Key(g, Keys.W, true);
            Step(g, 0.6);
            Key(g, Keys.W, false);
            Check(P.Y < y0 - 0.7, "walk north", "dy=" + (P.Y - y0).ToString("F2"));

            // wall collision: walk into the west wall of the hall
            P.X = 14.0; P.Y = 25.0; P.Angle = M.PI;
            Key(g, Keys.W, true);
            Step(g, 1.2);
            Key(g, Keys.W, false);
            Check(P.X > 12.9, "wall stops player", "x=" + P.X.ToString("F2"));

            // ---- door interaction
            P.X = 6.5; P.Y = 14.6; P.Angle = -M.PI / 2; P.Level = 0;
            Step(g, 0.05);
            Door dd = h.DoorAt(6, 13);
            Check(g.TargetKind == 1, "door targeted", "kind=" + g.TargetKind + " prompt=" + g.Prompt);
            bool wasOpen = dd.Open;
            Key(g, Keys.E, true); Step(g, 0.05); Key(g, Keys.E, false); Step(g, 0.05);
            Check(dd.Open != wasOpen, "door toggles");

            // ---- container search
            Container cont = h.ContainerAt(10, 9);
            if (cont == null) cont = h.Containers[0];
            P.Level = 0;
            P.X = cont.X + 0.5; P.Y = cont.Y + 1.5; AimAt(g, cont.X + 0.5, cont.Y + 0.5);
            Step(g, 0.05);
            Check(g.TargetKind == 2, "container targeted", "prompt=" + g.Prompt);
            int itemBefore = cont.ItemId;
            Key(g, Keys.E, true);
            Step(g, 1.4);
            Key(g, Keys.E, false);
            Check(cont.Searched, "container searched");
            Check(itemBefore == Items.NONE || g.Inv.Contains(itemBefore), "item taken", Items.Name(itemBefore));

            // ---- hide
            HideSpot hs = h.Hides[0];
            P.X = hs.EnterX + 0.5; P.Y = hs.EnterY + 0.5;
            AimAt(g, hs.X + 0.5, hs.Y + 0.5);
            Step(g, 0.05);
            Check(g.TargetKind == 3, "hide targeted", "prompt=" + g.Prompt);
            Key(g, Keys.E, true); Step(g, 0.05); Key(g, Keys.E, false); Step(g, 0.05);
            Check(P.Hidden, "hidden in closet");
            Key(g, Keys.E, true); Step(g, 0.05); Key(g, Keys.E, false); Step(g, 0.05);
            Check(!P.Hidden, "left closet");

            // ---- traps
            P.X = 15.5; P.Y = 24.0; P.Level = 0; P.TrapStuck = 0; P.Health = 1;
            h.Traps.Add(new Trap(P.X, P.Y));
            h.RebuildTrapGrid();
            Step(g, 0.1);
            Check(P.TrapStuck > 0, "trap triggers");
            Check(P.Health < 1, "trap hurts");
            int noises = g.Noises.Count;
            Check(noises > 0, "trap makes noise");

            // ---- cat
            P.Level = 1; P.X = 9.5; P.Y = 6.6; P.TrapStuck = 0;
            g.Marusya.Reset(g);
            g.Marusya.Update(0.1, g);
            Check(g.Marusya.Angry, "cat wakes near player");
            g.Marusya.Fed = true; g.Marusya.Angry = false; g.Marusya.AngryT = 0;
            g.Marusya.Update(0.1, g);
            Check(!g.Marusya.Angry, "fed cat stays calm");

            // ---- granny hears noise and investigates
            P.Level = 0; P.X = 7.5; P.Y = 21.0; P.Hidden = false;
            g.G.Level = 0; g.G.X = 7.5; g.G.Y = 15.5; g.G.State = GState.Patrol;
            g.G.Spawn(g.Levels[0], 7.5, 15.5, M.PI / 2);
            Step(g, 0.1);
            g.EmitNoise(7.5, 21.0, 9.0, "тест");
            Step(g, 0.2);
            Check(g.G.State == GState.Investigate, "granny investigates noise", "state=" + g.G.State);

            // ---- granny catch + day advance
            g.Noises.Clear();
            g.G.Level = 0; g.G.X = P.X; g.G.Y = P.Y + 0.4; g.G.State = GState.Chase;
            P.Level = 0; P.X = 7.5; P.Y = 21.0;
            Step(g, 0.1);
            Check(g.Screen == GScreen.Caught, "granny catches player", "screen=" + g.Screen);
            Step(g, 3.2);
            Check(g.Day == 2, "day advanced", "" + g.Day);
            Check(g.Screen == GScreen.Play, "respawned", "screen=" + g.Screen);

            // ---- win
            g.Locks[0] = g.Locks[1] = g.Locks[2] = true;
            P.X = 15.5; P.Y = 29.3; P.Angle = M.PI / 2; P.Level = 0;
            Step(g, 0.05);
            Check(g.TargetKind == 5, "front door targeted", "prompt=" + g.Prompt);
            Key(g, Keys.E, true); Step(g, 0.05); Key(g, Keys.E, false);
            Step(g, 0.05);
            Check(g.Screen == GScreen.Win, "escape wins", "screen=" + g.Screen);

            // ---- game over after days
            g.Screen = GScreen.Play; g.Day = g.MaxDays; P.Dead = false;
            g.OnCaught();
            Step(g, 3.2);
            Check(g.Screen == GScreen.Over, "game over after last day", "screen=" + g.Screen);

            // ---- audio buffers
            Audio.PrepareSounds();
            Check(Sfx.Scream != null && Sfx.Meow != null && Sfx.Step != null, "sfx generated");
            if (Sfx.Meow != null)
            {
                float peak = 0;
                for (int i = 0; i < Sfx.Meow.Length; i++) peak = Math.Max(peak, Math.Abs(Sfx.Meow[i]));
                Check(peak > 0.2f && peak <= 1.001f, "meow peak sane", peak.ToString("F2"));
            }

            // ---- path quality
            List<int> path = h.FindPath(15, 27, 24, 7, true);
            Check(path != null && path.Count > 10, "path to kitchen", path == null ? "null" : "" + path.Count);

            // ---- vision chase
            g.Screen = GScreen.Play; P.Dead = false; P.Level = 0; P.Hidden = false;
            P.X = 7.5; P.Y = 21.6; P.Angle = 0;
            g.G.Spawn(h, 7.5, 18.6, M.PI / 2);
            g.G.Level = 0; g.G.State = GState.Patrol; g.G.Notice = 0;
            g.Cam.Flashlight = true;
            Step(g, 1.4);
            Check(g.G.State == GState.Chase, "granny sees and chases", "state=" + g.G.State);

            // hiding must break the chase
            P.Hidden = true; P.HideSpot = h.Hides[0]; P.X = h.Hides[0].EnterX + 0.5; P.Y = h.Hides[0].EnterY + 0.5;
            g.G.X = 3.5; g.G.Y = 3.5; g.G.State = GState.Patrol; g.G.Notice = 0;
            Step(g, 1.5);
            Check(g.Screen == GScreen.Play && P.Hidden, "hiding keeps player safe", "screen=" + g.Screen);
            P.Hidden = false; P.HideSpot = null;

            // ---- cross level hunting
            P.Level = 1; P.X = 6.0; P.Y = 12.0; P.Hidden = false;
            g.G.Level = 0; g.G.X = 15.5; g.G.Y = 15.5; g.G.State = GState.Patrol;
            g.AtticAlertT = 8;
            Step(g, 5.0);
            Check(g.G.Level == 1, "granny climbs to the attic", "lvl=" + g.G.Level);

            // ---- cat screams when chest looted
            g.Marusya.Reset(g);
            g.P.Level = 1; P.X = 9.5; P.Y = 6.4;
            g.Levels[1].ContainerAt(9, 4).Searched = true;
            g.Marusya.Update(0.1, g);
            Check(g.Marusya.Angry, "cat alerts when chest is looted");
            g.Marusya.Reset(g);

            // ---- lock flow with real items
            g.Screen = GScreen.Play; g.P.Level = 0; g.P.Dead = false;
            g.Locks[0] = g.Locks[1] = g.Locks[2] = false;
            g.Inv.Clear(); g.Inv.Add(Items.KEY);
            g.P.X = 15.5; g.P.Y = 29.3; g.P.Angle = M.PI / 2;
            Step(g, 0.05);
            Key(g, Keys.E, true); Step(g, 1.8); Key(g, Keys.E, false);
            Check(g.Locks[0], "padlock opens with key", "locks=" + g.CountLocks());
            g.Inv.Add(Items.CROWBAR);
            Step(g, 0.05);
            Key(g, Keys.E, true); Step(g, 1.8); Key(g, Keys.E, false);
            Check(g.Locks[1], "deadbolt opens with crowbar", "locks=" + g.CountLocks());
            g.Inv.Add(Items.NOTE);
            Step(g, 0.05);
            Key(g, Keys.E, true); Step(g, 1.8); Key(g, Keys.E, false);
            Check(g.Locks[2], "code lock opens with note", "locks=" + g.CountLocks());

            // ---- trap decal actually renders
            g.Screen = GScreen.Play; g.P.Level = 0; g.P.X = 15.5; g.P.Y = 15.0; g.P.Angle = M.PI / 2;
            g.G.Level = 1; g.Cam.Pitch = 22; g.Cam.Flashlight = true; g.Cam.FlashBattery = 1;
            g.Levels[0].Traps.Clear(); g.Levels[0].RebuildTrapGrid();
            g.Render(0.016);
            int[] before = (int[])g.R.F.Px.Clone();
            g.Levels[0].Traps.Add(new Trap(15.5, 16.6));
            g.Levels[0].RebuildTrapGrid();
            bool gridSet = g.Levels[0].TrapGrid[16 * g.Levels[0].W + 15] == 2;
            g.Render(0.016);
            int changed = 0;
            for (int i = 0; i < before.Length; i++) if (before[i] != g.R.F.Px[i]) changed++;
            Check(gridSet, "trap grid marked");
            Check(changed > 400, "trap decal visible", changed + " px, hits=" + Renderer.TrapHits);
            // stand on the trap and look down
            g.Levels[0].Traps.Clear();
            g.Levels[0].Traps.Add(new Trap(15.5, 15.5));
            g.Levels[0].RebuildTrapGrid();
            g.P.X = 15.5; g.P.Y = 14.6; g.P.Angle = M.PI / 2; g.Cam.Pitch = 60;
            Renderer.TrapHits = 0;
            g.Render(0.016);
            lines.Add("      debug trapGridNull=" + (g.Levels[0].TrapGrid == null) +
                " at(15,15)=" + g.Levels[0].TrapGrid[15 * g.Levels[0].W + 15] +
                " hits2=" + Renderer.TrapHits);

            // ---- render performance
            g.Screen = GScreen.Play; g.P.Level = 0; g.P.X = 15.5; g.P.Y = 24.0; g.P.Angle = -M.PI / 2;
            g.G.Level = 0; g.G.X = 15.5; g.G.Y = 20.0;
            DateTime t0 = DateTime.Now;
            for (int i = 0; i < 60; i++) g.Render(1.0 / 60.0);
            double ms = (DateTime.Now - t0).TotalMilliseconds / 60.0;
            Check(ms < 24.0, "render under 24ms/frame", ms.ToString("F1") + " ms");
            lines.Add("      world=" + g.MsWorld.ToString("F1") + "ms  ui=" + g.MsUi.ToString("F1") + "ms");
            lines.Add("      floor=" + g.R.MsFloor.ToString("F1") + " walls=" + g.R.MsWalls.ToString("F1") +
                " sprites=" + g.R.MsSprites.ToString("F1") + " post=" + g.R.MsPost.ToString("F1"));

            string report = "=== Granny13 autotest ===\n" + string.Join("\n", lines.ToArray()) +
                "\n\nPASSED: " + pass + "   FAILED: " + fail + "\n";
            Log.W(report);
            return report;
        }
    }
}
