using System.Net;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Tests
{//start of namespace

    //tests for events and categories
    public class EventTests : IClassFixture<RaceDayFactory>
    {//start of class

        private readonly RaceDayFactory _factory;

        public EventTests(RaceDayFactory factory)
        {//start of constructor
            _factory = factory;
        }//end of constructor

        [Fact]
        public async Task Organiser_CanCreateEvent()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventRequest request = TestHelper.NewEventRequest("Cape Town Cycle Tour");
            request.EventType = EventType.Cycle;
            request.Distance = 109m;

            HttpResponseMessage response = await TestHelper.PostJson(organiser, "/api/events", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            EventResponse created = await TestHelper.ReadBody<EventResponse>(response);
            Assert.True(created.EventId > 0);
            Assert.Equal("Cape Town Cycle Tour", created.EventName);
            Assert.Equal(EventType.Cycle, created.EventType);
            Assert.Equal(109m, created.Distance);

        }//end of test

        [Fact]
        public async Task Organiser_CreatingEventWithoutName_ReturnsBadRequest()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventRequest request = TestHelper.NewEventRequest("");

            HttpResponseMessage response = await TestHelper.PostJson(organiser, "/api/events", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CreatingEventWithInvalidDistance_ReturnsBadRequest()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventRequest request = TestHelper.NewEventRequest("Bad Distance Event");
            request.Distance = 0m;

            HttpResponseMessage response = await TestHelper.PostJson(organiser, "/api/events", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Participant_CannotCreateEvent()
        {//start of test

            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage response = await TestHelper.PostJson(participant, "/api/events", TestHelper.NewEventRequest("Not Allowed"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }//end of test

        [Fact]
        public async Task LoggedOutUser_CannotCreateEvent()
        {//start of test

            HttpClient anonymous = _factory.CreateClient();

            HttpResponseMessage response = await TestHelper.PostJson(anonymous, "/api/events", TestHelper.NewEventRequest("Not Allowed"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        }//end of test

        [Theory]
        [InlineData("Organiser")]
        [InlineData("Participant")]
        public async Task BothRoles_CanViewEvents(string role)
        {//start of test

            // Create the event as its owner.
            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse created = await TestHelper.CreateEvent(organiser);

            // Organisers can view only events they own, so use the owner account
            // for the organiser case. Participants can view the public catalogue
            // and therefore use a separate participant account.
            HttpClient viewer = role == Roles.Organiser
                ? organiser
                : await TestHelper.LoginAsNewUser(_factory, role);

            HttpResponseMessage list = await viewer.GetAsync("/api/events");
            HttpResponseMessage single = await viewer.GetAsync("/api/events/" + created.EventId);

            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            List<EventResponse> events = await TestHelper.ReadBody<List<EventResponse>>(list);
            Assert.Contains(events, e => e.EventId == created.EventId);

            Assert.Equal(HttpStatusCode.OK, single.StatusCode);
            EventResponse found = await TestHelper.ReadBody<EventResponse>(single);
            Assert.Equal(created.EventName, found.EventName);

        }//end of test

        [Fact]
        public async Task GetEvent_ThatDoesNotExist_ReturnsNotFound()
        {//start of test

            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage response = await participant.GetAsync("/api/events/999999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CanUpdateOwnEvent()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse created = await TestHelper.CreateEvent(organiser);
            EventRequest changes = TestHelper.NewEventRequest("Renamed Event");
            changes.Location = "Pretoria";
            changes.EventType = EventType.Walk;

            HttpResponseMessage response = await TestHelper.PutJson(organiser, "/api/events/" + created.EventId, changes);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            EventResponse updated = await TestHelper.ReadBody<EventResponse>(response);
            Assert.Equal("Renamed Event", updated.EventName);
            Assert.Equal("Pretoria", updated.Location);
            Assert.Equal(EventType.Walk, updated.EventType);

        }//end of test

        [Fact]
        public async Task Organiser_CannotUpdateOrDeleteAnotherOrganisersEvent()
        {//start of test

            HttpClient owner = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse created = await TestHelper.CreateEvent(owner);
            HttpClient otherOrganiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);

            HttpResponseMessage update = await TestHelper.PutJson(otherOrganiser, "/api/events/" + created.EventId, TestHelper.NewEventRequest("Hijacked"));
            HttpResponseMessage delete = await otherOrganiser.DeleteAsync("/api/events/" + created.EventId);

            Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        }//end of test

        [Fact]
        public async Task Participant_CannotUpdateOrDeleteEvents()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse created = await TestHelper.CreateEvent(organiser);
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage update = await TestHelper.PutJson(participant, "/api/events/" + created.EventId, TestHelper.NewEventRequest("Changed"));
            HttpResponseMessage delete = await participant.DeleteAsync("/api/events/" + created.EventId);

            Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CanDeleteOwnEvent()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse created = await TestHelper.CreateEvent(organiser);

            HttpResponseMessage delete = await organiser.DeleteAsync("/api/events/" + created.EventId);
            HttpResponseMessage afterwards = await organiser.GetAsync("/api/events/" + created.EventId);

            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);

        }//end of test

        [Fact]
        public async Task Categories_OrganiserCanDefine_BothRolesCanView()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse created = await TestHelper.CreateCategory(organiser, ev.EventId, "Under 20");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage forParticipant = await participant.GetAsync("/api/events/" + ev.EventId + "/categories");
            HttpResponseMessage forOrganiser = await organiser.GetAsync("/api/events/" + ev.EventId + "/categories");

            Assert.Equal(HttpStatusCode.OK, forParticipant.StatusCode);
            List<CategoryResponse> participantList = await TestHelper.ReadBody<List<CategoryResponse>>(forParticipant);
            Assert.Contains(participantList, c => c.CategoryId == created.CategoryId);

            List<CategoryResponse> organiserList = await TestHelper.ReadBody<List<CategoryResponse>>(forOrganiser);
            Assert.Contains(organiserList, c => c.CategoryName == "Under 20");

        }//end of test

        [Fact]
        public async Task Categories_DuplicateNameInSameEvent_ReturnsConflict()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            await TestHelper.CreateCategory(organiser, ev.EventId, "Senior");

            CategoryRequest duplicate = new CategoryRequest();
            duplicate.CategoryName = "Senior";
            HttpResponseMessage response = await TestHelper.PostJson(organiser, "/api/events/" + ev.EventId + "/categories", duplicate);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Categories_OrganiserCanRenameAndDelete()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse created = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");

            CategoryRequest rename = new CategoryRequest();
            rename.CategoryName = "21km";
            HttpResponseMessage renameResponse = await TestHelper.PutJson(organiser, "/api/categories/" + created.CategoryId, rename);
            HttpResponseMessage deleteResponse = await organiser.DeleteAsync("/api/categories/" + created.CategoryId);

            Assert.Equal(HttpStatusCode.OK, renameResponse.StatusCode);
            CategoryResponse renamed = await TestHelper.ReadBody<CategoryResponse>(renameResponse);
            Assert.Equal("21km", renamed.CategoryName);
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        }//end of test

        [Fact]
        public async Task Categories_ParticipantCannotCreate()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            CategoryRequest request = new CategoryRequest();
            request.CategoryName = "Sneaky";
            HttpResponseMessage response = await TestHelper.PostJson(participant, "/api/events/" + ev.EventId + "/categories", request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }//end of test

    }//end of class
}//end of namespace
