namespace ASA_PLATFORM_SERVICE.DTOs.Response
{
    public class DashboardStatsResponse
    {
        public PeriodStats Today { get; set; }
        public PeriodStats Week { get; set; }
        public PeriodStats Month { get; set; }
    }

    public class PeriodStats
    {
        public int NewShops { get; set; }
        public int ActiveShops { get; set; }
        public decimal Revenue { get; set; }
        public int PremiumSold { get; set; }
    }
}

