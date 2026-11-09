using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{//start of namespace

    //data the user sends to register
    public class RegisterRequest
    {//start of class

        //email address used to log in
        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; }

        //at least 8 characters, it is hashed before it is stored
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string ContactDetails { get; set; }

        //either Organiser or Participant
        [Required]
        public string Role { get; set; }

        //secret code, only needed when registering as an Organiser
        public string OrganiserCode { get; set; }

    }//end of class

    //data the user sends to log in
    public class LoginRequest
    {//start of class

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

    }//end of class

    //data sent back after registering or logging in
    public class AuthResponse
    {//start of class

        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }

    }//end of class

}//end of namespace
