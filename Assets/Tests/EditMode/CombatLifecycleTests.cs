#if UNITY_EDITOR
using System.Reflection;
using CrossDefense.Core;
using CrossDefense.Data;
using CrossDefense.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class CombatLifecycleTests
    {
        [Test]
        public void SporeShaman_SplitsOnceIntoRewardlessChildren()
        {
            MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(
                "Assets/Data/Monsters/Monster_GoblinSporeShaman.asset");

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Behavior, Is.EqualTo(MonsterBehavior.Splitter));
            Assert.That(data.HasDefeatSplit, Is.True);
            Assert.That(data.SplitChild, Is.SameAs(data));
            Assert.That(data.SplitChildCount, Is.EqualTo(2));
            Assert.That(data.SplitChildRewardMultiplier, Is.Zero);
            Assert.That(data.SplitChildSizeMultiplier, Is.LessThan(1f));
        }

        [Test]
        public void DamagePacketScaling_PreservesStunAndStatusDurations()
        {
            var packet = new DamagePacket(
                null,
                10f,
                MonsterAttribute.Ice,
                0.4f,
                2f,
                3f,
                4f,
                stunDuration: 1.25f);

            DamagePacket scaled = packet.Scaled(0.5f);

            Assert.That(scaled.BaseDamage, Is.EqualTo(5f));
            Assert.That(scaled.DamageOverTime, Is.EqualTo(1.5f));
            Assert.That(scaled.SlowDuration, Is.EqualTo(2f));
            Assert.That(scaled.DamageOverTimeDuration, Is.EqualTo(4f));
            Assert.That(scaled.StunDuration, Is.EqualTo(1.25f));
        }

        [Test]
        public void ProjectilePoolReset_ClearsTargetsCallbacksAndTransform()
        {
            var projectileObject = new GameObject("Projectile Reset Test");
            var targetObject = new GameObject("Projectile Target Test");
            projectileObject.AddComponent<SpriteRenderer>();
            var projectile = projectileObject.AddComponent<CombatProjectileController>();
            var target = targetObject.AddComponent<MonsterController>();
            try
            {
                SetField(projectile, "_target", target);
                SetField(projectile, "_speed", 9f);
                SetField(projectile, "_impactCallback",
                    new System.Action<ProjectileImpactContext>(_ => { }));
                projectileObject.transform.localScale = Vector3.one * 3f;
                projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

                projectile.ResetForPool();

                Assert.That(GetField<MonsterController>(projectile, "_target"), Is.Null);
                Assert.That(GetField<float>(projectile, "_speed"), Is.Zero);
                Assert.That(
                    GetField<System.Action<ProjectileImpactContext>>(
                        projectile,
                        "_impactCallback"),
                    Is.Null);
                Assert.That(projectileObject.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(projectileObject.transform.rotation, Is.EqualTo(Quaternion.identity));
            }
            finally
            {
                Object.DestroyImmediate(projectileObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        static void SetField<T>(object target, string name, T value) =>
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);

        static T GetField<T>(object target, string name) =>
            (T)target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(target);
    }
}
#endif
