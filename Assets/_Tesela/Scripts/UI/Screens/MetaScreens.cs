using System;
using Tesela.Core;
using Tesela.Core.Raster;
using Tesela.Gameplay;
using Tesela.Meta;
using Tesela.Monetization;
using Tesela.Services;
using UnityEngine;

namespace Tesela.UI
{
    /// <summary>Álbum: un mosaico por capítulo que se revela con las estrellas, y los sets de azulejos coleccionados.</summary>
    public sealed class AlbumScreen : ScreenBase
    {
        protected override void Build()
        {
            ScreenChrome.Header(this, "nav.album");
            var content = ScreenChrome.Scroll(Root, 200, 20);
            float y = 10;
            var sets = Ui.Text(content, Loc.T("album.tilesets", Ctx.Profile.TileSetsCompleted, Ctx.Profile.TileSetProgress, Ctx.Data.Economy.TileSetSize), 34, Ui.Ink);
            sets.Rect.Band(true, y, 70, 40);
            y += 90;
            for (int chapter = 1; chapter <= 3; chapter++)
            {
                int total = 0, stars = 0;
                foreach (var l in Ctx.Data.Levels)
                {
                    if (l.Chapter != chapter) continue;
                    total += 3;
                    if (Ctx.Profile.LevelStars.TryGetValue(l.Id, out int s)) stars += s;
                }
                var card = Ui.Panel(content, "chapter" + chapter);
                card.rectTransform.Band(true, y, 560, 40);
                var title = Ui.Text(card.transform, Loc.T("chapter." + chapter), 44, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
                title.Rect.Band(true, 26, 70, 40);
                var progress = Ui.Text(card.transform, Loc.T("album.progress", stars, total), 30, Ui.InkSoft, TextAnchor.MiddleRight);
                progress.Rect.Band(true, 26, 70, 40);
                DrawMosaic(card.rectTransform, chapter, total == 0 ? 0f : stars / (float)total);
                y += 590;
            }
            content.sizeDelta = new Vector2(0, y + 60);
        }

        /// <summary>Mosaico de 8×6 azulejos con un dibujo determinista por capítulo; se revela en orden según el progreso.</summary>
        private void DrawMosaic(RectTransform card, int chapter, float progress)
        {
            const int cols = 10, rows = 6;
            float size = 76;
            var grid = Ui.Rect(card, "mosaic");
            grid.Place(0.5f, 0.45f, cols * size, rows * size, 0, -20);
            int reveal = Mathf.RoundToInt(progress * cols * rows);
            int k = 0;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++, k++)
                {
                    int glaze = MosaicGlaze(chapter, x, y);
                    var img = Ui.Image(grid, "m", k < reveal ? Ui.Art.Tile(glaze, (x + y) & 3) : Ui.Art.Socket, Color.white);
                    img.rectTransform.Place(0, 1, size - 4, size - 4, x * size + size * 0.5f, -(y * size + size * 0.5f));
                }
        }

        private static int MosaicGlaze(int chapter, int x, int y)
        {
            float cx = x - 4.5f, cy = y - 2.5f;
            float r = Mathf.Sqrt(cx * cx + cy * cy);
            switch (chapter)
            {
                case 1: return r < 1.6f ? 2 : r < 3f ? 1 : (x + y) % 2 == 0 ? 5 : 3;
                case 2: return Mathf.Abs(cx) + Mathf.Abs(cy) < 3 ? 5 : y < 2 ? 1 : (x % 3 == 0 ? 4 : 2);
                default: return y == 0 || y == 5 ? 6 : (x + y * 2) % 5 == 0 ? 4 : r < 2.5f ? 2 : 3;
            }
        }
    }

    /// <summary>Logros con barra de progreso hacia el siguiente escalón (sincronizados con Play Games).</summary>
    public sealed class AchievementsScreen : ScreenBase
    {
        protected override void Build()
        {
            ScreenChrome.Header(this, "nav.achievements");
            var content = ScreenChrome.Scroll(Root, 200, 20);
            float y = 10;
            foreach (var a in Ctx.Data.Economy.Achievements)
            {
                float p = Ctx.Meta.AchievementProgress(a, out int target);
                int tier = Ctx.Profile.AchievementTier.TryGetValue(a.Id, out int t) ? t : 0;
                var row = Ui.Panel(content, a.Id);
                row.rectTransform.Band(true, y, 170, 40);
                var title = Ui.Text(row.transform, Loc.T("ach." + a.Id), 36, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
                title.Rect.Band(true, 22, 60, 40);
                var detail = Ui.Text(row.transform, Loc.T("ach.progress", Loc.Number(Ctx.Meta.AchievementMetric(a)), Loc.Number(target), tier, a.Tiers.Length), 28, Ui.InkSoft, TextAnchor.MiddleLeft);
                detail.Rect.Band(true, 80, 40, 40);
                var bg = Ui.Image(row.transform, "bar", Ui.Art.White, Palette.SandDeep.ToColor());
                bg.rectTransform.Band(false, 22, 18, 40);
                var fill = Ui.Image(bg.transform, "fill", Ui.Art.White, Palette.Turquoise.ToColor());
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(p, 1);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                y += 186;
            }
            if (Ctx.Games.IsSignedIn)
            {
                var lb = Ui.Button(content, Loc.T("ach.leaderboards"), FrameStyle.ButtonSecondary, () => Ctx.Games.ShowLeaderboard("classic"), "trophy", 38);
                ((RectTransform)lb.transform).Band(true, y + 10, 110, 60);
                y += 140;
            }
            content.sizeDelta = new Vector2(0, y + 60);
        }
    }

