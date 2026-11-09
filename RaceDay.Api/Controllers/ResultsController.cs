using System.Globalization;
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
    public class ResultsController : BaseApiController
    {//start of class

        //object used to talk to the database
        private readonly RaceDayContext _db;

        //the format the finish time 
        private const string TimeFormat = @"hh\:mm\:ss";

        public ResultsController(RaceDayContext db)
        {//start of constructor
            _db = db;
        }//end of constructor

        //Capture a participant's finish time and position for an event (Organisers only).
        [HttpPost("events/{eventId:int}/results")]
        [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CaptureResult(int eventId, ResultRequest request)
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

            //check the finish time
            TimeSpan finishTime;
            bool timeOk = ReadFinishTime(request.FinishTime, out finishTime);
            if (timeOk == false)
            {//start of if
                return BadRequest("Finish time must be hh:mm:ss (for example 01:25:30) and more than 00:00:00.");
            }//end of if

            //the participant must be enrolled in this event
            bool enrolled = await _db.Assignments.AnyAsync(a => a.EventId == eventId && a.ParticipantId == request.ParticipantId);
            if (enrolled == false)
            {//start of if
                return BadRequest("This participant is not enrolled in the event.");
            }//end of if

            //only one result per participant
            bool hasResult = await _db.Results.AnyAsync(r => r.EventId == eventId && r.ParticipantId == request.ParticipantId);
            if (hasResult == true)
            {//start of if
                return Conflict("A result was already captured for this participant. Use PUT to change it.");
            }//end of if

            //two participants cannot share a position
            bool positionTaken = await _db.Results.AnyAsync(r => r.EventId == eventId && r.PositionNumber == request.PositionNumber);
            if (positionTaken == true)
            {//start of if
                return Conflict("Another participant already has this finishing position.");
            }//end of if

            Result result = new Result();
            result.EventId = eventId;
            result.ParticipantId = request.ParticipantId;
            result.FinishTime = finishTime;
            result.PositionNumber = request.PositionNumber;

            _db.Results.Add(result);
            await _db.SaveChangesAsync();

            //load the result again with the names so they are in the response
            Result saved = await GetResults().FirstAsync(r => r.ResultsId == result.ResultsId);

            return Created("/api/events/" + eventId + "/results", await MakeResponseAsync(saved));

        }//end of method

        // View all results of one of your events (Organisers only).
        [HttpGet("events/{eventId:int}/results")]
        [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventResults(int eventId)
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

            List<Result> results = await GetResults()
                .Where(r => r.EventId == eventId)
                .OrderBy(r => r.PositionNumber)
                .ToListAsync();

            return Ok(await MakeResponseListAsync(results));

        }//end of method

        //Correct a captured result (Organisers only).
        [HttpPut("results/{resultsId:int}")]
        [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateResult(int resultsId, UpdateResultRequest request)
        {//start of method

            IActionResult blocked = CheckOrganiser();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            Result result = await GetResults().FirstOrDefaultAsync(r => r.ResultsId == resultsId);
            if (result == null)
            {//start of if
                return NotFound("Result not found.");
            }//end of if

            if (result.Event.OrganiserId != GetOrganiserId())
            {//start of if
                return NotYourEvent();
            }//end of if

            TimeSpan finishTime;
            bool timeOk = ReadFinishTime(request.FinishTime, out finishTime);
            if (timeOk == false)
            {//start of if
                return BadRequest("Finish time must be hh:mm:ss (for example 01:25:30) and more than 00:00:00.");
            }//end of if

            bool positionTaken = await _db.Results.AnyAsync(r => r.EventId == result.EventId && r.ResultsId != resultsId && r.PositionNumber == request.PositionNumber);
            if (positionTaken == true)
            {//start of if
                return Conflict("Another participant already has this finishing position.");
            }//end of if

            result.FinishTime = finishTime;
            result.PositionNumber = request.PositionNumber;
            await _db.SaveChangesAsync();

            return Ok(await MakeResponseAsync(result));

        }//end of method

        //View your own results (Participants only).
        [HttpGet("results/my")]
        [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {//start of method

            IActionResult blocked = CheckParticipant();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            int participantId = GetParticipantId();

            List<Result> results = await GetResults()
                .Where(r => r.ParticipantId == participantId)
                .OrderByDescending(r => r.Event.EventDate)
                .ToListAsync();

            return Ok(await MakeResponseListAsync(results));

        }//end of method

        //method to read the finish time text, it returns false if the time is not valid
        private bool ReadFinishTime(string text, out TimeSpan finishTime)
        {//start of method

            bool parsed = TimeSpan.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, out finishTime);

            if (parsed == false)
            {//start of if
                return false;
            }//end of if

            if (finishTime <= TimeSpan.Zero)
            {//start of if
                return false;
            }//end of if

            return true;

        }//end of method

        //method that gets the results together with the event and participant
        private IQueryable<Result> GetResults()
        {//start of method

            return _db.Results
                .Include(r => r.Event)
                .Include(r => r.Participant);

        }//end of method

        //method to turn a list of results into the data we send back
        //The category is read from the enrolment for the matching participant and event.
        private async Task<List<ResultResponse>> MakeResponseListAsync(List<Result> results)
        {
            List<ResultResponse> responses = new List<ResultResponse>();
            foreach (Result result in results)
            {
                responses.Add(await MakeResponseAsync(result));
            }

            return responses;
        }

        //method to turn one result into the data we send back
        private async Task<ResultResponse> MakeResponseAsync(Result result)
        {
            string categoryName = await _db.Assignments
                .Where(a => a.EventId == result.EventId && a.ParticipantId == result.ParticipantId)
                .Select(a => a.Category.CategoryName)
                .FirstOrDefaultAsync();
            int totalParticipants = await _db.Assignments.CountAsync(a => a.EventId == result.EventId);

            ResultResponse response = new ResultResponse();
            response.ResultsId = result.ResultsId;
            response.EventId = result.EventId;
            response.ParticipantId = result.ParticipantId;
            response.FinishTime = result.FinishTime.ToString(TimeFormat);
            response.PositionNumber = result.PositionNumber;
            response.TotalParticipants = totalParticipants;
            response.CategoryName = categoryName;

            if (result.Event != null)
            {//start of if statement
                response.EventName = result.Event.EventName;
                response.EventDate = result.Event.EventDate;
            }//end of if statement

            if (result.Participant != null)
            {//start of if
                response.ParticipantName = result.Participant.Name;
            }//end of if

            return response;
        }//end of method

    }//end of class
}//end of namespace
