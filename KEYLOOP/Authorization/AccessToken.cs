using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KEYLOOP.Authorization
{
    public class AccessToken : IAccessToken
    {
        private readonly ILogger<AccessToken> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _tokenUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _keyloopApiBaseUrl;

        public AccessToken(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AccessToken> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _keyloopApiBaseUrl = configuration["Keyloop:BaseUrl"] ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _tokenUrl = $"{_keyloopApiBaseUrl}/oauth/client_credential/accesstoken";
            _clientId = configuration["Keyloop:ClientId"] ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _clientSecret = configuration["Keyloop:ClientSecret"] ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<string> GetAccessToken()
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_keyloopApiBaseUrl);

            var requestBody = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            };

            var response = await httpClient.PostAsync(_tokenUrl, new FormUrlEncodedContent(requestBody));
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync();

            using var document = await JsonDocument.ParseAsync(responseStream);
            
            var accessToken = document.RootElement.GetProperty("access_token").GetString();
            if (accessToken == null) 
            {
                _logger.LogError("Failed to retrieve access token: null value");
                throw new Exception("Failed to retrieve access token: null value.");
            }
            
            
            return accessToken;
        }
    }
}