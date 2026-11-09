namespace RaceDay.Mvc.Models
{
    // These DTOs mirror the public Part 2 API contracts. The MVC project deliberately
    // has no Entity Framework reference and does not connect to the database.
    public sealed class AuthApiResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
    }

    public sealed class ProfileApiResponse
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public string ContactDetails { get; set; }
        public string ProfilePictureUrl { get; set; }
    }

    public sealed class EventApiDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public decimal Distance { get; set; }
        public string EventType { get; set; }
        public int OrganiserId { get; set; }
        public string OrganiserName { get; set; }
        public string BannerImageUrl { get; set; }
    }

    public sealed class EventApiRequest
    {
        public string EventName { get; set; }
        public string Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public decimal Distance { get; set; }
        public string EventType { get; set; }
    }

    public sealed class CategoryApiDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int EventId { get; set; }
    }

    public sealed class CategoryApiRequest
    {
        public string CategoryName { get; set; }
    }

    public sealed class EnrolmentApiDto
    {
        public int EntryId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime EntryDate { get; set; }
        public string Status { get; set; }
    }

    public sealed class UpdateEnrolmentStatusApiRequest
    {
        public string Status { get; set; }
    }

    public sealed class EnrolApiRequest
    {
        public int CategoryId { get; set; }
    }

    public sealed class ResultApiDto
    {
        public int ResultsId { get; set; }
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string CategoryName { get; set; }
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; }
        public string FinishTime { get; set; }
        public int PositionNumber { get; set; }
        public int TotalParticipants { get; set; }
    }

    public sealed class ResultApiRequest
    {
        public int ParticipantId { get; set; }
        public string FinishTime { get; set; }
        public int PositionNumber { get; set; }
    }

    public sealed class UpdateResultApiRequest
    {
        public string FinishTime { get; set; }
        public int PositionNumber { get; set; }
    }

    public sealed class EventSummaryViewModel
    {
        public EventApiDto Event { get; set; }
        public int EnrolmentCount { get; set; }
    }

    public sealed class EventResultsViewModel
    {
        public EventApiDto Event { get; set; }
        public List<EnrolmentApiDto> Enrolments { get; set; } = new();
        public List<ResultApiDto> Results { get; set; } = new();
        public string NewFinishTime { get; set; }
        public int NewParticipantId { get; set; }
        public int NewPositionNumber { get; set; } = 1;
    }

    public sealed class ParticipantEventDetailsViewModel
    {
        public EventApiDto Event { get; set; }
        public List<CategoryApiDto> Categories { get; set; } = new();
        public bool AlreadyEnrolled { get; set; }
    }
}
