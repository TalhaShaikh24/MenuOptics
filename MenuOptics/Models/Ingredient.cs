using System.ComponentModel.DataAnnotations;

namespace MenuOptics.Models
{
    public class Ingredient
    {
        public int IngredientId { get; set; }
        public int UserId { get; set; }
        public int VendorId { get; set; }
        public string? VendorProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; } // e.g., kg, liters, units
        public decimal Size { get; set; } 
        public int? PackSize { get; set; } 
        public DateTime CreatedOn { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public string? FullName { get; set; }
        public decimal? Stock { get; set; }
        public int TotalCount { get; set; }
    }


    public class RestaurantPriceEntry
    {
        public int RestaurantId { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }

    public class VendorIngredientRequest
    {
        public string Name { get; set; }
        public string Unit { get; set; }

        public decimal? Weight { get; set; }
        public int? PackSize { get; set; }
        public decimal BasePrice { get; set; }
        public string? VendorProductId { get; set; } //Optional
        public decimal Size { get; set; }

        public List<RestaurantPriceEntry>? RestaurantPrices { get; set; }
    }

    public class IngredientResponse
    {
        public int IngredientId { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public string FullName { get; set; }
        public int? VendorId { get; set; }
        public int? UserId { get; set; }
        public int? AssignedRestaurantId { get; set; }
    }

    public class UpdateIngredientPriceRequest
    {
        public int IngredientId { get; set; }

        public decimal? PackSize { get; set; }
        public decimal? Weight { get; set; }
        public decimal BasePrice { get; set; }
        public List<RestaurantPriceEntry>? RestaurantPrices { get; set; }
    }


    public class LinkVendorRequest
    {
        public int? Id { get; set; }
        public int? VendorId { get; set; }

        public int? VendorIngredientId { get; set; }

        public int? RestaurantId { get; set; }

        public decimal Price { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public int? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public int? ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;

        public Ingredient? VendorIngredient { get; set; }

        public User? Restaurant { get; set; }
    }


    // DTO for patching ingredient name, unit, and/or status
    public class UpdateIngredientDetailsRequest
    {
        public int IngredientId { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public decimal? Size { get; set; }
        public bool? IsActive { get; set; }
    }

    // ─── jQuery DataTables Server-Side Protocol ───────────────────────────────

    /// <summary>
    /// Captures the query parameters sent by jQuery DataTables in server-side mode.
    /// Parameter names match what DataTables sends over GET/POST.
    /// </summary>
    public class DataTablesRequest
    {
        /// <summary>Draw counter — must be echoed back unchanged.</summary>
        public int Draw { get; set; }

        /// <summary>Row offset (0-based) for paging.</summary>
        public int Start { get; set; }

        /// <summary>Number of rows per page (-1 means all).</summary>
        public int Length { get; set; } = 10;

        /// <summary>Global search string typed by the user.</summary>
        public string? SearchValue { get; set; }

        /// <summary>Zero-based column index to sort by.</summary>
        public int OrderColumn { get; set; }

        /// <summary>"asc" or "desc".</summary>
        public string OrderDir { get; set; } = "asc";
    }

    /// <summary>
    /// Generic response wrapper for jQuery DataTables server-side responses.
    /// </summary>
    public class DataTablesResponse<T>
    {
        /// <summary>Echoed draw counter from the request.</summary>
        public int Draw { get; set; }

        /// <summary>Total records in the table (unfiltered).</summary>
        public int RecordsTotal { get; set; }

        /// <summary>Total records after applying search filter.</summary>
        public int RecordsFiltered { get; set; }

        /// <summary>The paged slice of data for this request.</summary>
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    }

}