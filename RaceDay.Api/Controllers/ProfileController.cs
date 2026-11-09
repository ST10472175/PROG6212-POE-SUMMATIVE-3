using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers
{//start of namespace

    [ApiController]
    [Route("api/profile")]
    [Produces("application/json")]
    public class ProfileController : BaseApiController
    {//start of class

        //object used to talk to the database
        private readonly RaceDayContext _db;
        private readonly BlobStorageService _blobStorage;

        public ProfileController(RaceDayContext db, BlobStorageService blobStorage)
        {//start of constructor
            _db = db;
            _blobStorage = blobStorage;
        }//end of constructor

        //View your own profile (Organisers and Participants).
        [HttpGet]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {//start of method

            IActionResult blocked = CheckLoggedIn();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            AppUser user = await LoadCurrentUser();
            if (user == null)
            {//start of if
                return Unauthorized("Your account no longer exists.");
            }//end of if

            return Ok(MakeResponse(user));

        }//end of method

        //Update your own profile information (Organisers and Participants).
        [HttpPut]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
        {//start of method

            IActionResult blocked = CheckLoggedIn();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            AppUser user = await LoadCurrentUser();
            if (user == null)
            {//start of if
                return Unauthorized("Your account no longer exists.");
            }//end of if

            //change the details of the organiser or participant
            if (user.Organiser != null)
            {//start of if
                user.Organiser.Name = request.Name.Trim();
                user.Organiser.ContactDetails = request.ContactDetails.Trim();
            }//end of if
            else if (user.Participant != null)
            {//start of else if
                user.Participant.Name = request.Name.Trim();
                user.Participant.ContactDetails = request.ContactDetails.Trim();
            }//end of else if

            await _db.SaveChangesAsync();
            return Ok(MakeResponse(user));

        }//end of method

        //Upload a participant profile picture through the API into Azure Blob Storage.
        [HttpPost("picture")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UploadPicture(IFormFile file, CancellationToken cancellationToken)
        {
            IActionResult blocked = CheckParticipant();
            if (blocked != null)
            {
                return blocked;
            }

            AppUser user = await LoadCurrentUser();
            if (user?.Participant == null)
            {
                return Unauthorized("Your participant account could not be found.");
            }

            try
            {
                user.Participant.ProfilePictureUrl = await _blobStorage.UploadImageAsync(file, "profile-pictures", cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                return Ok(MakeResponse(user));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
        }

        //method to get the logged in user from the database using the id in the session
        private async Task<AppUser> LoadCurrentUser()
        {//start of method

            int userId = GetUserId();

            AppUser user = await _db.Users
                .Include(u => u.Organiser)
                .Include(u => u.Participant)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            return user;

        }//end of method

        //method to turn the user into the data we send back
        private ProfileResponse MakeResponse(AppUser user)
        {//start of method

            ProfileResponse response = new ProfileResponse();
            response.UserId = user.UserId;
            response.Email = user.Email;
            response.Role = user.Role;

            if (user.Organiser != null)
            {//start of if
                response.Name = user.Organiser.Name;
                response.ContactDetails = user.Organiser.ContactDetails;
            }//end of if
            else if (user.Participant != null)
            {//start of else if
                response.Name = user.Participant.Name;
                response.ContactDetails = user.Participant.ContactDetails;
                response.ProfilePictureUrl = user.Participant.ProfilePictureUrl;
            }//end of else if

            return response;

        }//end of method

    }//end of class
}//end of namespace
