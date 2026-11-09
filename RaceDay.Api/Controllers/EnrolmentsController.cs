using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers
{//start of namespace

    [ApiController]
    [Route("api")]
    [Produces("application/json")]
    public class EnrolmentsController : BaseApiController
    {//start of class

        //object used to talk to the database
        private readonly RaceDayContext _db;

        public EnrolmentsController(RaceDayContext db)
        {//start of constructor
            _db = db;
        }//end of constructor
        //
        // Enter an event by selecting a category (Participants only)
        
        [HttpPost("events/{eventId:int}/enrolments")]
        [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int eventId, EnrolRequest request)
        {//start of method

            IActionResult blocked = CheckParticipant();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            int participantId = GetParticipantId();

            bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
            if (eventExists == false)
            {//start of if
                return NotFound("Event not found.");
            }//end of if

            //the chosen category must belong to this event
            bool categoryOk = await _db.Categories.AnyAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
            if (categoryOk == false)
            {//start of if
                return BadRequest("The selected category does not belong to this event.");
            }//end of if

            //a participant can only enter an event once
            bool alreadyEnrolled = await _db.Assignments.AnyAsync(a => a.EventId == eventId && a.ParticipantId == participantId);
            if (alreadyEnrolled == true)
            {//start of if
                return Conflict("You are already enrolled in this event.");
            }//end of if

            //save the link between the participant, the event and the category
            Assignment entry = new Assignment();
            entry.EventId = eventId;
            entry.ParticipantId = participantId;
            entry.CategoryId = request.CategoryId;
            entry.EntryDate = DateTime.UtcNow;
            entry.Status = "Pending";

            _db.Assignments.Add(entry);
            await _db.SaveChangesAsync();

            //load the entry again with the names so they are in the response
            Assignment saved = await GetEntries().FirstAsync(a => a.EntryId == entry.EntryId);

            return Created("/api/events/" + eventId + "/enrolments", MakeResponse(saved));

        }//end of method

        //View all enrolments of one of your events (Organisers only).
       
        [HttpGet("events/{eventId:int}/enrolments")]
        [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {//start of method

            IActionResult blocked = CheckOrganiser();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            Event ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
            if (ev == null)
            {//start of if
                return NotFound("Event not found.");
            }//end of if

            if (ev.OrganiserId != GetOrganiserId())
            {//start of if
                return NotYourEvent();
            }//end of if

            List<Assignment> entries = await GetEntries()
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.EntryDate)
                .ToListAsync();

            return Ok(MakeResponseList(entries));

        }//end of method

        //Confirm an enrolment or return it to pending (the event's Organiser only).
        [HttpPut("enrolments/{entryId:int}/status")]
        [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEnrolmentStatus(int entryId, UpdateEnrolmentStatusRequest request)
        {
            IActionResult blocked = CheckOrganiser();
            if (blocked != null)
            {
                return blocked;
            }

            Assignment entry = await _db.Assignments
                .Include(a => a.Event)
                .FirstOrDefaultAsync(a => a.EntryId == entryId);

            if (entry == null)
            {
                return NotFound("Enrolment not found.");
            }

            if (entry.Event.OrganiserId != GetOrganiserId())
            {
                return NotYourEvent();
            }

            string status = request.Status?.Trim();
            if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Status must be Pending or Confirmed.");
            }

            entry.Status = string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase)
                ? "Confirmed"
                : "Pending";
            await _db.SaveChangesAsync();

            Assignment saved = await GetEntries().FirstAsync(a => a.EntryId == entryId);
            return Ok(MakeResponse(saved));
        }

        //View the events you have entered (Participants only).
        [HttpGet("enrolments/my")]

        [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments()
        {//start of method

            IActionResult blocked = CheckParticipant();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            int participantId = GetParticipantId();

            List<Assignment> entries = await GetEntries()
                .Where(a => a.ParticipantId == participantId)
                .OrderBy(a => a.EntryDate)
                .ToListAsync();

            return Ok(MakeResponseList(entries));

        }//end of method

        //method that gets the enrolments together with the event, participant and category
        private IQueryable<Assignment> GetEntries()
        {//start of method

            return _db.Assignments
                .Include(a => a.Event)
                .Include(a => a.Participant)
                .Include(a => a.Category);

        }//end of method

        //method to turn a list of enrolments into the data we send back
        private List<EnrolmentResponse> MakeResponseList(List<Assignment> entries)
        {//start of method

            List<EnrolmentResponse> responses = new List<EnrolmentResponse>();
            foreach (Assignment entry in entries)
            {//start of foreach
                responses.Add(MakeResponse(entry));
            }//end of foreach

            return responses;

        }//end of method

        //method to turn one enrolment into the data we send back
        private EnrolmentResponse MakeResponse(Assignment entry)
        {//start of method

            EnrolmentResponse response = new EnrolmentResponse();
            response.EntryId = entry.EntryId;
            response.EventId = entry.EventId;
            response.ParticipantId = entry.ParticipantId;
            response.CategoryId = entry.CategoryId;
            response.EntryDate = entry.EntryDate;
            response.Status = string.IsNullOrWhiteSpace(entry.Status) ? "Pending" : entry.Status;

            if (entry.Event != null)
            {//start of if
                response.EventName = entry.Event.EventName;
                response.EventDate = entry.Event.EventDate;
            }//end of if

            if (entry.Participant != null)
            {//start of if
                response.ParticipantName = entry.Participant.Name;
            }//end of if

            if (entry.Category != null)
            {//start of if
                response.CategoryName = entry.Category.CategoryName;
            }//end of if

            return response;

        }//end of method

    }//end of class
}//end of namespace
