using CrossDefense.Units;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class CombatInputDragTests
    {
        [Test]
        public void DragThreshold_DistinguishesTapFromDrag()
        {
            Vector2 press = new(100f, 200f);

            Assert.That(
                CombatInputController.HasExceededDragThreshold(
                    press,
                    press + Vector2.right * (CombatInputController.DragStartThreshold - 0.1f)),
                Is.False);
            Assert.That(
                CombatInputController.HasExceededDragThreshold(
                    press,
                    press + Vector2.right * CombatInputController.DragStartThreshold),
                Is.True);
        }

        [Test]
        public void DragWorldPosition_PreservesOriginalGrabOffset()
        {
            Vector3 pointerWorld = new(2f, 3f, 7f);
            Vector3 grabOffset = new(-0.4f, 0.25f, 5f);

            Vector3 result = CombatInputController.GetDragWorldPosition(pointerWorld, grabOffset);

            Assert.That(result, Is.EqualTo(new Vector3(1.6f, 3.25f, 0f)));
        }

        [Test]
        public void PanelInputPosition_FlipsScreenYAxis()
        {
            Vector2 screen = new(320f, 1800f);

            Vector2 result = CombatInputController.GetPanelInputPosition(screen, 2400f);

            Assert.That(result, Is.EqualTo(new Vector2(320f, 600f)));
        }
    }
}
