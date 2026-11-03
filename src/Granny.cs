// Granny.cs - the huntress AI
using System;
using System.Collections.Generic;

namespace Granny13
{
    public static class GState
    {
        public const int Patrol = 0, Investigate = 1, Chase = 2, Search = 3, Climb = 4, Stagger = 5;
    }

    public class Granny
    {
        public double X, Y, Angle;
        public int State = GState.Patrol;
        public int Level;
        public double Bob, AnimT;
        public double Notice;
        public double RepathT, DecisionT, SearchT, StaggerT, ClimbT;
        public double TrapT;
        public double ScreamT;
        public double TargetX, TargetY;
        public double HatchX, HatchY;
        public int AtticWish;         // 0 none, 1 wants up, 2 wants down
        public bool SawPlayer;
        public double LostT;
        public double SpeedMul = 1.0;
        public bool Aggro;
        public double StuckT;
        public double LastX, LastY;
        public string Line;
        public double LineT;

        List<int> path;
        int pathIdx;
        Level lv;

        public double DistToPlayer(Game g) { return M.Dist(X, Y, g.P.X, g.P.Y); }

        public void Spawn(Level l, double x, double y, double ang)
        {
            X = x; Y = y; Angle = ang; Level = l.Id; lv = l;
            State = GState.Patrol; path = null; pathIdx = 0;
            Notice = 0; Aggro = false; ScreamT = 0; AtticWish = 0;
            TrapT = 8.0;
        }

        public void Say(Game g, string text, double dur)
        {
            Line = text; LineT = dur;
            g.PushSubtitle(text, dur, true);
        }

        public void SetPath(Game g, int tx, int ty)
        {
            Level lcur = g.Levels[Level];
            path = lcur.FindPath((int)X, (int)Y, tx, ty, true);
            pathIdx = 0;
        }

