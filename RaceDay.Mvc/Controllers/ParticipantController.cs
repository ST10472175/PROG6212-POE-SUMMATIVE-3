using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    //Handles participant actions such as viewing events, enrolling,
    //checking results, and managing profile information
    [Authorize(Roles = "Participant")]
    public sealed class ParticipantController : Controller
    {
        private readonly RaceDayApiClient _api;

        public ParticipantController(RaceDayApiClient api)
        {
            _api = api;
        }

        //Displays upcoming events and allows participants to filter them
        //by search term, event type, and starting date.
        [HttpGet]
        public async Task<IActionResult> Index(string search, string eventType, DateTime? fromDate)
        {
            try
            {
                List<EventApiDto> events = await _api.GetAsync<List<EventApiDto>>("api/events") ?? new();
                IEnumerable<EventApiDto> filtered = events.Where(e => e.EventDate >= DateTime.Now);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(e =>
                        (e.EventName ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (e.Location ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (e.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(eventType) && eventType != "All")
                {
                    filtered = filtered.Where(e => string.Equals(e.EventType, eventType, StringComparison.OrdinalIgnoreCase));
                }

                if (fromDate.HasValue)
                {
                    filtered = filtered.Where(e => e.EventDate.Date >= fromDate.Value.Date);
                }

                ViewBag.Search = search;
                ViewBag.EventType = eventType ?? "All";
                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                return View(filtered.OrderBy(e => e.EventDate).ToList());
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<EventApiDto>());
            }
        }

        //loads the selected event, its available categories,
        //and checks whether the participant has already enrolled
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                ParticipantEventDetailsViewModel model = new()
                {
                    Event = await _api.GetAsync<EventApiDto>($"api/events/{id}"),
                    Categories = await _api.GetAsync<List<CategoryApiDto>>($"api/events/{id}/categories") ?? new()
                };

                List<EnrolmentApiDto> myEntries = await _api.GetAsync<List<EnrolmentApiDto>>("api/enrolments/my") ?? new();
                model.AlreadyEnrolled = myEntries.Any(entry => entry.EventId == id);
                return View(model);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            try
            {
                await _api.PostAsync<EnrolmentApiDto>($"api/events/{eventId}/enrolments", new EnrolApiRequest { CategoryId = categoryId });
                TempData["Success"] = "Your enrolment has been recorded.";
                return RedirectToAction(nameof(MyEvents));
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = eventId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyEvents()
        {
            try
            {
                List<EnrolmentApiDto> entries = await _api.GetAsync<List<EnrolmentApiDto>>("api/enrolments/my") ?? new();
                return View(entries);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<EnrolmentApiDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            try
            {
                List<ResultApiDto> results = await _api.GetAsync<List<ResultApiDto>>("api/results/my") ?? new();
                return View(results);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<ResultApiDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                ProfileApiResponse profile = await _api.GetAsync<ProfileApiResponse>("api/profile");
                return View(new ProfileFormViewModel
                {
                    Name = profile.Name,
                    ContactDetails = profile.ContactDetails,
                    Email = profile.Email,
                    Role = profile.Role,
                    ProfilePictureUrl = profile.ProfilePictureUrl
                });
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                ProfileApiResponse profile = await _api.PutAsync<ProfileApiResponse>("api/profile", new UpdateProfileApiRequest
                {
                    Name = model.Name,
                    ContactDetails = model.ContactDetails
                });
                model.ProfilePictureUrl = profile.ProfilePictureUrl;

                if (model.ProfilePicture != null)
                {
                    ProfileApiResponse updated = await _api.UploadAsync<ProfileApiResponse>("api/profile/picture", model.ProfilePicture);
                    model.ProfilePictureUrl = updated.ProfilePictureUrl;
                }

                model.Email = User.FindFirst(ClaimTypes.Email)?.Value;
                model.Role = "Participant";
                TempData["Success"] = "Your profile was updated.";
                return View(model);
            }
            catch (RaceDayApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }
    }
}
