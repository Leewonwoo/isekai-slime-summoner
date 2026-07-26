#if UNITY_EDITOR
using CrossDefense.Core;
using CrossDefense.Data;
using NUnit.Framework;

namespace CrossDefense.Tests.EditMode
{
    public sealed class ElementalMatchupTests
    {
        [TestCase(MonsterAttribute.Fire, MonsterAttribute.Nature)]
        [TestCase(MonsterAttribute.Nature, MonsterAttribute.Ice)]
        [TestCase(MonsterAttribute.Nature, MonsterAttribute.Water)]
        [TestCase(MonsterAttribute.Ice, MonsterAttribute.Fire)]
        [TestCase(MonsterAttribute.Ice, MonsterAttribute.Wind)]
        [TestCase(MonsterAttribute.Water, MonsterAttribute.Fire)]
        [TestCase(MonsterAttribute.Lightning, MonsterAttribute.Water)]
        [TestCase(MonsterAttribute.Lightning, MonsterAttribute.Ice)]
        [TestCase(MonsterAttribute.Wind, MonsterAttribute.Lightning)]
        [TestCase(MonsterAttribute.Wind, MonsterAttribute.Nature)]
        public void Advantage_ReturnsOnePointFive(MonsterAttribute attack, MonsterAttribute defense)
        {
            Assert.That(
                ElementalMatchup.GetDamageMultiplier(attack, defense),
                Is.EqualTo(ElementalMatchup.WeaknessMultiplier));
            Assert.That(
                ElementalMatchup.GetRelation(attack, defense),
                Is.EqualTo(ElementalDamageRelation.Weakness));
        }

        [Test]
        public void ReverseOfEveryStrongRule_IsResisted()
        {
            foreach (ElementalMatchupRule rule in ElementalMatchup.Rules)
            {
                Assert.That(
                    ElementalMatchup.GetDamageMultiplier(rule.Defense, rule.Attack),
                    Is.EqualTo(ElementalMatchup.ResistanceMultiplier));
                Assert.That(
                    ElementalMatchup.GetRelation(rule.Defense, rule.Attack),
                    Is.EqualTo(ElementalDamageRelation.Resisted));
            }
        }

        [TestCase(MonsterAttribute.None, MonsterAttribute.Fire)]
        [TestCase(MonsterAttribute.Fire, MonsterAttribute.None)]
        public void NeutralMatchup_ReturnsOne(MonsterAttribute attack, MonsterAttribute defense)
        {
            Assert.That(ElementalMatchup.GetDamageMultiplier(attack, defense), Is.EqualTo(1f));
        }

        [TestCase(MonsterAttribute.Fire)]
        [TestCase(MonsterAttribute.Ice)]
        [TestCase(MonsterAttribute.Nature)]
        [TestCase(MonsterAttribute.Lightning)]
        [TestCase(MonsterAttribute.Water)]
        [TestCase(MonsterAttribute.Wind)]
        public void SameAttribute_IsResisted(MonsterAttribute attribute)
        {
            Assert.That(
                ElementalMatchup.GetDamageMultiplier(attribute, attribute),
                Is.EqualTo(ElementalMatchup.SameAttributeMultiplier));
        }
    }
}
#endif
