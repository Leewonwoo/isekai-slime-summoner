#if UNITY_EDITOR
using CrossDefense.Data;
using NUnit.Framework;
using UnityEditor;
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
        public void DefaultStageBalance_IncreasesEnemyPressure()
        {
            StageBalanceProfile profile = AssetDatabase.LoadAssetAtPath<StageBalanceProfile>(
                "Assets/Data/StageBalance_Default.asset");
            StageTimeline timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(
                "Assets/Data/StageTimelines/Stage_01.asset");

            Assert.That(profile, Is.Not.Null);
            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.BalanceProfile, Is.SameAs(profile));
            Assert.That(profile.HpMultiplier, Is.EqualTo(1.15f).Within(0.001f));
            Assert.That(profile.SpeedMultiplier, Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(profile.SpawnIntervalMultiplier, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(profile.RewardMultiplier, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void Stage01_FirstTenWavesUseBeginnerDifficultyCurve()
        {
            StageTimeline timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(
                "Assets/Data/StageTimelines/Stage_01.asset");
            int[] expectedMonsterCounts = { 3, 4, 4, 5, 6, 6, 10, 24, 14, 17 };

            Assert.That(timeline, Is.Not.Null);
            for (int index = 0; index < expectedMonsterCounts.Length; index++)
            {
                Assert.That(timeline.TryGetWave(index, out StageWave wave), Is.True);
                Assert.That(wave.HpMultiplier, Is.LessThanOrEqualTo(1.2f));
                Assert.That(wave.TotalMonsterCount, Is.EqualTo(expectedMonsterCounts[index]));
            }

            Assert.That(timeline.Waves[0].HpMultiplier, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void Stage01_AfterWaveTenHpPressureAcceleratesForPersistentBuilds()
        {
            StageTimeline timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(
                "Assets/Data/StageTimelines/Stage_01.asset");

            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.BalanceProfile, Is.Not.Null);

            float PressureAt(int waveNumber)
            {
                StageWave wave = timeline.Waves[waveNumber - 1];
                MonsterSpawnEntry spawn = wave.MonsterSpawns[0];
                float baseMultiplier = timeline.BalanceProfile.HpMultiplier *
                                       wave.HpMultiplier *
                                       spawn.HpMultiplier;
                return timeline.GetMonsterHpMultiplier(wave, spawn) / baseMultiplier;
            }

            Assert.That(PressureAt(10), Is.EqualTo(1f).Within(0.001f));
            Assert.That(PressureAt(20), Is.EqualTo(1.3f).Within(0.001f));
            Assert.That(PressureAt(30), Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(PressureAt(40), Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(PressureAt(50), Is.EqualTo(3.4f).Within(0.001f));
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
        public void PrototypeTimeline_AssignsRequestedMonsterMoveFrames()
        {
            var texture = new Texture2D(2, 1);
            var first = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            var second = Sprite.Create(texture, new Rect(1, 0, 1, 1), Vector2.one * 0.5f, 1f);
            var frames = new[] { first, second };
            var timeline = StageTimeline.CreatePrototype(1, first, frames);

            Assert.That(timeline.TryGetWave(0, out var wave), Is.True);
            Assert.That(wave.MonsterSpawns[0].Monster.MoveFrames, Is.SameAs(frames));
            Assert.That(wave.MonsterSpawns[0].Monster.MoveAnimationFps, Is.EqualTo(12f));

            Object.DestroyImmediate(timeline);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void PrototypeMonster_HasValidGoblinCombatValues()
        {
            var monster = MonsterData.CreatePrototype(
                "combat-probe",
                "Combat Probe",
                MonsterShape.Grunt,
                MonsterAttribute.None,
                100,
                1f,
                7,
                1,
                attacksPerSecond: 1.5f,
                attackRange: 0.65f);

            Assert.That(monster.ContactDamage, Is.EqualTo(7));
            Assert.That(monster.AttacksPerSecond, Is.EqualTo(1.5f));
            Assert.That(monster.AttackRange, Is.EqualTo(0.65f));

            Object.DestroyImmediate(monster);
        }

        [Test]
        public void SpawnEntry_SizeMultiplier_IsIndependentFromMonsterBaseSize()
        {
            var monster = MonsterData.CreatePrototype(
                "size-probe",
                "Size Probe",
                MonsterShape.Grunt,
                MonsterAttribute.None,
                100,
                1f,
                5,
                1,
                sizeMultiplier: 0.9f);
            var entry = MonsterSpawnEntry.CreatePrototype(monster, 1, 0.5f, 3f);

            Assert.That(monster.SizeMultiplier, Is.EqualTo(0.9f));
            Assert.That(entry.SizeMultiplier, Is.EqualTo(3f));

            Object.DestroyImmediate(monster);
        }

        [Test]
        public void MonsterController_PreservesSpriteColorAndCombinesBaseAndSpawnSize()
        {
            var monster = MonsterData.CreatePrototype(
                "boss-size-probe",
                "Boss Size Probe",
                MonsterShape.Grunt,
                MonsterAttribute.Fire,
                100,
                1f,
                5,
                1,
                sizeMultiplier: 0.9f);
            var gameObject = new GameObject("Boss Size Probe");
            gameObject.AddComponent<SpriteRenderer>();
            var controller = gameObject.AddComponent<CrossDefense.Units.MonsterController>();

            controller.Initialize(null, null, monster, 1f, 1f, 1f, 3f);

            Assert.That(controller.transform.localScale.x, Is.EqualTo(0.9f * 3f).Within(0.001f));
            Assert.That(gameObject.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));

            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(monster);
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

        [Test]
        public void Stage01_GoldenGoblin_IsGuaranteedEveryFifteenDays()
        {
            StageTimeline timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(
                "Assets/Data/StageTimelines/Stage_01.asset");

            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.GoldenGoblin, Is.Not.Null);
            Assert.That(timeline.GoldenGoblin.AppearanceChance, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(timeline.GoldenGoblin.GuaranteedInterval, Is.EqualTo(15));
            Assert.That(timeline.ShouldSpawnGoldenGoblin(15, 12345), Is.True);
            Assert.That(timeline.ShouldSpawnGoldenGoblin(30, 12345), Is.True);
            Assert.That(timeline.ShouldSpawnGoldenGoblin(45, 12345), Is.True);
        }

        [Test]
        public void GoldenGoblinRoll_IsStableForSameRunSeedAndDay()
        {
            StageTimeline timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(
                "Assets/Data/StageTimelines/Stage_01.asset");

            bool first = timeline.ShouldSpawnGoldenGoblin(7, 987654);
            bool second = timeline.ShouldSpawnGoldenGoblin(7, 987654);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(
                StageTimeline.GoldenGoblinRollSeed(20260720, 987654, 7),
                Is.EqualTo(StageTimeline.GoldenGoblinRollSeed(20260720, 987654, 7)));
        }
    }
}
#endif
