using System.Collections.Generic;
using CrossDefense.Units;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class CombatPbdSolverTests
    {
        [Test]
        public void SameTeamOverlap_IsResolvedToConfiguredSpacing()
        {
            var bodies = new List<CombatPbdBody>
            {
                new(Vector2.zero, 0.5f, 1f, CombatPbdTeam.Monster),
                new(new Vector2(0.2f, 0f), 0.5f, 1f, CombatPbdTeam.Monster),
            };

            CombatPbdSolver.Solve(bodies, 2, 1f, 1f, 0.9f, 0.65f);

            Assert.That(Vector2.Distance(bodies[0].Position, bodies[1].Position),
                Is.EqualTo(0.9f).Within(0.001f));
        }

        [Test]
        public void InverseMass_MovesLighterBodyFarther()
        {
            var bodies = new List<CombatPbdBody>
            {
                new(Vector2.zero, 0.5f, 0.5f, CombatPbdTeam.Monster),
                new(new Vector2(0.2f, 0f), 0.5f, 1f, CombatPbdTeam.Monster),
            };

            CombatPbdSolver.Solve(bodies, 1, 1f, 1f, 1f, 1f);

            float heavyMovement = Vector2.Distance(Vector2.zero, bodies[0].Position);
            float lightMovement = Vector2.Distance(new Vector2(0.2f, 0f), bodies[1].Position);
            Assert.That(lightMovement, Is.EqualTo(heavyMovement * 2f).Within(0.001f));
        }

        [Test]
        public void OpposingTeams_DoNotSeparateBeyondMeleeContactRange()
        {
            var bodies = new List<CombatPbdBody>
            {
                new(Vector2.zero, 0.7f, 1f, CombatPbdTeam.Monster, 0.55f),
                new(new Vector2(0.1f, 0f), 0.5f, 1f, CombatPbdTeam.SummonedUnit, 0.85f),
            };

            CombatPbdSolver.Solve(bodies, 2, 1f, 1f, 1f, 0.9f);

            float distance = Vector2.Distance(bodies[0].Position, bodies[1].Position);
            Assert.That(distance, Is.EqualTo(0.55f * 0.95f).Within(0.001f));
            Assert.That(distance, Is.LessThan(0.55f));
        }

        [Test]
        public void CoincidentBodies_UseFiniteDeterministicFallback()
        {
            var bodies = new List<CombatPbdBody>
            {
                new(Vector2.zero, 0.5f, 1f, CombatPbdTeam.SummonedUnit),
                new(Vector2.zero, 0.5f, 1f, CombatPbdTeam.SummonedUnit),
            };

            CombatPbdSolver.Solve(bodies, 1, 1f, 1f, 1f, 1f);

            Assert.That(float.IsNaN(bodies[0].Position.x), Is.False);
            Assert.That(float.IsNaN(bodies[1].Position.y), Is.False);
            Assert.That(Vector2.Distance(bodies[0].Position, bodies[1].Position),
                Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ShallowOverlap_InsideSlopIsNotPushedApart()
        {
            var bodies = new List<CombatPbdBody>
            {
                new(Vector2.zero, 0.5f, 1f, CombatPbdTeam.SummonedUnit),
                new(new Vector2(0.96f, 0f), 0.5f, 1f, CombatPbdTeam.SummonedUnit),
            };

            CombatPbdSolver.Solve(bodies, 1, 1f, 1f, 1f, 1f, 0.05f, 1f);

            Assert.That(bodies[0].Position, Is.EqualTo(Vector2.zero));
            Assert.That(bodies[1].Position, Is.EqualTo(new Vector2(0.96f, 0f)));
        }

        [Test]
        public void DeepOverlap_CorrectionPerBodyIsCapped()
        {
            var bodies = new List<CombatPbdBody>
            {
                new(Vector2.zero, 0.5f, 1f, CombatPbdTeam.SummonedUnit),
                new(Vector2.zero, 0.5f, 1f, CombatPbdTeam.SummonedUnit),
            };

            CombatPbdSolver.Solve(bodies, 4, 1f, 1f, 1f, 1f, 0f, 0.02f);

            Assert.That(Vector2.Distance(Vector2.zero, bodies[0].Position),
                Is.LessThanOrEqualTo(0.0201f));
            Assert.That(Vector2.Distance(Vector2.zero, bodies[1].Position),
                Is.LessThanOrEqualTo(0.0201f));
        }
    }
}
