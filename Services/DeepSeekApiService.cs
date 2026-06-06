using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using DeepSeekTokenMonitor.Models;

namespace DeepSeekTokenMonitor.Services;

/// <summary>
/// DeepSeek 平台 API 调用封装
/// 目前仅使用 GET /user/balance 查询余额
/// </summary>
public class DeepSeekApiService : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _apiKey;

    public DeepSeekApiService(string apiKey, string baseUrl = "https://api.deepseek.com")
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            Timeout = TimeSpan.FromSeconds(15)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        UpdateAuthHeader();
    }

    public void UpdateApiKey(string apiKey, string? baseUrl = null)
    {
        _apiKey = apiKey;
        UpdateAuthHeader();
        if (baseUrl != null)
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
    }

    private void UpdateAuthHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    /// <summary>
    /// 查询账户余额
    /// </summary>
    public async Task<(BalanceResponse? Data, string? Error)> GetBalanceAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/user/balance");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return (null, $"HTTP {(int)response.StatusCode}: {body}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<BalanceResponse>(json);
            if (data == null)
                return (null, $"解析失败: {json}");

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 测试 API Key 是否有效
    /// </summary>
    public async Task<(bool Ok, string? Error)> TestConnectionAsync()
    {
        var (data, error) = await GetBalanceAsync();
        return (data != null, error);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
