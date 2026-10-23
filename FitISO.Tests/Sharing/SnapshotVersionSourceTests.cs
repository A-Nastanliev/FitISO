using System;
using FitISO.Sharing.Session;
using NUnit.Framework;

namespace FitISO.Tests.Sharing
{
    [TestFixture]
    public class SnapshotVersionSourceTests
    {
        private static readonly DateTime Start = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Next_FirstVersion_IsClockTicks()
        {
            var source = new SnapshotVersionSource(() => Start);

            Assert.That(source.Next(), Is.EqualTo(Start.Ticks));
        }

        [Test]
        public void Next_WhenClockStandsStill_StillIncreases()
        {
            var source = new SnapshotVersionSource(() => Start);

            var first = source.Next();
            var second = source.Next();
            var third = source.Next();

            Assert.That(second, Is.GreaterThan(first));
            Assert.That(third, Is.GreaterThan(second));
        }

        [Test]
        public void Next_WhenClockMovesBackwards_NeverGoesBackwards()
        {
            var now = Start;
            var source = new SnapshotVersionSource(() => now);

            var first = source.Next();
            now = Start.AddMinutes(-5);
            var second = source.Next();

            Assert.That(second, Is.GreaterThan(first));
        }

        [Test]
        public void Next_AfterRestartLater_IsHigherThanBeforeRestart()
        {
            var before = new SnapshotVersionSource(() => Start).Next();
            var afterRestart = new SnapshotVersionSource(() => Start.AddMinutes(10)).Next();

            Assert.That(afterRestart, Is.GreaterThan(before));
        }
    }
}
