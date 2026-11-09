using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models
{//start of namespace
    public class Assignment
    {//start of class

        //this is the enrolment table (called Assignment in the Part 1 ERD)
        //database columns are entryId, eventId, participantId, categoryId, entryDate and status
        public int EntryId { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }

        public int ParticipantId { get; set; }
        public Participant Participant { get; set; }

        //added because the brief says the enrolment must record the chosen category
        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public DateTime EntryDate { get; set; }

        // New enrolments wait for organiser confirmation; participants can see the current state.
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

    }//end of class
}//end of namespace
