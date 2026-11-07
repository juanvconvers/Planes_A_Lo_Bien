using System;
using System.Collections.Generic;

namespace Tesela.Core
{
    /// <summary>
    /// Envoltorio de guardado con checksum. El contenido se guarda como cadena para que el checksum se calcule
    /// sobre los bytes exactos (re-serializar podría cambiar el orden o el formato de números).
    /// El checksum detecta corrupción (cierre a mitad de escritura), no pretende ser antitrampas.
    /// </summary>
    public static class SaveCodec
    {
        public const int Version = 1;
        private const string Salt = "tesela:azulejo:v1:";

        public static string Wrap(object payload)
        {
            string body = Json.Serialize(payload);
            var envelope = new Dictionary<string, object>
            {
                { "v", Version },
                { "payload", body },
                { "sum", Checksum(body) }
            };
            return Json.Serialize(envelope);
        }

        public static bool TryUnwrap(string text, out JsonNode payload, out string error)
        {
            payload = default;
            error = null;
            if (string.IsNullOrEmpty(text)) { error = "vacío"; return false; }
            try
            {
                var env = Json.ParseNode(text);
                string body = env["payload"].AsString();
                string sum = env["sum"].AsString();
                if (body == null || sum == null) { error = "formato"; return false; }
                if (env["v"].AsInt() > Version) { error = "versión futura"; return false; }
                if (!string.Equals(Checksum(body), sum, StringComparison.Ordinal)) { error = "checksum"; return false; }
                payload = Json.ParseNode(body);
                return true;
            }
            catch (FormatException e)
            {
                error = e.Message;
                return false;
            }
        }

        public static string Checksum(string body) => Hashing.Fnv1a64Hex(Salt + body);
    }
}
