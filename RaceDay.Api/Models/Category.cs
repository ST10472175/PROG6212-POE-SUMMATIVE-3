using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Models
{//start of namespace
    public class Category
    {//start of class

        //database columns are categoryId, categoryName and eventId
        public int CategoryId { get; set; }

        //max length to enter name
        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; }

        //each category belongs to one event
        public int EventId { get; set; }
        public Event Event { get; set; }

        public List<Assignment> Assignments { get; set; } = new List<Assignment>();

    }//end of class
}//end of namespace
