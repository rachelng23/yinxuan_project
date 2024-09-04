namespace yinxuan_project.Models
{
    public class PricingRule
    {
        public int Id { get; set; }
        public string FacilityDescription { get; set; } // Describes the facility (e.g., "Tennis Court")
        public string UserType { get; set; } // The type of user (e.g., "Member", "Guest")
        public TimeSpan StartTime { get; set; } // The start time for when this rule applies
        public TimeSpan EndTime { get; set; } // The end time for when this rule applies
        public decimal BasePrice { get; set; } // The base price before applying the dynamic factor
        public decimal DynamicFactor { get; set; } // The factor used to adjust the base price
    }
}
