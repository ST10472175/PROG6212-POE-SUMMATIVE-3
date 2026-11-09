using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

//unit test class for enrollment
namespace RaceDay.Tests
{//start of namespace

    //tests for event enrolments
    public class EnrolmentTests : IClassFixture<RaceDayFactory>
    {//start of class

        private readonly RaceDayFactory _factory;

        public EnrolmentTests(RaceDayFactory factory)
        {//start of constructor
            _factory = factory;
        }//end of constructor

        [Fact]
        public async Task Participant_CanEnrol_AndTheLinkIsRecorded()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "21km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            EnrolRequest request = new EnrolRequest();
            request.CategoryId = category.CategoryId;
            HttpResponseMessage response = await TestHelper.PostJson(participant, "/api/events/" + ev.EventId + "/enrolments", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            EnrolmentResponse enrolment = await TestHelper.ReadBody<EnrolmentResponse>(response);
            Assert.Equal(ev.EventId, enrolment.EventId);
            Assert.Equal(category.CategoryId, enrolment.CategoryId);

            //check the database to see that the participant, event and category are linked
            using (IServiceScope scope = _factory.Services.CreateScope())
            {//start of using
                RaceDayContext db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
                Assignment stored = await db.Assignments.SingleAsync(a => a.EntryId == enrolment.EntryId);

                Assert.Equal(ev.EventId, stored.EventId);
                Assert.Equal(category.CategoryId, stored.CategoryId);
                Assert.Equal(enrolment.ParticipantId, stored.ParticipantId);
            }//end of using

        }//end of test

        [Fact]
        public async Task Participant_CannotEnrolTwiceInTheSameEvent()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);

            EnrolRequest request = new EnrolRequest();
            request.CategoryId = category.CategoryId;
            HttpResponseMessage second = await TestHelper.PostJson(participant, "/api/events/" + ev.EventId + "/enrolments", request);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        }//end of test

        [Fact]
        public async Task Enrol_WithCategoryFromAnotherEvent_ReturnsBadRequest()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse firstEvent = await TestHelper.CreateEvent(organiser);
            EventResponse secondEvent = await TestHelper.CreateEvent(organiser);
            CategoryResponse categoryOfSecondEvent = await TestHelper.CreateCategory(organiser, secondEvent.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            EnrolRequest request = new EnrolRequest();
            request.CategoryId = categoryOfSecondEvent.CategoryId;
            HttpResponseMessage response = await TestHelper.PostJson(participant, "/api/events/" + firstEvent.EventId + "/enrolments", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Enrol_InEventThatDoesNotExist_ReturnsNotFound()
        {//start of test

            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            EnrolRequest request = new EnrolRequest();
            request.CategoryId = 1;
            HttpResponseMessage response = await TestHelper.PostJson(participant, "/api/events/999999/enrolments", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CanViewAllEnrolmentsOfTheirEvent()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient first = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            HttpClient second = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse firstEntry = await TestHelper.Enrol(first, ev.EventId, category.CategoryId);
            EnrolmentResponse secondEntry = await TestHelper.Enrol(second, ev.EventId, category.CategoryId);

            HttpResponseMessage response = await organiser.GetAsync("/api/events/" + ev.EventId + "/enrolments");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<EnrolmentResponse> entries = await TestHelper.ReadBody<List<EnrolmentResponse>>(response);
            Assert.Equal(2, entries.Count);
            Assert.Contains(entries, e => e.EntryId == firstEntry.EntryId);
            Assert.Contains(entries, e => e.EntryId == secondEntry.EntryId);

        }//end of test

        [Fact]
        public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent()
        {//start of test

            HttpClient owner = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(owner);
            HttpClient otherOrganiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);

            HttpResponseMessage response = await otherOrganiser.GetAsync("/api/events/" + ev.EventId + "/enrolments");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Participant_SeesOnlyTheirOwnEnrolments()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient me = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            HttpClient someoneElse = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse mine = await TestHelper.Enrol(me, ev.EventId, category.CategoryId);
            await TestHelper.Enrol(someoneElse, ev.EventId, category.CategoryId);

            HttpResponseMessage response = await me.GetAsync("/api/enrolments/my");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<EnrolmentResponse> entries = await TestHelper.ReadBody<List<EnrolmentResponse>>(response);
            Assert.Single(entries);
            Assert.Equal(mine.EntryId, entries[0].EntryId);

        }//end of test

    }//end of class
}//end of namespace
