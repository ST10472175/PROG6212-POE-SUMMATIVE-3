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
    public class CategoriesController : BaseApiController
    {//start of class

        //object used to talk to the database
        private readonly RaceDayContext _db;

        public CategoriesController(RaceDayContext db)
        {//start of constructor
            _db = db;
        }//end of constructor

        //View the categories of an event (Organisers and Participants)
        
        [HttpGet("events/{eventId:int}/categories")]
        [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategories(int eventId)
        {//start of method

            IActionResult blocked = CheckLoggedIn();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            bool eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
            if (eventExists == false)
            {//start of if
                return NotFound("Event not found.");
            }//end of if

            List<Category> categories = await _db.Categories
                .Where(c => c.EventId == eventId)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            List<CategoryResponse> responses = new List<CategoryResponse>();
            foreach (Category category in categories)
            {//start of foreach
                responses.Add(MakeResponse(category));
            }//end of foreach

            return Ok(responses);

        }//end of method

        //Add an age or distance category to one of your events (Organisers only).
       
        [HttpPost("events/{eventId:int}/categories")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
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

            //the same category name cannot be used twice in one event
            string name = request.CategoryName.Trim();
            bool nameUsed = await _db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name);
            if (nameUsed == true)
            {//start of if
                return Conflict("This event already has a category with that name.");
            }//end of if

            Category category = new Category();
            category.CategoryName = name;
            category.EventId = eventId;

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return Created("/api/events/" + eventId + "/categories", MakeResponse(category));

        }//end of method

        //Rename a category of one of your events (Organisers only)
        
        [HttpPut("categories/{categoryId:int}")]
        [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(int categoryId, CategoryRequest request)
        {//start of method

            IActionResult blocked = CheckOrganiser();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            Category category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null)
            {//start of if
                return NotFound("Category not found.");
            }//end of if

            if (category.Event.OrganiserId != GetOrganiserId())
            {//start of if
                return NotYourEvent();
            }//end of if

            string name = request.CategoryName.Trim();
            bool nameUsed = await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.CategoryId != categoryId && c.CategoryName == name);
            if (nameUsed == true)
            {//start of if
                return Conflict("This event already has a category with that name.");
            }//end of if

            category.CategoryName = name;
            await _db.SaveChangesAsync();

            return Ok(MakeResponse(category));

        }//end of method

        /// Delete a category of one of your events (Organisers only).
       
        [HttpDelete("categories/{categoryId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        //method to delete any events in a category
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {//start of method

            IActionResult blocked = CheckOrganiser();
            if (blocked != null)
            {//start of if
                return blocked;
            }//end of if

            Category category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
            if (category == null)
            {//start of if
                return NotFound("Category not found.");
            }//end of if

            if (category.Event.OrganiserId != GetOrganiserId())
            {//start of if
                return NotYourEvent();
            }//end of if

            //a category that participants already entered cannot be deleted
            bool inUse = await _db.Assignments.AnyAsync(a => a.CategoryId == categoryId);
            if (inUse == true)
            {//start of if
                return Conflict("Participants are enrolled in this category, so it cannot be deleted.");
            }//end of if

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            return NoContent();

        }//end of method

        //method to turn the category into the data we send back
        private CategoryResponse MakeResponse(Category category)
        {//start of method

            CategoryResponse response = new CategoryResponse();
            response.CategoryId = category.CategoryId;
            response.CategoryName = category.CategoryName;
            response.EventId = category.EventId;

            return response;

        }//end of method

    }//end of class
}//end of namespace
