using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models
{//start of namespace
    public class AppUser
    {//start of class
        //each of these are defined using entity models in part 1
        //login account table (added to the Part 1 ERD so that
        //the login details and role are stored in one place)
        //database columns are userId, email, passwordHash, role, organiserId and participantId
        public int UserId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Email { get; set; }

        // The password is hashed with BCrypt; the original password is never stored.
// A length limit helps keep the stored hash within a defined size.
[Required]
[MaxLength(100)]
public string PasswordHash { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; }

        //an organiser account links to one EventOrganiser row
        public int? OrganiserId { get; set; }
        public EventOrganiser Organiser { get; set; }

        //a participant account links to one Participant row
        public int? ParticipantId { get; set; }
        public Participant Participant { get; set; }

    }//end of class
}//end of namespace
