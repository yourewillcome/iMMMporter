using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

// ImportResult.cs
namespace iMMMporter.Models
{
    public class ImportResult
    {
        public string Subject { get; set; }
        public string Email { get; set; }
        public bool Successful { get; set; }
        public string RecordId { get; set; }
        public string Message { get; set; }
    }
}