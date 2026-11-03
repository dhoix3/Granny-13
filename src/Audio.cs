// Audio.cs - tiny software synth + waveOut mixer (no external assets)
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Granny13
{
    public class Voice
    {
        public float[] Data;
        public double Pos;
        public float Vol = 1f;
        public float Pitch = 1f;
        public bool Loop;
        public bool Dead;
    }

    public static class Sfx
    {
        public static float[] Step, Step2, Creak, DoorSlam, Scream, Meow, Hiss, Purr, Snap,
            Pickup, Lock, Unlock, Heart, Whisper, Win, Lose, Climb, Search, Thunder, Page, Bottle, ScreenHit;
    }

    public static class Audio
    {
        public const int SR = 44100;
        public static float Volume = 0.75f;
        public static bool Enabled;
        public static int BuffersFilled;
        static List<Voice> voices = new List<Voice>();
        static object sync = new object();
        static Thread thread;
        static volatile bool running;
        static IntPtr hwo = IntPtr.Zero;
        static List<IntPtr> hdrPtrs = new List<IntPtr>();
        static List<IntPtr> dataPtrs = new List<IntPtr>();
        static WAVEHDR[] hdrs;
        static int nBuf = 4;
        static int bufFrames = 1024;
        static float[] ambience;
        static Voice ambientVoice;
        static Random rnd = new Random(1234);

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEFORMATEX
        {
            public ushort wFormatTag, nChannels;
            public uint nSamplesPerSec, nAvgBytesPerSec;
            public ushort nBlockAlign, wBitsPerSample, cbSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WAVEHDR
        {
            public IntPtr lpData;
            public uint dwBufferLength;
            public uint dwBytesRecorded;
            public IntPtr dwUser;
            public uint dwFlags;
            public uint dwLoops;
            public IntPtr lpNext;
            public IntPtr reserved;
        }

        [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, ref WAVEFORMATEX lpFormat, IntPtr dwCallback, IntPtr dwInstance, int dwFlags);
        [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr h, IntPtr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr h, IntPtr lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr h);
        [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr h);

        static bool soundsReady;

        public static void PrepareSounds()
        {
            if (soundsReady) return;
            try { BuildSounds(); soundsReady = true; }
            catch (Exception e) { Log.W("sound build failed: " + e.Message); }
        }

        public static bool Init()
        {
            try
            {
                PrepareSounds();
                WAVEFORMATEX fmt = new WAVEFORMATEX();
                fmt.wFormatTag = 1;
                fmt.nChannels = 2;
                fmt.nSamplesPerSec = SR;
                fmt.wBitsPerSample = 16;
                fmt.nBlockAlign = (ushort)(fmt.nChannels * fmt.wBitsPerSample / 8);
                fmt.nAvgBytesPerSec = fmt.nSamplesPerSec * fmt.nBlockAlign;
                fmt.cbSize = 0;
                int rc = waveOutOpen(out hwo, -1, ref fmt, IntPtr.Zero, IntPtr.Zero, 0);
                if (rc != 0) { Log.W("waveOutOpen failed " + rc); return false; }
                int hdrSize = Marshal.SizeOf(typeof(WAVEHDR));
                hdrs = new WAVEHDR[nBuf];
                for (int i = 0; i < nBuf; i++)
                {
                    IntPtr dp = Marshal.AllocHGlobal(bufFrames * 4);
                    dataPtrs.Add(dp);
                    IntPtr hp = Marshal.AllocHGlobal(hdrSize);
                    hdrPtrs.Add(hp);
                    WAVEHDR h = new WAVEHDR();
                    h.lpData = dp;
                    h.dwBufferLength = (uint)(bufFrames * 4);
                    h.dwFlags = 0;
                    Marshal.StructureToPtr(h, hp, false);
                    waveOutPrepareHeader(hwo, hp, hdrSize);
                    for (int k = 0; k < 1; k++)
                    {
                        Fill(dp, bufFrames);
                        waveOutWrite(hwo, hp, hdrSize);
                    }
                    hdrs[i] = h;
                }
                ambientVoice = new Voice();
                ambientVoice.Data = ambience;
                ambientVoice.Loop = true;
                ambientVoice.Vol = 0.32f;
                running = true;
                thread = new Thread(new ThreadStart(MixLoop));
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.AboveNormal;
                thread.Start();
                Enabled = true;
                Log.W("audio started");
                return true;
            }
            catch (Exception e)
            {
                Log.W("audio init failed: " + e.Message);
                Enabled = false;
                return false;
            }
        }

        public static void Shutdown()
        {
            running = false;
            try { if (hwo != IntPtr.Zero) { waveOutReset(hwo); waveOutClose(hwo); hwo = IntPtr.Zero; } } catch { }
        }

        static void MixLoop()
        {
            int hdrSize = Marshal.SizeOf(typeof(WAVEHDR));
            int cur = 0;
            while (running)
            {
                try
                {
                    IntPtr hp = hdrPtrs[cur];
                    WAVEHDR h = (WAVEHDR)Marshal.PtrToStructure(hp, typeof(WAVEHDR));
                    if ((h.dwFlags & 0x01) != 0)
                    {
                        Fill(h.lpData, bufFrames);
                        waveOutWrite(hwo, hp, hdrSize);
                        cur = (cur + 1) % nBuf;
                    }
                    else
                    {
                        Thread.Sleep(2);
                    }
                }
                catch (Exception e)
                {
                    Log.W("mix error: " + e.Message);
                    Thread.Sleep(10);
                }
            }
        }

        static float[] mixBuf = new float[8192];

        static unsafe void Fill(IntPtr dst, int frames)
        {
            int n = frames;
            BuffersFilled++;
            if (mixBuf.Length < n * 2) mixBuf = new float[n * 2];
            for (int i = 0; i < n * 2; i++) mixBuf[i] = 0f;
            lock (sync)
            {
                if (ambientVoice != null)
                {
                    // ambient drone mixed in at low level
                    double pos = ambientVoice.Pos;
                    for (int i = 0; i < n; i++)
                    {
                        int p = (int)pos; if (p >= ambientVoice.Data.Length) p = 0;
                        float v = ambientVoice.Data[p] * ambientVoice.Vol;
                        mixBuf[i * 2] += v; mixBuf[i * 2 + 1] += v;
                        pos += 1.0;
                        if (pos >= ambientVoice.Data.Length) pos = 0;
                    }
                    ambientVoice.Pos = pos;
                }
                for (int vi = voices.Count - 1; vi >= 0; vi--)
                {
                    Voice vv = voices[vi];
                    if (vv.Dead) { voices.RemoveAt(vi); continue; }
                    float[] d = vv.Data;
                    double pos = vv.Pos;
                    for (int i = 0; i < n; i++)
                    {
                        int p = (int)pos;
                        if (p >= d.Length)
                        {
                            if (vv.Loop) { pos -= d.Length; p = (int)pos; if (p >= d.Length) p = 0; }
                            else { vv.Dead = true; break; }
                        }
                        float v = d[p] * vv.Vol;
                        mixBuf[i * 2] += v;
                        mixBuf[i * 2 + 1] += v;
                        pos += vv.Pitch;
                    }
                    vv.Pos = pos;
                    if (vv.Dead) voices.RemoveAt(vi);
                }
            }
            short* out16 = (short*)dst;
            for (int i = 0; i < n * 2; i++)
            {
                float s = mixBuf[i] * Volume;
                if (s > 1f) s = 1f;
                if (s < -1f) s = -1f;
                out16[i] = (short)(s * 32000);
            }
        }

        public static void Play(float[] data, double vol, double pitch, bool loop)
        {
            if (!Enabled || data == null) return;
            lock (sync)
            {
                if (voices.Count > 40) voices.RemoveAt(0);
                Voice v = new Voice();
                v.Data = data; v.Vol = (float)vol; v.Pitch = (float)pitch; v.Loop = loop;
                voices.Add(v);
            }
        }

        public static void StopAll() { lock (sync) voices.Clear(); }

        // ------------------------------------------------------------- synthesis
        static float[] Alloc(double seconds)
        {
            int n = (int)(SR * seconds);
            return new float[n];
        }

        static float Noise() { return (float)(rnd.NextDouble() * 2.0 - 1.0); }

        static float[] Env(float[] buf, double attack, double decay)
        {
            int a = (int)(SR * attack);
            for (int i = 0; i < buf.Length; i++)
            {
                double t = (double)i / SR;
                float e = 1f;
                if (i < a) e = (float)i / Math.Max(1, a);
                else e = (float)Math.Exp(-(i - a) / (SR * decay) * 3.2);
                buf[i] *= e;
            }
            return buf;
        }

        static void Lowpass(float[] b, double cutoff)
        {
            double dt = 1.0 / SR;
            double rc = 1.0 / (2 * Math.PI * cutoff);
            double alpha = dt / (rc + dt);
            float y = 0;
            for (int i = 0; i < b.Length; i++) { y += (float)(alpha * (b[i] - y)); b[i] = y; }
        }

        static void Highpass(float[] b, double cutoff)
        {
            double dt = 1.0 / SR;
            double rc = 1.0 / (2 * Math.PI * cutoff);
            double alpha = rc / (rc + dt);
            float prevIn = 0, prevOut = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float x = b[i];
                float y = (float)(alpha * (prevOut + x - prevIn));
                prevIn = x; prevOut = y; b[i] = y;
            }
        }

        static void Normalize(float[] b, float peak)
        {
            float mx = 0.0001f;
            for (int i = 0; i < b.Length; i++) mx = Math.Max(mx, Math.Abs(b[i]));
            float k = peak / mx;
            for (int i = 0; i < b.Length; i++) b[i] *= k;
        }

        static void BuildSounds()
        {
            // footsteps on old wood
            Sfx.Step = MakeStep(0.24, 120, 0.55f);
            Sfx.Step2 = MakeStep(0.22, 96, 0.5f);

            // door creak
            {
                float[] b = Alloc(1.1);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    double f = 220 + 90 * Math.Sin(t * 5.5) - t * 40;
                    double wob = Math.Sin(t * 31.0) * 0.4 + Math.Sin(t * 17.0) * 0.3;
                    b[i] = (float)(Math.Sin(2 * Math.PI * f * t + wob) * 0.35 + Noise() * 0.06);
                }
                Env(b, 0.05, 0.45);
                Lowpass(b, 1800);
                Normalize(b, 0.6f);
                Sfx.Creak = b;
            }
            // door slam
            {
                float[] b = Alloc(0.7);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float env = (float)Math.Exp(-t * 9);
                    b[i] = (float)(Math.Sin(2 * Math.PI * 62 * t) * env * 0.9 + Noise() * env * 0.7);
                }
                Lowpass(b, 900);
                Normalize(b, 0.8f);
                Sfx.DoorSlam = b;
            }
            // granny scream
            {
                float[] b = Alloc(1.5);
                double phase = 0;
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    double f = 520 - t * 190 + Math.Sin(t * 42) * 55 + Math.Sin(t * 7) * 30;
                    if (f < 90) f = 90;
                    phase += 2 * Math.PI * f / SR;
                    float saw = (float)((2.0 / Math.PI) * Math.Atan(Math.Tan(phase * 0.5)) * 0.45);
                    float n = Noise() * 0.35f;
                    b[i] = saw + n;
                }
                Env(b, 0.03, 0.7);
                Highpass(b, 180);
                Lowpass(b, 3200);
                Normalize(b, 0.9f);
                Sfx.Scream = b;
            }
            // cat meow
            {
                float[] b = Alloc(0.85);
                double phase = 0;
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    double f = 480 + 260 * Math.Sin(M.PI * Math.Min(1.0, t / 0.55)) - t * 60;
                    f *= 1.0 + 0.03 * Math.Sin(t * 33);
                    phase += 2 * Math.PI * f / SR;
                    float v = (float)(Math.Sin(phase) * 0.6 + Math.Sin(phase * 2.02) * 0.25 + Math.Sin(phase * 3.1) * 0.1);
                    b[i] = v;
                }
                Env(b, 0.06, 0.35);
                Lowpass(b, 3000);
                Normalize(b, 0.65f);
                Sfx.Meow = b;
            }
            // cat hiss
            {
                float[] b = Alloc(0.7);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    b[i] = Noise() * (float)(0.6 + 0.4 * Math.Sin(t * 70));
                }
                Highpass(b, 1400);
                Env(b, 0.04, 0.25);
                Normalize(b, 0.5f);
                Sfx.Hiss = b;
            }
            // purr
            {
                float[] b = Alloc(1.6);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float mod = (float)(0.5 + 0.5 * Math.Sin(t * 2 * Math.PI * 26));
                    b[i] = Noise() * mod * 0.5f;
                }
                Lowpass(b, 420);
                Env(b, 0.2, 0.9);
                Normalize(b, 0.35f);
                Sfx.Purr = b;
            }
            // bear trap snap
            {
                float[] b = Alloc(0.6);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 26);
                    b[i] = (float)(Math.Sin(2 * Math.PI * 2400 * t) * e * 0.5 + Math.Sin(2 * Math.PI * 3800 * t) * e * 0.3 + Noise() * e * 0.8);
                }
                Normalize(b, 0.85f);
                Sfx.Snap = b;
            }
            // pickup
            {
                float[] b = Alloc(0.5);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 7);
                    double f = 660 + t * 300;
                    b[i] = (float)(Math.Sin(2 * Math.PI * f * t) * e * 0.5 + Math.Sin(2 * Math.PI * f * 2 * t) * e * 0.2);
                }
                Normalize(b, 0.45f);
                Sfx.Pickup = b;
            }
            // lock click
            {
                float[] b = Alloc(0.35);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 40);
                    b[i] = (float)(Math.Sin(2 * Math.PI * 1900 * t) * e * 0.5 + Noise() * e * 0.5 + Math.Sin(2 * Math.PI * 900 * t) * e * 0.2);
                }
                Normalize(b, 0.5f);
                Sfx.Lock = b;
            }
            // unlock (heavier)
            {
                float[] b = Alloc(0.6);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 12);
                    b[i] = (float)(Math.Sin(2 * Math.PI * 420 * t) * e * 0.5 + Noise() * e * 0.4);
                }
                Normalize(b, 0.6f);
                Sfx.Unlock = b;
            }
            // heartbeat
            {
                float[] b = Alloc(0.55);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float thump = 0;
                    double tt = t;
                    thump += (float)(Math.Sin(2 * Math.PI * 46 * tt) * Math.Exp(-tt * 20));
                    if (t > 0.16) { double t2 = t - 0.16; thump += (float)(Math.Sin(2 * Math.PI * 40 * t2) * Math.Exp(-t2 * 22)) * 0.7f; }
                    b[i] = thump * 0.8f;
                }
                Lowpass(b, 220);
                Normalize(b, 0.55f);
                Sfx.Heart = b;
            }
            // whisper
            {
                float[] b = Alloc(1.4);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float env = (float)(0.5 + 0.5 * Math.Sin(t * 3.3)) * (float)(0.6 + 0.4 * Math.Sin(t * 11.0));
                    b[i] = Noise() * env;
                }
                Highpass(b, 900);
                Lowpass(b, 3600);
                Env(b, 0.2, 0.6);
                Normalize(b, 0.25f);
                Sfx.Whisper = b;
            }
            // win chime
            {
                float[] b = Alloc(2.2);
                double[] notes = { 523.25, 659.25, 783.99, 1046.5 };
                for (int n = 0; n < notes.Length; n++)
                {
                    int start = (int)(SR * n * 0.18);
                    for (int i = start; i < b.Length; i++)
                    {
                        double t = (double)(i - start) / SR;
                        float e = (float)Math.Exp(-t * 2.2);
                        b[i] += (float)(Math.Sin(2 * Math.PI * notes[n] * t) * e * 0.3);
                    }
                }
                Normalize(b, 0.6f);
                Sfx.Win = b;
            }
            // lose drone
            {
                float[] b = Alloc(2.4);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    double f = 180 - t * 30;
                    b[i] = (float)(Math.Sin(2 * Math.PI * f * t) * 0.5 + Math.Sin(2 * Math.PI * f * 1.5 * t) * 0.2);
                }
                Env(b, 0.05, 1.2);
                Lowpass(b, 800);
                Normalize(b, 0.6f);
                Sfx.Lose = b;
            }
            // climb ladder
            {
                float[] b = Alloc(0.7);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 18);
                    b[i] = (float)(Math.Sin(2 * Math.PI * 150 * t) * e * 0.4 + Noise() * e * 0.3);
                }
                Lowpass(b, 1200);
                Normalize(b, 0.5f);
                Sfx.Climb = b;
            }
            // search drawer
            {
                float[] b = Alloc(0.55);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 11);
                    b[i] = Noise() * e * 0.7f;
                }
                Lowpass(b, 2400);
                Normalize(b, 0.45f);
                Sfx.Search = b;
            }
            // thunder
            {
                float[] b = Alloc(2.6);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)(Math.Exp(-t * 2.2) * (0.6 + 0.4 * Math.Sin(t * 8)));
                    b[i] = Noise() * e;
                }
                Lowpass(b, 340);
                Normalize(b, 0.85f);
                Sfx.Thunder = b;
            }
            // paper page
            {
                float[] b = Alloc(0.45);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 14);
                    b[i] = Noise() * e;
                }
                Highpass(b, 1500);
                Normalize(b, 0.3f);
                Sfx.Page = b;
            }
            // bottle clink
            {
                float[] b = Alloc(0.4);
                for (int i = 0; i < b.Length; i++)
                {
                    double t = (double)i / SR;
                    float e = (float)Math.Exp(-t * 22);
                    b[i] = (float)(Math.Sin(2 * Math.PI * 2200 * t) * e * 0.4 + Math.Sin(2 * Math.PI * 3100 * t) * e * 0.3);
                }
                Normalize(b, 0.4f);
                Sfx.Bottle = b;
            }
            Sfx.ScreenHit = Sfx.Snap;
            BuildAmbience();
        }

        static float[] MakeStep(double dur, double freq, float vol)
        {
            float[] b = Alloc(dur);
            for (int i = 0; i < b.Length; i++)
            {
                double t = (double)i / SR;
                float e = (float)Math.Exp(-t * 22);
                float thump = (float)(Math.Sin(2 * Math.PI * freq * t) * Math.Exp(-t * 26) * 0.7);
                float creak = (float)(Math.Sin(2 * Math.PI * (freq * 3.4) * t) * Math.Exp(-t * 40) * 0.15);
                b[i] = thump + creak + Noise() * e * 0.25f;
            }
            Lowpass(b, 2200);
            Normalize(b, vol);
            return b;
        }

        static void BuildAmbience()
        {
            int n = SR * 12;
            ambience = new float[n];
            double p1 = 0, p2 = 0, p3 = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / SR;
                p1 += 2 * Math.PI * 41.0 / SR;
                p2 += 2 * Math.PI * 55.5 / SR;
                p3 += 2 * Math.PI * 82.4 / SR;
                double lfo = 0.55 + 0.45 * Math.Sin(t * 0.33) * Math.Sin(t * 0.11 + 1.4);
                double v = (Math.Sin(p1) * 0.5 + Math.Sin(p2) * 0.28 + Math.Sin(p3) * 0.12) * lfo;
                v += (rnd.NextDouble() - 0.5) * 0.03 * (0.6 + 0.4 * Math.Sin(t * 0.7));
                ambience[i] = (float)(v * 0.5);
            }
            // fade edges for loop smoothness (crossfade)
            int fade = SR / 2;
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                ambience[i] *= k;
                ambience[n - 1 - i] *= k;
            }
            Normalize(ambience, 0.5f);
        }
    }
}
