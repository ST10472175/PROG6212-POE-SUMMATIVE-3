using System.Net;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

//results class test  that is for capturing and viewing results
namespace RaceDay.Tests
{//start of namespace

    //tests for capturing and viewing results
    public class ResultsTests : IClassFixture<RaceDayFactory>
    {//start of class

        private readonly RaceDayFactory _factory;

        public ResultsTests(RaceDayFactory factory)
        {//start of constructor
            _factory = factory;
        }//end of constructor

        [Fact]
        public async Task Organiser_CanCaptureResultForEnrolledParticipant()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);

            ResultRequest request = TestHelper.NewResultRequest(entry.ParticipantId, 1, "00:45:10");
            HttpResponseMessage response = await TestHelper.PostJson(organiser, "/api/events/" + ev.EventId + "/results", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            ResultResponse result = await TestHelper.ReadBody<ResultResponse>(response);
            Assert.Equal(entry.ParticipantId, result.ParticipantId);
            Assert.Equal(1, result.PositionNumber);
            Assert.Equal("00:45:10", result.FinishTime);
            Assert.Equal("10km", result.CategoryName);
            Assert.Equal(ev.EventDate, result.EventDate);
            Assert.Equal(1, result.TotalParticipants);

        }//end of test

        [Fact]
        public async Task Organiser_CannotCaptureResultForParticipantWhoIsNotEnrolled()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            EventResponse otherEvent = await TestHelper.CreateEvent(organiser);
            CategoryResponse otherCategory = await TestHelper.CreateCategory(organiser, otherEvent.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            //the participant is only enrolled in the other event
            EnrolmentResponse entryInOtherEvent = await TestHelper.Enrol(participant, otherEvent.EventId, otherCategory.CategoryId);

            ResultRequest request = TestHelper.NewResultRequest(entryInOtherEvent.ParticipantId, 1, "00:45:10");
            HttpResponseMessage response = await TestHelper.PostJson(organiser, "/api/events/" + ev.EventId + "/results", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CannotCaptureTheSameParticipantsResultTwice()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);

            string url = "/api/events/" + ev.EventId + "/results";
            HttpResponseMessage first = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(entry.ParticipantId, 1, "00:45:10"));
            first.EnsureSuccessStatusCode();

            HttpResponseMessage second = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(entry.ParticipantId, 2, "00:50:00"));

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CannotGiveTwoParticipantsTheSamePosition()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient first = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            HttpClient second = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse firstEntry = await TestHelper.Enrol(first, ev.EventId, category.CategoryId);
            EnrolmentResponse secondEntry = await TestHelper.Enrol(second, ev.EventId, category.CategoryId);

            string url = "/api/events/" + ev.EventId + "/results";
            HttpResponseMessage firstResult = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(firstEntry.ParticipantId, 1, "00:45:10"));
            firstResult.EnsureSuccessStatusCode();

            HttpResponseMessage response = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(secondEntry.ParticipantId, 1, "00:46:00"));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        }//end of test

        [Theory]
        [InlineData(0, "00:45:10")]
        [InlineData(1, "00:00:00")]
        [InlineData(1, "not a time")]
        public async Task CaptureResult_WithInvalidPositionOrTime_ReturnsBadRequest(int position, string time)
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);

            ResultRequest request = TestHelper.NewResultRequest(entry.ParticipantId, position, time);
            HttpResponseMessage response = await TestHelper.PostJson(organiser, "/api/events/" + ev.EventId + "/results", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CannotCaptureResultsForAnotherOrganisersEvent()
        {//start of test

            HttpClient owner = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(owner);
            CategoryResponse category = await TestHelper.CreateCategory(owner, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);
            HttpClient otherOrganiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);

            ResultRequest request = TestHelper.NewResultRequest(entry.ParticipantId, 1, "00:45:10");
            HttpResponseMessage response = await TestHelper.PostJson(otherOrganiser, "/api/events/" + ev.EventId + "/results", request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Organiser_CanViewAllResultsOfTheirEvent_InPositionOrder()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient first = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            HttpClient second = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse firstEntry = await TestHelper.Enrol(first, ev.EventId, category.CategoryId);
            EnrolmentResponse secondEntry = await TestHelper.Enrol(second, ev.EventId, category.CategoryId);

            //capture the second place first to prove the list is sorted by position
            string url = "/api/events/" + ev.EventId + "/results";
            HttpResponseMessage secondPlace = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(secondEntry.ParticipantId, 2, "00:50:00"));
            HttpResponseMessage firstPlace = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(firstEntry.ParticipantId, 1, "00:45:00"));
            secondPlace.EnsureSuccessStatusCode();
            firstPlace.EnsureSuccessStatusCode();

            HttpResponseMessage response = await organiser.GetAsync(url);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<ResultResponse> results = await TestHelper.ReadBody<List<ResultResponse>>(response);
            Assert.Equal(2, results.Count);
            Assert.Equal(1, results[0].PositionNumber);
            Assert.Equal(2, results[1].PositionNumber);

        }//end of test

        [Fact]
        public async Task Participant_SeesOnlyTheirOwnResults()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient me = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            HttpClient someoneElse = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse myEntry = await TestHelper.Enrol(me, ev.EventId, category.CategoryId);
            EnrolmentResponse otherEntry = await TestHelper.Enrol(someoneElse, ev.EventId, category.CategoryId);

            string url = "/api/events/" + ev.EventId + "/results";
            HttpResponseMessage mine = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(myEntry.ParticipantId, 1, "00:40:00"));
            HttpResponseMessage theirs = await TestHelper.PostJson(organiser, url, TestHelper.NewResultRequest(otherEntry.ParticipantId, 2, "00:55:00"));
            mine.EnsureSuccessStatusCode();
            theirs.EnsureSuccessStatusCode();

            HttpResponseMessage response = await me.GetAsync("/api/results/my");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            List<ResultResponse> results = await TestHelper.ReadBody<List<ResultResponse>>(response);
            Assert.Single(results);
            Assert.Equal(myEntry.ParticipantId, results[0].ParticipantId);
            Assert.Equal(1, results[0].PositionNumber);

        }//end of test

        [Fact]
        public async Task Organiser_CanCorrectACapturedResult()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            EventResponse ev = await TestHelper.CreateEvent(organiser);
            CategoryResponse category = await TestHelper.CreateCategory(organiser, ev.EventId, "10km");
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);
            EnrolmentResponse entry = await TestHelper.Enrol(participant, ev.EventId, category.CategoryId);

            HttpResponseMessage createResponse = await TestHelper.PostJson(organiser, "/api/events/" + ev.EventId + "/results", TestHelper.NewResultRequest(entry.ParticipantId, 3, "01:00:00"));
            ResultResponse created = await TestHelper.ReadBody<ResultResponse>(createResponse);

            UpdateResultRequest correction = new UpdateResultRequest();
            correction.FinishTime = "00:58:30";
            correction.PositionNumber = 2;
            HttpResponseMessage response = await TestHelper.PutJson(organiser, "/api/results/" + created.ResultsId, correction);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ResultResponse updated = await TestHelper.ReadBody<ResultResponse>(response);
            Assert.Equal(2, updated.PositionNumber);
            Assert.Equal("00:58:30", updated.FinishTime);

        }//end of test

    }//end of class
}//end of namespace
