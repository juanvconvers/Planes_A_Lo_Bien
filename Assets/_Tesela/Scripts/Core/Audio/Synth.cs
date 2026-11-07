using System;

namespace Tesela.Core.Audio
{
    public enum Sfx
    {
        Pick = 0,
        Place = 1,
        Invalid = 2,
        Button = 3,
        Coin = 4,
        LevelUp = 5,
        GameOver = 6,
        Sweep = 7,
        Reward = 8
    }

    /// <summary>
    /// Efectos de sonido sintetizados (100 % propios): clic de cerámica, toques de esmalte y combo en escala
    /// pentatónica ascendente. Muestras mono en [-1, 1].
    /// </summary>
    public static class SfxSynth
    {
        public const int SampleRate = 22050;

        /// <summary>Escala pentatónica mayor (do re mi sol la) en semitonos.</summary>
        private static readonly int[] Pentatonic = { 0, 2, 4, 7, 9 };

        public static float[] Render(Sfx sfx, int seed = 1)
        {
            switch (sfx)
            {
                case Sfx.Pick: return Ceramic(0.06f, 3400f, 0.35f, seed);
                case Sfx.Place: return Ceramic(0.12f, 2300f, 0.8f, seed);
                case Sfx.Invalid: return Thud(0.18f, 140f);
                case Sfx.Button: return Ceramic(0.05f, 2900f, 0.45f, seed + 3);
                case Sfx.Coin: return Arpeggio(new[] { 12, 19 }, 0.07f, 0.35f);
                case Sfx.LevelUp: return Arpeggio(new[] { 0, 4, 7, 12, 16 }, 0.09f, 0.4f);
                case Sfx.GameOver: return Arpeggio(new[] { 7, 4, 0, -5 }, 0.16f, 0.35f);
                case Sfx.Sweep: return Sweep(0.35f, seed);
                case Sfx.Reward: return Arpeggio(new[] { 0, 7, 12, 19, 24 }, 0.07f, 0.4f);
                default: return new float[1];
            }
        }

        /// <summary>Toque de esmalte del combo: cada racha sube un grado de la pentatónica.</summary>
        public static float[] ComboNote(int comboStep)
        {
            int degree = Math.Max(0, comboStep);
            int semitone = Pentatonic[degree % 5] + 12 * (degree / 5);
            return Bell(MidiToHz(72 + Math.Min(semitone, 24)), 0.55f, 0.5f);
        }

        public static float MidiToHz(float midi) => 440f * (float)Math.Pow(2, (midi - 69) / 12f);

        /// <summary>Clic de cerámica: ráfaga de ruido muy corta + resonancias inarmónicas que decaen rápido.</summary>
        private static float[] Ceramic(float seconds, float baseHz, float gain, int seed)
        {
            int n = (int)(seconds * SampleRate);
            var buf = new float[n];
            var rng = new Pcg32((ulong)seed * 7919UL + 13UL);
            float lp = 0f;
            float[] ratios = { 1f, 1.73f, 2.41f, 3.11f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = (float)Math.Exp(-t * 70f);
                float noise = rng.NextFloat() * 2f - 1f;
                lp += (noise - lp) * 0.55f;
                float tone = 0f;
                for (int k = 0; k < ratios.Length; k++)
                    tone += (float)Math.Sin(2 * Math.PI * baseHz * ratios[k] * t) * (float)Math.Exp(-t * (60f + 25f * k)) / (k + 1);
                buf[i] = (lp * env * 0.5f + tone * 0.6f) * gain;
            }
            return Normalize(buf, gain);
        }

        private static float[] Thud(float seconds, float hz)
        {
            int n = (int)(seconds * SampleRate);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float f = hz * (1f + 0.6f * (float)Math.Exp(-t * 30f));
                buf[i] = (float)Math.Sin(2 * Math.PI * f * t) * (float)Math.Exp(-t * 18f);
            }
            return Normalize(buf, 0.5f);
        }

        /// <summary>Campana/esmalte: parciales de una placa (síntesis modal simple).</summary>
        public static float[] Bell(float hz, float seconds, float gain)
        {
            int n = (int)(seconds * SampleRate);
            var buf = new float[n];
            float[] ratios = { 1f, 2.0f, 2.76f, 5.4f };
            float[] amps = { 1f, 0.45f, 0.3f, 0.12f };
            float[] decay = { 6f, 9f, 13f, 22f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Math.Min(1f, t * 400f);
                float s = 0f;
                for (int k = 0; k < ratios.Length; k++)
                    s += amps[k] * (float)Math.Sin(2 * Math.PI * hz * ratios[k] * t) * (float)Math.Exp(-t * decay[k]);
                buf[i] = s * attack;
            }
            return Normalize(buf, gain);
        }

        private static float[] Arpeggio(int[] semitones, float step, float gain)
        {
            int noteLen = (int)(0.45f * SampleRate);
            int stepLen = (int)(step * SampleRate);
            var buf = new float[stepLen * (semitones.Length - 1) + noteLen];
            for (int i = 0; i < semitones.Length; i++)
            {
                var note = Bell(MidiToHz(72 + semitones[i]), 0.45f, 1f);
                int off = i * stepLen;
                for (int j = 0; j < note.Length && off + j < buf.Length; j++) buf[off + j] += note[j];
            }
            return Normalize(buf, gain);
        }

