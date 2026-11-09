using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos
{//start of namespace

    //data an organiser sends to create or update an event
    public class EventRequest
    {//start of class

        [Required]
        [StringLength(150)]
        public string EventName { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        //date and time of the event, for example 2026-11-20T08:00:00
        public DateTime EventDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; }

        //distance in kilometres
        [Range(0.01, 1000)]
        public decimal Distance { get; set; }

        //Run, Walk or Cycle
        public EventType EventType { get; set; }

    }//end of class

    //event data sent back to the user
    public class EventResponse
    {//start of class

        public int EventId { get; set; }
        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public decimal Distance { get; set; }
        public EventType EventType { get; set; }
        public int OrganiserId { get; set; }
        public string OrganiserName { get; set; }
        public string BannerImageUrl { get; set; }

    }//end of class

}//end of namespace
