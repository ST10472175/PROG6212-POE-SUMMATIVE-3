namespace RaceDay.Mvc.Services
{
    public sealed class RaceDayApiException : Exception
    {
        public int StatusCode { get; }

        public RaceDayApiException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
