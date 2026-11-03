namespace MenuOptics.Models.DTOs
{
    public class GetActivityLogDTO
    {
        public int LogId { get; set; }
        public long UserId { get; set; }
        public string? RestaurantName { get; set; }
        public required string UserName { get; set; }
        public required string UserEmail { get; set; }
        public required string RequestType { get; set; }
        public required string Payload { get; set; }
        public required string Response { get; set; }
        public required string LogLevel { get; set; }
        public string? IpAddress { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
