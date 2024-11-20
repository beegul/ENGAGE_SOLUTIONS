using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using KEYLOOP.Authorization;
using KEYLOOP.Entities.Brands;
using KEYLOOP.Entities.Orders;
using KEYLOOP.Entities.Parts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KEYLOOP.Controllers
{
    /// <summary>
    /// Controller for managing parts.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PartsController : ControllerBase
    {
        private readonly ILogger<PartsController> _logger;
        private readonly string _keyloopApiBaseUrl;
        private readonly string _enterpriseId;
        private readonly string _storeId;
        private readonly KeyloopApiClient _keyloopApiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartsController"/> class.
        /// </summary>
        /// <param name="keyloopApiClient">The Keyloop API client.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configuration">The configuration.</param>
        public PartsController(KeyloopApiClient keyloopApiClient, ILogger<PartsController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _keyloopApiClient = keyloopApiClient;
            
            // Retrieve configuration values or throw exceptions if missing
            _keyloopApiBaseUrl = configuration["Keyloop:BaseUrl"] ?? throw new ArgumentNullException(nameof(configuration));
            _enterpriseId = configuration["Keyloop:EnterpriseId"] ?? throw new ArgumentNullException(nameof(configuration));
            _storeId = configuration["Keyloop:StoreId"] ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Searches for parts based on brand code and part code.
        /// </summary>
        /// <param name="brandCode">The brand code.</param>
        /// <param name="partCode">The part code.</param>
        /// <returns>An IActionResult containing the search results or an error response.</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchParts([Required]string brandCode, [Required]string partCode)
        {
            // Check if the model state is valid (validates input using data annotations)
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                // Construct the URL for the parts search endpoint
                var url = new Uri(new Uri(_keyloopApiBaseUrl), $"/{_enterpriseId}/{_storeId}/v1/parts?brandCode={brandCode}&partCode={partCode}");
                
                // Send a GET request to the Keyloop API
                var response = await _keyloopApiClient.Client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response into a PartResponse object
                    var partResponse = await response.Content.ReadFromJsonAsync<PartResponse>();

                    if (partResponse?.Parts != null)
                    {
                        // Fetch price and availability for each part
                        foreach (var part in partResponse.Parts)
                        {
                            await GetPriceAndAvailability(part);
                        }
                    }
                    else
                    {
                        _logger.LogError("Parts list is null in the response");
                    }

                    return Ok(partResponse);
                }
                
                // Handle different status codes from the Keyloop API response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        return NotFound("No parts found.");
                    case HttpStatusCode.Unauthorized:
                        return Unauthorized();
                    default:
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Keyloop API request failed with status code {StatusCode} and error: {ErrorContent}", response.StatusCode, errorContent);
                        return StatusCode((int)response.StatusCode, "Keyloop API request failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching for parts");
                return StatusCode(500, "An error occurred.");
            }
        }
        
        /// <summary>
        /// Gets a list of brands.
        /// </summary>
        /// <returns>An IActionResult containing the list of brands or an error response.</returns>
        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
        {
            try
            {
                // Construct the URL for the parts/brands endpoint
                var url = new Uri(new Uri(_keyloopApiBaseUrl), $"/{_enterpriseId}/{_storeId}/v1/parts/brands");
                
                // Send a GET request to the Keyloop API
                var response = await _keyloopApiClient.Client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Configure JsonSerializerOptions for case-insensitive deserialization
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    // Deserialize the response content into a BrandResponse object
                    var brandResponse = JsonSerializer.Deserialize<BrandResponse>(responseContent, options); 
                    return Ok(brandResponse);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Keyloop API request failed: {errorContent}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Places an order for parts.
        /// </summary>
        /// <param name="orderRequest">The order request.</param>
        /// <returns>An IActionResult containing the order response or an error response.</returns>
        [HttpPost("orders")]
        public async Task<IActionResult> PlaceOrder([FromBody] PartsOrderRequest orderRequest)
        {
            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }
            
            try
            {
                // Create a new PartsOrderRequest object with the necessary data
                var partsOrderRequest = new PartsOrderRequest
                {
                    CustomerId = orderRequest.CustomerId,
                    CompanyId = orderRequest.CompanyId,
                    OrderContact = orderRequest.OrderContact,
                };
                
                // Serialize the partsOrderRequest object to JSON
                var jsonPayload = JsonSerializer.Serialize(partsOrderRequest);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // Construct the URL for the parts-orders endpoint
                var url = new Uri(new Uri(_keyloopApiBaseUrl), $"{_enterpriseId}/{_storeId}/v1/parts-orders");
                
                // Send a POST request to the Keyloop API
                var response = await _keyloopApiClient.Client.PostAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    // Deserialize the response content into a PartsOrderResponse object
                    var orderResponse = JsonSerializer.Deserialize<PartsOrderResponse>(responseContent);
                    return Ok(orderResponse);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Keyloop API request failed: {errorContent}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the price and availability for a part.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private async Task GetPriceAndAvailability(Part part)
        {
            // Construct the URL for the price-availability endpoint
            var priceAvailabilityUrl = new Uri(new Uri(_keyloopApiBaseUrl), $"/{_enterpriseId}/{_storeId}/v1/parts/{part.PartId}/price-availability");
            
            // Send a GET request to the Keyloop API
            var priceAvailabilityResponse = await _keyloopApiClient.Client.GetAsync(priceAvailabilityUrl);

            if (priceAvailabilityResponse.IsSuccessStatusCode)
            {
                var priceAvailabilityContent = await priceAvailabilityResponse.Content.ReadFromJsonAsync<PriceAvailability>();
                if (priceAvailabilityContent?.ListPrice?.NetValue != null)
                {
                    part.Price = priceAvailabilityContent.ListPrice.NetValue.Value;
                    part.Availability = priceAvailabilityContent.AvailableStock;
                }
                else
                {
                    _logger.LogError("Price or NetValue is null for part {PartId}", part.PartId);
                }
            }
            else
            {
                var priceAvailabilityErrorContent = await priceAvailabilityResponse.Content.ReadAsStringAsync();
                _logger.LogError("Error fetching price and availability for part {PartId}: {ErrorContent}", part.PartId, priceAvailabilityErrorContent);
            }
        }
    }
}