        private static float[] Sweep(float seconds, int seed)
        {
            int n = (int)(seconds * SampleRate);
            var buf = new float[n];
            var rng = new Pcg32((ulong)seed + 99UL);
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float cutoff = 0.05f + 0.5f * t;
                lp += ((rng.NextFloat() * 2f - 1f) - lp) * cutoff;
                buf[i] = lp * (float)Math.Sin(Math.PI * t);
            }
            return Normalize(buf, 0.35f);
        }

        public static float[] Normalize(float[] buf, float peak)
        {
            float max = 1e-6f;
            for (int i = 0; i < buf.Length; i++) max = Math.Max(max, Math.Abs(buf[i]));
            float k = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
            // Rampa de 2 ms al final para evitar clics.
            int ramp = Math.Min(buf.Length, SampleRate / 500);
            for (int i = 0; i < ramp; i++) buf[buf.Length - 1 - i] *= i / (float)ramp;
            return buf;
        }
    }

    /// <summary>
    /// Música acústica en capas (guitarra por Karplus-Strong y marimba modal). Las tres capas tienen la misma
    /// duración y se reproducen sincronizadas; la UI sube el volumen de las capas superiores con la racha.
    /// </summary>
    public static class MusicSynth
    {
        public const int SampleRate = 22050;
        public const float Bpm = 84f;
        public const int Bars = 8;

        /// <summary>Progresión I–vi–IV–V en sol mayor (grados como semitonos desde sol).</summary>
        private static readonly int[][] Chords =
        {
            new[] { 0, 4, 7 }, new[] { 9, 12, 16 }, new[] { 5, 9, 12 }, new[] { 7, 11, 14 },
            new[] { 0, 4, 7 }, new[] { 9, 12, 16 }, new[] { 5, 9, 12 }, new[] { 2, 7, 11 }
        };

        public static int LoopSamples => (int)(Bars * 4 * 60f / Bpm * SampleRate);

        /// <summary>layer 0 = base (marimba grave), 1 = guitarra arpegiada, 2 = melodía de marimba aguda.</summary>
        public static float[] RenderLayer(int layer, int seed = 7)
        {
            int total = LoopSamples;
            var buf = new float[total];
            int beat = total / (Bars * 4);
            var rng = new Pcg32((ulong)(seed * 31 + layer));
            const int Root = 55; // sol
            for (int bar = 0; bar < Bars; bar++)
            {
                int[] chord = Chords[bar];
                int barStart = bar * beat * 4;
                switch (layer)
                {
                    case 0:
                        Mix(buf, barStart, Marimba(SfxSynth.MidiToHz(Root - 12 + chord[0]), 1.6f), 0.55f);
                        Mix(buf, barStart + beat * 2, Marimba(SfxSynth.MidiToHz(Root - 12 + chord[2] - 12), 1.2f), 0.4f);
                        break;
                    case 1:
                        for (int eighth = 0; eighth < 8; eighth++)
                        {
                            int note = chord[eighth % 3] + (eighth >= 4 ? 12 : 0);
                            int jitter = rng.NextInt(SampleRate / 200);
                            Mix(buf, barStart + eighth * beat / 2 + jitter, Pluck(SfxSynth.MidiToHz(Root + note), 0.9f, rng.NextUInt()), 0.32f);
                        }
                        break;
                    case 2:
                        int[] scale = { 0, 2, 4, 7, 9, 12, 14 };
                        for (int q = 0; q < 4; q++)
                        {
                            if (rng.NextDouble() < 0.3) continue;
                            int deg = scale[rng.NextInt(scale.Length)];
                            Mix(buf, barStart + q * beat, Marimba(SfxSynth.MidiToHz(Root + 12 + deg), 0.7f), 0.28f);
                        }
                        break;
                }
            }
            return SfxSynth.Normalize(buf, 0.5f);
        }

        private static void Mix(float[] dst, int offset, float[] src, float gain)
        {
            for (int i = 0; i < src.Length; i++)
            {
                int j = (offset + i) % dst.Length; // la cola del último compás vuelve al principio: el bucle no tiene costura
                dst[j] += src[i] * gain;
            }
        }

        private static float[] Marimba(float hz, float seconds)
        {
            int n = (int)(seconds * SampleRate);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float s = (float)Math.Sin(2 * Math.PI * hz * t) * (float)Math.Exp(-t * 3.2f)
                        + 0.35f * (float)Math.Sin(2 * Math.PI * hz * 3.93f * t) * (float)Math.Exp(-t * 11f)
                        + 0.12f * (float)Math.Sin(2 * Math.PI * hz * 9.2f * t) * (float)Math.Exp(-t * 30f);
                buf[i] = s * Math.Min(1f, t * 600f);
            }
            return buf;
        }

        /// <summary>Cuerda pulsada Karplus-Strong.</summary>
        private static float[] Pluck(float hz, float seconds, uint seed)
        {
            int n = (int)(seconds * SampleRate);
            int period = Math.Max(2, (int)(SampleRate / hz));
            var ring = new float[period];
            var rng = new Pcg32(seed);
            for (int i = 0; i < period; i++) ring[i] = rng.NextFloat() * 2f - 1f;
            var buf = new float[n];
            int idx = 0;
            for (int i = 0; i < n; i++)
            {
                int next = (idx + 1) % period;
                float v = 0.5f * (ring[idx] + ring[next]) * 0.996f;
                buf[i] = ring[idx];
                ring[idx] = v;
                idx = next;
            }
            return buf;
        }
    }
}
