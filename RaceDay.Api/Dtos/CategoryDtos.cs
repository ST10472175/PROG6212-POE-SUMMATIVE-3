using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{//start of namespace

    //data an organiser sends to create or rename a category
    public class CategoryRequest
    {//start of class

        //for example Under 20, Senior, 10km or 21km
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }

    }//end of class

    //category data sent back to the user
    public class CategoryResponse
    {//start of class

        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int EventId { get; set; }

    }//end of class

}//end of namespace
