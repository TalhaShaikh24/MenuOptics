namespace MenuOptics.Models
{
    public class EventOnlyItemDto
    {
        public int EventOnlyItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DefaultCost { get; set; }
        public bool IsActive { get; set; }
    }
}
