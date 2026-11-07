using System.Collections.Generic;
using Tesela.Core;
using Tesela.Core.Raster;
using Tesela.Gameplay;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>
    /// Tutorial interactivo de 3 pasos dentro de la primera partida (jugable en menos de 10 s):
    /// 1) arrastra una pieza, 2) completa una línea, 3) al colocar las tres llegan tres nuevas.
    /// </summary>
    public sealed class TutorialOverlay
    {
        private readonly AppContext _ctx;
        private readonly RectTransform _bubble;
        private readonly UiText _text;
        private readonly RectTransform _hand;
        private readonly BoardView _board;
        private readonly TrayView _tray;
        private int _step;
        private int _movesInStep;
        private int _handTween;

        public TutorialOverlay(RectTransform root, AppContext ctx, BoardView board, TrayView tray)
        {
            _ctx = ctx;
            _board = board;
            _tray = tray;
            var bubble = Ui.Panel(root, "tutorial", FrameStyle.ButtonGold);
            _bubble = bubble.rectTransform;
            _bubble.Band(true, 240, 130, 70);
            _text = Ui.Text(_bubble, "", 38, Ui.Ink);
            _text.Rect.Stretch(30, 10, 30, 10);
            _hand = Ui.Icon(root, "hand", 110, Ui.Ink).rectTransform;
            SetStep(1);
        }

        private void SetStep(int step)
        {
            _step = step;
            _movesInStep = 0;
            if (step > 3)
            {
                Object.Destroy(_bubble.gameObject);
                Object.Destroy(_hand.gameObject);
                return;
            }
            _text.Text = Loc.T("tutorial.step" + step);
            Motion.Instance.Scale(_bubble, Vector3.one * 0.9f, Vector3.one, 0.2f, Ease.Settle);
            _ctx.Log("tutorial_step", new Dictionary<string, object> { { "step", step } });
            _hand.gameObject.SetActive(step == 1);
            if (step == 1) LoopHand();
        }

        private void LoopHand()
        {
            if (_step != 1 || _hand == null) return;
            var parent = (RectTransform)_hand.parent;
            Vector2 from = parent.InverseTransformPoint(_tray.Slots[1].TransformPoint(Vector3.zero));
            Vector2 to = parent.InverseTransformPoint(_board.Root.TransformPoint(Vector3.zero));
            _hand.anchorMin = _hand.anchorMax = new Vector2(0.5f, 0.5f);
            _hand.anchoredPosition = from + new Vector2(30, -40);
            _handTween = Motion.Instance.Move(_hand, to + new Vector2(30, -40), 1.1f, Ease.InOut, 0.3f, () => Motion.Delay(0.4f, LoopHand), true);
        }

        public void OnMove(MoveResult r)
        {
            _movesInStep++;
            switch (_step)
            {
                case 1:
                    Motion.Instance.Kill(_handTween);
                    SetStep(2);
                    break;
                case 2:
                    if (r.Clear.Lines > 0 || _movesInStep >= 5) SetStep(3);
                    break;
                case 3:
                    if (r.TrayRefilled || _movesInStep >= 3) SetStep(4);
                    break;
            }
        }
    }
}
