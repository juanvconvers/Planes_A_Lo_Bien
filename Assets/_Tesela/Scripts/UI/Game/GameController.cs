using System;
using System.Collections.Generic;
using Tesela.Core;
using Tesela.Gameplay;
using Tesela.Meta;
using Tesela.Monetization;

namespace Tesela.UI
{
    /// <summary>
    /// Orquesta una partida: crea la sesión del modo, guarda tras cada jugada, cobra potenciadores y, al terminar,
    /// entrega recompensas de meta. No sabe nada de anuncios para el generador: solo pasa el resultado hacia afuera.
    /// </summary>
    public sealed class GameController
    {
        private readonly AppContext _ctx;
        public GameSession Session { get; private set; }
        public string ModeKey { get; private set; }
        public LevelDefinition Level => Session?.Level;
        public int BoardsEmptied { get; private set; }
        public RunRewards LastRewards { get; private set; }
        public RunSummary LastSummary { get; private set; }
        public bool Finished { get; private set; }
        public bool IsTutorial { get; private set; }

        public GameController(AppContext ctx) { _ctx = ctx; }

        public static string KeyFor(GameMode m)
        {
            switch (m)
            {
                case GameMode.Daily: return "daily";
                case GameMode.Zen: return "zen";
                case GameMode.Restoration: return "restoration";
                case GameMode.Event: return "event";
                default: return "classic";
            }
        }

        public void StartClassic(bool tutorial = false)
        {
            var rules = ModeRules.Classic();
            rules.Relaxed = _ctx.Settings.RelaxedMode;
            IsTutorial = tutorial;
            Begin(rules, Seeds.Random(_ctx.UtcNow.Ticks), null);
        }

        public void StartDaily() => Begin(ModeRules.Daily(), Seeds.Daily(_ctx.Today), null);

        public void StartZen() => Begin(ModeRules.Zen(), Seeds.Random(_ctx.UtcNow.Ticks), null);

        public void StartLevel(LevelDefinition level) => Begin(ModeRules.Restoration(), level.Seed, level);

        public void StartEvent(ModeRules rules) => Begin(rules, Seeds.Event(rules.EventId ?? "event", DayKey.WeekFromDay(_ctx.Today)), null);

        private void Begin(ModeRules rules, ulong seed, LevelDefinition level)
        {
            Session = new GameSession(rules, _ctx.Data.Scoring, _ctx.Data.Pieces, seed, level);
            ModeKey = KeyFor(rules.Mode);
            BoardsEmptied = 0;
            Finished = false;
            LastRewards = null;
            _ctx.AdState.OnRunStarted();
            _ctx.Log("run_start", new Dictionary<string, object> { { "mode", ModeKey }, { "seed", Session.SeedCode } });
            if (level != null) _ctx.Log("level_start", new Dictionary<string, object> { { "level", level.Id } });
            Save();
        }

        /// <summary>Reanuda la partida guardada (si la app se cerró o un anuncio la colgó).</summary>
        public bool TryResume()
        {
            if (!_ctx.Saves.TryLoadRun(out var state)) return false;
            try
            {
                Session = GameSession.FromState(state["session"], _ctx.Data.Scoring, _ctx.Data.Pieces,
                    id => _ctx.Data.LevelById.TryGetValue(id, out var l) ? l : null);
                ModeKey = state["mode"].AsString("classic");
                BoardsEmptied = state["emptied"].AsInt();
                Finished = false;
                _ctx.AdState.ReviveUsedThisRun = state["reviveUsed"].AsBool();
                if (!Session.IsOver || Session.CanRevive) return true;
                _ctx.Saves.ClearRun();
                return false;
            }
            catch (Exception e) when (e is FormatException || e is KeyNotFoundException || e is ArgumentException)
            {
                _ctx.Saves.ClearRun();
                return false;
            }
        }

        public void Save()
        {
            if (Session == null || Finished) return;
            _ctx.Saves.SaveRun(new Dictionary<string, object>
            {
                { "mode", ModeKey }, { "emptied", BoardsEmptied }, { "reviveUsed", _ctx.AdState.ReviveUsedThisRun },
                { "session", Session.ToState() }
            });
        }

        public MoveResult Place(int slot, int x, int y)
        {
            var r = Session.Place(slot, x, y);
            if (r.BoardEmptied) BoardsEmptied++;
            Save();
            return r;
        }

        // ───────────── Potenciadores ─────────────

        public int Charges(BoosterType t) => _ctx.Profile.Boosters[(int)t];
        public int Price(BoosterType t) => _ctx.Data.Economy.BoosterPrice(t);

