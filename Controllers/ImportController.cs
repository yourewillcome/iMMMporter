using iMMMporter.Models;
using iMMMporter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace iMMMporter.Controllers
{
    [AllowAnonymous]
    public class ImportController : Controller
    {
        private readonly ILogger<ImportController> _logger;
        private readonly CsvParserService _csvParserService;
        private readonly DynamicsService _dynamicsService;

        public ImportController(
            ILogger<ImportController> logger,
            CsvParserService csvParserService,
            DynamicsService dynamicsService)
        {
            _logger = logger;
            _csvParserService = csvParserService;
            _dynamicsService = dynamicsService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ImportSettings { SkipFirstRow = true, PreviewOnly = false });
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> Index([FromForm] ImportSettings settings)
        {
            _logger.LogInformation("POST request received for Import/Index");
            
            try
            {
                if (settings == null)
                {
                    _logger.LogWarning("Import settings are null");
                    ModelState.AddModelError("", "No form data received.");
                    return View(new ImportSettings());
                }

                _logger.LogInformation($"Received settings: SkipFirstRow={settings.SkipFirstRow}, PreviewOnly={settings.PreviewOnly}");
                
                if (settings.CsvFile == null || settings.CsvFile.Length == 0)
                {
                    _logger.LogWarning("No CSV file provided");
                    ModelState.AddModelError("CsvFile", "Please select a CSV file to upload.");
                    return View(settings);
                }

                _logger.LogInformation($"Processing file: {settings.CsvFile.FileName}, Size: {settings.CsvFile.Length} bytes");
                
                // Use a try-catch specifically for the CSV parsing to handle format issues
                List<EmailRecord> records;
                try
                {
                    records = await _csvParserService.ParseCsvFileAsync(settings.CsvFile);
                    _logger.LogInformation($"Successfully parsed {records.Count} records from CSV");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing CSV file");
                    ModelState.AddModelError("", $"Error parsing CSV file: {ex.Message}");
                    return View(settings);
                }
                
                if (settings.PreviewOnly)
                {
                    _logger.LogInformation("Preview mode enabled, returning records for preview");
                    ViewBag.PreviewRecords = records;
                    return View(settings);
                }
                
                // For testing/debugging, let's just return success without actual Dynamics import
                _logger.LogInformation("Import mode enabled, proceeding with Dynamics import");
                
                try
                {
                    // If we want to skip actual import for testing
                    if (false) // Change to true for bypass
                    {
                        var mockResults = records.Select(r => new ImportResult 
                        { 
                            Subject = r.Subject, 
                            Email = r.Email, 
                            Successful = true, 
                            RecordId = "mock-id-" + Guid.NewGuid().ToString().Substring(0, 8),
                            Message = "Mock import successful" 
                        }).ToList();
                        
                        return View("Result", mockResults);
                    }
                    
                    // Actual import to Dynamics
                    var results = await _dynamicsService.ImportEmailRecordsAsync(records);
                    return View("Result", results);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Dynamics import");
                    ModelState.AddModelError("", $"Error importing to Dynamics 365: {ex.Message}");
                    return View(settings);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in Import/Index");
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                return View(new ImportSettings());
            }
        }
    }
}