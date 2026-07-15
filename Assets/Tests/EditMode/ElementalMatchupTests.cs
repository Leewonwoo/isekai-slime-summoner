using CrossDefense.Core;
using CrossDefense.Data;
using NUnit.Framework;

namespace CrossDefense.Tests.EditMode
{
    public sealed class ElementalMatchupTests
    {
        [TestCase(MonsterAttribute.Fire, MonsterAttribute.Nature)]
        [TestCase(MonsterAttribute.Nature, MonsterAttribute.Ice)]
        [TestCase(MonsterAttribute.Ice, MonsterAttribute.Fire)]
        public void Advantage_ReturnsOnePointFive(MonsterAttribute attack, MonsterAttribute defense)
        {
            Assert.That(ElementalMatchup.GetDamageMultiplier(attack, defense), Is.EqualTo(1.5f));
        }

        [TestCase(MonsterAttribute.Nature, MonsterAttribute.Fire)]
        [TestCase(MonsterAttribute.Ice, MonsterAttribute.Nature)]
        [TestCase(MonsterAttribute.Fire, MonsterAttribute.Ice)]
        public void Disadvantage_ReturnsZeroPointSevenFive(MonsterAttribute attack, MonsterAttribute defense)
        {
            Assert.That(ElementalMatchup.GetDamageMultiplier(attack, defense), Is.EqualTo(0.75f));
        }

        [TestCase(MonsterAttribute.None, MonsterAttribute.Fire)]
        [TestCase(MonsterAttribute.Fire, MonsterAttribute.None)]
        [TestCase(MonsterAttribute.Ice, MonsterAttribute.Ice)]
        public void NeutralMatchup_ReturnsOne(MonsterAttribute attack, MonsterAttribute defense)
        {
            Assert.That(ElementalMatchup.GetDamageMultiplier(attack, defense), Is.EqualTo(1f));
        }
    }
}
