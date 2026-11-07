using System;

namespace Tesela.Core
{
    /// <summary>PCG32 (XSH RR). Determinista entre plataformas: la misma semilla da la misma partida en cualquier dispositivo.</summary>
    [Serializable]
    public struct Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;

        public ulong State;
        public ulong Increment;

        public Pcg32(ulong seed, ulong stream = 0xda3e39cb94b95bdbUL)
        {
            State = 0UL;
            Increment = (stream << 1) | 1UL;
            NextUInt();
            State += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = State;
            State = unchecked(old * Multiplier + Increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>Entero uniforme en [0, maxExclusive) sin sesgo de módulo.</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            uint bound = (uint)maxExclusive;
            uint threshold = unchecked((uint)(-(int)bound)) % bound;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % bound);
            }
        }

        public int NextInt(int minInclusive, int maxExclusive) => minInclusive + NextInt(maxExclusive - minInclusive);

        /// <summary>Flotante uniforme en [0, 1).</summary>
        public double NextDouble() => NextUInt() * (1.0 / 4294967296.0);

        public float NextFloat() => (float)NextDouble();
    }
}
