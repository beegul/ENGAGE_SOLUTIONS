using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using KEYLOOP.Authorization;
using KEYLOOP.Entities.Customer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KEYLOOP.Controllers
{
    /// <summary>
    /// Controller for managing customers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ILogger<CustomerController> _logger;
        private readonly string _keyloopApiBaseUrl;
        private readonly string _enterpriseId;
        private readonly string _storeId;
        private readonly KeyloopApiClient _keyloopApiClient;

        
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomerController"/> class.
        /// </summary>
        /// <param name="keyloopApiClient">The Keyloop API client.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        public CustomerController(KeyloopApiClient keyloopApiClient, ILogger<CustomerController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _keyloopApiClient = keyloopApiClient;
            
            // Retrieve configuration values or throw exceptions if missing
            _keyloopApiBaseUrl = configuration["Keyloop:BaseUrl"] ?? throw new ArgumentNullException(nameof(configuration));
            _enterpriseId = configuration["Keyloop:EnterpriseId"] ?? throw new ArgumentNullException(nameof(configuration));
            _storeId = configuration["Keyloop:StoreId"] ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets a customer by ID.
        /// </summary>
        /// <param name="customerId">The customer ID.</param>
        /// <returns>An IActionResult containing the customer data or an error response.</returns>
        [HttpGet("{customerId}")]
        public async Task<IActionResult> GetCustomer([Required]string customerId)
        {
            try
            {
                // Construct the URL for the customer endpoint
                var url = new Uri(new Uri(_keyloopApiBaseUrl), $"/{_enterpriseId}/{_storeId}/v3/customers/{customerId}");
                
                // Send a GET request to the Keyloop API
                var response = await _keyloopApiClient.Client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStreamAsync();
                    
                    // Deserialize the response into a CustomerResponse object
                    var customerResponse = await JsonSerializer.DeserializeAsync<CustomerResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });

                    if (customerResponse != null)
                    {
                        return Ok(customerResponse); 
                    }

                    _logger.LogError("Failed to deserialize customer response");
                    return StatusCode(500, "Failed to process customer data.");
                }
                
                // Handle different status codes from the Keyloop API response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        _logger.LogInformation("Customer not found for ID: {CustomerId}", customerId);
                        return NotFound();
                    case HttpStatusCode.Unauthorized:
                        return Unauthorized();
                    default:
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Keyloop API request failed with status code {StatusCode} and error: {ErrorContent}", response.StatusCode, errorContent);
                        return StatusCode((int)response.StatusCode, "Keyloop API request failed.");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "An error occurred while deserializing customer data");
                return StatusCode(500, "An error occurred while processing the response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching customer data");
                return StatusCode(500, "An error occurred.");
            }
        }
    }
}