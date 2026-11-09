using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos
{//start of namespace

    //data an organiser sends to capture a result
    public class ResultRequest
    {//start of class

        //the participant the result belongs to (they must be enrolled in the event)
        [Range(1, int.MaxValue)]
        public int ParticipantId { get; set; }

        //finish time as hh:mm:ss, for example 01:25:30
        [Required]
        public string FinishTime { get; set; }

        //finishing position, starting at 1
        [Range(1, int.MaxValue)]
        public int PositionNumber { get; set; }

    }//end of class

    //data an organiser sends to correct a result
    public class UpdateResultRequest
    {//start of class

        //finish time as hh:mm:ss, for example 01:25:30
        [Required]
        public string FinishTime { get; set; }

        //finishing position, starting at 1
        [Range(1, int.MaxValue)]
        public int PositionNumber { get; set; }

    }//end of class

    //result data sent back to the user
    public class ResultResponse
    {//start of class

        public int ResultsId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; }

        //finish time as hh:mm:ss
        public string FinishTime { get; set; }

        public int PositionNumber { get; set; }
        public int TotalParticipants { get; set; }

    }//end of class

}//end of namespace
