using NUnit.Framework;
using Bralaia;

namespace Bralaia.Tests
{
    public sealed class BralaiaCourtTests
    {
        [Test]
        public void NewCourtHasThreeDistinctAdvisors()
        {
            var court = new BralaiaCourt();
            Assert.That(court.Advisors.Count, Is.EqualTo(3));
            Assert.That(court.GetAdvisor("vizier"), Is.Not.Null);
            Assert.That(court.GetAdvisor("marshal"), Is.Not.Null);
            Assert.That(court.GetAdvisor("steward"), Is.Not.Null);
        }

        [Test]
        public void AudienceImprovesRelationshipAndLoyaltyWithinBounds()
        {
            var court = new BralaiaCourt();
            var advisor = court.GetAdvisor("vizier");
            int oldRelationship = advisor.Relationship;
            int oldLoyalty = advisor.Loyalty;

            Assert.That(court.HoldAudience("vizier"), Is.True);

            Assert.That(advisor.Relationship, Is.EqualTo(oldRelationship + 8));
            Assert.That(advisor.Loyalty, Is.EqualTo(oldLoyalty + 2));
            Assert.That(court.HoldAudience("unknown"), Is.False);
        }

        [Test]
        public void ProjectsAreSpecificToKnownAdvisorRoles()
        {
            var court = new BralaiaCourt();
            var marshal = court.GetAdvisor("marshal");
            int oldInfluence = marshal.Influence;

            Assert.That(court.AssignProject("marshal"), Is.True);
            Assert.That(marshal.Assignment, Is.EqualTo("تدريب حرس الحدود"));
            Assert.That(marshal.Influence, Is.EqualTo(oldInfluence + 3));
            Assert.That(court.AssignProject("unknown"), Is.False);
        }

        [Test]
        public void CampaignTurnCannotAdvanceTwiceOrMoveBackwards()
        {
            var court = new BralaiaCourt();
            Assert.That(court.AdvanceTurn(2), Is.True);
            Assert.That(court.AdvanceTurn(2), Is.False);
            Assert.That(court.AdvanceTurn(1), Is.False);
            Assert.That(court.CampaignTurn, Is.EqualTo(2));
        }

        [Test]
        public void CouncilEventsResolveDeterministicallyOnThirdTurn()
        {
            var first = new BralaiaCourt();
            var second = new BralaiaCourt();
            int oldRelationship = first.GetAdvisor("vizier").Relationship;

            Assert.That(first.AdvanceTurn(3), Is.True);
            Assert.That(second.AdvanceTurn(3), Is.True);

            Assert.That(first.GetAdvisor("vizier").Relationship, Is.EqualTo(oldRelationship + 4));
            Assert.That(first.EventLog[0], Is.EqualTo(second.EventLog[0]));
        }

        [Test]
        public void SaveAndLoadRestoresCourtRelationshipsAssignmentsAndTurn()
        {
            var court = new BralaiaCourt();
            court.HoldAudience("vizier");
            court.AssignProject("marshal");
            court.AdvanceTurn(3);

            var restored = new BralaiaCourt();
            Assert.That(restored.LoadFromJson(court.SaveToJson()), Is.True);
            Assert.That(restored.CampaignTurn, Is.EqualTo(3));
            Assert.That(restored.GetAdvisor("vizier").Relationship, Is.EqualTo(court.GetAdvisor("vizier").Relationship));
            Assert.That(restored.GetAdvisor("marshal").Assignment, Is.EqualTo(court.GetAdvisor("marshal").Assignment));
            Assert.That(restored.EventLog[0], Is.EqualTo(court.EventLog[0]));
        }

        [Test]
        public void InvalidCourtSaveDoesNotMutateExistingState()
        {
            var court = new BralaiaCourt();
            int originalRelationship = court.GetAdvisor("vizier").Relationship;

            Assert.That(court.LoadFromJson("not json"), Is.False);
            Assert.That(court.LoadFromJson("{}"), Is.False);

            Assert.That(court.GetAdvisor("vizier").Relationship, Is.EqualTo(originalRelationship));
            Assert.That(court.CampaignTurn, Is.EqualTo(1));
        }
    }
}
