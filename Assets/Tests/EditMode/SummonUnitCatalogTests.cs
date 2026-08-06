#if UNITY_EDITOR
using System.Collections.Generic;
using CrossDefense.Data;
using NUnit.Framework;
using UnityEditor;

namespace CrossDefense.Tests.EditMode
{
    public sealed class SummonUnitCatalogTests
    {
        const string CatalogPath = "Assets/Data/SummonUnitCatalog_Default.asset";

        [Test]
        public void DefaultCatalog_ContainsEightValidUniqueSlimes()
        {
            SummonUnitCatalog catalog =
                AssetDatabase.LoadAssetAtPath<SummonUnitCatalog>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Units, Has.Count.EqualTo(8));
            Assert.That(catalog.Validate(out string error), Is.True, error);

            var ids = new HashSet<string>();
            foreach (SummonUnitData unit in catalog.Units)
            {
                Assert.That(unit, Is.Not.Null);
                Assert.That(ids.Add(unit.UnitId), Is.True, unit.UnitId);
                Assert.That(unit.WorldSpriteAtRank(0), Is.Not.Null, unit.UnitId);
                Assert.That(unit.WorldSpriteAtRank(1), Is.Not.Null, unit.UnitId);
                Assert.That(unit.WorldSpriteAtRank(2), Is.Not.Null, unit.UnitId);
                Assert.That(unit.UnlockLevel, Is.GreaterThanOrEqualTo(1), unit.UnitId);
            }
        }

        [Test]
        public void DefaultCatalog_UsesExpectedUnlockOrder()
        {
            SummonUnitCatalog catalog =
                AssetDatabase.LoadAssetAtPath<SummonUnitCatalog>(CatalogPath);

            Assert.That(catalog.Find("punch-slime").UnlockLevel, Is.EqualTo(1));
            Assert.That(catalog.Find("watergun-slime").UnlockLevel, Is.EqualTo(2));
            Assert.That(catalog.Find("flame-slime").UnlockLevel, Is.EqualTo(6));
            Assert.That(catalog.Find("ice-slime").UnlockLevel, Is.EqualTo(8));
            Assert.That(catalog.Find("green-slime").UnlockLevel, Is.EqualTo(12));
            Assert.That(catalog.Find("buff-slime").UnlockLevel, Is.EqualTo(16));
            Assert.That(catalog.Find("explosion-slime").UnlockLevel, Is.EqualTo(20));
            Assert.That(catalog.Find("freeze-slime").UnlockLevel, Is.EqualTo(24));
        }
    }
}
#endif
