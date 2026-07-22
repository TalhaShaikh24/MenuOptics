namespace MenuOptics.Models
{
    public class DataTablesRequestDto
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; } = 10;
        public string? SearchValue { get; set; }
        public int OrderColumn { get; set; }
        public string OrderDir { get; set; } = "asc";
    }
}
