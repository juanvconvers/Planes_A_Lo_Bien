using System;
using System.Collections.Generic;
using Tesela.Core;
using Tesela.Meta;

namespace Tesela.Monetization
{
    public enum Segment
    {
        New = 0,
        EngagedNonPayer = 1,
        FirstPayer = 2,
        Repeat = 3,
        Whale = 4,
        AtRisk = 5
    }

    public enum OfferMoment
    {
        ChapterComplete = 0,
        NewBest = 1,
        PassTierUp = 2,
        ReturnAfterAbsence = 3,
        GameOver = 4
    }

    /// <summary>Segmentos calculados en el dispositivo (sin enviar datos a ningún servidor).</summary>
    public static class Segmentation
    {
        public static Segment Compute(PlayerProfile p, int today, int previousActiveDay)
        {
            if (previousActiveDay >= 0 && today - previousActiveDay >= 5) return Segment.AtRisk;
            if (p.TotalSpentUsd >= 50) return Segment.Whale;
            if (p.PurchaseCount >= 2) return Segment.Repeat;
            if (p.PurchaseCount == 1) return Segment.FirstPayer;
            int age = p.FirstDay < 0 ? 0 : today - p.FirstDay;
            if (age <= 3) return Segment.New;
            return Segment.EngagedNonPayer;
        }

        public static string Key(Segment s)
        {
            switch (s)
            {
                case Segment.New: return "new";
                case Segment.EngagedNonPayer: return "engaged_nonpayer";
                case Segment.FirstPayer: return "first_payer";
                case Segment.Repeat: return "repeat";
                case Segment.Whale: return "whale";
                default: return "at_risk";
            }
        }
    }

    public sealed class OfferRule
    {
        public string Id;
        public string Segment;
        public string[] Moments;
        public string ProductKey;
        public string Gift;
        public int MinAgeDays;
        public int Priority;
    }

    /// <summary>Motor de ofertas (reglas en Remote Config). Máximo una ventana por sesión, nunca antes de la 2.ª partida y solo en momentos de logro.</summary>
    public sealed class OfferEngine
    {
        public readonly List<OfferRule> Rules = new List<OfferRule>();
        public int MinGamesInSession = 2;
        public bool ShownThisSession { get; private set; }

        public static OfferEngine FromJson(JsonNode n)
        {
            var e = new OfferEngine { MinGamesInSession = n["minGamesInSession"].AsInt(2) };
            foreach (var r in n["rules"].Items)
            {
                e.Rules.Add(new OfferRule
                {
                    Id = r["id"].AsString(),
                    Segment = r["segment"].AsString(),
                    Moments = r["moments"].AsStringArray(),
                    ProductKey = r["product"].AsString(),
                    Gift = r["gift"].AsString(),
                    MinAgeDays = r["minAgeDays"].AsInt(0),
                    Priority = r["priority"].AsInt(0)
                });
            }
            e.Rules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            return e;
        }

        public void StartSession() => ShownThisSession = false;

        public static string MomentKey(OfferMoment m)
        {
            switch (m)
            {
                case OfferMoment.ChapterComplete: return "chapter_complete";
                case OfferMoment.NewBest: return "new_best";
                case OfferMoment.PassTierUp: return "pass_tier";
                case OfferMoment.ReturnAfterAbsence: return "return";
                default: return "game_over";
            }
        }

        /// <summary>Devuelve la regla a mostrar o null. Nunca ofrece nada para "explotar" una derrota.</summary>
        public OfferRule Pick(OfferMoment moment, Segment segment, PlayerProfile profile, int gamesThisSession, int today, Func<string, bool> productAvailable)
        {
            if (ShownThisSession || moment == OfferMoment.GameOver) return null;
            if (moment != OfferMoment.ReturnAfterAbsence && gamesThisSession < MinGamesInSession) return null;
            string seg = Segmentation.Key(segment);
            string mom = MomentKey(moment);
            int age = profile.FirstDay < 0 ? 0 : today - profile.FirstDay;
            foreach (var r in Rules)
            {
                if (r.Segment != seg || Array.IndexOf(r.Moments, mom) < 0 || age < r.MinAgeDays) continue;
                if (!string.IsNullOrEmpty(r.ProductKey) && productAvailable != null && !productAvailable(r.ProductKey)) continue;
                return r;
            }
            return null;
        }

        public void MarkShown() => ShownThisSession = true;
    }
}
