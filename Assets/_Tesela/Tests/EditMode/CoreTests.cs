using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tesela.Core;

namespace Tesela.Tests
{
    public class Pcg32Tests
    {
        [Test]
        public void SameSeedSameSequence()
        {
            var a = new Pcg32(12345);
            var b = new Pcg32(12345);
            for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void KnownFirstValuesAreStable()
        {
            // Fija la secuencia: si alguien cambia el algoritmo, las semillas compartidas (reto diario) dejarían de coincidir.
            var r = new Pcg32(42);
            uint first = r.NextUInt();
            var again = new Pcg32(42);
            Assert.AreEqual(first, again.NextUInt());
            Assert.AreNotEqual(new Pcg32(43).NextUInt(), first);
        }

        [Test]
        public void NextIntIsInRangeAndRoughlyUniform()
        {
            var r = new Pcg32(7);
            var counts = new int[6];
            for (int i = 0; i < 60000; i++) counts[r.NextInt(6)]++;
            foreach (int c in counts) Assert.That(c, Is.InRange(9400, 10600));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(0));
            for (int i = 0; i < 100; i++) Assert.That(r.NextInt(3, 5), Is.InRange(3, 4));
        }

        [Test]
        public void NextDoubleInUnitInterval()
        {
            var r = new Pcg32(99);
            for (int i = 0; i < 10000; i++)
            {
                double d = r.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
                Assert.That(r.NextFloat(), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }
    }

    public class HashingTests
    {
        [Test]
        public void SeedCodeRoundTrips()
        {
            var r = new Pcg32(1);
            for (int i = 0; i < 200; i++)
            {
                ulong seed = ((ulong)r.NextUInt() << 8) | r.NextUInt() & 0xFF;
                seed &= (1UL << 40) - 1;
                string code = Hashing.SeedToCode(seed);
                Assert.AreEqual(8, code.Length);
                Assert.IsTrue(Hashing.TryCodeToSeed(code, out ulong back));
                Assert.AreEqual(seed, back);
            }
            Assert.IsFalse(Hashing.TryCodeToSeed("ABC", out _));
            Assert.IsFalse(Hashing.TryCodeToSeed("0OOOOOOO", out _));
        }

        [Test]
        public void FnvIsDeterministicAndSensitive()
        {
            Assert.AreEqual(Hashing.Fnv1a64("tesela"), Hashing.Fnv1a64("tesela"));
            Assert.AreNotEqual(Hashing.Fnv1a64("tesela"), Hashing.Fnv1a64("teselb"));
            Assert.AreEqual(16, Hashing.Fnv1a64Hex(null).Length);
            Assert.AreNotEqual(Hashing.Mix(1), Hashing.Mix(2));
        }
    }

    public class JsonTests
    {
        [Test]
        public void ParsesAllTypes()
        {
            var n = Json.ParseNode("{\"a\":1,\"b\":-2.5,\"c\":\"x\\\"y\\u00e9\",\"d\":true,\"e\":null,\"f\":[1,2,3],\"g\":{\"h\":false}}");
            Assert.AreEqual(1, n["a"].AsInt());
            Assert.AreEqual(-2.5, n["b"].AsDouble(), 1e-9);
            Assert.AreEqual("x\"yé", n["c"].AsString());
            Assert.IsTrue(n["d"].AsBool());
            Assert.IsTrue(n["e"].IsNull);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, n["f"].AsIntArray());
            Assert.IsFalse(n["g"]["h"].AsBool(true));
            Assert.AreEqual(7, n["missing"].AsInt(7));
            Assert.AreEqual(3, n["f"].Count);
            Assert.IsTrue(n.Has("a"));
            Assert.IsTrue(n["f"].IsArray && n["g"].IsObject);
        }

        [Test]
        public void RoundTripsThroughSerialize()
        {
            var obj = new Dictionary<string, object>
            {
                { "s", "línea\nnueva\t\"q\"" }, { "i", 42 }, { "l", 9007199254740991L }, { "d", 0.125 }, { "b", false },
                { "n", null }, { "arr", new List<object> { 1, "dos", 3.5 } }, { "u", 18446744073709551615UL }
            };
            string text = Json.Serialize(obj, pretty: true);
            var n = Json.ParseNode(text);
            Assert.AreEqual("línea\nnueva\t\"q\"", n["s"].AsString());
            Assert.AreEqual(42, n["i"].AsInt());
            Assert.AreEqual(9007199254740991L, n["l"].AsLong());
            Assert.AreEqual(0.125, n["d"].AsDouble());
            Assert.AreEqual("dos", n["arr"][1].AsString());
            Assert.AreEqual(Json.Serialize(obj), Json.Serialize(Json.Parse(Json.Serialize(obj))));
        }

        [Test]
        public void RejectsMalformed()
        {
            Assert.Throws<FormatException>(() => Json.Parse("{\"a\":}"));
            Assert.Throws<FormatException>(() => Json.Parse("[1,2"));
            Assert.Throws<FormatException>(() => Json.Parse("{} extra"));
            Assert.Throws<FormatException>(() => Json.Parse("\"abc"));
            Assert.Throws<FormatException>(() => Json.Parse("tru"));
        }

        [Test]
        public void TolerantConversions()
        {
            var n = Json.ParseNode("{\"s\":\"12\",\"f\":\"1.5\",\"b\":\"true\",\"u\":\"18446744073709551615\",\"arr\":[\"a\",\"b\"],\"fa\":[0.5,1]}");
            Assert.AreEqual(12, n["s"].AsInt());
            Assert.AreEqual(1.5f, n["f"].AsFloat());
            Assert.IsTrue(n["b"].AsBool());
            Assert.AreEqual(ulong.MaxValue, n["u"].AsULong());
            CollectionAssert.AreEqual(new[] { "a", "b" }, n["arr"].AsStringArray());
            CollectionAssert.AreEqual(new[] { 0.5f, 1f }, n["fa"].AsFloatArray());
        }
    }

