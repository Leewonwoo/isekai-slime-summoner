using CrossDefense.Data;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class StageTimelineTests
    {
        [Test]
        public void PrototypeTimeline_UsesRequestedWaveCount()
        {
            var timeline = StageTimeline.CreatePrototype(7);

            Assert.That(timeline.WaveCount, Is.EqualTo(7));
            Assert.That(timeline.TryGetWave(0, out var first), Is.True);
            Assert.That(first.TotalMonsterCount, Is.GreaterThan(0));
            Assert.That(timeline.TryGetWave(7, out _), Is.False);

            Object.DestroyImmediate(timeline);
        }

        [Test]
        public void PrototypeTimeline_ValidatesWithoutErrors()
        {
            var timeline = StageTimeline.CreatePrototype(3);

            Assert.That(timeline.Validate(), Is.Empty);

            Object.DestroyImmediate(timeline);
        }

        [Test]
        public void PrototypeTimeline_AssignsRequestedMonsterSprite()
        {
            var texture = new Texture2D(1, 1);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            var timeline = StageTimeline.CreatePrototype(1, sprite);

            Assert.That(timeline.TryGetWave(0, out var wave), Is.True);
            Assert.That(wave.MonsterSpawns[0].Monster.Sprite, Is.SameAs(sprite));

            Object.DestroyImmediate(timeline);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SpawnZoneSelection_IsDeterministicForSameSeed()
        {
            var timeline = StageTimeline.CreatePrototype(1);
            Assert.That(timeline.TryGetWave(0, out var wave), Is.True);

            var firstRandom = new System.Random(1234);
            var secondRandom = new System.Random(1234);
            for (int i = 0; i < 20; i++)
                Assert.That(timeline.ChooseSpawnZone(wave, firstRandom), Is.EqualTo(timeline.ChooseSpawnZone(wave, secondRandom)));

            Object.DestroyImmediate(timeline);
        }
    }
}
