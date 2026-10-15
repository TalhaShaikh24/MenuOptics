namespace MenuOptics.Models
{
    public class EventOnlyItemDto
    {
        public int EventOnlyItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DefaultCost { get; set; }
        public decimal Cost
        {
            get => DefaultCost;
            set => DefaultCost = value;
        }
        public decimal SellingPrice { get; set; }
        public bool IsActive { get; set; }
    }
}
