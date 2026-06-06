using System.Text.Json.Serialization;

namespace DeepSeekTokenMonitor.Models;

/// <summary>
/// DeepSeek /user/balance 接口返回的顶层结构
/// 官方文档: https://api-docs.deepseek.com/zh-cn/api/get-user-balance
/// </summary>
public class BalanceResponse
{
    [JsonPropertyName("is_available")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("balance_infos")]
    public List<BalanceInfo> BalanceInfos { get; set; } = new();

    /// <summary>所有币种的余额总和</summary>
    public decimal TotalBalance =>
        BalanceInfos.Sum(b => b.TotalBalance);

    /// <summary>所有币种的赠送额度总和</summary>
    public decimal TotalGranted =>
        BalanceInfos.Sum(b => b.GrantedBalance);

    /// <summary>所有币种的充值额度总和</summary>
    public decimal TotalToppedUp =>
        BalanceInfos.Sum(b => b.ToppedUpBalance);
}

public class BalanceInfo
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "CNY";

    /// <summary>可用余额（字符串，如 "110.00"）</summary>
    [JsonPropertyName("total_balance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal TotalBalance { get; set; }

    /// <summary>赠送额度（字符串，如 "10.00"）</summary>
    [JsonPropertyName("granted_balance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal GrantedBalance { get; set; }

    /// <summary>充值额度（字符串，如 "100.00"）</summary>
    [JsonPropertyName("topped_up_balance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal ToppedUpBalance { get; set; }
}
