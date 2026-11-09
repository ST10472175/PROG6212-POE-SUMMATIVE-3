namespace RaceDay.Mvc.Models
{
    public sealed class RegisterApiRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string ContactDetails { get; set; }
        public string Role { get; set; }
        public string OrganiserCode { get; set; }
    }

    public sealed class LoginApiRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public sealed class UpdateProfileApiRequest
    {
        public string Name { get; set; }
        public string ContactDetails { get; set; }
    }
}
