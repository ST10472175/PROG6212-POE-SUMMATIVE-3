using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{//start of namespace

    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : BaseApiController
    {//start of class

        //object used to talk to the database
        private readonly RaceDayContext _db;

        //object used to read the settings in appsettings.json
        private readonly IConfiguration _config;

        //constructor, the database and settings are given to us by ASP.NET
        public AuthController(RaceDayContext db, IConfiguration config)
        {//start of constructor
            _db = db;
            _config = config;
        }//end of constructor

        //Registration for users to pick their roles
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {//start of method

            //work out which role the user picked
            string chosenRole = request.Role.Trim().ToLower();
            string role = "";

            //if statement attributions: https://www.w3schools.com/cs/cs_conditions.php
            if (chosenRole == "organiser")
            {//start of if
                role = Roles.Organiser;
            }//end of if
            else if (chosenRole == "participant")
            {//start of else if
                role = Roles.Participant;
            }//end of else if
            else
            {//start of else
                return BadRequest("Role must be either Organiser or Participant.");
            }//end of else

            //organisers must enter the secret organiser code
            if (role == Roles.Organiser)
            {//start of if
                string expectedCode = _config["Registration:OrganiserCode"];

                if (string.IsNullOrEmpty(expectedCode) || request.OrganiserCode != expectedCode)
                {//start of inner if
                    return StatusCode(StatusCodes.Status403Forbidden, "A valid organiser code is needed to register as an Organiser.");
                }//end of inner if
            }//end of if

            //check if the email is already used
            string email = request.Email.Trim().ToLower();
            bool emailExists = await _db.Users.AnyAsync(u => u.Email == email);


            //if statement attributions: https://www.w3schools.com/cs/cs_conditions.php
            if (emailExists == true)
            {//start of if
                return Conflict("An account with this email already exists.");
            }//end of if

            //create the user account
            AppUser user = new AppUser();
            user.Email = email;
            user.Role = role;

            //create the organiser or participant details that belong to the account
            if (role == Roles.Organiser)
            {//start of if
                EventOrganiser organiser = new EventOrganiser();
                organiser.Name = request.Name.Trim();
                organiser.ContactDetails = request.ContactDetails.Trim();
                user.Organiser = organiser;
            }//end of if
            else
            {//start of else
                Participant participant = new Participant();
                participant.Name = request.Name.Trim();
                participant.ContactDetails = request.ContactDetails.Trim();
                user.Participant = participant;
            }//end of else

            //hash the password with BCrypt so the original password is never stored
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            //save to the database
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Created("/api/profile", MakeResponse(user));

        }//end of method

       //Logins in user with email
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginRequest request)
        {//start of method

            //find the user with this email
            string email = request.Email.Trim().ToLower();
            AppUser user = await _db.Users
                .Include(u => u.Organiser)
                .Include(u => u.Participant)
                .FirstOrDefaultAsync(u => u.Email == email);


            //if statement attributions: https://www.w3schools.com/cs/cs_conditions.php
            if (user == null)
            {//start of if
                return Unauthorized("Invalid email or password.");
            }//end of if

            //check the password against the hash
            bool passwordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);


            //if statement attributions: https://www.w3schools.com/cs/cs_conditions.php
            if (passwordCorrect == false)
            {//start of if
                return Unauthorized("Invalid email or password.");
            }//end of if

            //start a fresh session and store the user id and role in it
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);

            if (user.OrganiserId != null)
            {//start of if
                HttpContext.Session.SetInt32("OrganiserId", user.OrganiserId.Value);
            }//end of if

            if (user.ParticipantId != null)
            {//start of if
                HttpContext.Session.SetInt32("ParticipantId", user.ParticipantId.Value);
            }//end of if

            return Ok(MakeResponse(user));

        }//end of method

        //User Logput
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {//start of method

            IActionResult blocked = CheckLoggedIn();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            //remove everything from the session
            HttpContext.Session.Clear();
            return NoContent();

        }//end of method

        //method to turn the user into the data we send back
        private AuthResponse MakeResponse(AppUser user)
        {//start of method

            AuthResponse response = new AuthResponse();
            response.UserId = user.UserId;
            response.Email = user.Email;
            response.Role = user.Role;

            if (user.Organiser != null)
            {//start of if
                response.Name = user.Organiser.Name;
            }//end of if
            else if (user.Participant != null)
            {//start of else if
                response.Name = user.Participant.Name;
            }//end of else if

            return response;

        }//end of method

    }//end of class
}//end of namespace
