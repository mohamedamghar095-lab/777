using NUnit.Framework;
using UnityEngine;

namespace Bralaia.Tests
{
    public sealed class BralaiaWorldTests
    {
        [Test]
        public void NewWorldHasPlayableStartingState()
        {
            var world = new BralaiaWorld();
            Assert.That(world.Regions.Count, Is.EqualTo(9));
            Assert.That(world.CountOwned(BralaiaWorld.PlayerFaction), Is.EqualTo(4));
            Assert.That(world.Treasury, Is.EqualTo(500));
            Assert.That(world.GetRegion("center"), Is.Not.Null);
        }

        [Test]
        public void RecruitingConsumesResourcesAndAddsArmy()
        {
            var world = new BralaiaWorld();
            var region = world.GetRegion("center");
            int oldArmy = region.Army;
            world.Recruit(region);
            Assert.That(region.Army, Is.EqualTo(oldArmy + 40));
            Assert.That(world.Treasury, Is.EqualTo(420));
            Assert.That(world.Manpower, Is.EqualTo(310));
        }

        [Test]
        public void SaveAndLoadRestoresCampaignState()
        {
            var world = new BralaiaWorld();
            world.Recruit(world.GetRegion("center"));
            world.NextTurn();
            string json = world.SaveToJson();

            var restored = new BralaiaWorld();
            Assert.That(restored.LoadFromJson(json), Is.True);
            Assert.That(restored.Turn, Is.EqualTo(world.Turn));
            Assert.That(restored.Treasury, Is.EqualTo(world.Treasury));
            Assert.That(restored.GetRegion("center").Army, Is.EqualTo(world.GetRegion("center").Army));
        }

        [Test]
        public void AttackRequiresWarAndAdjacentPlayerRegion()
        {
            var world = new BralaiaWorld();
            var target = world.GetRegion("north");
            int targetOwnerBefore = world.CountOwned("اتحاد السهوب");
            world.Attack(target);
            Assert.That(world.CountOwned("اتحاد السهوب"), Is.EqualTo(targetOwnerBefore));
            world.DeclareWar(target);
            world.Attack(target);
            Assert.That(world.EventLog.Count, Is.GreaterThan(0));
        }
    }
}
