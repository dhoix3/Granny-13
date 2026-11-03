// Cat.cs - Marusya, the attic cat
using System;

namespace Granny13
{
    public class Cat
    {
        public double X = 9.5, Y = 5.2;
        public bool Fed, Angry;
        public double MeowT, IdleT, BlinkT, PurrT, AngryT;
        public double Tail;

        public void Reset(Game g)
        {
            X = 9.5; Y = 5.3;
            Fed = false; Angry = false;
            MeowT = 0; IdleT = 18 + g.Rand.Next() * 14; AngryT = 0;
        }

        public void Feed(Game g)
        {
            Fed = true; Angry = false; AngryT = 0;
            Audio.Play(Sfx.Purr, 0.5f, 1.0f, false);
            g.EmitNoise(X, Y, 2.0, "мурчание");
            g.PushSubtitle("Маруся: муррр...", 2.4, false);
        }

        public void PurrNow(Game g)
        {
            Audio.Play(Sfx.Purr, 0.45f, 1.0f, false);
        }

        public void Alert(Game g, string what)
        {
            if (AngryT > 0) return;
            Angry = true;
            AngryT = 9.0;
            MeowT = 0.9;
            Audio.Play(what == "шипение" ? Sfx.Hiss : Sfx.Meow, 0.8f, 1.0f, false);
            g.EmitNoise(X, Y, 15.0, "мяу");
            g.AtticAlertT = Math.Max(g.AtticAlertT, 8.0);
            g.PushSubtitle("Маруся: МЯУ!!!", 2.0, false);
        }

        public void Update(double dt, Game g)
        {
            Tail += dt * 1.6;
            if (AngryT > 0)
            {
                AngryT -= dt;
                if (AngryT <= 0) { Angry = false; }
            }
            if (Fed) return;
            if (g.P.Level != 1) return;
            double d = M.Dist(X, Y, g.P.X, g.P.Y);
            double catRange = g.Diff.CatRange;
            bool movingFast = g.P.MoveSpeed > 1.6 && !g.P.Crouch;
            bool lightOn = g.Cam.Flashlight && d < 3.4;
            bool near = d < catRange;
            if (!g.P.Hidden && ((near && movingFast) || (d < 1.5) || (lightOn && d < 2.6)))
                Alert(g, "мяу");

            // chest next to the cat
            Container chest = g.Levels[1].ContainerAt(9, 4);
            if (chest != null && chest.Searched && d < 4.0) Alert(g, "мяу");

            IdleT -= dt;
            if (IdleT <= 0)
            {
                IdleT = 22 + g.Rand.Next() * 20;
                if (d < 9)
                {
                    Audio.Play(Sfx.Meow, 0.35f, 0.85f + (float)g.Rand.Next() * 0.3f, false);
                    if (g.P.Level == 1) g.EmitNoise(X, Y, 5.5, "мяу");
                }
            }
        }
    }
}
