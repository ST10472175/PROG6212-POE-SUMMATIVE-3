namespace RaceDay.Api.Models
{//start of namespace
    public class Result
    {//start of class

        //database columns are resultsId, participantId, eventId, finishTime and positionNumber
        public int ResultsId { get; set; }

        public int ParticipantId { get; set; }
        public Participant Participant { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }

        public TimeSpan FinishTime { get; set; }

        public int PositionNumber { get; set; }

    }//end of class
}//end of namespace
