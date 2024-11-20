using System.Net.Http.Headers;

namespace KEYLOOP.Authorization;

public class KeyloopApiClient
{
    private readonly HttpClient _httpClient;

    public KeyloopApiClient(IHttpClientFactory httpClientFactory, IAccessToken accessTokenService)
    {
        _httpClient = httpClientFactory.CreateClient("KeyloopApi");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenService.GetAccessToken().Result);
    }

    public HttpClient Client => _httpClient;
}