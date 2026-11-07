using System;
using System.IO;
using System.IO.Compression;
using Tesela.Core;
using Tesela.Core.Raster;

namespace Tesela.Tools
{
    /// <summary>Hoja de contacto del arte procedural (azulejos, superposiciones, marcos, iconos) en PNG para revisión visual.</summary>
    public static class PreviewTool
    {
        public static int Run(string outPath)
        {
            int cell = 128, cols = 8;
            int rows = 2 + 1 + (IconRasterizer.Ids.Length + cols - 1) / cols;
            var sheet = new RasterCanvas(cols * cell, rows * cell);
            for (int i = 0; i < sheet.Pixels.Length; i++) sheet.Pixels[i] = Palette.Sand;
            int r = rows - 1;
            for (int g = 1; g <= 6; g++) Blit(sheet, TileRasterizer.Render(cell, Palette.Glaze(g), Palette.Pattern(g), g * 17), g - 1, r);
            Blit(sheet, TileRasterizer.RenderSocket(cell, 3), 6, r);
            r--;
            var baseTile = TileRasterizer.Render(cell, Palette.Ochre, GlazePattern.None, 5);
            int col = 0;
            foreach (TileOverlay o in new[] { TileOverlay.Cracked, TileOverlay.CrackedHeavy, TileOverlay.Mud, TileOverlay.Gold, TileOverlay.Bomb })
            {
                Blit(sheet, baseTile, col, r);
                Blit(sheet, TileRasterizer.RenderOverlay(cell, o, 4), col, r);
                col++;
            }
            Blit(sheet, UiRasterizer.RenderGrain(cell, 2, 60), col, r);
            r--;
            col = 0;
            foreach (FrameStyle f in Enum.GetValues(typeof(FrameStyle))) Blit(sheet, UiRasterizer.RenderFrame(f, 9), col++, r);
            r--;
            for (int i = 0; i < IconRasterizer.Ids.Length; i++)
            {
                var icon = IconRasterizer.Render(IconRasterizer.Ids[i]);
                var tinted = new RasterCanvas(cell, cell);
                for (int p = 0; p < icon.Pixels.Length; p++) tinted.Pixels[p] = Palette.Indigo.WithAlpha(icon.Pixels[p].A);
                Blit(sheet, tinted, i % cols, r - i / cols);
            }
            File.WriteAllBytes(outPath, Png(sheet));
            Console.WriteLine(outPath);
            return 0;
        }

        private static void Blit(RasterCanvas dst, RasterCanvas src, int cx, int cy)
        {
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                    dst.Blend(cx * src.Width + x, cy * src.Height + y, src.Get(x, y), 1f);
        }

        private static byte[] Png(RasterCanvas c)
        {
            var raw = new MemoryStream();
            for (int y = c.Height - 1; y >= 0; y--)
            {
                raw.WriteByte(0);
                for (int x = 0; x < c.Width; x++) { var p = c.Get(x, y); raw.WriteByte(p.R); raw.WriteByte(p.G); raw.WriteByte(p.B); raw.WriteByte(p.A); }
            }
            var z = new MemoryStream();
            using (var zs = new ZLibStream(z, CompressionLevel.Optimal, true)) raw.WriteTo(zs);
            var o = new MemoryStream();
            o.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
            var hdr = new MemoryStream();
            WriteBE(hdr, c.Width); WriteBE(hdr, c.Height); hdr.Write(new byte[] { 8, 6, 0, 0, 0 });
            Chunk(o, "IHDR", hdr.ToArray()); Chunk(o, "IDAT", z.ToArray()); Chunk(o, "IEND", Array.Empty<byte>());
            return o.ToArray();
        }

        private static void WriteBE(Stream s, int v) { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }

        private static void Chunk(Stream o, string type, byte[] data)
        {
            WriteBE(o, data.Length);
            var td = new byte[4 + data.Length];
            System.Text.Encoding.ASCII.GetBytes(type).CopyTo(td, 0);
            data.CopyTo(td, 4);
            o.Write(td);
            WriteBE(o, (int)Crc(td));
        }

        private static uint Crc(byte[] d)
        {
            uint c = 0xFFFFFFFF;
            foreach (byte b in d) { c ^= b; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; }
            return ~c;
        }
    }
}
