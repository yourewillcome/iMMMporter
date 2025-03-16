// ImportSettings.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace iMMMporter.Models
{
    public class ImportSettings
    {
        [Required(ErrorMessage = "Please select a CSV file")]
        [Display(Name = "CSV File")]
        public IFormFile CsvFile { get; set; }
        
        [Display(Name = "Skip First Row (Headers)")]
        public bool SkipFirstRow { get; set; } = true;
        
        [Display(Name = "Preview Only (No Import)")]
        public bool PreviewOnly { get; set; } = false;
    }
}