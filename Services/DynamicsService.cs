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

        public DynamicsService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var tenantId = _configuration["Dynamics:TenantId"];
            var clientId = _configuration["Dynamics:ClientId"];
            var clientSecret = _configuration["Dynamics:ClientSecret"];
            var scope = _configuration["Dynamics:Scope"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(scope))
            {
                throw new InvalidOperationException("Dynamics 365 connection settings are missing in configuration.");
            }

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            var result = await app.AcquireTokenForClient(new[] { scope })
                .ExecuteAsync();

            return result.AccessToken;
        }

        public async Task<List<ImportResult>> ImportEmailRecordsAsync(List<EmailRecord> records)
        {
            var results = new List<ImportResult>();
            var orgUrl = _configuration["Dynamics:OrganizationUrl"];
            var entitySetName = "mmm_contents"; // The plural name of your entity

            if (string.IsNullOrEmpty(orgUrl))
            {
                throw new InvalidOperationException("Dynamics 365 organization URL is missing in configuration.");
            }

            var accessToken = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            foreach (var record in records)
            {
                var result = new ImportResult
                {
                    Subject = record.Subject,
                    Email = record.Email,
                    Successful = false
                };

                try
                {
                    var jsonContent = JsonConvert.SerializeObject(record.ToDynamicsEntity());
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    var response = await _httpClient.PostAsync($"{orgUrl}/api/data/v9.2/{entitySetName}", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        var responseObject = JObject.Parse(responseContent);
                        
                        result.Successful = true;
                        result.RecordId = responseObject.Value<string>("mmm_contentid");
                        result.Message = "Successfully imported";
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        result.Message = $"Failed to import: {response.StatusCode} - {errorContent}";
                    }
                }
                catch (Exception ex)
                {
                    result.Message = $"Error during import: {ex.Message}";
                }

                results.Add(result);
            }

            return results;
        }
    }
}