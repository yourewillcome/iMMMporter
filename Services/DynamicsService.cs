using System.Net.Http.Headers;
using System.Text;
using iMMMporter.Models;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace iMMMporter.Services
{
    public class DynamicsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DynamicsService> _logger;

        public DynamicsService(IConfiguration configuration, HttpClient httpClient, ILogger<DynamicsService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var tenantId = _configuration["Dynamics:TenantId"];
            var clientId = _configuration["Dynamics:ClientId"];
            var clientSecret = _configuration["Dynamics:ClientSecret"];
            var scope = _configuration["Dynamics:Scope"];

            _logger.LogInformation($"Getting access token for tenant {tenantId}, client {clientId}, scope {scope}");

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(scope))
            {
                throw new InvalidOperationException("Dynamics 365 connection settings are missing in configuration.");
            }

            try 
            {
                var app = ConfidentialClientApplicationBuilder
                    .Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();

                _logger.LogInformation("Attempting to acquire token");
                var result = await app.AcquireTokenForClient(new[] { scope })
                    .ExecuteAsync();

                _logger.LogInformation("Token acquired successfully");
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acquiring access token");
                throw;
            }
        }

        public async Task<List<ImportResult>> ImportEmailRecordsAsync(List<EmailRecord> records)
        {
            var results = new List<ImportResult>();
            var orgUrl = _configuration["Dynamics:OrganizationUrl"];
            
            // Update to use the correct entity name - both singular and plural attempts
            var entityLogicalName = "mmm_email"; // The singular logical name
            var entitySetName = "mmm_emails"; // The likely plural form for the API
            
            _logger.LogInformation($"Preparing to import {records.Count} records to {orgUrl}/api/data/v9.2/{entitySetName}");

            if (string.IsNullOrEmpty(orgUrl))
            {
                throw new InvalidOperationException("Dynamics 365 organization URL is missing in configuration.");
            }

            try
            {
                var accessToken = await GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                // Add additional headers that might be needed
                _httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                _httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

                foreach (var record in records)
                {
                    var result = new ImportResult
                    {
                        Subject = record.Subject ?? "No Subject",
                        Email = record.Email ?? "No Email",
                        Successful = false
                    };

                    try
                    {
                        _logger.LogInformation($"Converting record to Dynamics entity: {record.Subject}");
                        var entityData = record.ToDynamicsEntity();
                        var jsonContent = JsonConvert.SerializeObject(entityData);
                        _logger.LogInformation($"Entity JSON: {jsonContent}");
                        
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        
                        _logger.LogInformation($"Sending POST request to Dynamics API");
                        
                        // Try with the plural entity set name first
                        var response = await _httpClient.PostAsync($"{orgUrl}/api/data/v9.2/{entitySetName}", content);
                        
                        // If that fails with a 404, try with the singular logical name
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning($"Entity set {entitySetName} not found, trying with singular name {entityLogicalName}");
                            content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                            response = await _httpClient.PostAsync($"{orgUrl}/api/data/v9.2/{entityLogicalName}", content);
                        }
                        
                        var responseContent = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Response content: {responseContent}");
                        
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation($"Successfully created record in Dynamics");
                            var responseObject = JObject.Parse(responseContent);
                            
                            result.Successful = true;
                            result.RecordId = responseObject.Value<string>("mmm_emailid");
                            result.Message = "Successfully imported";
                        }
                        else
                        {
                            _logger.LogWarning($"Failed to import record: {response.StatusCode} - {responseContent}");
                            result.Message = $"Failed to import: {response.StatusCode} - {responseContent}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error importing record {record.Subject}");
                        result.Message = $"Error during import: {ex.Message}";
                    }

                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportEmailRecordsAsync");
                throw;
            }

            return results;
        }
    }
}