using NUnit.Framework;
using Bralaia;

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
        public void RecruitingInEnemyRegionDoesNotChangeCampaignResources()
        {
            var world = new BralaiaWorld();
            var enemyRegion = world.GetRegion("north");
            int originalArmy = enemyRegion.Army;
            int originalTreasury = world.Treasury;
            int originalManpower = world.Manpower;

            world.Recruit(enemyRegion);

            Assert.That(enemyRegion.Army, Is.EqualTo(originalArmy));
            Assert.That(world.Treasury, Is.EqualTo(originalTreasury));
            Assert.That(world.Manpower, Is.EqualTo(originalManpower));
        }

        [Test]
        public void DevelopingFarmsCostsGoldAndIncreasesFood()
        {
            var world = new BralaiaWorld();
            var region = world.GetRegion("center");
            int oldRegionFood = region.Food;
            int oldTotalFood = world.TotalFood;

            world.DevelopFarms(region);

            Assert.That(region.Development, Is.EqualTo(5));
            Assert.That(region.Food, Is.EqualTo(oldRegionFood + 35));
            Assert.That(world.TotalFood, Is.EqualTo(oldTotalFood + 35));
            Assert.That(world.Treasury, Is.EqualTo(400));
        }

        [Test]
        public void SaveAndLoadRestoresCampaignStateAndWar()
        {
            var world = new BralaiaWorld();
            world.Recruit(world.GetRegion("center"));
            world.DeclareWar(world.GetRegion("north"));
            world.NextTurn();
            string json = world.SaveToJson();

            var restored = new BralaiaWorld();
            Assert.That(restored.LoadFromJson(json), Is.True);
            Assert.That(restored.Turn, Is.EqualTo(world.Turn));
            Assert.That(restored.Treasury, Is.EqualTo(world.Treasury));
            Assert.That(restored.GetRegion("center").Army, Is.EqualTo(world.GetRegion("center").Army));
            Assert.That(restored.IsAtWar("اتحاد السهوب"), Is.True);
        }

        [Test]
        public void InvalidSaveDoesNotChangeCampaign()
        {
            var world = new BralaiaWorld();
            int originalTreasury = world.Treasury;
            Assert.That(world.LoadFromJson("not json"), Is.False);
            Assert.That(world.LoadFromJson("{}"), Is.False);
            Assert.That(world.Treasury, Is.EqualTo(originalTreasury));
        }

        [Test]
        public void AttackRequiresWarAndAdjacentPlayerRegion()
        {
            var world = new BralaiaWorld();
            var target = world.GetRegion("north");
            int originalArmy = target.Army;
            world.Attack(target);
            Assert.That(target.Army, Is.EqualTo(originalArmy));
            Assert.That(world.IsAtWar("اتحاد السهوب"), Is.False);
            world.DeclareWar(target);
            Assert.That(world.IsAtWar("اتحاد السهوب"), Is.True);
            world.Attack(target);
            Assert.That(world.EventLog.Count, Is.GreaterThan(0));
        }

        [Test]
        public void PeaceOfferOutsideWarDoesNotCreateWar()
        {
            var world = new BralaiaWorld();
            world.OfferPeace("ممالك الساحل");
            Assert.That(world.IsAtWar("ممالك الساحل"), Is.False);
        }

        [Test]
        public void StrongPlayerCanEndWarWithPeaceOffer()
        {
            var world = new BralaiaWorld();
            world.DeclareWar(world.GetRegion("north"));
            Assert.That(world.IsAtWar("اتحاد السهوب"), Is.True);

            world.OfferPeace("اتحاد السهوب");

            Assert.That(world.IsAtWar("اتحاد السهوب"), Is.False);
        }

        [Test]
        public void NextTurnAdvancesEconomyAndAi()
        {
            var world = new BralaiaWorld();
            int oldTurn = world.Turn;
            int oldTreasury = world.Treasury;
            world.NextTurn();
            Assert.That(world.Turn, Is.EqualTo(oldTurn + 1));
            Assert.That(world.Treasury, Is.EqualTo(oldTreasury + 78));
        }
    }
}
