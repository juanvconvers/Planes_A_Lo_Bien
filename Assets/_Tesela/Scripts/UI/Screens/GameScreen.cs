using System.Collections.Generic;
using Tesela.Core;
using Tesela.Core.Audio;
using Tesela.Core.Raster;
using Tesela.Gameplay;
using Tesela.Meta;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tesela.UI
{
    public sealed class GameScreen : ScreenBase
    {
        private enum Targeting { None, Hammer, Broom, Rotate, Hold }

        private readonly GameController _game;
        private BoardView _board;
        private TrayView _tray;
        private UiParticles _particles;
        private RectTransform _fx;
        private UiText _score, _best, _streak, _tiles, _moves, _popup, _banner;
        private RectTransform _streakChip, _tilesTarget, _bannerRect;
        private readonly List<UiText> _objectiveTexts = new List<UiText>();
        private readonly Dictionary<BoosterType, UiText> _boosterCounts = new Dictionary<BoosterType, UiText>();
        private PieceVisual _drag;
        private int _dragSlot = -1;
        private Vector2Int _dragOrigin;
        private bool _dragValid;
        private Targeting _targeting;
        private bool _broomColumns;
        private bool _busy;
        private int _displayScore;
        private int _tilesShown;
        private TutorialOverlay _tutorial;
        private float _lift;

        public GameScreen(GameController game) { _game = game; }

        public override bool IsMenu => false;

        protected override void Build()
        {
            float w = Layout.Width, h = Layout.Height;
            var s = _game.Session;
            bool lefty = Ctx.Settings.LeftHanded;
            float sideX = lefty ? 1f : 0f;

            // ── HUD ──
            var hud = Ui.Rect(Root, "hud").Band(true, 0, 230);
            var pause = Ui.IconButton(hud, "settings", OpenPause, 112);
            ((RectTransform)pause.transform).Place(sideX, 0.5f, 112, 112, lefty ? -86 : 86, 0);
            _score = Ui.Text(hud, "0", 104, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
            _score.Rect.Place(0.5f, 0.62f, 520, 130);
            _best = Ui.Text(hud, "", 32, Ui.InkSoft);
            _best.Rect.Place(0.5f, 0.2f, 520, 44);
            var tilesChip = Ui.Panel(hud, "tiles", FrameStyle.Chip);
            tilesChip.rectTransform.Place(1f - sideX, 0.5f, 190, 96, lefty ? 120 : -120, 0);
            var tileIcon = Ui.Image(tilesChip.transform, "tileIcon", Ui.Art.Tile(1), Color.white);
            tileIcon.rectTransform.Place(0, 0.5f, 60, 60, 46, 2);
            _tilesTarget = tileIcon.rectTransform;
            _tiles = Ui.Text(tilesChip.transform, "0", 40, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
            _tiles.Rect.Place(0, 0.5f, 100, 70, 130, 0);
            var streakImg = Ui.Panel(hud, "streak", FrameStyle.ButtonGold);
            _streakChip = streakImg.rectTransform;
            _streakChip.Place(0.5f, 0f, 230, 64, 0, -26);
            _streak = Ui.Text(_streakChip, "", 34, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
            _streak.Rect.Stretch();
            _streakChip.gameObject.SetActive(false);

            // ── Objetivos (Restauración) ──
            float objectivesH = s.IsLevel ? 120 : 0;
            if (s.IsLevel) BuildObjectives(Ui.Rect(Root, "objectives").Band(true, 250, objectivesH, 40));

            // ── Potenciadores y bandeja ──
            var bar = Ui.Rect(Root, "boosters").Band(false, 16, 170, 30);
            BuildBoosters(bar, lefty);
            float trayH = 300;
            float top = 250 + objectivesH + 30;
            float bottom = 16 + 170 + trayH;
            float boardSize = Mathf.Min(w - 100, h - top - bottom - 60);
            var boardArea = Ui.Rect(Root, "boardArea");
            boardArea.anchorMin = new Vector2(0, 0);
            boardArea.anchorMax = new Vector2(1, 1);
            boardArea.offsetMin = new Vector2(0, bottom);
            boardArea.offsetMax = new Vector2(0, -top);
            _board = new BoardView(boardArea, s.Board.Width, s.Board.Height, boardSize, Ui.Art);
            ((RectTransform)_board.Root.parent).Place(0.5f, 0.5f, boardSize + 44, boardSize + 44);

            var trayArea = Ui.Rect(Root, "trayArea").Band(false, 16 + 170, trayH);
            _tray = new TrayView(trayArea, w - 160, trayH, _board.Cell, Ui.Art, OnReserveTap);
            _tray.Root.Place(0.5f, 0.5f, w - 160, trayH);
            for (int i = 0; i < _tray.Inputs.Length; i++)
            {
                _tray.Inputs[i].Down = OnSlotDown;
                _tray.Inputs[i].Drag = OnSlotDrag;
                _tray.Inputs[i].Up = OnSlotUp;
            }

            // ── Efectos y arrastre ──
            _fx = Ui.Rect(Root, "fx").Stretch();
            _fx.gameObject.AddComponent<Canvas>();
            _particles = new UiParticles(_fx);
            _drag = new PieceVisual(_fx, "drag", Ui.Art);
            _drag.Root.gameObject.SetActive(false);
            _popup = Ui.Text(_fx, "", 64, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
            _popup.Rect.Place(0.5f, 0.5f, 900, 120);
            _popup.Text = "";

            _bannerRect = Ui.Panel(Root, "targeting", FrameStyle.Chip).rectTransform;
            _bannerRect.Band(false, 16 + 170 + trayH + 10, 90, 120);
            _banner = Ui.Text(_bannerRect, "", 36, Ui.Ink);
            _banner.Rect.Stretch(20, 0, 20, 0);
            var bannerBtn = _bannerRect.gameObject.AddComponent<TeselaButton>();
            bannerBtn.Init(OnBannerTap);
            ((Image)_bannerRect.GetComponent<Image>()).raycastTarget = true;
            _bannerRect.gameObject.SetActive(false);

            var boardInput = _board.Root.gameObject.AddComponent<Image>();
            boardInput.color = new Color(1, 1, 1, 0.001f);
            boardInput.raycastTarget = true;
            var tap = _board.Root.gameObject.AddComponent<BoardTapInput>();
            tap.Tapped = OnBoardTap;

            _lift = Ctx.Settings.LiftCells;
            _displayScore = s.Score;
            _tilesShown = 0;
            SyncAll();
            if (_game.IsTutorial) _tutorial = new TutorialOverlay(Root, Ctx, _board, _tray);
        }

        private void BuildObjectives(RectTransform row)
        {
            var s = _game.Session;
            int n = s.Objectives.Definitions.Count + 1;
            float slot = Mathf.Min(260, (Layout.Width - 80) / n);
            for (int i = 0; i < s.Objectives.Definitions.Count; i++)
            {
                var def = s.Objectives.Definitions[i];
                var chip = Ui.Panel(row, "obj" + i, FrameStyle.Chip);
                chip.rectTransform.Place(0.5f, 0.5f, slot - 16, 100, (i - (n - 1) * 0.5f) * slot, 0);
                Sprite icon = def.Type == ObjectiveType.CollectGlaze ? Ui.Art.Tile(def.Glaze)
                    : def.Type == ObjectiveType.BreakCracked ? Ui.Art.Overlay(TileOverlay.CrackedHeavy)
                    : def.Type == ObjectiveType.FreeMud ? Ui.Art.Overlay(TileOverlay.Mud)
                    : Ui.Art.Icon(def.Type == ObjectiveType.ReachScore ? "star" : "check");
                var img = Ui.Image(chip.transform, "icon", icon, def.Type == ObjectiveType.ReachScore || def.Type == ObjectiveType.ClearLines ? Ui.Ink : Color.white);
                img.rectTransform.Place(0, 0.5f, 58, 58, 44, 2);
                var t = Ui.Text(chip.transform, "", 34, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
                t.Rect.Place(0, 0.5f, slot - 100, 70, 84 + (slot - 100) * 0.5f, 0);
                _objectiveTexts.Add(t);
            }
            var movesChip = Ui.Panel(row, "moves", FrameStyle.ButtonSecondary);
            movesChip.rectTransform.Place(0.5f, 0.5f, slot - 16, 100, ((n - 1) - (n - 1) * 0.5f) * slot, 0);
            _moves = Ui.Text(movesChip.transform, "", 34, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
            _moves.Rect.Stretch();
        }

        private void BuildBoosters(RectTransform bar, bool lefty)
        {
            var order = new[] { BoosterType.Undo, BoosterType.Rotate, BoosterType.Hammer, BoosterType.Broom, BoosterType.NewTray, BoosterType.Reserve };
            string[] icons = { "undo", "rotate", "hammer", "broom", "newtray", "reserve" };
            float step = (Layout.Width - 60) / order.Length;
            for (int i = 0; i < order.Length; i++)
            {
                int idx = lefty ? order.Length - 1 - i : i;
                var type = order[idx];
                var btn = Ui.IconButton(bar, icons[idx], () => OnBooster(type), 126, FrameStyle.ButtonSecondary);
                ((RectTransform)btn.transform).Place(0, 0.5f, 126, 126, step * (i + 0.5f), 6);
                var badge = Ui.Panel(btn.transform, "badge", FrameStyle.ButtonGold);
                badge.rectTransform.Place(1, 0, 64, 48, -6, 6);
                var count = Ui.Text(badge.transform, "0", 28, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
                count.Rect.Stretch();
                _boosterCounts[type] = count;
            }
        }

        public override void OnShow()
        {
            Ctx.Audio?.SetIntensity(_game.Session.Streak.Streak, _game.Session.Rules.Mode == GameMode.Zen);
            Ctx.Ads.HideBanner();
        }

        public override bool OnBack()
        {
            if (_targeting != Targeting.None) { EndTargeting(); return true; }
            OpenPause();
            return true;
        }

        public override void Refresh() => SyncAll();

        // ───────────── Sincronización ─────────────

        private void SyncAll()
        {
            var s = _game.Session;
            _board.Sync(s.Board);
            _tray.Sync(s);
            _displayScore = s.Score;
            _score.Text = Loc.Number(s.Score);
            int best = Ctx.Profile.Best(_game.ModeKey);
            _best.Text = s.Rules.Competitive ? Loc.T("game.best", Loc.Number(Mathf.Max(best, s.Score))) : Loc.T("mode.zen.subtitle");
            _tiles.Text = Loc.Number(Ctx.Profile.Tiles + _tilesShown);
            SyncStreak();
            SyncObjectives();
            foreach (var kv in _boosterCounts)
            {
                int c = _game.Charges(kv.Key);
                kv.Value.Text = c > 0 ? c.ToString() : Loc.T("game.coins_short", _game.Price(kv.Key));
            }
        }

        private void SyncStreak()
        {
            var s = _game.Session;
            bool show = s.Streak.Streak >= 2;
            if (show && !_streakChip.gameObject.activeSelf) Motion.Instance.Scale(_streakChip, Vector3.one * 0.6f, Vector3.one, 0.22f, Ease.Settle);
            _streakChip.gameObject.SetActive(show);
            if (show) _streak.Text = Loc.T("game.streak", s.Scoring.MultiplierFor(s.Streak.Streak).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', Loc.Language == "en" ? '.' : ','));
        }

        private void SyncObjectives()
        {
            var s = _game.Session;
            if (!s.IsLevel) return;
            for (int i = 0; i < _objectiveTexts.Count; i++)
            {
                var def = s.Objectives.Definitions[i];
                int p = s.Objectives.Progress[i];
                _objectiveTexts[i].Text = def.Type == ObjectiveType.ReachScore ? Loc.Number(p) + "/" + Loc.Number(def.Target) : p + "/" + def.Target;
                _objectiveTexts[i].Color = p >= def.Target ? Palette.Olive.ToColor() : Ui.Ink;
            }
            _moves.Text = Loc.T("game.moves_left", s.MovesLeft);
        }

        // ───────────── Arrastre ─────────────

        private Vector2 FxLocal(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_fx, e.position, e.pressEventCamera, out var p);
            return p;
        }

        private void OnSlotDown(int slot, PointerEventData e)
        {
            if (_busy || _game.Finished) return;
            var s = _game.Session;
            if (_targeting == Targeting.Rotate) { DoRotate(slot); return; }
            if (_targeting == Targeting.Hold) { DoHold(slot); return; }
            if (s.Tray[slot].IsEmpty || s.IsOver) return;
            _dragSlot = slot;
            _drag.Show(s.Tray[slot], _board.Cell);
            _drag.Root.anchoredPosition = DragPosition(FxLocal(e), s.Tray[slot].Shape);
            Motion.Instance.Scale(_drag.Root, Vector3.one * TrayView.PreviewScale, Vector3.one, 0.12f, Ease.Out, 0f, null, true);
            _tray.Pieces[slot].Root.gameObject.SetActive(false);
            Ctx.Audio?.Play(Sfx.Pick);
            Haptics.Light();
        }

        private Vector2 DragPosition(Vector2 finger, PieceShape shape)
        {
            // La pieza se eleva sobre el dedo para que este no tape la jugada (0–3 celdas, ajustable).
            float lift = _lift * _board.Cell + shape.Height * _board.Cell * 0.5f + _board.Cell * 0.35f;
            return finger + new Vector2(0, lift);
        }

        private void OnSlotDrag(int slot, PointerEventData e)
        {
            if (slot != _dragSlot) return;
            var s = _game.Session;
            var shape = s.Tray[slot].Shape;
            Vector2 pos = DragPosition(FxLocal(e), shape);
            _drag.Root.anchoredPosition = pos;
            Vector3 world = _fx.TransformPoint(pos);
            Vector2 boardLocal = _board.Root.InverseTransformPoint(world);
            _dragValid = FindMagnet(slot, shape, _board.OriginFor(shape, boardLocal), out _dragOrigin);
            if (_dragValid)
            {
                s.PreviewLines(slot, _dragOrigin.x, _dragOrigin.y, out var rows, out var cols);
                _board.ShowGhost(shape, _dragOrigin.x, _dragOrigin.y, s.Tray[slot].Glaze, rows, cols);
            }
            else _board.ClearGhost();
        }

        /// <summary>Imán: si la celda exacta no sirve, prueba las vecinas (distancia 1) y elige la más cercana.</summary>
        private bool FindMagnet(int slot, PieceShape shape, Vector2Int origin, out Vector2Int best)
        {
            var s = _game.Session;
            best = origin;
            if (s.CanPlace(slot, origin.x, origin.y)) return true;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (s.CanPlace(slot, origin.x + dx, origin.y + dy) && (dx == 0 || dy == 0))
                    {
                        best = new Vector2Int(origin.x + dx, origin.y + dy);
                        return true;
                    }
                }
            return false;
        }

        private void OnSlotUp(int slot, PointerEventData e)
        {
            if (slot != _dragSlot) return;
            _dragSlot = -1;
            _board.ClearGhost();
            var s = _game.Session;
            if (_dragValid && s.CanPlace(slot, _dragOrigin.x, _dragOrigin.y))
            {
                _dragValid = false;
                CommitPlace(slot, _dragOrigin.x, _dragOrigin.y);
                return;
            }
            // Se soltó fuera: vuelve a la bandeja con rebote.
            Ctx.Audio?.Play(Sfx.Invalid, 0.6f);
            Vector3 world = _tray.Slots[slot].TransformPoint(Vector3.zero);
            Vector2 home = _fx.InverseTransformPoint(world);
            _busy = true;
            Motion.Instance.Scale(_drag.Root, Vector3.one, Vector3.one * TrayView.PreviewScale, 0.2f, Ease.Out, 0f, null, true);
            Motion.Instance.Move(_drag.Root, home, 0.2f, Ease.Settle, 0f, () =>
            {
                _drag.Root.gameObject.SetActive(false);
                _tray.Pieces[slot].Root.gameObject.SetActive(true);
                _busy = false;
            }, true);
        }

        private void CommitPlace(int slot, int x, int y)
        {
            var s = _game.Session;
            var shape = s.Tray[slot].Shape;
            Vector3 world = _board.Root.TransformPoint(_board.PieceCenter(shape, x, y));
            Vector2 target = _fx.InverseTransformPoint(world);
            _busy = true;
            Motion.Instance.Move(_drag.Root, target, 0.08f, Ease.Out, 0f, () =>
            {
                _drag.Root.gameObject.SetActive(false);
                int scoreBefore = s.Score;
                var r = _game.Place(slot, x, y);
                AfterMove(r, scoreBefore);
                _busy = false;
            }, true);
        }

        private void AfterMove(MoveResult r, int scoreBefore)
        {
            var s = _game.Session;
            _board.Sync(s.Board);
            _board.Settle(r.Shape, r.X, r.Y);
            Ctx.Audio?.Play(Sfx.Place);
            if (r.Clear.Lines > 0)
            {
                Ctx.Audio?.PlayCombo(r.Streak - 1);
                if (r.Clear.Lines >= 2) Haptics.Strong(); else Haptics.Medium();
                AnimateClear(r);
                ShowPopup(r);
            }
            else Haptics.Light();
            Motion.Instance.Value(scoreBefore, s.Score, 0.35f, v => _score.Text = Loc.Number(Mathf.RoundToInt(v)));
            if (r.TrayRefilled) _tray.AnimateRefill();
            _tray.Sync(s);
            SyncStreak();
            SyncObjectives();
            Ctx.Audio?.SetIntensity(s.Streak.Streak, s.Rules.Mode == GameMode.Zen);
            _tutorial?.OnMove(r);
            if (r.ZenSweeps > 0) Ctx.Audio?.Play(Sfx.Sweep);
            if (s.IsOver) Motion.Delay(0.55f, OnGameOver);
        }

        private void AnimateClear(MoveResult r)
        {
            Vector2 target = _fx.InverseTransformPoint(_tilesTarget.TransformPoint(Vector3.zero));
            Vector2 originCell = _board.CellCenter(r.X, r.Y);
            int arrived = 0;
            foreach (var c in r.ClearedCells)
            {
                if (c.Survived) continue;
                Vector2 from = _fx.InverseTransformPoint(_board.Root.TransformPoint(_board.CellCenter(c.X, c.Y)));
                float delay = Vector2.Distance(_board.CellCenter(c.X, c.Y), originCell) / (_board.Cell * 18f);
                _particles.FlyTile(Ui.Art.Tile(c.Glaze), Color.white, from, target, _board.Cell, delay, () =>
                {
                    arrived++;
                    _tilesShown = Mathf.Min(_tilesShown + 1, r.Clear.Lines * _game.Session.Board.Width);
                    _tiles.Text = Loc.Number(Ctx.Profile.Tiles + _tilesShown);
                    if (arrived % 4 == 1) Motion.Instance.Scale(_tilesTarget, Vector3.one * 1.25f, Vector3.one, 0.14f, Ease.Settle);
                });
                _particles.Burst(Ui.Art.White, Ctx.Data != null ? Palette.Glaze(c.Glaze).ToColor() : Color.white, from, 3, _board.Cell * 0.8f);
            }
        }

        private void ShowPopup(MoveResult r)
        {
            string key = r.BoardEmptied ? "game.board_clean" : r.Clear.Lines >= 4 ? "game.lines4" : r.Clear.Lines == 3 ? "game.lines3" : r.Clear.Lines == 2 ? "game.lines2" : null;
            string text = "+" + Loc.Number(r.Points);
            if (key != null) text = Loc.T(key) + "  " + text;
            _popup.Text = text;
            Vector2 at = _fx.InverseTransformPoint(_board.Root.TransformPoint(_board.PieceCenter(r.Shape, r.X, r.Y)));
            _popup.Rect.anchoredPosition = at + new Vector2(0, _board.Cell);
            Motion.Instance.Scale(_popup.Rect, Vector3.one * 0.7f, Vector3.one, 0.2f, Ease.Settle);
            Motion.Instance.Move(_popup.Rect, at + new Vector2(0, _board.Cell * 2.2f), 0.9f, Ease.Out, 0f, () => _popup.Text = "");
        }

        // ───────────── Potenciadores ─────────────

        private void OnBooster(BoosterType t)
        {
            if (_busy || _game.Finished || _game.Session.IsOver) return;
            if (_game.Charges(t) <= 0 && Ctx.Profile.Coins < _game.Price(t))
            {
                Nav.Push(new BoosterOfferDialog(_game, t, SyncAll));
                return;
            }
            switch (t)
            {
                case BoosterType.Undo:
                    if (_game.UseBooster(t, _game.Session.Undo)) { _tilesShown = 0; SyncAll(); }
                    break;
                case BoosterType.NewTray:
                    if (_game.UseBooster(t, _game.Session.NewTray)) { _tray.AnimateRefill(); SyncAll(); }
                    break;
                case BoosterType.Reserve:
                    if (_game.Session.ReserveUnlocked) StartTargeting(Targeting.Hold);
                    else if (_game.UseBooster(t, () => { _game.Session.UnlockReserve(); return true; })) { SyncAll(); StartTargeting(Targeting.Hold); }
                    break;
                case BoosterType.Rotate: StartTargeting(Targeting.Rotate); break;
                case BoosterType.Hammer: StartTargeting(Targeting.Hammer); break;
                case BoosterType.Broom: StartTargeting(Targeting.Broom); break;
            }
        }

        private void StartTargeting(Targeting t)
        {
            _targeting = t;
            _bannerRect.gameObject.SetActive(true);
            _banner.Text = t == Targeting.Hammer ? Loc.T("booster.hammer.hint")
                : t == Targeting.Broom ? Loc.T(_broomColumns ? "booster.broom.hint_col" : "booster.broom.hint_row")
                : t == Targeting.Rotate ? Loc.T("booster.rotate.hint")
                : Loc.T("booster.reserve.hint");
        }

        private void EndTargeting()
        {
            _targeting = Targeting.None;
            _bannerRect.gameObject.SetActive(false);
        }

        private void OnBannerTap()
        {
            if (_targeting == Targeting.Broom)
            {
                _broomColumns = !_broomColumns;
                StartTargeting(Targeting.Broom);
            }
            else EndTargeting();
        }

        private void OnReserveTap()
        {
            if (_targeting == Targeting.Hold) EndTargeting();
            else if (_game.Session.ReserveUnlocked) StartTargeting(Targeting.Hold);
        }

        private void OnBoardTap(Vector2 screen, Camera cam)
        {
            if (_targeting != Targeting.Hammer && _targeting != Targeting.Broom) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_board.Root, screen, cam, out var local);
            int x = Mathf.FloorToInt((local.x + _board.Width * _board.Cell * 0.5f) / _board.Cell);
            int y = Mathf.FloorToInt((_board.Height * _board.Cell * 0.5f - local.y) / _board.Cell);
            if (x < 0 || y < 0 || x >= _board.Width || y >= _board.Height) return;
            var s = _game.Session;
            bool ok = _targeting == Targeting.Hammer
                ? _game.UseBooster(BoosterType.Hammer, () => s.Hammer(x, y))
                : _game.UseBooster(BoosterType.Broom, () => _broomColumns ? s.BroomColumn(x) : s.BroomRow(y));
            if (!ok) return;
            foreach (var c in s.LastResult.ClearedCells)
            {
                Vector2 from = _fx.InverseTransformPoint(_board.Root.TransformPoint(_board.CellCenter(c.X, c.Y)));
                _particles.Burst(Ui.Art.White, Palette.Glaze(c.Glaze).ToColor(), from, 4, _board.Cell);
            }
            Ctx.Audio?.Play(Sfx.Sweep);
            Haptics.Medium();
            EndTargeting();
            SyncAll();
        }

        private void DoRotate(int slot)
        {
            var s = _game.Session;
            bool ok = s.Rules.FreeRotation ? s.RotateSlot(slot) : _game.UseBooster(BoosterType.Rotate, () => s.RotateSlot(slot));
            if (!ok) return;
            Motion.Instance.Scale(_tray.Pieces[slot].Root, Vector3.one * 0.8f, Vector3.one, 0.18f, Ease.Settle);
            EndTargeting();
            SyncAll();
        }

        private void DoHold(int slot)
        {
            if (!_game.Session.HoldSlot(slot)) return;
            _game.Save();
            EndTargeting();
            SyncAll();
        }

        // ───────────── Pausa y fin ─────────────

        private void OpenPause() => Nav.Push(new PauseDialog(_game, SyncAll));

        private void OnGameOver()
        {
            var s = _game.Session;
            if (s.CanRevive && !_game.IsTutorial) Nav.Push(new ReviveDialog(_game, ShowResults, () => { SyncAll(); _tray.AnimateRefill(); }));
            else ShowResults();
        }

        private void ShowResults()
        {
            _game.Finish();
            Nav.Push(new ResultsScreen(_game));
        }
    }

    /// <summary>Toques sobre el tablero (Martillo y Escoba).</summary>
    public sealed class BoardTapInput : MonoBehaviour, IPointerClickHandler
    {
        public System.Action<Vector2, Camera> Tapped;
        public void OnPointerClick(PointerEventData e) => Tapped?.Invoke(e.position, e.pressEventCamera);
    }
}
