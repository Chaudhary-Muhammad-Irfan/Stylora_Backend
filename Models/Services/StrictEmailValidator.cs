using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace WebApplication1.Models.Services
{
    public class StrictEmailValidator
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<StrictEmailValidator> _logger;

        public StrictEmailValidator(HttpClient httpClient, IConfiguration config, ILogger<StrictEmailValidator> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateEmailAsync(string email)
        {
            try
            {
                // 1. Basic format check
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return ValidationResult.Invalid("Invalid email format");

                // 2. Check AbstractAPI
                var apiKey = _config["AbstractApi:Key"];
                var response = await _httpClient.GetAsync(
                    $"https://emailvalidation.abstractapi.com/v1/?api_key={apiKey}&email={email}");

                if (!response.IsSuccessStatusCode)
                    return ValidationResult.Invalid("Validation service unavailable");

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResponse>(content);

                // 3. Comprehensive checks
                if (!result.is_valid_format || !result.is_deliverable || result.is_disposable_email)
                    return ValidationResult.Invalid("Invalid or disposable email address");

                return ValidationResult.Valid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email validation failed");
                return ValidationResult.Invalid("Email validation failed");
            }
        }

        private record ApiResponse(
            bool is_valid_format,
            bool is_deliverable,
            bool is_disposable_email);
    }

    public record ValidationResult(bool IsValid, string Message)
    {
        public static ValidationResult Valid() => new(true, "Valid email");
        public static ValidationResult Invalid(string message) => new(false, message);
    }
}
