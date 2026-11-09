using System.Net;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests
{
    public class Part3ApiRegressionTests : IClassFixture<RaceDayFactory>
    {
        private readonly RaceDayFactory _factory;

        public Part3ApiRegressionTests(RaceDayFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Organiser_EventCatalogueContainsOnlyTheirOwnEvents()
        {
            HttpClient firstOrganiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse firstEvent = await TestHelper.CreateEvent(firstOrganiser);

            HttpClient secondOrganiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse secondEvent = await TestHelper.CreateEvent(secondOrganiser);

            HttpResponseMessage response = await firstOrganiser.GetAsync("/api/events");
            response.EnsureSuccessStatusCode();
            List<EventResponse> events = await TestHelper.ReadBody<List<EventResponse>>(response);

            Assert.Contains(events, item => item.EventId == firstEvent.EventId);
            Assert.DoesNotContain(events, item => item.EventId == secondEvent.EventId);
        }

        [Fact]
        public async Task ParticipantCannotUploadAnEventBanner()
        {
            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            using MultipartFormDataContent content = new();
            content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "banner.png");
            HttpResponseMessage response = await participant.PostAsync($"/api/events/{ev.EventId}/banner", content);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task NewEnrolmentStartsPendingAndOrganiserCanConfirmIt()
        {
            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);

            Assert.Equal("Pending", entry.Status);

            HttpResponseMessage response = await TestHelper.PutJson(organiser,
                $"/api/enrolments/{entry.EntryId}/status", new UpdateEnrolmentStatusRequest { Status = "Confirmed" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            EnrolmentResponse updated = await TestHelper.ReadBody<EnrolmentResponse>(response);
            Assert.Equal("Confirmed", updated.Status);
        }

        [Fact]
        public async Task ParticipantCannotChangeEnrolmentStatus()
        {
            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);

            HttpResponseMessage response = await TestHelper.PutJson(participant,
                $"/api/enrolments/{entry.EntryId}/status", new UpdateEnrolmentStatusRequest { Status = "Confirmed" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OrganiserCannotChangeAnotherOrganisersEnrolmentStatus()
        {
            HttpClient owner = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(owner);
            CategoryResponse category = await TestHelper.CreateCategory(owner, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);
            HttpClient otherOrganiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);

            HttpResponseMessage response = await TestHelper.PutJson(otherOrganiser,
                $"/api/enrolments/{entry.EntryId}/status", new UpdateEnrolmentStatusRequest { Status = "Confirmed" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task OrganiserCannotUploadAParticipantProfilePicture()
        {
            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);

            using MultipartFormDataContent content = new();
            content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "profile.png");
            HttpResponseMessage response = await organiser.PostAsync("/api/profile/picture", content);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
