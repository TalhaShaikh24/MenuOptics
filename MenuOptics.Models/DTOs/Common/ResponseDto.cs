namespace MenuOptics.Models
{
    public class ResponseDto
    {
        public int Status { get; set; }
        public string? ResponseMsg { get; set; }
        public object? Data { get; set; }
    }
}
