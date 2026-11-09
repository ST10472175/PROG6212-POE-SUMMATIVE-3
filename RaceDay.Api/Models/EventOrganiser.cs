using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models
{//start of namespace
    public class EventOrganiser
    {//start of class

        //database columns are organiserId, name and contactDetails
        public int OrganiserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(200)]
        public string ContactDetails { get; set; }

        //one organiser creates many events
        public List<Event> Events { get; set; } = new List<Event>();

    }//end of class
}//end of namespace