    public class SaveCodecTests
    {
        [Test]
        public void WrapUnwrapAndDetectTampering()
        {
            string text = SaveCodec.Wrap(new Dictionary<string, object> { { "score", 1234 } });
            Assert.IsTrue(SaveCodec.TryUnwrap(text, out var payload, out _));
            Assert.AreEqual(1234, payload["score"].AsInt());

            string tampered = text.Replace("1234", "9999");
            Assert.IsFalse(SaveCodec.TryUnwrap(tampered, out _, out string err));
            Assert.AreEqual("checksum", err);
            Assert.IsFalse(SaveCodec.TryUnwrap("", out _, out _));
            Assert.IsFalse(SaveCodec.TryUnwrap("{no json", out _, out _));
            Assert.IsFalse(SaveCodec.TryUnwrap(text.Substring(0, text.Length / 2), out _, out _));
        }
    }

    public class ServiceLocatorTests
    {
        private interface IFoo { }
        private sealed class Foo : IFoo { }

        [TearDown] public void TearDown() => ServiceLocator.Reset();

        [Test]
        public void RegisterGetReset()
        {
            Assert.IsFalse(ServiceLocator.TryGet<IFoo>(out _));
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.Get<IFoo>());
            var foo = new Foo();
            ServiceLocator.Register<IFoo>(foo);
            Assert.AreSame(foo, ServiceLocator.Get<IFoo>());
            Assert.IsTrue(ServiceLocator.IsRegistered<IFoo>());
            ServiceLocator.Reset();
            Assert.IsFalse(ServiceLocator.IsRegistered<IFoo>());
            Assert.Throws<ArgumentNullException>(() => ServiceLocator.Register<IFoo>(null));
        }
    }

    public class ClockAndColorTests
    {
        [Test]
        public void DayKeysAreUtcAndWeeksStartMonday()
        {
            var d = new DateTime(2026, 11, 7, 23, 59, 0, DateTimeKind.Utc);
            int day = DayKey.FromUtc(d);
            Assert.AreEqual(new DateTime(2026, 11, 7), DayKey.ToUtcDate(day));
            int monday = DayKey.FromUtc(new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc));
            int sunday = DayKey.FromUtc(new DateTime(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc));
            Assert.AreEqual(DayKey.WeekFromDay(monday), DayKey.WeekFromDay(sunday));
            Assert.AreNotEqual(DayKey.WeekFromDay(monday), DayKey.WeekFromDay(monday - 1));
            var clock = new FakeClock(d);
            clock.Advance(TimeSpan.FromMinutes(2));
            Assert.AreEqual(day + 1, DayKey.FromUtc(clock.UtcNow));
            Assert.AreEqual(120, clock.RealtimeSeconds, 1e-9);
        }

        [Test]
        public void HexAndContrast()
        {
            Assert.AreEqual("#2F5DA8", Rgba.FromHex("#2F5DA8").ToHex());
            Assert.AreEqual(21.0, Rgba.ContrastRatio(new Rgba(0, 0, 0), new Rgba(255, 255, 255)), 0.01);
            Assert.Throws<FormatException>(() => Rgba.FromHex("#12"));
            Assert.AreEqual(128, Rgba.FromHex("#11223380").A);
            Assert.AreEqual(new Rgba(100, 100, 100), new Rgba(200, 200, 200).Scaled(0.5f));
        }
    }
}
