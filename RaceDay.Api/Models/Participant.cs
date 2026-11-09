using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models
{//start of namespace
    public class Participant
    {//start of class

        //database columns are participantId, name and contactDetails
        public int ParticipantId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(200)]
        public string ContactDetails { get; set; }

        // Public URL of the participant profile picture stored in Azure Blob Storage.
        [MaxLength(2048)]
        public string ProfilePictureUrl { get; set; }

        //one participant can have many enrolments and many results
        public List<Assignment> Assignments { get; set; } = new List<Assignment>();
        public List<Result> Results { get; set; } = new List<Result>();

    }//end of class
}//end of namespace
