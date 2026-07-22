#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class SummonerSkillEffectAssetTests
    {
        [TestCase(
            "Assets/Art/Projectiles/effect_fire_explosion_sheet.png",
            64f)]
        [TestCase(
            "Assets/Art/Projectiles/effect_ice_wall_sheet.png",
            12.8f)]
        [TestCase(
            "Assets/Art/Projectiles/effect_explosion_slime_star3_eruption_sheet.png",
            12.8f)]
        public void SkillEffectSheet_UsesNineFixedCellsAndApprovedPivot(
            string path,
            float expectedPivotY)
        {
            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();

            Assert.That(frames.Length, Is.EqualTo(9));
            for (int i = 0; i < frames.Length; i++)
            {
                Assert.That(frames[i].rect.width, Is.EqualTo(128f));
                Assert.That(frames[i].rect.height, Is.EqualTo(128f));
                Assert.That(frames[i].pivot.x, Is.EqualTo(64f).Within(0.01f));
                Assert.That(frames[i].pivot.y, Is.EqualTo(expectedPivotY).Within(0.01f));
                Assert.That(frames[i].texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
        }

        [TestCase("Assets/Art/UIIcons/icon_skill_meteor.png")]
        [TestCase("Assets/Art/UIIcons/icon_skill_ice_wall.png")]
        [TestCase("Assets/Art/UIIcons/icon_skill_aegis.png")]
        public void SkillIcon_UsesTransparent128PixelPointSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            Assert.That(importer, Is.Not.Null);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.EqualTo(128));
            Assert.That(sprite.texture.height, Is.EqualTo(128));
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.alphaIsTransparency, Is.True);
        }

        [Test]
        public void MeteorProjectile_UsesTransparent128PixelPointSprite()
        {
            const string path =
                "Assets/Art/Projectiles/projectile_summoner_meteor.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            Assert.That(importer, Is.Not.Null);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.EqualTo(128));
            Assert.That(sprite.texture.height, Is.EqualTo(128));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
        }
    }
}
#endif
