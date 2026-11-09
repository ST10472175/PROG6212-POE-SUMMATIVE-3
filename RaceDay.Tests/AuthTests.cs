using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

//this unit test class is to test all authentication needed in the begining all the way to the end
namespace RaceDay.Tests
{//start of namespace

    //tests for registration, login, logout, sessions and the profile
    public class AuthTests : IClassFixture<RaceDayFactory>
    {//start of class

        private readonly RaceDayFactory _factory;

        public AuthTests(RaceDayFactory factory)
        {//start of constructor
            _factory = factory;
        }//end of constructor

        [Fact]
        public async Task Register_Participant_ReturnsCreated()
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest request = TestHelper.NewRegistration(Roles.Participant);

            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/register", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            AuthResponse body = await TestHelper.ReadBody<AuthResponse>(response);
            Assert.Equal(Roles.Participant, body.Role);
            Assert.Equal(request.Email, body.Email);

        }//end of test

        [Fact]
        public async Task Register_Organiser_WithValidCode_ReturnsCreated()
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest request = TestHelper.NewRegistration(Roles.Organiser);

            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/register", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            AuthResponse body = await TestHelper.ReadBody<AuthResponse>(response);
            Assert.Equal(Roles.Organiser, body.Role);

        }//end of test

        [Theory]
        [InlineData("wrong-code")]
        [InlineData(null)]
        public async Task Register_Organiser_WithoutValidCode_IsForbidden(string code)
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest request = TestHelper.NewRegistration(Roles.Organiser);
            request.OrganiserCode = code;

            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/register", request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest request = TestHelper.NewRegistration(Roles.Participant);
            HttpResponseMessage first = await TestHelper.PostJson(client, "/api/auth/register", request);
            first.EnsureSuccessStatusCode();

            HttpResponseMessage second = await TestHelper.PostJson(client, "/api/auth/register", request);

            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        }//end of test

        [Theory]
        [InlineData("not-an-email", "Passw0rd!23", "Participant")]
        [InlineData("valid@raceday.test", "short", "Participant")]
        [InlineData("valid@raceday.test", "Passw0rd!23", "Admin")]
        public async Task Register_InvalidData_ReturnsBadRequest(string email, string password, string role)
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest request = TestHelper.NewRegistration(Roles.Participant);
            request.Email = email;
            request.Password = password;
            request.Role = role;

            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/register", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Register_StoresHashedPassword_NotThePlainPassword()
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest request = TestHelper.NewRegistration(Roles.Participant);
            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/register", request);
            response.EnsureSuccessStatusCode();

            //look in the database to see what was stored
            using (IServiceScope scope = _factory.Services.CreateScope())
            {//start of using
                RaceDayContext db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
                AppUser user = await db.Users.SingleAsync(u => u.Email == request.Email);

                Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
                Assert.NotEqual(request.Password, user.PasswordHash);
                Assert.DoesNotContain(request.Password, user.PasswordHash);
            }//end of using

        }//end of test

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOk()
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest registration = TestHelper.NewRegistration(Roles.Participant);
            HttpResponseMessage registerResponse = await TestHelper.PostJson(client, "/api/auth/register", registration);
            registerResponse.EnsureSuccessStatusCode();

            LoginRequest login = new LoginRequest();
            login.Email = registration.Email;
            login.Password = registration.Password;

            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/login", login);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AuthResponse body = await TestHelper.ReadBody<AuthResponse>(response);
            Assert.Equal(Roles.Participant, body.Role);

        }//end of test

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {//start of test

            HttpClient client = _factory.CreateClient();
            RegisterRequest registration = TestHelper.NewRegistration(Roles.Participant);
            HttpResponseMessage registerResponse = await TestHelper.PostJson(client, "/api/auth/register", registration);
            registerResponse.EnsureSuccessStatusCode();

            LoginRequest login = new LoginRequest();
            login.Email = registration.Email;
            login.Password = "WrongPassword!1";

            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/login", login);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
        {//start of test

            HttpClient client = _factory.CreateClient();
            LoginRequest login = new LoginRequest();
            login.Email = TestHelper.NewEmail();
            login.Password = TestHelper.Password;

            HttpResponseMessage response = await TestHelper.PostJson(client, "/api/auth/login", login);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Session_KeepsTheUserLoggedInAcrossRequests()
        {//start of test

            //after login the same client (same session cookie) can call protected endpoints
            HttpClient client = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage first = await client.GetAsync("/api/profile");
            HttpResponseMessage second = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        }//end of test

        [Fact]
        public async Task Session_StoresTheRoleOfTheUser()
        {//start of test

            HttpClient organiser = await TestHelper.LoginAsNewUser(_factory, Roles.Organiser);
            HttpClient participant = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage organiserProfile = await organiser.GetAsync("/api/profile");
            HttpResponseMessage participantProfile = await participant.GetAsync("/api/profile");

            ProfileResponse organiserBody = await TestHelper.ReadBody<ProfileResponse>(organiserProfile);
            ProfileResponse participantBody = await TestHelper.ReadBody<ProfileResponse>(participantProfile);
            Assert.Equal(Roles.Organiser, organiserBody.Role);
            Assert.Equal(Roles.Participant, participantBody.Role);

        }//end of test

        [Theory]
        [InlineData("/api/profile")]
        [InlineData("/api/events")]
        [InlineData("/api/events/1")]
        [InlineData("/api/events/1/categories")]
        [InlineData("/api/enrolments/my")]
        [InlineData("/api/results/my")]
        public async Task ProtectedEndpoints_WithoutLogin_ReturnUnauthorized(string url)
        {//start of test

            HttpClient client = _factory.CreateClient();

            HttpResponseMessage response = await client.GetAsync(url);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        }//end of test

        [Fact]
        public async Task Logout_EndsTheSession()
        {//start of test

            HttpClient client = await TestHelper.LoginAsNewUser(_factory, Roles.Participant);

            HttpResponseMessage logout = await client.PostAsync("/api/auth/logout", null);
            HttpResponseMessage afterLogout = await client.GetAsync("/api/profile");

            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);

        }//end of test

        [Theory]
        [InlineData("Organiser")]
        [InlineData("Participant")]
        public async Task Profile_BothRoles_CanViewAndUpdateTheirOwnProfile(string role)
        {//start of test

            HttpClient client = await TestHelper.LoginAsNewUser(_factory, role);

            //view the profile
            HttpResponseMessage view = await client.GetAsync("/api/profile");
            Assert.Equal(HttpStatusCode.OK, view.StatusCode);
            ProfileResponse before = await TestHelper.ReadBody<ProfileResponse>(view);
            Assert.Equal(role, before.Role);

            //update the profile
            UpdateProfileRequest update = new UpdateProfileRequest();
            update.Name = "Updated Name";
            update.ContactDetails = "updated@raceday.test";
            HttpResponseMessage updateResponse = await TestHelper.PutJson(client, "/api/profile", update);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            //view it again to see the change
            HttpResponseMessage viewAgain = await client.GetAsync("/api/profile");
            ProfileResponse after = await TestHelper.ReadBody<ProfileResponse>(viewAgain);
            Assert.Equal("Updated Name", after.Name);
            Assert.Equal("updated@raceday.test", after.ContactDetails);

        }//end of test

    }//end of class
}//end of namespace
