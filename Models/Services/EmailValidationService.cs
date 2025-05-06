using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WebApplication1.Models.Services
{
    public class EmailValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<EmailValidationService> _logger;

        public EmailValidationService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<EmailValidationService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public class EmailValidationResult
        {
            public bool IsValid { get; set; }
            public bool IsDisposable { get; set; }
            public bool IsDeliverable { get; set; }
            public string Message { get; set; }
        }

        public async Task<EmailValidationResult> GetFullValidationResult(string email)
        {
            try
            {
                // Basic format check
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return new EmailValidationResult
                    {
                        IsValid = false,
                        Message = "Invalid email format"
                    };

                // API validation
                var apiKey = _config["AbstractApi:ApiKey"];
                var apiUrl = $"{_config["AbstractApi:EmailValidationUrl"]}?api_key={apiKey}&email={email}";

                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AbstractApiResponse>(content);

                return new EmailValidationResult
                {
                    IsValid = result.IsValidFormat && result.IsDeliverable && !result.IsDisposableEmail,
                    IsDisposable = result.IsDisposableEmail,
                    IsDeliverable = result.IsDeliverable,
                    Message = result.IsDisposableEmail ? "Disposable email not allowed" :
                              !result.IsDeliverable ? "Email is not deliverable" :
                              !result.IsValidFormat ? "Invalid email format" : "Valid email"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email validation failed");
                return new EmailValidationResult
                {
                    IsValid = false,
                    Message = "Validation service unavailable"
                };
            }
        }

        // Simplified version if you still need it
        public async Task<bool> IsValidEmail(string email)
        {
            var result = await GetFullValidationResult(email);
            return result.IsValid;
        }

        private class AbstractApiResponse
        {
            [JsonProperty("is_valid_format")]
            public bool IsValidFormat { get; set; }

            [JsonProperty("is_deliverable")]
            public bool IsDeliverable { get; set; }

            [JsonProperty("is_disposable_email")]
            public bool IsDisposableEmail { get; set; }
        }
        public async Task<(bool isValid, string errorMessage)> StrictEmailValidation(string email)
        {
            try
            {
                // 1. Basic null/empty check
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email cannot be empty");

                // 2. Strict format validation
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return (false, "Invalid email format");

                // 3. API validation with timeout
                var apiKey = _config["AbstractApi:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    return (false, "Validation service error");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var apiUrl = $"{_config["AbstractApi:EmailValidationUrl"]}?api_key={apiKey}&email={email}";

                var response = await _httpClient.GetAsync(apiUrl, cts.Token);

                if (!response.IsSuccessStatusCode)
                    return (false, "Email validation service unavailable");

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AbstractApiResponse>(content)
                    ?? throw new InvalidOperationException("Invalid API response");

                // 4. Comprehensive checks
                if (!result.IsValidFormat)
                    return (false, "Invalid email format");
                if (result.IsDisposableEmail)
                    return (false, "Disposable emails not allowed");
                if (!result.IsDeliverable)
                    return (false, "Email is not deliverable");

                return (true, "Valid email");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Email validation timed out for {Email}", email);
                return (false, "Validation timed out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email validation failed for {Email}", email);
                return (false, "Email validation failed");
            }
        }
    }
    }