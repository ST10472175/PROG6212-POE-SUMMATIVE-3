using System.Net;
using System.Text;
using RaceDay.Api.Models;

namespace RaceDay.Tests
{//start of namespace

    //tests for role based access
    //every Organiser only endpoint must reject Participants,
    //every Participant only endpoint must reject Organisers
    //and a user who is not logged in must be rejected everywhere
    public class RoleAccessTests : IClassFixture<RaceDayFactory>
    {//start of class

        private readonly RaceDayFactory _factory;

        public RoleAccessTests(RaceDayFactory factory)
        {//start of constructor
            _factory = factory;
        }//end of constructor

        [Theory]
        [InlineData("POST", "/api/events", "event")]
        [InlineData("PUT", "/api/events/1", "event")]
        [InlineData("DELETE", "/api/events/1", "none")]
        [InlineData("POST", "/api/events/1/categories", "category")]
        [InlineData("PUT", "/api/categories/1", "category")]
        [InlineData("DELETE", "/api/categories/1", "none")]
        [InlineData("GET", "/api/events/1/enrolments", "none")]
        [InlineData("POST", "/api/events/1/results", "result")]
        [InlineData("GET", "/api/events/1/results", "none")]
        [InlineData("PUT", "/api/results/1", "updateResult")]
        public async Task Participant_IsRejectedFromOrganiserEndpoints(string method, string url, string bodyType)
        {//start of test

            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage response = await participant.SendAsync(MakeRequest(method, url, bodyType));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }//end of test

        [Theory]
        [InlineData("POST", "/api/events/1/enrolments", "enrol")]
        [InlineData("GET", "/api/enrolments/my", "none")]
        [InlineData("GET", "/api/results/my", "none")]
        public async Task Organiser_IsRejectedFromParticipantEndpoints(string method, string url, string bodyType)
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);

            HttpResponseMessage response = await organiser.SendAsync(MakeRequest(method, url, bodyType));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }//end of test

        [Theory]
        [InlineData("GET", "/api/profile", "none")]
        [InlineData("PUT", "/api/profile", "profile")]
        [InlineData("POST", "/api/auth/logout", "none")]
        [InlineData("GET", "/api/events", "none")]
        [InlineData("POST", "/api/events", "event")]
        [InlineData("PUT", "/api/events/1", "event")]
        [InlineData("DELETE", "/api/events/1", "none")]
        [InlineData("GET", "/api/events/1/categories", "none")]
        [InlineData("POST", "/api/events/1/categories", "category")]
        [InlineData("POST", "/api/events/1/enrolments", "enrol")]
        [InlineData("GET", "/api/events/1/enrolments", "none")]
        [InlineData("GET", "/api/enrolments/my", "none")]
        [InlineData("POST", "/api/events/1/results", "result")]
        [InlineData("GET", "/api/events/1/results", "none")]
        [InlineData("GET", "/api/results/my", "none")]
        public async Task LoggedOutUser_IsRejectedEverywhere(string method, string url, string bodyType)
        {//start of test

            HttpClient anonymous = _factory.CreateClient();

            HttpResponseMessage response = await anonymous.SendAsync(MakeRequest(method, url, bodyType));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        }//end of test

        //method to build a request with a valid body, so that the
        //role check is what decides the result and not the data validation
        private HttpRequestMessage MakeRequest(string method, string url, string bodyType)
        {//start of method

            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), url);

            string json = GetJson(bodyType);
            if (json != "")
            {//start of if
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }//end of if

            return request;

        }//end of method

        //method that returns valid JSON for every kind of request body
        private string GetJson(string bodyType)
        {//start of method

            if (bodyType == "event")
            {//start of if
                return "{\"eventName\":\"Test Run\",\"description\":\"Test\",\"eventDate\":\"2030-01-01T08:00:00\",\"location\":\"Soweto\",\"distance\":10,\"eventType\":\"Run\"}";
            }//end of if

            if (bodyType == "category")
            {//start of if
                return "{\"categoryName\":\"10km\"}";
            }//end of if

            if (bodyType == "result")
            {//start of if
                return "{\"participantId\":1,\"finishTime\":\"00:45:10\",\"positionNumber\":1}";
            }//end of if

            if (bodyType == "updateResult")
            {//start of if
                return "{\"finishTime\":\"00:45:10\",\"positionNumber\":1}";
            }//end of if

            if (bodyType == "enrol")
            {//start of if
                return "{\"categoryId\":1}";
            }//end of if

            if (bodyType == "profile")
            {//start of if
                return "{\"name\":\"Test\",\"contactDetails\":\"Test\"}";
            }//end of if

            return "";

        }//end of method

    }//end of class
}//end of namespace
