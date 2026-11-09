using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    [Authorize(Roles = "Organiser")]
    public sealed class OrganiserController : Controller
    {
        private readonly RaceDayApiClient _api;

        public OrganiserController(RaceDayApiClient api)
        {
            _api = api;
        }

        //loads organiser dashboard stats, including  event enrolment totals
        public async Task<IActionResult> Index()
        {
            try
            {
                List<EventApiDto> events = await _api.GetAsync<List<EventApiDto>>("api/events") ?? new();
                List<EventSummaryViewModel> summaries = new();

                foreach (EventApiDto ev in events)
                {
                    List<EnrolmentApiDto> enrolments = await _api.GetAsync<List<EnrolmentApiDto>>($"api/events/{ev.EventId}/enrolments") ?? new();
                    summaries.Add(new EventSummaryViewModel { Event = ev, EnrolmentCount = enrolments.Count });
                }

                ViewBag.TotalEnrolments = summaries.Sum(s => s.EnrolmentCount);
                ViewBag.UpcomingEvents = summaries.Count(s => s.Event.EventDate >= DateTime.Now);
                return View("Dashboard", summaries);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return View("Dashboard", new List<EventSummaryViewModel>());
            }
        }

        public async Task<IActionResult> Events()
        {
            try
            {
                List<EventApiDto> events = await _api.GetAsync<List<EventApiDto>>("api/events") ?? new();
                return View(events);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return View(new List<EventApiDto>());
            }
        }

        [HttpGet]
        public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

        //creates a new event and uploads its optional  banner image
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EventForm", model);
            }

            EventApiDto created;
            try
            {
                // The event record is created first; the optional file is then sent to the API upload endpoint.
                // Reference: https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads
                created = await _api.PostAsync<EventApiDto>("api/events", ToApiRequest(model));
            }
            catch (RaceDayApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("EventForm", model);
            }

            bool bannerUploadFailed = false;
            if (model.BannerImage != null)
            {
                try
                {
                    await _api.UploadAsync<EventApiDto>($"api/events/{created.EventId}/banner", model.BannerImage);
                }
                catch (RaceDayApiException ex)
                {
                    bannerUploadFailed = true;
                    TempData["Error"] = $"The event was created, but its banner could not be uploaded: {ex.Message}";
                }
            }

            if (!bannerUploadFailed)
            {
                TempData["Success"] = "The event was created successfully.";
            }
            return RedirectToAction(nameof(Events));
        }

        //retrieves the selected event details for editing
        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            try
            {
                EventApiDto ev = await _api.GetAsync<EventApiDto>($"api/events/{id}");
                return View("EventForm", new EventFormViewModel
                {
                    EventId = ev.EventId,
                    EventName = ev.EventName,
                    Description = ev.Description,
                    EventDate = ev.EventDate,
                    Location = ev.Location,
                    Distance = ev.Distance,
                    EventType = ev.EventType,
                    BannerImageUrl = ev.BannerImageUrl
                });
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Events));
            }
        }

        //deletes the selected event through API
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EventFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EventForm", model);
            }

            try
            {
                await _api.PutAsync<EventApiDto>($"api/events/{model.EventId}", ToApiRequest(model));
            }
            catch (RaceDayApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("EventForm", model);
            }

            bool bannerUploadFailed = false;
            if (model.BannerImage != null)
            {
                try
                {
                    await _api.UploadAsync<EventApiDto>($"api/events/{model.EventId}/banner", model.BannerImage);
                }
                catch (RaceDayApiException ex)
                {
                    bannerUploadFailed = true;
                    TempData["Error"] = $"The event details were saved, but the banner could not be uploaded: {ex.Message}";
                }
            }

            if (!bannerUploadFailed)
            {
                TempData["Success"] = "The event details were updated.";
            }
            return RedirectToAction(nameof(Events));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                await _api.DeleteAsync($"api/events/{id}");
                TempData["Success"] = "The event was deleted.";
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> Categories(int eventId)
        {
            try
            {
                ViewBag.Event = await _api.GetAsync<EventApiDto>($"api/events/{eventId}");
                List<CategoryApiDto> categories = await _api.GetAsync<List<CategoryApiDto>>($"api/events/{eventId}/categories") ?? new();
                return View(categories);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Events));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int eventId, string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["Error"] = "Enter a category name.";
                return RedirectToAction(nameof(Categories), new { eventId });
            }

            try
            {
                await _api.PostAsync<CategoryApiDto>($"api/events/{eventId}/categories", new CategoryApiRequest { CategoryName = categoryName.Trim() });
                TempData["Success"] = "The category was added.";
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Categories), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameCategory(int eventId, int categoryId, string categoryName)
        {
            try
            {
                await _api.PutAsync<CategoryApiDto>($"api/categories/{categoryId}", new CategoryApiRequest { CategoryName = categoryName?.Trim() });
                TempData["Success"] = "The category was renamed.";
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Categories), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
        {
            try
            {
                await _api.DeleteAsync($"api/categories/{categoryId}");
                TempData["Success"] = "The category was deleted.";
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Categories), new { eventId });
        }

        //displays the participants enrolled in the selected event.
        [HttpGet]
        public async Task<IActionResult> Enrolments(int eventId)
        {
            try
            {
                ViewBag.Event = await _api.GetAsync<EventApiDto>($"api/events/{eventId}");
                List<EnrolmentApiDto> entries = await _api.GetAsync<List<EnrolmentApiDto>>($"api/events/{eventId}/enrolments") ?? new();
                return View(entries);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Events));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEnrolmentStatus(int eventId, int entryId, string status)
        {
            try
            {
                await _api.PutAsync<EnrolmentApiDto>($"api/enrolments/{entryId}/status", new UpdateEnrolmentStatusApiRequest
                {
                    Status = status
                });
                TempData["Success"] = "The enrolment status was updated.";
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Enrolments), new { eventId });
        }

        [HttpGet]
        public async Task<IActionResult> Results(int eventId)
        {
            try
            {
                EventResultsViewModel model = new()
                {
                    Event = await _api.GetAsync<EventApiDto>($"api/events/{eventId}"),
                    Enrolments = await _api.GetAsync<List<EnrolmentApiDto>>($"api/events/{eventId}/enrolments") ?? new(),
                    Results = await _api.GetAsync<List<ResultApiDto>>($"api/events/{eventId}/results") ?? new()
                };
                return View(model);
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Events));
            }
        }
        //records a participant's results after the event has concluded.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CaptureResult(int eventId, EventResultsViewModel model)
        {
            try
            {
                EventApiDto ev = await _api.GetAsync<EventApiDto>($"api/events/{eventId}");
                if (ev.EventDate > DateTime.Now)
                {
                    TempData["Error"] = "Results can only be recorded after the event has concluded.";
                    return RedirectToAction(nameof(Results), new { eventId });
                }

                await _api.PostAsync<ResultApiDto>($"api/events/{eventId}/results", new ResultApiRequest
                {
                    ParticipantId = model.NewParticipantId,
                    FinishTime = model.NewFinishTime,
                    PositionNumber = model.NewPositionNumber
                });
                TempData["Success"] = "The participant's result was recorded.";
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Results), new { eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateResult(int eventId, int resultsId, string finishTime, int positionNumber)
        {
            try
            {
                await _api.PutAsync<ResultApiDto>($"api/results/{resultsId}", new UpdateResultApiRequest
                {
                    FinishTime = finishTime,
                    PositionNumber = positionNumber
                });
                TempData["Success"] = "The result was updated.";
            }
            catch (RaceDayApiException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Results), new { eventId });
        }

        private static EventApiRequest ToApiRequest(EventFormViewModel model) => new()
        {
            EventName = model.EventName,
            Description = model.Description,
            EventDate = model.EventDate,
            Location = model.Location,
            Distance = model.Distance,
            EventType = model.EventType
        };
    }
}
