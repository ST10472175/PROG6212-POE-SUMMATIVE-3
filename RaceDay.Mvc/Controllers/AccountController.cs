using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers
{
    public sealed class AccountController : Controller
    {
        private readonly RaceDayApiClient _api;

        public AccountController(RaceDayApiClient api)
        {
            _api = api;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                AuthApiResponse user = await _api.PostAsync<AuthApiResponse>("api/auth/login", new LoginApiRequest
                {
                    Email = model.Email,
                    Password = model.Password
                });

                // Code attribution for claims-based identities:
                // https://learn.microsoft.com/aspnet/core/security/authentication/claims
                List<Claim> claims = new()
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Name ?? user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                ClaimsPrincipal principal = new(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return user.Role == "Organiser"
                    ? RedirectToAction("Index", "Organiser")
                    : RedirectToAction("Index", "Participant");
            }
            catch (RaceDayApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "The RaceDay API is unavailable. Start the API and try again.");
                return View(model);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _api.PostAsync<AuthApiResponse>("api/auth/register", new RegisterApiRequest
                {
                    Email = model.Email,
                    Password = model.Password,
                    Name = model.Name,
                    ContactDetails = model.ContactDetails,
                    Role = model.Role,
                    OrganiserCode = model.OrganiserCode
                });

                TempData["Success"] = "Your RaceDay account has been created. Sign in to continue.";
                return RedirectToAction(nameof(Login));
            }
            catch (RaceDayApiException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(string.Empty, "The RaceDay API is unavailable. Please try again.");
                return View(model);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _api.PostNoContentAsync("api/auth/logout");
            }
            catch (RaceDayApiException)
            {
                // The local sign-out must still complete when the API session has expired.
            }
            finally
            {
                _api.ClearApiSessionCookie();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
            }

            return RedirectToAction(nameof(Login));
        }
    }
}
