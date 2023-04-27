using System.Text.Json.Serialization;

namespace StarRailTool.GameRecord.Ledger;

/// <summary>
/// 开拓月历-每月数据
/// </summary>
public class LedgerMonthData
{

    /// <summary>
    /// 本月的星琼
    /// </summary>
    [JsonPropertyName("current_hcoin")]
    public int CurrentHcoin { get; set; }


    /// <summary>
    /// 本月的星轨通票&星轨专票
    /// </summary>
    [JsonPropertyName("current_rails_pass")]
    public int CurrentRailsPass { get; set; }

    /// <summary>
    /// 上月的星琼
    /// </summary>
    [JsonPropertyName("last_hcoin")]
    public int LastHcoin { get; set; }

    /// <summary>
    /// 上月的星轨通票&星轨专票
    /// </summary>
    [JsonPropertyName("last_rails_pass")]
    public int LastRailsPass { get; set; }

    /// <summary>
    /// 星琼增长率（尚不清楚单位
    /// </summary>
    [JsonPropertyName("hcoin_rate")]
    public int HcoinRate { get; set; }

    /// <summary>
    /// 星轨通票&星轨专票增长率（尚不清楚单位
    /// </summary>
    [JsonPropertyName("rails_rate")]
    public int RailsRate { get; set; }

    /// <summary>
    /// 分组统计
    /// </summary>
    [JsonPropertyName("group_by")]
    public List<LedgerMonthDataGroupBy> GroupBy { get; set; }
}




