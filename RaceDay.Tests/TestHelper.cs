using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

//test class to verify if password hashing is successfu;
namespace RaceDay.Tests
{//start of namespace

    //small helper methods so that the test methods stay short and easy to read
    public static class TestHelper
    {//start of class

        //organiser code used by the tests (set in RaceDayFactory)
        public const string OrganiserCode = "TEST-ORGANISER-CODE";

        //password used for every test user
        public const string Password = "Passw0rd!23";

        //JSON settings that match the API (camelCase and enums as text)
        public static JsonSerializerOptions JsonOptions = MakeJsonOptions();

        private static JsonSerializerOptions MakeJsonOptions()
        {//start of method

            JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;

        }//end of method

        //method to make an email that has never been used
        public static string NewEmail()
        {//start of method
            return Guid.NewGuid().ToString("N") + "@raceday.test";
        }//end of method

        //method to make registration data for a new user
        public static RegisterRequest NewRegistration(string role)
        {//start of method

            RegisterRequest request = new RegisterRequest();
            request.Email = NewEmail();
            request.Password = Password;
            request.Name = "Test " + role;
            request.ContactDetails = "082 123 4567";
            request.Role = role;

            if (role == Roles.Organiser)
            {//start of if
                request.OrganiserCode = OrganiserCode;
            }//end of if

            return request;

        }//end of method

        //method to make event data
        public static EventRequest NewEventRequest(string name)
        {//start of method

            EventRequest request = new EventRequest();
            request.EventName = name;
            request.Description = "Annual community race";
            request.EventDate = DateTime.UtcNow.AddDays(30);
            request.Location = "Soweto";
            request.Distance = 10m;
            request.EventType = EventType.Run;

            return request;

        }//end of method

        //method to send a POST request with a JSON body
        public static async Task<HttpResponseMessage> PostJson<T>(HttpClient client, string url, T body)
        {//start of method
            return await client.PostAsJsonAsync(url, body, JsonOptions);
        }//end of method

        //method to send a PUT request with a JSON body
        public static async Task<HttpResponseMessage> PutJson<T>(HttpClient client, string url, T body)
        {//start of method
            return await client.PutAsJsonAsync(url, body, JsonOptions);
        }//end of method

        //method to read the JSON body of a response
        public static async Task<T> ReadBody<T>(HttpResponseMessage response)
        {//start of method

            T value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return value;

        }//end of method

        //method to register a new user and log them in
        //the client keeps the session cookie, so it stays logged in
        public static async Task<HttpClient> LoginAsNewUser(RaceDayFactory factory, string role)
        {//start of method

            HttpClient client = factory.CreateClient();
            RegisterRequest registration = NewRegistration(role);

            HttpResponseMessage registerResponse = await PostJson(client, "/api/auth/register", registration);
            registerResponse.EnsureSuccessStatusCode();

            LoginRequest login = new LoginRequest();
            login.Email = registration.Email;
            login.Password = registration.Password;

            HttpResponseMessage loginResponse = await PostJson(client, "/api/auth/login", login);
            loginResponse.EnsureSuccessStatusCode();

            return client;

        }//end of method

        //method for an organiser to create an event
        public static async Task<EventResponse> CreateEvent(HttpClient organiser)
        {//start of method

            string name = "Soweto Fun Run " + Guid.NewGuid().ToString("N").Substring(0, 6);
            HttpResponseMessage response = await PostJson(organiser, "/api/events", NewEventRequest(name));
            response.EnsureSuccessStatusCode();

            return await ReadBody<EventResponse>(response);

        }//end of method

        //method for an organiser to create a category
        public static async Task<CategoryResponse> CreateCategory(HttpClient organiser, int eventId, string name)
        {//start of method

            CategoryRequest request = new CategoryRequest();
            request.CategoryName = name;

            HttpResponseMessage response = await PostJson(organiser, "/api/events/" + eventId + "/categories", request);
            response.EnsureSuccessStatusCode();

            return await ReadBody<CategoryResponse>(response);

        }//end of method

        //method for a participant to enter an event
        public static async Task<EnrolmentResponse> Enrol(HttpClient participant, int eventId, int categoryId)
        {//start of method

            EnrolRequest request = new EnrolRequest();
            request.CategoryId = categoryId;

            HttpResponseMessage response = await PostJson(participant, "/api/events/" + eventId + "/enrolments", request);
            response.EnsureSuccessStatusCode();

            return await ReadBody<EnrolmentResponse>(response);

        }//end of method

        //method to make result data is avaiable
        public static ResultRequest NewResultRequest(int participantId, int position, string time)
        {//start of method

            ResultRequest request = new ResultRequest();
            request.ParticipantId = participantId;
            request.PositionNumber = position;
            request.FinishTime = time;

            return request;

        }//end of method

    }//end of class
}//end of namespace
