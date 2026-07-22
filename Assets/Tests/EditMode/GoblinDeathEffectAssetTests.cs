#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class GoblinDeathEffectAssetTests
    {
        const string SheetPath = "Assets/Art/Enemies/effect_goblin_death_sheet.png";

        [Test]
        public void CommonDeathEffect_UsesNineFixedCenterPivotCells()
        {
            TextureImporter importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SheetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(frames.Length, Is.EqualTo(9));
            for (int i = 0; i < frames.Length; i++)
            {
                Assert.That(frames[i].rect.width, Is.EqualTo(128f));
                Assert.That(frames[i].rect.height, Is.EqualTo(128f));
                Assert.That(frames[i].pivot.x, Is.EqualTo(64f).Within(0.01f));
                Assert.That(frames[i].pivot.y, Is.EqualTo(64f).Within(0.01f));
            }
        }
    }
}
#endif
