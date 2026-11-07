using System.Collections.Generic;
using Tesela.Core;
using Tesela.Core.Raster;
using Tesela.Gameplay;
using Tesela.Meta;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;

namespace Tesela.UI
{
    public sealed class HomeScreen : ScreenBase
    {
        private UiText _coins, _streak, _level;
        private RectTransform _missions;

        protected override void Build()
        {
            float w = Layout.Width;
            // Barra superior: nivel, racha y monedas (asimétrica a propósito).
            var top = Ui.Rect(Root, "top").Band(true, 20, 120, 30);
            var levelChip = Ui.Panel(top, "level", FrameStyle.Chip);
            levelChip.rectTransform.Place(0, 0.5f, 210, 96, 105, 0);
            Ui.Icon(levelChip.transform, "crown", 54, Ui.Ink).rectTransform.Place(0, 0.5f, 54, 54, 50, 2);
            _level = Ui.Text(levelChip.transform, "", 38, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
            _level.Rect.Place(0, 0.5f, 120, 70, 140, 0);
            var streakChip = Ui.Panel(top, "streak", FrameStyle.Chip);
            streakChip.rectTransform.Place(0.42f, 0.5f, 200, 96);
            Ui.Icon(streakChip.transform, "flame", 54, Palette.Coral.ToColor()).rectTransform.Place(0, 0.5f, 54, 54, 48, 2);
            _streak = Ui.Text(streakChip.transform, "", 38, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
            _streak.Rect.Place(0, 0.5f, 120, 70, 136, 0);
            var coinChip = Ui.Panel(top, "coins", FrameStyle.Chip);
            coinChip.rectTransform.Place(1, 0.5f, 260, 96, -130, 0);
            Ui.Icon(coinChip.transform, "coin", 54, Palette.GoldDeep.ToColor()).rectTransform.Place(0, 0.5f, 54, 54, 48, 2);
            _coins = Ui.Text(coinChip.transform, "", 38, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
            _coins.Rect.Place(0, 0.5f, 170, 70, 170, 0);

            // Título con un azulejo inclinado al lado (asimetría intencional).
            var title = Ui.Text(Root, Loc.T("app.name"), 150, Ui.Ink, TextAnchor.MiddleCenter, TextRole.Display);
            title.Rect.Band(true, 170, 190);
            var accent = Ui.Image(Root, "accent", Ui.Art.Tile(4, 1), Color.white);
            accent.rectTransform.Place(0.5f, 1, 86, 86, 250, -205);
            accent.rectTransform.localEulerAngles = new Vector3(0, 0, -9);
            var tagline = Ui.Text(Root, Loc.T("app.tagline"), 34, Ui.InkSoft);
            tagline.Rect.Band(true, 360, 100, 90);

            // Modos.
            var modes = Ui.Rect(Root, "modes").Band(true, 480, 720, 60);
            var play = Ui.Button(modes, Loc.T("mode.classic"), FrameStyle.ButtonPrimary, PlayClassic, "play", 56);
            ((RectTransform)play.transform).Band(true, 0, 170);
            ModeButton(modes, 0, 200, Loc.T("mode.restoration"), "album", () => Nav.Push(new ChapterScreen()));
            ModeButton(modes, 1, 200, DailyLabel(), "calendar", PlayDaily);
            ModeButton(modes, 0, 360, Loc.T("mode.zen"), "zen", PlayZen);
            var ev = Ctx.ActiveEventRules();
            if (ev != null) ModeButton(modes, 1, 360, Loc.T(Ctx.ActiveEventNameKey()), "star", () => Play(c => c.StartEvent(ev)));
            if (TryResumable(out string resumeMode))
            {
                var resume = Ui.Button(modes, Loc.T("home.continue", Loc.T("mode." + resumeMode)), FrameStyle.ButtonGold, Resume, "restore", 40);
                ((RectTransform)resume.transform).Band(true, 530, 130);
            }

            _missions = Ui.Rect(Root, "missions").Band(false, 200, 380, 50);

            // Navegación inferior.
            var nav = Ui.Rect(Root, "nav").Band(false, 30, 150, 40);
            string[] icons = { "shop", "pass", "album", "trophy", "settings" };
            string[] keys = { "nav.shop", "nav.pass", "nav.album", "nav.achievements", "nav.settings" };
            System.Action[] actions =
            {
                () => Nav.Push(new ShopScreen()), () => Nav.Push(new PassScreen()), () => Nav.Push(new AlbumScreen()),
                () => Nav.Push(new AchievementsScreen()), () => Nav.Push(new SettingsScreen())
            };
            float step = (w - 80) / icons.Length;
            for (int i = 0; i < icons.Length; i++)
            {
                var b = Ui.IconButton(nav, icons[i], actions[i], 120, i == 1 ? FrameStyle.ButtonGold : FrameStyle.Chip);
                ((RectTransform)b.transform).Place(0, 0.5f, 120, 120, step * (i + 0.5f), 14);
                var label = Ui.Text(b.transform, Loc.T(keys[i]), 24, Ui.InkSoft);
                label.Rect.Place(0.5f, 0, 200, 34, 0, -24);
            }
            Refresh();
        }

        private string DailyLabel()
        {
            bool played = Ctx.Profile.DailyChallengeDay == Ctx.Today;
            return played ? Loc.T("mode.daily.played", Loc.Number(Ctx.Profile.DailyChallengeBest)) : Loc.T("mode.daily");
        }

        private void ModeButton(RectTransform parent, int column, float y, string label, string icon, System.Action onClick)
        {
            var b = Ui.Button(parent, label, FrameStyle.ButtonSecondary, onClick, icon, 40);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(column * 0.5f, 1);
            rt.anchorMax = new Vector2(column * 0.5f + 0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-24, 140);
            rt.anchoredPosition = new Vector2(column == 0 ? -6 : 6, -y);
        }

        public override void OnShow()
        {
            Refresh();
            UpdateBanner();
        }

        public override void Refresh()
        {
            _coins.Text = Loc.Number(Ctx.Profile.Coins);
            _streak.Text = Ctx.Profile.StreakDays.ToString();
            _level.Text = Ctx.Profile.Level.ToString();
            BuildMissions();
        }

        private void UpdateBanner()
        {
            if (AdPolicy.ShowBanner(Ctx.AdConfig, true, false, Ctx.AdFree)) Ctx.Ads.ShowBanner();
            else Ctx.Ads.HideBanner();
        }

        public override void OnHide() => Ctx.Ads.HideBanner();

        public override bool OnBack() => false;

        private void BuildMissions()
        {
            for (int i = _missions.childCount - 1; i >= 0; i--) Object.Destroy(_missions.GetChild(i).gameObject);
            var panel = Ui.Panel(_missions, "panel");
            panel.rectTransform.Stretch();
            var title = Ui.Text(panel.transform, Loc.T("missions.daily"), 40, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
            title.Rect.Band(true, 22, 60, 40);
            float y = 92;
            foreach (var m in Ctx.Profile.DailyMissions)
            {
                var def = Ctx.Meta.FindMission(m.Id);
                if (def == null) continue;
                var row = Ui.Rect(panel.transform, m.Id).Band(true, y, 80, 36);
                var label = Ui.Text(row, Loc.T("mission." + def.Metric, def.Target), 32, Ui.Ink, TextAnchor.MiddleLeft);
                label.Rect.Stretch(0, 0, 330, 0);
                var barBg = Ui.Image(row, "bar", Ui.Art.White, Palette.SandDeep.ToColor());
                barBg.rectTransform.Place(1, 0.5f, 170, 18, -235, 0);
                var bar = Ui.Image(barBg.transform, "fill", Ui.Art.White, Palette.Olive.ToColor());
                float progress = Mathf.Clamp01(m.Progress / (float)def.Target);
                bar.rectTransform.anchorMin = Vector2.zero;
                bar.rectTransform.anchorMax = new Vector2(progress, 1);
                bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
                bool ready = m.Progress >= def.Target && !m.Claimed;
                if (m.Claimed)
                {
                    Ui.Icon(row, "check", 56, Palette.Olive.ToColor()).rectTransform.Place(1, 0.5f, 56, 56, -60, 0);
                }
                else
                {
                    string id = m.Id;
                    var claim = Ui.Button(row, "+" + def.Reward.Coins, ready ? FrameStyle.ButtonGold : FrameStyle.Chip, () => Claim(id), "coin", 30);
                    ((RectTransform)claim.transform).Place(1, 0.5f, 130, 72, -65, 0);
                    claim.SetInteractable(ready);
                }
                y += 90;
            }
        }

        private void Claim(string id)
        {
            if (!Ctx.Meta.ClaimMission(id, out _)) return;
            Ctx.Audio?.Play(Tesela.Core.Audio.Sfx.Coin);
            Ctx.Saves.SaveProfile(Ctx.Profile);
            Refresh();
        }

        private bool TryResumable(out string mode)
        {
            mode = null;
            if (!Ctx.Saves.TryLoadRun(out var run)) return false;
            mode = run["mode"].AsString("classic");
            return true;
        }

        private void Resume()
        {
            var game = new GameController(Ctx);
            if (game.TryResume()) Nav.Push(new GameScreen(game));
            else Refresh();
        }

        private void Play(System.Action<GameController> start)
        {
            var game = new GameController(Ctx);
            start(game);
            Nav.Push(new GameScreen(game));
        }

        private void PlayClassic() => Play(g => g.StartClassic());
        private void PlayDaily() => Play(g => g.StartDaily());
        private void PlayZen() => Play(g => g.StartZen());
    }

    /// <summary>Restauración: capítulos por lugar del pueblo, niveles con estrellas y el mosaico que se revela.</summary>
    public sealed class ChapterScreen : ScreenBase
    {
        protected override void Build()
        {
            ScreenChrome.Header(this, "mode.restoration");
            var scroll = ScreenChrome.Scroll(Root, 200, 40);
            float y = 20;
            int chapter = 0;
            var levels = Ctx.Data.Levels;
            bool previousDone = true;
            foreach (var l in levels)
            {
                if (l.Chapter != chapter)
                {
                    chapter = l.Chapter;
                    var head = Ui.Text(scroll, Loc.T("chapter." + chapter), 48, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
                    head.Rect.Band(true, y, 80, 40);
                    y += 100;
                }
                int col = (l.Index - 1) % 5;
                if (col == 0 && l.Index > 1) y += 170;
                int stars = Ctx.Profile.LevelStars.TryGetValue(l.Id, out int st) ? st : 0;
                bool unlocked = previousDone || stars > 0;
                var level = l;
                var b = Ui.Button(scroll, l.Index.ToString(), stars > 0 ? FrameStyle.ButtonSecondary : unlocked ? FrameStyle.ButtonPrimary : FrameStyle.Chip, () => Start(level), null, 48);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(150, 150);
                rt.anchoredPosition = new Vector2(110 + col * ((Layout.Width - 160) / 5f), -y);
                b.SetInteractable(unlocked);
                if (!unlocked) Ui.Icon(rt, "lock", 44, Ui.InkSoft).rectTransform.Place(0.5f, 0, 44, 44, 0, 30);
                for (int s = 0; s < 3 && stars > 0; s++)
                    Ui.Icon(rt, "star", 36, s < stars ? Ui.Gold : Palette.SandDeep.ToColor()).rectTransform.Place(0.5f, 0, 36, 36, (s - 1) * 38, 24);
                previousDone = stars > 0;
                if (l.Index % 20 == 0) y += 190;
            }
            scroll.sizeDelta = new Vector2(scroll.sizeDelta.x, y + 200);
        }

        private void Start(LevelDefinition level)
        {
            var g = new GameController(Ctx);
            g.StartLevel(level);
            Nav.Push(new GameScreen(g));
        }
    }

    /// <summary>Elementos comunes de pantallas de menú: encabezado con volver y contenido desplazable.</summary>
    public static class ScreenChrome
    {
        public static void Header(ScreenBase screen, string titleKey)
        {
            var head = Ui.Rect(screen.Root, "header").Band(true, 20, 140, 30);
            var back = Ui.IconButton(head, "back", () => screen.OnBack(), 110);
            ((RectTransform)back.transform).Place(0, 0.5f, 110, 110, 70, 0);
            var t = Ui.Title(head, Loc.T(titleKey), 64);
            t.Rect.Stretch(150, 0, 150, 0);
        }

        public static RectTransform Scroll(RectTransform root, float top, float bottom)
        {
            var view = Ui.Rect(root, "scroll");
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = new Vector2(0, bottom);
            view.offsetMax = new Vector2(0, -top);
            view.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hit = view.gameObject.AddComponent<UnityEngine.UI.Image>();
            hit.color = new Color(1, 1, 1, 0.001f);
            var content = Ui.Rect(view, "content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(0, 3000);
            var sr = view.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30f;
            return content;
        }
    }
}