        public void Update(double dt, Game g)
        {
            lv = g.Levels[Level];
            double dist = DistToPlayer(g);
            bool sameLevel = g.P.Level == Level;
            AnimT += dt;
            if (ScreamT > 0) ScreamT -= dt;
            if (LineT > 0) LineT -= dt;

            // ---------------------------------------------------------- senses
            bool canSee = false;
            if (sameLevel && !g.P.Hidden && !g.P.Dead)
            {
                double sight = g.Diff.Sight;
                if (g.Cam.Flashlight) sight *= 1.3;
                if (g.P.Crouch) sight *= 0.8;
                if (dist < sight && lv.LineOfSight(X, Y, g.P.X, g.P.Y))
                {
                    double ang = Math.Atan2(g.P.Y - Y, g.P.X - X);
                    double diff = Math.Abs(M.AngleDiff(ang, Angle));
                    if (diff < 1.35 || dist < 2.4) canSee = true;
                }
            }
            SawPlayer = canSee;

            if (canSee)
            {
                Notice = Math.Min(1.0, Notice + dt * (2.6 / (1.0 + dist * 0.2)));
                if (Notice >= 1.0 && State != GState.Chase)
                {
                    State = GState.Chase;
                    ScreamT = 1.4;
                    Aggro = true;
                    string[] lines = { "А ВОТ И ТЫ, СОЛНЫШКО!", "СТОЙ, ВНУЧОК!", "Я ТЕБЯ ВИЖУ, ЗАИНЬКА!", "КУДА СОБРАЛСЯ БЕЗ УЖИНА?!" };
                    Say(g, lines[(int)(g.Rand.Next() * lines.Length)], 2.2);
                    Audio.Play(Sfx.Scream, 0.85f, 1.0f, false);
                    g.Cam.AddShake(0.7);
                }
            }
            else
            {
                Notice = Math.Max(0, Notice - dt * 0.55);
            }

            // hearing
            for (int i = 0; i < g.Noises.Count; i++)
            {
                Noise n = g.Noises[i];
                if (n.Time < g.NoiseSeen) continue;
                double d = M.Dist(X, Y, n.X, n.Y);
                double range = n.Radius * g.Diff.HearMul;
                if (sameLevel || (n.X > 0 && g.NoiseCrossLevel))
                {
                    if (d < range && State != GState.Chase && State != GState.Climb)
                    {
                        State = GState.Investigate;
                        TargetX = n.X; TargetY = n.Y;
                        path = null;
                        DecisionT = 0;
                        if (g.Rand.Next() < 0.35) Say(g, n.What == "крик" ? "Что там у тебя?" : "Кто здесь ходит?", 1.6);
                    }
                }
            }

            // ---------------------------------------------------------- states
            switch (State)
            {
                case GState.Patrol:
                    {
                        if (path == null || pathIdx >= path.Count)
                        {
                            DecisionT -= dt;
                            if (DecisionT <= 0)
                            {
                                PickPatrolTarget(g);
                                DecisionT = 2.0 + g.Rand.Next() * 2.0;
                            }
                        }
                        FollowPath(dt, g, g.Diff.Walk);
                        TryPlaceTrap(dt, g);
                        break;
                    }
                case GState.Investigate:
                    {
                        double d = M.Dist(X, Y, TargetX, TargetY);
                        if (path == null || pathIdx >= path.Count)
                        {
                            if (d < 1.0)
                            {
                                State = GState.Search;
                                SearchT = 5.0 + g.Rand.Next() * 3.0;
                                ChooseHatch(g);
                                path = null;
                            }
                            else SetPath(g, (int)TargetX, (int)TargetY);
                        }
                        FollowPath(dt, g, g.Diff.Walk * 1.15);
                        if (d < 0.8 && State == GState.Investigate) { State = GState.Search; SearchT = 5.0; }
                        break;
                    }
                case GState.Search:
                    {
                        SearchT -= dt;
                        // look around slowly
                        Angle += Math.Sin(AnimT * 1.6) * dt * 1.4;
                        if (SearchT <= 0)
                        {
                            State = GState.Patrol;
                            path = null;
                            DecisionT = 1.0;
                        }
                        if (g.P.Hidden && g.P.HideSpot != null && M.Dist(X, Y, g.P.HideSpot.X + 0.5, g.P.HideSpot.Y + 0.5) < 2.0)
                        {
                            if (g.Rand.Next() < g.Diff.HideCheck * dt)
                            {
                                g.OnCaughtInHide();
                            }
                        }
                        break;
                    }
                case GState.Chase:
                    {
                        if (sameLevel)
                        {
                            RepathT -= dt;
                            if (RepathT <= 0)
                            {
                                SetPath(g, (int)g.P.X, (int)g.P.Y);
                                RepathT = 0.4;
                            }
                            FollowPath(dt, g, g.Diff.Run);
                            if (dist < 0.62)
                            {
                                g.OnCaught();
                                break;
                            }
                            if (!canSee && !g.P.Hidden)
                            {
                                LostT += dt;
                                if (LostT > 3.5)
                                {
                                    State = GState.Search;
                                    SearchT = 5.0;
                                    TargetX = g.P.X; TargetY = g.P.Y;
                                    path = null;
                                    LostT = 0;
                                    if (g.Rand.Next() < 0.5) Say(g, "Ну и где ты спрятался?", 1.8);
                                }
                            }
                            else LostT = 0;
                        }
                        else
                        {
                            // player on another floor
                            State = GState.Investigate;
                            TargetX = g.P.X; TargetY = g.P.Y;
                            AtticWish = g.P.Level > Level ? 1 : 2;
                            path = null;
                        }
                        break;
                    }
                case GState.Climb:
                    {
                        ClimbT -= dt;
                        if (ClimbT <= 0)
                        {
                            int nl = M.ClampI(AtticWish, 0, 1);
                            Level = nl;
                            if (nl == 0) { X = 15.5; Y = 4.2; }
                            else { X = 5.4; Y = 19.4; }
                            path = null;
                            State = GState.Patrol;
                            DecisionT = 0.5;
                            Audio.Play(Sfx.Climb, 0.5f, 0.85f, false);
                        }
                        break;
                    }
            }

            // level change wish
            UpdateAtticWish(dt, g);
            g.NoiseSeen = g.Time;
            Bob += dt * (State == GState.Chase ? 9.0 : 4.4) * (0.4 + 0.6 * (path != null && pathIdx < path.Count ? 1 : 0.15));
        }

        double levelSwitchT = 4.0;

        void UpdateAtticWish(double dt, Game g)
        {
            if (State == GState.Climb) return;
            if (g.P.Level != Level)
            {
                // she follows the noise upstairs / downstairs
                levelSwitchT -= dt * (g.AtticAlertT > 0 ? 2.2 : 1.0);
                AtticWish = g.P.Level;
                if (levelSwitchT <= 0)
                {
                    State = GState.Climb;
                    ClimbT = 1.4;
                    path = null;
                    HatchX = Level == 0 ? 15.5 : 4.5;
                    HatchY = Level == 0 ? 3.5 : 20.5;
                    Audio.Play(Sfx.Climb, 0.55f, 0.9f, false);
                }
            }
            else
            {
                levelSwitchT = 4.0;
                AtticWish = 0;
            }
        }

        void ChooseHatch(Game g)
        {
            if (Level == 0) { HatchX = 15.5; HatchY = 3.5; }
            else { HatchX = 4.5; HatchY = 20.5; }
        }

