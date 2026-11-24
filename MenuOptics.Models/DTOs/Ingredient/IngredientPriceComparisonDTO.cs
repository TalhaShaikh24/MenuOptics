using System;
using System.Collections.Generic;

namespace MenuOptics.Models.DTOs
{
    public class IngredientPriceComparisonDTO
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal OldPrice { get; set; }
        public decimal CurrentPrice { get; set; }

        public decimal PriceDifference => Math.Round(CurrentPrice - OldPrice, 2);

        public decimal PercentageChange
        {
            get
            {
                if (OldPrice <= 0) return 0;
                return Math.Round(Math.Abs((CurrentPrice - OldPrice) / OldPrice) * 100, 2);
            }
        }

        public bool IsPriceIncrease => CurrentPrice > OldPrice;

        public List<string> AffectedMenuItems { get; set; } = new();
    }
}
