using Tesela.Core;

namespace Tesela.Gameplay
{
    /// <summary>Semillas de partida. El reto diario usa la fecha UTC: todos juegan exactamente la misma secuencia.</summary>
    public static class Seeds
    {
        public static ulong Daily(int utcDay) => Hashing.Mix(0x7E5E1A00DA11UL ^ (ulong)utcDay) & ((1UL << 40) - 1);

        /// <summary>Semilla aleatoria para Clásico/Zen (40 bits: cabe en el código de 8 caracteres).</summary>
        public static ulong Random(long ticks) => Hashing.Mix((ulong)ticks) & ((1UL << 40) - 1);

        public static ulong Event(string eventId, int utcWeek) => Hashing.Mix(Hashing.Fnv1a64(eventId) ^ (ulong)utcWeek) & ((1UL << 40) - 1);
    }
}