        void PickPatrolTarget(Game g)
        {
            Level l = g.Levels[Level];
            if (Level == 0)
            {
                double[][] pts = new double[][] {
                    new double[]{ 7.5, 7.5 }, new double[]{ 8.5, 11.5 }, new double[]{ 24.5, 7.5 },
                    new double[]{ 28.5, 11.5 }, new double[]{ 7.5, 15.5 }, new double[]{ 22.5, 15.5 },
                    new double[]{ 6.5, 21.5 }, new double[]{ 25.5, 21.5 }, new double[]{ 6.5, 27.5 },
                    new double[]{ 24.5, 27.5 }, new double[]{ 15.5, 20.5 }, new double[]{ 15.5, 27.0 },
                    new double[]{ 15.5, 8.0 }
                };
                int i = (int)(g.Rand.Next() * pts.Length);
                SetPath(g, (int)pts[i][0], (int)pts[i][1]);
            }
            else
            {
                double[][] pts = new double[][] {
                    new double[]{ 6.5, 6.5 }, new double[]{ 16.5, 6.5 }, new double[]{ 6.5, 16.5 },
                    new double[]{ 16.5, 16.5 }, new double[]{ 11.5, 11.5 }, new double[]{ 19.5, 19.5 }
                };
                int i = (int)(g.Rand.Next() * pts.Length);
                SetPath(g, (int)pts[i][0], (int)pts[i][1]);
            }
        }

        void TryPlaceTrap(double dt, Game g)
        {
            TrapT -= dt;
            if (TrapT > 0) return;
            TrapT = 16.0 + g.Rand.Next() * 14.0;
            Level l = g.Levels[Level];
            int tx = (int)X, ty = (int)Y;
            if (l.Get(tx, ty) != T.FLOOR && l.Get(tx, ty) != T.RUG) return;
            if (l.TrapNear(tx + 0.5, ty + 0.5, 2.2) != null) return;
            if (M.Dist(X, Y, g.P.X, g.P.Y) < 2.5) return;
            l.Traps.Add(new Trap(tx + 0.5, ty + 0.5));
            l.RebuildTrapGrid();
            int cap = g.Diff.Traps + 4;
            if (l.Traps.Count > cap) { l.Traps.RemoveAt(0); l.RebuildTrapGrid(); }
        }

        public void FollowPath(double dt, Game g, double speed)
        {
            if (path == null || pathIdx >= path.Count) return;
            Level l = g.Levels[Level];
            int node = path[pathIdx];
            int nx = node % l.W, ny = node / l.W;
            double tx = nx + 0.5, ty = ny + 0.5;
            // door?
            byte tile = l.Get(nx, ny);
            if (tile == T.DOOR)
            {
                Door d = l.DoorAt(nx, ny);
                if (d != null && !d.Open)
                {
                    double dd = M.Dist(X, Y, tx, ty);
                    if (dd < 1.4)
                    {
                        d.Open = true;
                        Audio.Play(Sfx.Creak, 0.6f, 1.0f, false);
                        g.EmitNoise(tx, ty, 6.0, "дверь");
                        return;
                    }
                }
            }
            double dx = tx - X, dy = ty - Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.09)
            {
                pathIdx++;
                return;
            }
            double step = speed * dt;
            if (step > dist) step = dist;
            double mx = X + dx / dist * step;
            double my = Y + dy / dist * step;
            // wall slide
            if (!g.Levels[Level].SolidAt(mx, Y) && !BlockedByDeco(g, mx, Y)) X = mx;
            else { StepAround(g, dx, dy, dy, dx); }
            if (!g.Levels[Level].SolidAt(X, my) && !BlockedByDeco(g, X, my)) Y = my;
            double targetAng = Math.Atan2(dy, dx);
            double ad = M.AngleDiff(targetAng, Angle);
            Angle += M.Clamp(ad, -3.2 * dt, 3.2 * dt);
            double moved = M.Dist(X, Y, LastX, LastY);
            LastX = X; LastY = Y;
            if (moved < 0.0005) { StuckT += dt; if (StuckT > 1.2) { path = null; StuckT = 0; } }
            else StuckT = 0;
        }

        void StepAround(Game g, double dx, double dy, double ax, double ay)
        {
            double l = Math.Sqrt(ax * ax + ay * ay);
            if (l < 0.001) return;
            double px = -ay / l * 0.03, py = ax / l * 0.03;
            if (!g.Levels[Level].SolidAt(X + px, Y + py)) { X += px; Y += py; }
        }

        bool BlockedByDeco(Game g, double x, double y)
        {
            Level l = g.Levels[Level];
            for (int i = 0; i < l.Decos.Count; i++)
            {
                Deco d = l.Decos[i];
                if (!d.Solid) continue;
                if (Math.Abs(x - d.X) < d.RadiusX + 0.2 && Math.Abs(y - d.Y) < d.RadiusY + 0.2) return true;
            }
            return false;
        }
    }
}
