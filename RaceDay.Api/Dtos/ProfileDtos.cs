using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{//start of namespace

    //profile data sent back to the user
    public class ProfileResponse
    {//start of class

        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public string ContactDetails { get; set; }
        public string ProfilePictureUrl { get; set; }

    }//end of class

    //data the user sends to update their profile
    public class UpdateProfileRequest
    {//start of class

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string ContactDetails { get; set; }

    }//end of class

}//end of namespace
