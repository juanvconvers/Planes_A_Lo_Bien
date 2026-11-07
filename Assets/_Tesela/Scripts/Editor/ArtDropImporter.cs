using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tesela.Core;
using Tesela.UI;
using UnityEditor;
using UnityEngine;

namespace Tesela.EditorTools
{
    /// <summary>
    /// Fase 10: importa el arte final de ArtDrop/ sin tocar código de juego. Valida nombres contra el manifiesto,
    /// tamaño y alfa; quita el croma si hace falta; configura la importación (ASTC, sin mipmaps en interfaz, bordes
    /// 9-slice); asigna las texturas en Theme.asset y activa el interruptor. Lo que falte sigue procedural.
    /// Escribe docs/ART_SWAP_REPORT.md.
    /// </summary>
    public static class ArtDropImporter
    {
        private const string ManifestPath = "docs/ASSET_MANIFEST.json";
        private const string DropDir = "ArtDrop";
        private const string ThemePath = "Assets/_Tesela/Config/Resources/Theme.asset";

        [MenuItem("Tesela/Importar arte de ArtDrop")]
        public static void Import()
        {
            var manifest = Json.ParseNode(File.ReadAllText(ManifestPath));
            var theme = AssetDatabase.LoadAssetAtPath<Theme>(ThemePath);
            var report = new StringBuilder("# Informe de cambio de arte (Fase 10)\n\n");
            report.AppendLine("Generado por **Tesela > Importar arte de ArtDrop** el " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ".\n");
            report.AppendLine("| id | Estado | Detalle |");
            report.AppendLine("|---|---|---|");
            var known = new HashSet<string>();
            int imported = 0, missing = 0, rejected = 0;
            var icons = new List<Theme.IconOverride>();

            foreach (var a in manifest["assets"].Items)
            {
                string id = a["id"].AsString();
                known.Add(id);
                string src = Path.Combine(DropDir, id + ".png");
                if (!File.Exists(src))
                {
                    missing++;
                    report.AppendLine("| " + id + " | falta | se mantiene la versión procedural |");
                    continue;
                }
                int w = a["width"].AsInt(), h = a["height"].AsInt();
                bool needsAlpha = a["alpha"].AsBool();
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(File.ReadAllBytes(src));
                string problem = CheckSize(tex, w, h);
                if (problem != null)
                {
                    rejected++;
                    report.AppendLine("| " + id + " | rechazado | " + problem + " |");
                    UnityEngine.Object.DestroyImmediate(tex);
                    continue;
                }
                string note = "";
                if (needsAlpha && !HasTransparency(tex))
                {
                    bool magenta = id.Contains("oliva") || id.Contains("turquesa");
                    KeyChroma(tex, magenta);
                    note = "croma " + (magenta ? "#FF00FF" : "#00FF00") + " eliminado; ";
                }
                string dest = a["destination"].AsString();
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.WriteAllBytes(dest, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(dest, ImportAssetOptions.ForceUpdate);
                Configure(dest, a, Mathf.Max(w, h));
                var final = AssetDatabase.LoadAssetAtPath<Texture2D>(dest);
                Assign(theme, a["themeField"].AsString(), final, icons);
                imported++;
                report.AppendLine("| " + id + " | importado | " + note + w + "×" + h + " → " + dest + " |");
            }

            foreach (string file in Directory.Exists(DropDir) ? Directory.GetFiles(DropDir, "*.png") : new string[0])
            {
                string id = Path.GetFileNameWithoutExtension(file);
                if (!known.Contains(id) && !id.EndsWith(".orig")) report.AppendLine("| " + id + " | ignorado | no está en el manifiesto (revisa el nombre) |");
            }

            if (theme != null)
            {
                theme.icons = icons.ToArray();
                theme.useTextures = imported > 0;
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssets();
            }
            report.AppendLine();
            report.AppendLine("**Resumen:** " + imported + " importados, " + missing + " faltan (procedurales), " + rejected + " rechazados. " +
                              "Theme.useTextures = " + (imported > 0 ? "sí" : "no") + ".");
            File.WriteAllText("docs/ART_SWAP_REPORT.md", report.ToString());
            Debug.Log("[Tesela] Arte: " + imported + " importados, " + missing + " faltan, " + rejected + " rechazados. Ver docs/ART_SWAP_REPORT.md");
        }

        /// <summary>Acepta el tamaño final o el doble (se reduce al importar). La proporción debe coincidir.</summary>
        private static string CheckSize(Texture2D t, int w, int h)
        {
            if (t.width == w * 2 && t.height == h * 2) return null;
            if (t.width == w && t.height == h) return null;
            float expected = w / (float)h, actual = t.width / (float)t.height;
            if (Mathf.Abs(expected - actual) < 0.01f && t.width >= w) return null;
            return "tamaño " + t.width + "×" + t.height + ", se esperaba " + w + "×" + h + " o " + (w * 2) + "×" + (h * 2);
        }

        private static bool HasTransparency(Texture2D t)
        {
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i += 7) if (px[i].a < 250) return true;
            return false;
        }

