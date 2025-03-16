using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using iMMMporter.Models;
using Microsoft.AspNetCore.Http;

namespace iMMMporter.Services
{
    public class CsvParserService
    {
        private readonly ILogger<CsvParserService> _logger;

        public CsvParserService(ILogger<CsvParserService> logger)
        {
            _logger = logger;
        }

        public async Task<List<EmailRecord>> ParseCsvFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            _logger.LogInformation($"Starting to parse CSV file: {file.FileName}, Size: {file.Length} bytes");
            
            var result = new List<EmailRecord>();

            try
            {
                using (var stream = file.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    _logger.LogInformation("Setting up CSV configuration");
                    
                    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        HeaderValidated = null, // Ignore header validation errors
                        MissingFieldFound = null, // Ignore missing fields
                        BadDataFound = null, // Continue on bad data
                        IgnoreBlankLines = true,
                        TrimOptions = TrimOptions.Trim,
                        HasHeaderRecord = true,
                    };

                    _logger.LogInformation("Creating CSV reader");
                    
                    using (var csv = new CsvReader(reader, config))
                    {
                        // Register custom mapping to handle potential mismatches between CSV headers and property names
                        _logger.LogInformation("Registering class map");
                        csv.Context.RegisterClassMap<EmailRecordMap>();
                        
                        _logger.LogInformation("Reading records from CSV");
                        result = csv.GetRecords<EmailRecord>().ToList();
                        
                        _logger.LogInformation($"Successfully read {result.Count} records from CSV");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing CSV file");
                throw new Exception($"Failed to parse CSV file: {ex.Message}", ex);
            }

            return result;
        }
    }

    public class EmailRecordMap : ClassMap<EmailRecord>
    {
        public EmailRecordMap()
        {
            Map(m => m.Content).Name("Content").Optional();
            Map(m => m.Email).Name("Email").Optional();
            Map(m => m.Recipients).Name("Recipients").Optional();
            Map(m => m.Sender).Name("Sender").Optional();
            Map(m => m.SentDate).Name("Sent Date").Optional();
            Map(m => m.Subject).Name("Subject").Optional();
        }
    }
}