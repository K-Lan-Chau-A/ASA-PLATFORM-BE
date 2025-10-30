using System;

namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class MonthlyOrderSummaryResponse
    {
        public int TotalTransactions { get; set; }
        public int SuccessTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal Revenue { get; set; }
    }
}


