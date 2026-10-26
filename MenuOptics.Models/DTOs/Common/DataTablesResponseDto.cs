using System.Collections.Generic;
using System.Linq;

namespace MenuOptics.Models
{
    public class DataTablesResponseDTO<T>
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public int TotalSuccessCount { get; set; }
        public int TotalErrorCount { get; set; }
        public int TotalWarningCount { get; set; }
        public int TotalPendingCount { get => TotalWarningCount; set => TotalWarningCount = value; }
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    }
}
