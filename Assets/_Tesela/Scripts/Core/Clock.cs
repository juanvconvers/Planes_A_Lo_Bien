using System;

namespace Tesela.Core
{
    public interface IClock
    {
        DateTime UtcNow { get; }
        /// <summary>Segundos monótonos desde el arranque; no cambian si el jugador ajusta la hora del teléfono.</summary>
        double RealtimeSeconds { get; }
    }

    public sealed class SystemClock : IClock
    {
        private readonly System.Diagnostics.Stopwatch _watch = System.Diagnostics.Stopwatch.StartNew();
        public DateTime UtcNow => DateTime.UtcNow;
        public double RealtimeSeconds => _watch.Elapsed.TotalSeconds;
    }

    public sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; }
        public double RealtimeSeconds { get; set; }

        public FakeClock(DateTime start)
        {
            UtcNow = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        }

        public void Advance(TimeSpan span)
        {
            UtcNow += span;
            RealtimeSeconds += span.TotalSeconds;
        }
    }

    public static class DayKey
    {
        /// <summary>Número de día UTC (días desde 2000-01-01). Clave estable para misiones, reto diario y topes diarios.</summary>
        public static int FromUtc(DateTime utc)
        {
            return (int)(utc.Date - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
        }

        public static DateTime ToUtcDate(int day) => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(day);

        /// <summary>Semana ISO aproximada: bloques de 7 días que empiezan en lunes.</summary>
        public static int WeekFromDay(int day) => (day + 5) / 7; // 2000-01-03 fue lunes
    }
}
