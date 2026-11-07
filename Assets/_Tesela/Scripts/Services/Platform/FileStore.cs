using System;
using System.IO;
using Tesela.Core;
using UnityEngine;

namespace Tesela.Services
{
    /// <summary>
    /// Guardado en archivos con escritura atómica: se escribe a .tmp y se reemplaza, conservando una copia .bak.
    /// Si la app se cierra a mitad de escritura (o un anuncio la cuelga), el archivo anterior sigue intacto.
    /// </summary>
    public sealed class FileStore : IKeyValueStore
    {
        private readonly string _dir;

        public FileStore(string directory = null)
        {
            _dir = directory ?? Path.Combine(Application.persistentDataPath, "tesela");
            Directory.CreateDirectory(_dir);
        }

        private string PathFor(string key) => Path.Combine(_dir, key + ".json");

        public string Read(string key)
        {
            string path = PathFor(key);
            try
            {
                if (File.Exists(path)) return File.ReadAllText(path);
                if (File.Exists(path + ".bak")) return File.ReadAllText(path + ".bak");
            }
            catch (IOException) { }
            return null;
        }

        /// <summary>Lee y valida con checksum; si el principal está dañado, prueba la copia .bak.</summary>
        public bool TryReadVerified(string key, out JsonNode payload)
        {
            string path = PathFor(key);
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                try
                {
                    if (File.Exists(candidate) && SaveCodec.TryUnwrap(File.ReadAllText(candidate), out payload, out _)) return true;
                }
                catch (IOException) { }
            }
            payload = default;
            return false;
        }

        public void Write(string key, string value)
        {
            string path = PathFor(key);
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, value);
                if (File.Exists(path)) File.Replace(tmp, path, path + ".bak");
                else File.Move(tmp, path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is PlatformNotSupportedException)
            {
                // File.Replace no existe en algunos sistemas de archivos: se cae a copia + movimiento.
                try
                {
                    if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                    File.Copy(tmp, path, true);
                    File.Delete(tmp);
                }
                catch (IOException) { }
            }
        }

        public void Delete(string key)
        {
            string path = PathFor(key);
            foreach (string p in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
        }

        public void DeleteAll()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            Directory.CreateDirectory(_dir);
        }
    }
}
