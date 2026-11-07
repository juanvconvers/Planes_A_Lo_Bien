using System;
using Tesela.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tesela.UI
{
    /// <summary>Recibe los toques de una casilla de la bandeja y los pasa a la pantalla de juego.</summary>
    public sealed class TraySlotInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public int Slot;
        public Action<int, PointerEventData> Down, Drag, Up;

        public void OnPointerDown(PointerEventData e) => Down?.Invoke(Slot, e);
        public void OnDrag(PointerEventData e) => Drag?.Invoke(Slot, e);
        public void OnPointerUp(PointerEventData e) => Up?.Invoke(Slot, e);
    }

    /// <summary>Una pieza dibujada con hasta 9 imágenes de azulejo (para la bandeja, la reserva y la pieza arrastrada).</summary>
    public sealed class PieceVisual
    {
        public readonly RectTransform Root;
        private readonly Image[] _cells = new Image[9];
        private readonly ArtLibrary _art;
        public PieceShape Shape { get; private set; }
        public float CellSize { get; private set; }

        public PieceVisual(Transform parent, string name, ArtLibrary art)
        {
            _art = art;
            Root = Ui.Rect(parent, name);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = Ui.Image(Root, "c" + i, null, Color.white);
                _cells[i].enabled = false;
            }
        }

        public void Show(TrayPiece piece, float cell, float alpha = 1f)
        {
            Shape = piece.Shape;
            CellSize = cell;
            for (int i = 0; i < _cells.Length; i++) _cells[i].enabled = false;
            if (piece.IsEmpty)
            {
                Root.gameObject.SetActive(false);
                return;
            }
            Root.gameObject.SetActive(true);
            Root.sizeDelta = new Vector2(piece.Shape.Width * cell, piece.Shape.Height * cell);
            var sprite = _art.Tile(piece.Glaze);
            for (int i = 0; i < piece.Shape.Cells.Length && i < _cells.Length; i++)
            {
                var c = piece.Shape.Cells[i];
                var img = _cells[i];
                img.enabled = true;
                img.sprite = sprite;
                img.color = new Color(1, 1, 1, alpha);
                img.rectTransform.Place(0.5f, 0.5f, cell, cell,
                    (c.X + 0.5f) * cell - piece.Shape.Width * cell * 0.5f,
                    piece.Shape.Height * cell * 0.5f - (c.Y + 0.5f) * cell);
            }
            if (piece.Golden)
                for (int i = 0; i < piece.Shape.Cells.Length && i < _cells.Length; i++) _cells[i].color = new Color(1f, 0.93f, 0.7f, alpha);
        }

        public void SetAlpha(float a)
        {
            foreach (var img in _cells)
                if (img.enabled) img.color = new Color(img.color.r, img.color.g, img.color.b, a);
        }

        public Vector2 CellLocalCenter(int index)
        {
            var c = Shape.Cells[index];
            return new Vector2((c.X + 0.5f) * CellSize - Shape.Width * CellSize * 0.5f, Shape.Height * CellSize * 0.5f - (c.Y + 0.5f) * CellSize);
        }
    }

    /// <summary>Bandeja de 3 piezas (más la casilla de reserva si se desbloquea).</summary>
    public sealed class TrayView
    {
        public const float PreviewScale = 0.52f;
        public readonly RectTransform Root;
        public readonly RectTransform[] Slots = new RectTransform[FairBag.TraySize];
        public readonly PieceVisual[] Pieces = new PieceVisual[FairBag.TraySize];
        public readonly TraySlotInput[] Inputs = new TraySlotInput[FairBag.TraySize];
        public readonly RectTransform ReserveSlot;
        public readonly PieceVisual ReservePiece;
        public readonly TeselaButton ReserveButton;
        private readonly float _boardCell;

        public TrayView(Transform parent, float width, float height, float boardCell, ArtLibrary art, Action onReserveTap)
        {
            _boardCell = boardCell;
            Root = Ui.Rect(parent, "tray");
            Root.sizeDelta = new Vector2(width, height);
            float slotW = width / 3.4f;
            for (int i = 0; i < Slots.Length; i++)
            {
                var slot = Ui.Image(Root, "slot" + i, art.White, new Color(1, 1, 1, 0.001f), true);
                slot.rectTransform.Place(0.5f, 0.5f, slotW, height, (i - 1) * slotW * 1.08f, 0);
                Slots[i] = slot.rectTransform;
                Inputs[i] = slot.gameObject.AddComponent<TraySlotInput>();
                Inputs[i].Slot = i;
                Pieces[i] = new PieceVisual(slot.transform, "piece", art);
            }
            ReserveButton = Ui.IconButton(Root, "reserve", onReserveTap, height * 0.62f);
            ReserveSlot = (RectTransform)ReserveButton.transform;
            ReserveSlot.Place(1f, 0.5f, height * 0.62f, height * 0.62f, height * 0.25f, 0);
            ReservePiece = new PieceVisual(ReserveSlot, "reservePiece", art);
            ReserveSlot.gameObject.SetActive(false);
        }

        public void Sync(GameSession s)
        {
            for (int i = 0; i < Pieces.Length; i++)
            {
                var p = s.Tray[i];
                bool fits = !p.IsEmpty && s.SlotFits(i);
                Pieces[i].Show(p, _boardCell * PreviewScale, p.IsEmpty ? 1f : fits ? 1f : 0.32f);
                Pieces[i].Root.anchoredPosition = Vector2.zero;
                Pieces[i].Root.localScale = Vector3.one;
            }
            ReserveSlot.gameObject.SetActive(s.ReserveUnlocked);
            if (s.ReserveUnlocked)
            {
                ReservePiece.Show(s.Reserve, _boardCell * 0.26f);
                if (ReserveButton.IconImage != null) ReserveButton.IconImage.enabled = s.Reserve.IsEmpty;
            }
        }

        /// <summary>Animación de llegada de una bandeja nueva: las piezas entran desde abajo escalonadas.</summary>
        public void AnimateRefill()
        {
            for (int i = 0; i < Pieces.Length; i++)
            {
                var rt = Pieces[i].Root;
                rt.anchoredPosition = new Vector2(0, -80);
                Motion.Instance.Move(rt, Vector2.zero, 0.26f, Ease.Settle, i * 0.05f);
                Motion.Instance.Scale(rt, Vector3.one * 0.6f, Vector3.one, 0.26f, Ease.Settle, i * 0.05f);
            }
        }
    }
}
