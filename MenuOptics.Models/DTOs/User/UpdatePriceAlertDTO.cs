namespace MenuOptics.Models.DTOs
{
    public class UpdatePriceAlertDTO
    {
        public decimal PriceAlertThreshold { get; set; } = 10.0m;
        public bool IsPriceAlertOn { get; set; } = true;
    }
}
