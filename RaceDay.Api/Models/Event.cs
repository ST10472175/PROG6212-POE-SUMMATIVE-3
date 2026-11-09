using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models
{//start of namespace
    public class Event
    {//start of class

        //database columns are eventId, eventName, description, eventDate,
        //location, distance, eventType and organiserId
        public int EventId { get; set; }

        [Required]
        [MaxLength(150)]
        public string EventName { get; set; }

        //added because the brief says every event needs a description
        [Required]
        [MaxLength(1000)]
        public string Description { get; set; }

        public DateTime EventDate { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; }

        //added because the brief says every event needs a distance (in km)
        public decimal Distance { get; set; }

        public EventType EventType { get; set; }

        // Public URL of the event banner stored in Azure Blob Storage.
        [MaxLength(2048)]
        public string BannerImageUrl { get; set; }

        //(one organiser creates many events | link with ERD)
        public int OrganiserId { get; set; }
        public EventOrganiser Organiser { get; set; }

        //one event has many categories, enrolments and results
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Assignment> Assignments { get; set; } = new List<Assignment>();
        public List<Result> Results { get; set; } = new List<Result>();

    }//end of class
}//end of namespace
