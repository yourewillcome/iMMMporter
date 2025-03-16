using iMMMporter.Models;
using iMMMporter.Services;
using Microsoft.AspNetCore.Mvc;

namespace iMMMporter.Controllers
{
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

        public IActionResult Index()
        {
            return View(new ImportSettings());
        }

        [HttpPost]
        public async Task<IActionResult> Index(ImportSettings settings)
        {
            if (!ModelState.IsValid)
            {
                return View(settings);
            }

            try
            {
                var records = await _csvParserService.ParseCsvFileAsync(settings.CsvFile!);
                
                if (settings.PreviewOnly)
                {
                    ViewBag.PreviewRecords = records;
                    return View(settings);
                }
                
                var results = await _dynamicsService.ImportEmailRecordsAsync(records);
                
                return View("Result", results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import");
                ModelState.AddModelError("", $"Error processing file: {ex.Message}");
                return View(settings);
            }
        }
    }
}