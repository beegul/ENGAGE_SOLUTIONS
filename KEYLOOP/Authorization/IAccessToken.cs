namespace KEYLOOP.Authorization;

public interface IAccessToken
{
    Task<string> GetAccessToken();
}