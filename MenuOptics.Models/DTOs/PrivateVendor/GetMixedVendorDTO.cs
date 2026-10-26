namespace MenuOptics.Models.DTOs
{
    public class GetMixedVendorDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsActive { get; set; }
    }
}
