using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;
using WebApplication1.Models.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("EmailValidationPolicy")]  
    public class ValidateEmailController : ControllerBase
    {
        private readonly EmailValidationService _validationService;
        private readonly ILogger<ValidateEmailController> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        public ValidateEmailController(EmailValidationService validationService, ILogger<ValidateEmailController> logger , HttpClient httpClient,
            IConfiguration config)
        {
            _validationService = validationService;
            _logger = logger;
            _httpClient = httpClient;
            _config = config;
        }
        public class EmailValidationResult
        {
            public bool IsValid { get; set; }
            public bool IsDisposable { get; set; }
            public string Message { get; set; }
        }

        public async Task<EmailValidationResult> GetFullValidationResult(string email)
        {
            try
            {
                // 1. Null/empty check
                if (string.IsNullOrWhiteSpace(email))
                    return InvalidResult("Email cannot be empty");

                // 2. Basic format check
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    return InvalidResult("Invalid email format");

                // 3. API validation
                var apiKey = _config["AbstractApi:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    return InvalidResult("Validation service not configured");

                var apiUrl = $"{_config["AbstractApi:EmailValidationUrl"]}?api_key={apiKey}&email={email}";

                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                    return InvalidResult("Validation service error");

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<AbstractApiResponse>(content);

                // 4. Comprehensive validation
                if (result == null)
                    return InvalidResult("Invalid validation response");

                if (!result.IsValidFormat)
                    return InvalidResult("Invalid email format");

                if (result.IsDisposableEmail)
                    return InvalidResult("Disposable emails not allowed");

                if (!result.IsDeliverable)
                    return InvalidResult("Email is not deliverable");

                return new EmailValidationResult
                {
                    IsValid = true,
                    Message = "Valid email"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Email validation failed for {email}");
                return InvalidResult("Email validation service unavailable");
            }
        }

        private EmailValidationResult InvalidResult(string message) =>
            new EmailValidationResult { IsValid = false, Message = message };
        private class AbstractApiResponse
        {
            [JsonProperty("is_valid_format")]
            public bool IsValidFormat { get; set; }

            [JsonProperty("is_deliverable")]
            public bool IsDeliverable { get; set; }

            [JsonProperty("is_disposable_email")]
            public bool IsDisposableEmail { get; set; }

            [JsonProperty("is_free_email")]
            public bool IsFreeEmail { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> ValidateEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest();

            try
            {
                var isValid = await _validationService.IsValidEmail(email);
                _logger.LogInformation($"Email validation result for {email}: {isValid}");
                return Ok(new { isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email validation failed for {Email}", email);
                return StatusCode(500, "Error validating email");
            }
        }
       
    }
}
