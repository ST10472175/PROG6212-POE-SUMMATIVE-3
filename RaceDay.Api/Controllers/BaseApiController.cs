using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{//start of namespace

    //all the other controllers inherit from this class
    //it reads the logged in user from the session
    //and checks the user's role
    public abstract class BaseApiController : ControllerBase
    {//start of class

        //method to check if the user is logged in
        protected bool IsLoggedIn()
        {//start of method

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {//start of if
                return false;
            }//end of if

            return true;

        }//end of method

        //method to get the id of the logged in user (0 if nobody is logged in)
        protected int GetUserId()
        {//start of method

            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {//start of if
                return 0;
            }//end of if

            return userId.Value;

        }//end of method

        //method to get the role of the logged in user
        protected string GetRole()
        {//start of method

            string role = HttpContext.Session.GetString("Role");

            if (role == null)
            {//start of if
                return "";
            }//end of if

            return role;

        }//end of method

        //method to get the organiser id of the logged in organiser (0 if not an organiser)
        protected int GetOrganiserId()
        {//start of method

            int? organiserId = HttpContext.Session.GetInt32("OrganiserId");

            if (organiserId == null)
            {//start of if
                return 0;
            }//end of if

            return organiserId.Value;

        }//end of method

        //method to get the participant id of the logged in participant (0 if not a participant)
        protected int GetParticipantId()
        {//start of method

            int? participantId = HttpContext.Session.GetInt32("ParticipantId");

            if (participantId == null)
            {//start of if
                return 0;
            }//end of if

            return participantId.Value;

        }//end of method

        //method to make sure the user is logged in
        //it returns null when everything is fine
        protected IActionResult CheckLoggedIn()
        {//start of method

            if (IsLoggedIn() == false)
            {//start of if
                return Unauthorized("You must log in first.");
            }//end of if

            return null;

        }//end of method

        //method to make sure the user is logged in AND is an organiser
        protected IActionResult CheckOrganiser()
        {//start of method

            if (IsLoggedIn() == false)
            {//start of if
                return Unauthorized("You must log in first.");
            }//end of if

            if (GetRole() != Roles.Organiser)
            {//start of if
                return StatusCode(StatusCodes.Status403Forbidden, "This action is only for Organisers.");
            }//end of if

            return null;

        }//end of method

        //method to make sure the user is logged in AND is a participant
        protected IActionResult CheckParticipant()
        {//start of method

            if (IsLoggedIn() == false)
            {//start of if
                return Unauthorized("You must log in first.");
            }//end of if

            if (GetRole() != Roles.Participant)
            {//start of if
                return StatusCode(StatusCodes.Status403Forbidden, "This action is only for Participants.");
            }//end of if

            return null;

        }//end of method

        //method used when an organiser tries to change another organiser's event
        protected IActionResult NotYourEvent()
        {//start of method

            return StatusCode(StatusCodes.Status403Forbidden, "This event belongs to a different Organiser.");

        }//end of method

    }//end of class
}//end of namespace
