using Tesela.Core;
using Tesela.Core.Raster;
using Tesela.Gameplay;
using Tesela.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Tesela.UI
{
    /// <summary>
    /// Vista del tablero. Capas: casillas (estáticas, canvas propio), azulejos, superposiciones, sombra de destino y
    /// resaltado de líneas. Todas las imágenes comparten el atlas → un draw call por capa.
    /// </summary>
    public sealed class BoardView
    {
        public readonly RectTransform Root;
        public readonly float Cell;
        public readonly int Width, Height;

        private readonly Image[] _tiles;
        private readonly Image[] _overlays;
        private readonly Image[] _ghost;
        private readonly Image[] _rowGlow;
        private readonly Image[] _colGlow;
        private readonly Image[] _sockets;
        private readonly byte[] _rotation;
        private readonly float[] _tint;
        private readonly ArtLibrary _art;
        private uint _variation = 1;

        public BoardView(Transform parent, int width, int height, float size, ArtLibrary art)
        {
            _art = art;
            Width = width;
            Height = height;
            Cell = size / Mathf.Max(width, height);
            var frame = Ui.Panel(parent, "board_frame", FrameStyle.Board);
            frame.rectTransform.sizeDelta = new Vector2(Cell * width + 44, Cell * height + 44);
            Root = Ui.Rect(frame.transform, "board");
            Root.Place(0.5f, 0.5f, Cell * width, Cell * height, -2, 2);

            var socketLayer = Ui.Rect(Root, "sockets").Stretch();
            socketLayer.gameObject.AddComponent<Canvas>();
            var rowLayer = Ui.Rect(Root, "glow").Stretch();
            var tileLayer = Ui.Rect(Root, "tiles").Stretch();
            tileLayer.gameObject.AddComponent<Canvas>();
            var ghostLayer = Ui.Rect(Root, "ghost").Stretch();

            int n = width * height;
            _sockets = new Image[n];
            _tiles = new Image[n];
            _overlays = new Image[n];
            _ghost = new Image[n];
            _rotation = new byte[n];
            _tint = new float[n];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    Vector2 c = CellCenter(x, y);
                    _sockets[i] = MakeCell(socketLayer, art.Socket, c, Color.white);
                    _tiles[i] = MakeCell(tileLayer, null, c, Color.white);
                    _tiles[i].enabled = false;
                    _overlays[i] = MakeCell(tileLayer, null, c, Color.white);
                    _overlays[i].enabled = false;
                    _ghost[i] = MakeCell(ghostLayer, null, c, new Color(1, 1, 1, 0.42f));
                    _ghost[i].enabled = false;
                }
            _rowGlow = new Image[height];
            _colGlow = new Image[width];
            Color glow = Palette.Gold.ToColor(0.38f);
            for (int y = 0; y < height; y++)
            {
                _rowGlow[y] = Ui.Image(rowLayer, "row" + y, art.White, glow);
                _rowGlow[y].rectTransform.Place(0.5f, 0.5f, Cell * width, Cell * 0.92f, 0, CellCenter(0, y).y);
                _rowGlow[y].enabled = false;
            }
            for (int x = 0; x < width; x++)
            {
                _colGlow[x] = Ui.Image(rowLayer, "col" + x, art.White, glow);
                _colGlow[x].rectTransform.Place(0.5f, 0.5f, Cell * 0.92f, Cell * height, CellCenter(x, 0).x, 0);
                _colGlow[x].enabled = false;
            }
        }

        private Image MakeCell(Transform layer, Sprite sprite, Vector2 center, Color color)
        {
            var img = Ui.Image(layer, "c", sprite, color);
            img.rectTransform.Place(0.5f, 0.5f, Cell, Cell, center.x, center.y);
            return img;
        }

        /// <summary>Centro de la celda en coordenadas locales del tablero (y = 0 es la fila superior).</summary>
        public Vector2 CellCenter(int x, int y) => new Vector2((x + 0.5f) * Cell - Width * Cell * 0.5f, Height * Cell * 0.5f - (y + 0.5f) * Cell);

        /// <summary>Celda de origen (esquina superior izquierda) para una pieza cuyo centro está en <paramref name="localCenter"/>.</summary>
        public Vector2Int OriginFor(PieceShape shape, Vector2 localCenter)
        {
            float left = localCenter.x - shape.Width * Cell * 0.5f;
            float top = localCenter.y + shape.Height * Cell * 0.5f;
            int gx = Mathf.RoundToInt((left + Width * Cell * 0.5f) / Cell);
            int gy = Mathf.RoundToInt((Height * Cell * 0.5f - top) / Cell);
            return new Vector2Int(gx, gy);
        }

        public Vector2 PieceCenter(PieceShape shape, int gx, int gy)
        {
            Vector2 a = CellCenter(gx, gy);
            return a + new Vector2((shape.Width - 1) * Cell * 0.5f, -(shape.Height - 1) * Cell * 0.5f);
        }

        /// <summary>Redibuja todo desde el modelo. La variación (giro del patrón y tono ±3 %) se fija al aparecer cada azulejo.</summary>
        public void Sync(Board board)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    bool occ = board.IsOccupied(x, y);
                    var tile = _tiles[i];
                    if (!occ)
                    {
                        tile.enabled = false;
                        _overlays[i].enabled = false;
                        continue;
                    }
                    if (!tile.enabled)
                    {
                        _variation = _variation * 1664525u + 1013904223u;
                        _rotation[i] = (byte)(_variation >> 30);
                        _tint[i] = 0.97f + ((_variation >> 8) & 0xFF) / 255f * 0.06f;
                        tile.enabled = true;
                        tile.rectTransform.localScale = Vector3.one;
                    }
                    byte glaze = board.GetGlaze(x, y);
                    tile.sprite = _art.Tile(glaze, _rotation[i]);
                    float k = _tint[i];
                    tile.color = new Color(k, k, k, 1f);
                    var ov = OverlayFor(board.GetModifier(x, y), board.GetHp(x, y));
                    _overlays[i].enabled = ov != TileOverlay.None;
                    if (ov != TileOverlay.None) _overlays[i].sprite = _art.Overlay(ov);
                }
        }

        private static TileOverlay OverlayFor(CellModifier m, int hp)
        {
            switch (m)
            {
                case CellModifier.Cracked: return hp >= 2 ? TileOverlay.CrackedHeavy : TileOverlay.Cracked;
                case CellModifier.Mud: return TileOverlay.Mud;
                case CellModifier.Gold: return TileOverlay.Gold;
                case CellModifier.Bomb: return TileOverlay.Bomb;
                default: return TileOverlay.None;
            }
        }

        public RectTransform TileRect(int x, int y) => _tiles[y * Width + x].rectTransform;
        public Image TileImage(int x, int y) => _tiles[y * Width + x];

        public void ShowGhost(PieceShape shape, int gx, int gy, byte glaze, bool[] rows, bool[] cols)
        {
            ClearGhost();
            if (shape == null) return;
            foreach (var c in shape.Cells)
            {
                int x = gx + c.X, y = gy + c.Y;
                if (x < 0 || y < 0 || x >= Width || y >= Height) continue;
                var g = _ghost[y * Width + x];
                g.sprite = _art.Tile(glaze);
                g.enabled = true;
            }
            for (int y = 0; y < Height; y++) _rowGlow[y].enabled = rows != null && rows[y];
            for (int x = 0; x < Width; x++) _colGlow[x].enabled = cols != null && cols[x];
        }

        public void ClearGhost()
        {
            for (int i = 0; i < _ghost.Length; i++) _ghost[i].enabled = false;
            for (int y = 0; y < Height; y++) _rowGlow[y].enabled = false;
            for (int x = 0; x < Width; x++) _colGlow[x].enabled = false;
        }

        /// <summary>Squash &amp; stretch leve al asentar las piezas.</summary>
        public void Settle(PieceShape shape, int gx, int gy)
        {
            foreach (var c in shape.Cells)
            {
                var rt = TileRect(gx + c.X, gy + c.Y);
                Motion.Instance.Scale(rt, new Vector3(1.1f, 0.9f, 1f), Vector3.one, 0.18f, Ease.Settle);
            }
        }
    }
}
