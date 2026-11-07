using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tesela.Core
{
    /// <summary>
    /// Lector/escritor JSON mínimo y sin dependencias. Los ensamblados puros no pueden usar JsonUtility,
    /// y los datos (niveles, economía, ofertas) deben leerse igual en el editor, el teléfono y los tests.
    /// Objetos → Dictionary&lt;string, object&gt;, arreglos → List&lt;object&gt;, números → double, o long/ulong si son enteros.
    /// </summary>
    public static class Json
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var p = new Parser(json);
            object value = p.ParseValue();
            p.SkipWhitespace();
            if (!p.AtEnd) throw new FormatException("JSON: contenido extra en la posición " + p.Position);
            return value;
        }

        public static JsonNode ParseNode(string json) => new JsonNode(Parse(json));

        public static string Serialize(object value, bool pretty = false)
        {
            var sb = new StringBuilder(256);
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object value, bool pretty, int indent)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case JsonNode node:
                    Write(sb, node.Raw, pretty, indent);
                    break;
                case string s:
                    WriteString(sb, s);
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case double d:
                    WriteDouble(sb, d);
                    break;
                case float f:
                    WriteDouble(sb, f);
                    break;
                case int or long or short or byte or uint or ushort or sbyte:
                    sb.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                    break;
                case ulong ul:
                    sb.Append(ul.ToString(CultureInfo.InvariantCulture));
                    break;
                case IDictionary dict:
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (DictionaryEntry e in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        NewLine(sb, pretty, indent + 1);
                        WriteString(sb, Convert.ToString(e.Key, CultureInfo.InvariantCulture));
                        sb.Append(pretty ? ": " : ":");
                        Write(sb, e.Value, pretty, indent + 1);
                    }
                    if (!first) NewLine(sb, pretty, indent);
                    sb.Append('}');
                    break;
                }
                case IEnumerable list:
                {
                    sb.Append('[');
                    bool first = true;
                    foreach (object item in list)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        NewLine(sb, pretty, indent + 1);
                        Write(sb, item, pretty, indent + 1);
                    }
                    if (!first) NewLine(sb, pretty, indent);
                    sb.Append(']');
                    break;
                }
                default:
                    WriteString(sb, Convert.ToString(value, CultureInfo.InvariantCulture));
                    break;
            }
        }

        private static void NewLine(StringBuilder sb, bool pretty, int indent)
        {
            if (!pretty) return;
            sb.Append('\n');
            sb.Append(' ', indent * 2);
        }

        private static void WriteDouble(StringBuilder sb, double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append('0'); return; }
            if (Math.Abs(d % 1) < double.Epsilon && Math.Abs(d) < 9e15)
                sb.Append(((long)d).ToString(CultureInfo.InvariantCulture));
            else
                sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s; }
            public bool AtEnd => _i >= _s.Length;
            public int Position => _i;

            public void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (AtEnd) throw new FormatException("JSON: fin inesperado");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw new FormatException("JSON: carácter inesperado '" + c + "' en " + _i);
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0)
                    throw new FormatException("JSON: se esperaba " + word + " en " + _i);
                _i += word.Length;
            }

            private Dictionary<string, object> ParseObject()
            {
                var dict = new Dictionary<string, object>();
                _i++;
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == '}') { _i++; return dict; }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != '"') throw new FormatException("JSON: se esperaba clave en " + _i);
                    string key = ParseString();
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != ':') throw new FormatException("JSON: se esperaba ':' en " + _i);
                    _i++;
                    dict[key] = ParseValue();
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("JSON: objeto sin cerrar");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return dict; }
                    throw new FormatException("JSON: se esperaba ',' o '}' en " + _i);
                }
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _i++;
                SkipWhitespace();
                if (_i < _s.Length && _s[_i] == ']') { _i++; return list; }
                while (true)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("JSON: arreglo sin cerrar");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return list; }
                    throw new FormatException("JSON: se esperaba ',' o ']' en " + _i);
                }
            }

            private string ParseString()
            {
                var sb = new StringBuilder();
                _i++;
                while (true)
                {
                    if (AtEnd) throw new FormatException("JSON: cadena sin cerrar");
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw new FormatException("JSON: escape incompleto");
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw new FormatException("JSON: \\u incompleto");
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default: throw new FormatException("JSON: escape inválido \\" + e);
                    }
                }
            }

            private object ParseNumber()
            {
                int start = _i;
                if (_s[_i] == '-') _i++;
                bool isFloat = false;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c >= '0' && c <= '9') { _i++; continue; }
                    if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { isFloat = true; _i++; continue; }
                    break;
                }
                string token = _s.Substring(start, _i - start);
                if (!isFloat && long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
                if (!isFloat && ulong.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ul)) return ul;
                return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>Envoltorio de lectura tolerante: las claves que faltan devuelven el valor por defecto en lugar de lanzar.</summary>
    public readonly struct JsonNode
    {
        public readonly object Raw;

        public JsonNode(object raw) { Raw = raw; }

        public bool IsNull => Raw == null;
        public bool IsObject => Raw is Dictionary<string, object>;
        public bool IsArray => Raw is List<object>;

        public JsonNode this[string key]
        {
            get
            {
                if (Raw is Dictionary<string, object> d && d.TryGetValue(key, out object v)) return new JsonNode(v);
                return default;
            }
        }

        public JsonNode this[int index]
        {
            get
            {
                if (Raw is List<object> l && index >= 0 && index < l.Count) return new JsonNode(l[index]);
                return default;
            }
        }

        public bool Has(string key) => Raw is Dictionary<string, object> d && d.ContainsKey(key);

        public int Count => Raw is List<object> l ? l.Count : Raw is Dictionary<string, object> d ? d.Count : 0;

        public IEnumerable<JsonNode> Items
        {
            get
            {
                if (Raw is List<object> l)
                    foreach (object o in l) yield return new JsonNode(o);
            }
        }

        public IEnumerable<KeyValuePair<string, JsonNode>> Properties
        {
            get
            {
                if (Raw is Dictionary<string, object> d)
                    foreach (var kv in d) yield return new KeyValuePair<string, JsonNode>(kv.Key, new JsonNode(kv.Value));
            }
        }

        public string AsString(string fallback = null) => Raw switch
        {
            null => fallback,
            string s => s,
            bool b => b ? "true" : "false",
            _ => Convert.ToString(Raw, CultureInfo.InvariantCulture)
        };

        public double AsDouble(double fallback = 0) => Raw switch
        {
            double d => d,
            long l => l,
            ulong u => u,
            string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double p) => p,
            bool b => b ? 1 : 0,
            IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
            _ => fallback
        };

        public float AsFloat(float fallback = 0f) => (float)AsDouble(fallback);
        public int AsInt(int fallback = 0) => Raw == null ? fallback : (int)Math.Round(AsDouble(fallback));
        public long AsLong(long fallback = 0) => Raw is long l ? l : Raw == null ? fallback : (long)Math.Round(AsDouble(fallback));

        public ulong AsULong(ulong fallback = 0) => Raw switch
        {
            ulong u => u,
            long l => unchecked((ulong)l),
            double d => (ulong)d,
            string s when ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong p) => p,
            IConvertible c => c.ToUInt64(CultureInfo.InvariantCulture),
            _ => fallback
        };

        public bool AsBool(bool fallback = false) => Raw switch
        {
            bool b => b,
            long l => l != 0,
            double d => Math.Abs(d) > double.Epsilon,
            string s => s == "true" || s == "1",
            _ => fallback
        };

        public int[] AsIntArray()
        {
            if (!(Raw is List<object> l)) return Array.Empty<int>();
            var result = new int[l.Count];
            for (int i = 0; i < l.Count; i++) result[i] = new JsonNode(l[i]).AsInt();
            return result;
        }

        public float[] AsFloatArray()
        {
            if (!(Raw is List<object> l)) return Array.Empty<float>();
            var result = new float[l.Count];
            for (int i = 0; i < l.Count; i++) result[i] = new JsonNode(l[i]).AsFloat();
            return result;
        }

        public string[] AsStringArray()
        {
            if (!(Raw is List<object> l)) return Array.Empty<string>();
            var result = new string[l.Count];
            for (int i = 0; i < l.Count; i++) result[i] = new JsonNode(l[i]).AsString(string.Empty);
            return result;
        }
    }
}