    public sealed class SettingsScreen : ScreenBase
    {
        private RectTransform _content;

        protected override void Build()
        {
            ScreenChrome.Header(this, "nav.settings");
            _content = ScreenChrome.Scroll(Root, 200, 20);
            Fill();
        }

        public override void Refresh()
        {
            for (int i = _content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);
            Fill();
        }

        private void Fill()
        {
            var s = Ctx.Settings;
            float y = 10;
            y = Section(y, "settings.audio");
            y = Cycle(y, "settings.music", Pct(s.MusicVolume), () => s.MusicVolume = NextVolume(s.MusicVolume));
            y = Cycle(y, "settings.sfx", Pct(s.SfxVolume), () => s.SfxVolume = NextVolume(s.SfxVolume));
            y = Toggle(y, "settings.vibration", s.Vibration, v => s.Vibration = v);

            y = Section(y, "settings.access");
            y = Toggle(y, "settings.reduce_motion", s.ReduceMotion, v => s.ReduceMotion = v);
            y = Toggle(y, "settings.strong_patterns", s.StrongPatterns, v => s.StrongPatterns = v);
            y = Cycle(y, "settings.text_size", Mathf.RoundToInt(s.TextScale * 100) + " %", () => s.TextScale = s.TextScale < 1.1f ? 1.15f : s.TextScale < 1.2f ? 1.3f : 1f);
            y = Toggle(y, "settings.left_handed", s.LeftHanded, v => s.LeftHanded = v);
            y = Cycle(y, "settings.lift", Loc.T("settings.lift_cells", s.LiftCells), () => s.LiftCells = (s.LiftCells + 1) % 4);
            y = Toggle(y, "settings.relaxed", s.RelaxedMode, v => s.RelaxedMode = v);
            y = Toggle(y, "settings.reminder", s.SessionReminder, v => s.SessionReminder = v);
            y = Cycle(y, "settings.language", Loc.T("lang." + Loc.Language), CycleLanguage);
            y = Toggle(y, "settings.notifications", Ctx.Profile.NotificationsAllowed, SetNotifications);

            y = Section(y, "settings.info");
            y = Link(y, "settings.fair_play", () => Nav.Push(new FairPlayScreen()));
            y = Link(y, "settings.credits", () => Nav.Push(new CreditsScreen()));
            y = Link(y, "settings.privacy", () => Ctx.Consent.ShowPrivacyOptions(null));
            y = Link(y, "settings.privacy_policy", () => OpenIfSet(ExternalIds.Instance.privacyPolicyUrl));
            y = Link(y, "shop.restore", () => Shop.Restore(Ctx, ok => Nav.Push(new MessageDialog("shop.restore", Loc.T(ok ? "shop.restore_ok" : "shop.restore_fail")))));
            y = Link(y, "settings.delete_data", () => Nav.Push(new DeleteDataDialog()));
            var version = Ui.Text(_content, "Tesela " + Application.version, 26, Ui.InkSoft);
            version.Rect.Band(true, y + 20, 50);
            _content.sizeDelta = new Vector2(0, y + 120);
        }

        private static float NextVolume(float v) => v >= 0.95f ? 0f : Mathf.Min(1f, Mathf.Round((v + 0.25f) * 4f) / 4f);
        private static string Pct(float v) => Mathf.RoundToInt(v * 100) + " %";

        private void CycleLanguage()
        {
            int i = Array.IndexOf(Loc.Languages, Loc.Language);
            Ctx.Settings.Language = Loc.Languages[(i + 1) % Loc.Languages.Length];
            Loc.SetLanguage(Ctx.Settings.Language);
        }

        private void SetNotifications(bool on)
        {
            if (!on) { Ctx.Profile.NotificationsAllowed = false; Ctx.Notifications.CancelAll(); return; }
            Ctx.Notifications.RequestPermission(granted =>
            {
                Ctx.Profile.NotificationsAllowed = granted;
                Ctx.Saves.SaveProfile(Ctx.Profile);
                Refresh();
            });
        }

        private static void OpenIfSet(string url)
        {
            if (ExternalIds.IsSet(url)) Application.OpenURL(url);
        }

        private void Changed()
        {
            TeselaApp.ApplySettings(Ctx);
            Ctx.Saves.SaveSettings(Ctx.Settings);
            Ctx.Saves.SaveProfile(Ctx.Profile);
            Refresh();
        }

        private float Section(float y, string key)
        {
            var t = Ui.Text(_content, Loc.T(key), 42, Ui.Ink, TextAnchor.MiddleLeft, TextRole.Display);
            t.Rect.Band(true, y + 16, 70, 50);
            return y + 96;
        }

