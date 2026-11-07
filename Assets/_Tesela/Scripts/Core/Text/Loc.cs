using System;
using System.Collections.Generic;
using System.Globalization;

namespace Tesela.Core
{
    /// <summary>Tablas de localización JSON propias (es, en, pt). Si falta una clave, cae al español y luego a la clave.</summary>
    public static class Loc
    {
        public static readonly string[] Languages = { "es", "en", "pt" };
        public const string Fallback = "es";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();

        public static string Language { get; private set; } = Fallback;

        public static event Action Changed;

        public static void Load(string language, string json)
        {
            var table = new Dictionary<string, string>();
            foreach (var kv in Json.ParseNode(json).Properties)
                if (!kv.Key.StartsWith("_", StringComparison.Ordinal)) table[kv.Key] = kv.Value.AsString(string.Empty);
            Tables[language] = table;
        }

        public static IReadOnlyDictionary<string, string> Table(string language) =>
            Tables.TryGetValue(language, out var t) ? t : new Dictionary<string, string>();

        public static void SetLanguage(string language)
        {
            Language = Array.IndexOf(Languages, language) >= 0 ? language : Fallback;
            Changed?.Invoke();
        }

        /// <summary>Idioma del sistema (código ISO de dos letras) → idioma soportado.</summary>
        public static string FromSystem(string iso2)
        {
            if (string.IsNullOrEmpty(iso2)) return Fallback;
            string l = iso2.ToLowerInvariant();
            return Array.IndexOf(Languages, l) >= 0 ? l : "en";
        }

        public static bool Has(string key) => Tables.TryGetValue(Fallback, out var t) && t.ContainsKey(key);

        public static string T(string key)
        {
            if (Tables.TryGetValue(Language, out var t) && t.TryGetValue(key, out string v) && v.Length > 0) return v;
            if (Tables.TryGetValue(Fallback, out t) && t.TryGetValue(key, out v)) return v;
            return key;
        }

        public static string T(string key, params object[] args)
        {
            try { return string.Format(CultureInfo.InvariantCulture, T(key), args); }
            catch (FormatException) { return T(key); }
        }

        /// <summary>Números con separador de miles según el idioma (12.345 / 12,345).</summary>
        public static string Number(long n)
        {
            string s = n.ToString("N0", CultureInfo.InvariantCulture);
            return Language == "en" ? s : s.Replace(',', '.');
        }
    }
}
