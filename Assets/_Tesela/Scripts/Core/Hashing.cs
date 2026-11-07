using System.Text;

namespace Tesela.Core
{
    public static class Hashing
    {
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static ulong Fnv1a64(string text)
        {
            ulong hash = FnvOffset;
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash = unchecked(hash * FnvPrime);
            }
            return hash;
        }

        public static string Fnv1a64Hex(string text) => Fnv1a64(text).ToString("x16");

        /// <summary>Mezclador SplitMix64: convierte enteros cercanos (fechas, índices) en semillas bien repartidas.</summary>
        public static ulong Mix(ulong x)
        {
            x = unchecked(x + 0x9E3779B97F4A7C15UL);
            x = unchecked((x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL);
            x = unchecked((x ^ (x >> 27)) * 0x94D049BB133111EBUL);
            return x ^ (x >> 31);
        }

        /// <summary>Semilla legible de 8 caracteres (sin 0/O/1/I) para mostrar al jugador.</summary>
        public static string SeedToCode(ulong seed)
        {
            const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            var sb = new StringBuilder(8);
            ulong v = seed;
            for (int i = 0; i < 8; i++)
            {
                sb.Append(alphabet[(int)(v & 31UL)]);
                v >>= 5;
            }
            return sb.ToString();
        }

        public static bool TryCodeToSeed(string code, out ulong seed)
        {
            const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            seed = 0;
            if (string.IsNullOrEmpty(code) || code.Length != 8) return false;
            for (int i = 7; i >= 0; i--)
            {
                int idx = alphabet.IndexOf(char.ToUpperInvariant(code[i]));
                if (idx < 0) return false;
                seed = (seed << 5) | (uint)idx;
            }
            return true;
        }
    }
}