        private float Row(float y, string key, string value, Action onClick, bool highlighted)
        {
            var row = Ui.Panel(_content, key, FrameStyle.Chip);
            row.rectTransform.Band(true, y, 116, 40);
            var label = Ui.Text(row.transform, Loc.T(key), 34, Ui.Ink, TextAnchor.MiddleLeft);
            label.Rect.Stretch(40, 0, 280, 0);
            var b = Ui.Button(row.transform, value, highlighted ? FrameStyle.ButtonPrimary : FrameStyle.ButtonSecondary, onClick, null, 32);
            ((RectTransform)b.transform).Place(1, 0.5f, 230, 88, -140, 0);
            return y + 130;
        }

        private float Toggle(float y, string key, bool value, Action<bool> set) =>
            Row(y, key, Loc.T(value ? "common.on" : "common.off"), () => { set(!value); Changed(); }, value);

        private float Cycle(float y, string key, string value, Action next) => Row(y, key, value, () => { next(); Changed(); }, false);

        private float Link(float y, string key, Action open) => Row(y, key, Loc.T("common.open"), open, false);
    }

    public sealed class DeleteDataDialog : DialogBase
    {
        protected override string TitleKey => "settings.delete_data";
        protected override float PanelHeight => 760;

        protected override void BuildContent()
        {
            Line(Loc.T("delete.body"), 34, 220);
            Action(Loc.T("delete.confirm"), FrameStyle.ButtonSecondary, Delete, "close");
            Action(Loc.T("common.cancel"), FrameStyle.ButtonPrimary, Close);
        }

        private void Delete()
        {
            Ctx.Saves.DeleteEverything();
            Ctx.Log("data_deleted");
            TeselaApp.Restart();
        }
    }

    /// <summary>Pantalla "Juego limpio": las reglas del generador, en palabras simples (fuente: docs/FAIR_PLAY.md).</summary>
    public sealed class FairPlayScreen : ScreenBase
    {
        protected override void Build()
        {
            ScreenChrome.Header(this, "settings.fair_play");
            var content = ScreenChrome.Scroll(Root, 200, 20);
            float y = 10;
            for (int i = 1; i <= 7; i++)
            {
                var card = Ui.Panel(content, "rule" + i);
                var t = Ui.Text(card.transform, Loc.T("fair.rule" + i), 32, Ui.Ink, TextAnchor.MiddleLeft);
                t.Rect.Stretch(40, 16, 40, 16);
                card.rectTransform.Band(true, y, 200, 40);
                y += 216;
            }
            var table = Ui.Text(content, DifficultyTable(), 30, Ui.InkSoft, TextAnchor.UpperLeft);
            table.Rect.Band(true, y + 10, 440, 60);
            content.sizeDelta = new Vector2(0, y + 520);
        }

        private string DifficultyTable()
        {
            var sb = new System.Text.StringBuilder(Loc.T("fair.table") + "\n");
            foreach (var step in Ctx.Data.Pieces.Difficulty)
                sb.Append(Loc.T("fair.table_row", Loc.Number(step.MinScore), step.LargeWeightMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))).Append('\n');
            return sb.ToString();
        }
    }

    public sealed class CreditsScreen : ScreenBase
    {
        protected override void Build()
        {
            ScreenChrome.Header(this, "settings.credits");
            var content = ScreenChrome.Scroll(Root, 200, 20);
            float y = 10;
            foreach (string c in Ctx.Data.Credits)
            {
                var t = Ui.Text(content, c, 30, Ui.Ink, TextAnchor.MiddleLeft);
                t.Rect.Band(true, y, 150, 50);
                y += 160;
            }
            content.sizeDelta = new Vector2(0, y + 60);
        }
    }

    /// <summary>
    /// Consentimiento antes de cargar anuncios. Llega después de la primera partida (nunca bloquea el tutorial).
    /// En producción se muestra la CMP certificada (UMP); sin ella, este paso explica la opción y continúa.
    /// </summary>
    public sealed class ConsentScreen : DialogBase
    {
        protected override string TitleKey => "consent.title";
        protected override float PanelHeight => 980;
        protected override bool ShowClose => false;

        protected override void BuildContent()
        {
            Line(Loc.T("consent.body"), 32, 330);
            Action(Loc.T("consent.continue"), FrameStyle.ButtonPrimary, Continue, "check");
            Action(Loc.T("settings.privacy_policy"), FrameStyle.ButtonSecondary, () =>
            {
                if (ExternalIds.IsSet(ExternalIds.Instance.privacyPolicyUrl)) Application.OpenURL(ExternalIds.Instance.privacyPolicyUrl);
            }, "info");
        }

        private void Continue()
        {
            Ctx.Consent.RequestIfNeeded(status =>
            {
                Ctx.Profile.ConsentAsked = true;
                Ctx.Ads.Initialize(Ctx.Consent.PersonalizedAdsAllowed);
                Ctx.Saves.SaveProfile(Ctx.Profile);
                Close();
            });
        }
    }
}
