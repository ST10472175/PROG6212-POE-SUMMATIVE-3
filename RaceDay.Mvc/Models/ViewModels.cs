using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Models
{
    public sealed class LoginViewModel
    {
        [Required, EmailAddress, StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public sealed class RegisterViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full name")]
        public string Name { get; set; }

        [Required, EmailAddress, StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Contact details")]
        public string ContactDetails { get; set; }

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, Compare(nameof(Password)), DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } = "Participant";

        [DataType(DataType.Password)]
        [Display(Name = "Organiser registration code")]
        public string OrganiserCode { get; set; }
    }

    public sealed class EventFormViewModel
    {
        public int EventId { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Event name")]
        public string EventName { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; }

        [Required, DataType(DataType.DateTime)]
        [Display(Name = "Event date and time")]
        public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

        [Required, StringLength(200)]
        public string Location { get; set; }

        [Range(typeof(decimal), "0.01", "1000")]
        [Display(Name = "Distance (km)")]
        public decimal Distance { get; set; }

        [Required]
        [RegularExpression("Run|Walk|Cycle", ErrorMessage = "Choose Run, Walk or Cycle.")]
        [Display(Name = "Event type")]
        public string EventType { get; set; } = "Run";

        public string BannerImageUrl { get; set; }

        [Display(Name = "Banner image (JPG, PNG or WEBP; maximum 5 MB)")]
        public IFormFile BannerImage { get; set; }
    }

    public sealed class ProfileFormViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full name")]
        public string Name { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Contact details")]
        public string ContactDetails { get; set; }

        public string Email { get; set; }
        public string Role { get; set; }
        public string ProfilePictureUrl { get; set; }

        [Display(Name = "Profile picture (JPG, PNG or WEBP; maximum 5 MB)")]
        public IFormFile ProfilePicture { get; set; }
    }
}