        /// <summary>Gasta una carga (o monedas si no quedan) y aplica el efecto. Si el efecto no se pudo aplicar, no cobra.</summary>
        public bool UseBooster(BoosterType t, Func<bool> apply)
        {
            if (Session == null || Finished) return false;
            bool hasCharge = Charges(t) > 0;
            if (!hasCharge && _ctx.Profile.Coins < Price(t)) return false;
            if (!apply()) return false;
            _ctx.Meta.ConsumeBooster(t, true);
            _ctx.Log("booster_use", new Dictionary<string, object> { { "booster", Boosters.Key(t) }, { "paid", hasCharge ? "charge" : "coins" } });
            Save();
            _ctx.Saves.SaveProfile(_ctx.Profile);
            return true;
        }

        /// <summary>Potenciador gratis por anuncio con premio: suma una carga.</summary>
        public void GrantBooster(BoosterType t) => _ctx.Profile.Boosters[(int)t]++;

        public bool ReviveWithCoins()
        {
            if (!Session.CanRevive || !_ctx.Meta.PayRevive()) return false;
            Session.Revive();
            Save();
            return true;
        }

        public bool ReviveFree()
        {
            if (!Session.CanRevive) return false;
            Session.Revive();
            Save();
            return true;
        }

        // ───────────── Fin de partida ─────────────

        public int Stars
        {
            get
            {
                if (Level == null || Session.OverReason != GameOverReason.LevelComplete) return 0;
                int left = Session.MovesLeft;
                return left >= Level.ThreeStarMovesLeft ? 3 : left >= Level.TwoStarMovesLeft ? 2 : 1;
            }
        }

        public RunRewards Finish()
        {
            if (Finished) return LastRewards;
            Finished = true;
            bool completed = Session.OverReason == GameOverReason.LevelComplete;
            var summary = new RunSummary
            {
                Mode = ModeKey,
                Score = Session.Score,
                Lines = Session.LinesCleared,
                Pieces = Session.Moves,
                BestStreak = Session.BestStreak,
                BoostersUsed = Session.BoostersUsed,
                BoardsEmptied = BoardsEmptied,
                LevelId = Level?.Id,
                LevelCompleted = completed,
                Stars = Stars,
                Competitive = Session.Rules.Competitive && !IsTutorial
            };
            LastSummary = summary;
            LastRewards = _ctx.Meta.OnRunFinished(summary, _ctx.UtcNow);
            _ctx.AdState.OnRunFinished();
            _ctx.GamesThisSession++;
            if (IsTutorial) _ctx.Profile.TutorialDone = true;
            _ctx.Saves.ClearRun();
            _ctx.SaveAll();

            _ctx.Log("run_end", new Dictionary<string, object>
            {
                { "mode", ModeKey }, { "score", summary.Score }, { "lines", summary.Lines }, { "moves", summary.Pieces },
                { "duration_moves", Session.Moves }, { "reason", Session.OverReason.ToString() }
            });
            if (Level != null) _ctx.Log(completed ? "level_complete" : "level_fail", new Dictionary<string, object> { { "level", Level.Id }, { "stars", summary.Stars } });
            if (LastRewards.PassTiersGained > 0) _ctx.Log("pass_level", new Dictionary<string, object> { { "tier", _ctx.Meta.PassTier() } });
            _ctx.Log("streak_update", new Dictionary<string, object> { { "days", _ctx.Profile.StreakDays } });

            if (summary.Competitive && (ModeKey == "classic" || ModeKey == "daily")) _ctx.Games.SubmitScore(ModeKey, summary.Score);
            foreach (string a in LastRewards.AchievementsUnlocked) _ctx.Games.UnlockAchievement(a);
            return LastRewards;
        }

        /// <summary>Duplicar monedas (anuncio con premio): entrega otra vez las monedas de la partida.</summary>
        public void DoubleCoins()
        {
            if (LastRewards == null) return;
            _ctx.Profile.AddCoins(LastRewards.Coins, "ads");
            _ctx.Saves.SaveProfile(_ctx.Profile);
        }

        /// <summary>¿El capítulo del nivel quedó completo con esta victoria? (momento de logro para ofertas y álbum).</summary>
        public bool ChapterJustCompleted()
        {
            if (Level == null || Session.OverReason != GameOverReason.LevelComplete) return false;
            foreach (var l in _ctx.Data.Levels)
                if (l.Chapter == Level.Chapter && !_ctx.Profile.LevelStars.ContainsKey(l.Id)) return false;
            return true;
        }
    }
}