        private static void KeyChroma(Texture2D t, bool magenta)
        {
            var px = t.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                int dominance = magenta ? Mathf.Min(c.r, c.b) - c.g : c.g - Mathf.Max(c.r, c.b);
                if (dominance > 90) c.a = 0;
                else if (dominance > 30)
                {
                    c.a = (byte)Mathf.Min(c.a, 255 * (1f - (dominance - 30) / 60f));
                    if (magenta) { c.r = c.g; c.b = c.g; } else c.g = (byte)Mathf.Max(c.r, c.b);
                }
                px[i] = c;
            }
            t.SetPixels32(px);
            t.Apply();
        }

        private static void Configure(string path, JsonNode a, int maxSize)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            bool store = a["themeField"].AsString().StartsWith("store:");
            imp.textureType = store ? TextureImporterType.Default : TextureImporterType.Sprite;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = a["alpha"].AsBool();
            imp.isReadable = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.maxTextureSize = Mathf.NextPowerOfTwo(maxSize);
            var border = a["nineSliceBorder"];
            if (border.IsArray)
            {
                int[] b = border.AsIntArray();
                // El borde del manifiesto es en el tamaño final; si llegó al doble se escala.
                imp.spriteBorder = new Vector4(b[0], b[1], b[2], b[3]);
            }
            var android = imp.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.maxTextureSize = imp.maxTextureSize;
            imp.SetPlatformTextureSettings(android);
            imp.SaveAndReimport();
        }

        private static void Assign(Theme theme, string field, Texture2D tex, List<Theme.IconOverride> icons)
        {
            if (theme == null || tex == null || string.IsNullOrEmpty(field)) return;
            if (field.StartsWith("icons:")) { icons.Add(new Theme.IconOverride { id = field.Substring(6), texture = tex }); return; }
            if (field.StartsWith("store:")) return;
            int bracket = field.IndexOf('[');
            if (bracket > 0)
            {
                string name = field.Substring(0, bracket);
                int index = int.Parse(field.Substring(bracket + 1, field.Length - bracket - 2));
                if (name == "tileTextures") { EnsureSize(ref theme.tileTextures, index + 1); theme.tileTextures[index] = tex; }
                else if (name == "mosaics") { EnsureSize(ref theme.mosaics, index + 1); theme.mosaics[index] = tex; }
                return;
            }
            switch (field)
            {
                case "socketTexture": theme.socketTexture = tex; break;
                case "panelFrame": theme.panelFrame = tex; break;
                case "buttonPrimaryFrame": theme.buttonPrimaryFrame = tex; break;
                case "buttonSecondaryFrame": theme.buttonSecondaryFrame = tex; break;
                case "background": theme.background = tex; break;
            }
        }

        private static void EnsureSize(ref Texture2D[] arr, int size)
        {
            if (arr == null) arr = new Texture2D[size];
            if (arr.Length < size) Array.Resize(ref arr, size);
        }
    }
}
