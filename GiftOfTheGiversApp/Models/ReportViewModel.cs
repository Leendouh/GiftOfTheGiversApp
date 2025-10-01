using System.ComponentModel.DataAnnotations;

namespace GiftOfTheGiversApp.Models
{
    public class ReportViewModel
    {
        [Display(Name = "Report Type")]
        public string ReportType { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Disaster Type")]
        public string DisasterType { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; }
    }

    public class ReportResultViewModel
    {
        public string ReportTitle { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<ReportData> Data { get; set; } = new List<ReportData>();
        public ReportSummary Summary { get; set; }
    }

    public class ReportData
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }

    public class ReportSummary
    {
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string TimePeriod { get; set; }
    }
}