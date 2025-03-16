using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using iMMMporter.Models;
using Microsoft.AspNetCore.Http;

namespace iMMMporter.Services
{
    public class CsvParserService
    {
        public async Task<List<EmailRecord>> ParseCsvFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            var result = new List<EmailRecord>();

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null, // Ignore header validation
                    MissingFieldFound = null // Ignore missing fields
                };

                using (var csv = new CsvReader(reader, config))
                {
                    // Register custom mapping to handle potential mismatches between CSV headers and property names
                    csv.Context.RegisterClassMap<EmailRecordMap>();
                    
                    result = csv.GetRecords<EmailRecord>().ToList();
                }
            }

            return result;
        }
    }

    public class EmailRecordMap : ClassMap<EmailRecord>
    {
        public EmailRecordMap()
        {
            Map(m => m.Content).Name("Content");
            Map(m => m.Email).Name("Email");
            Map(m => m.Recipients).Name("Recipients");
            Map(m => m.Sender).Name("Sender");
            Map(m => m.SentDate).Name("Sent Date");
            Map(m => m.Subject).Name("Subject");
        }
    }
}