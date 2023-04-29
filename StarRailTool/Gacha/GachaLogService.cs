using ConsoleTableExt;
using Dapper;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using System.Data;
using System.Text.Json;

namespace StarRailTool.Gacha;

internal class GachaLogService
{


    private readonly GachaLogClient _client;



    private static readonly Lazy<GachaLogService> _lazy = new Lazy<GachaLogService>(() => new GachaLogService());


    public static GachaLogService Instance => _lazy.Value;


    private static Dictionary<CharMapPositions, char> FramePipDefinition = new Dictionary<CharMapPositions, char>
    {
        [CharMapPositions.TopLeft] = '┌',
        [CharMapPositions.TopCenter] = '┬',
        [CharMapPositions.TopRight] = '┐',
        [CharMapPositions.MiddleLeft] = '├',
        [CharMapPositions.MiddleCenter] = '┼',
        [CharMapPositions.MiddleRight] = '┤',
        [CharMapPositions.BottomLeft] = '└',
        [CharMapPositions.BottomCenter] = '┴',
        [CharMapPositions.BottomRight] = '┘',
        [CharMapPositions.BorderLeft] = '│',
        [CharMapPositions.BorderRight] = '│',
        [CharMapPositions.BorderTop] = '─',
        [CharMapPositions.BorderBottom] = '─',
        [CharMapPositions.DividerY] = '│',
        [CharMapPositions.DividerX] = '─',
    };



    public GachaLogService()
    {
        _client = new GachaLogClient();
    }




    public async Task GetGachaLogAsync(string url, int uid, string server, bool all, string lang)
    {
        try
        {
            using var con = DatabaseService.Instance.CreateConnection();
            if (string.IsNullOrWhiteSpace(url))
            {

                if (uid != 0)
                {
                    var urlItem = con.QueryFirstOrDefault<GachaLogUrl>("SELECT * FROM GachaLogUrl WHERE Uid = @uid LIMIT 1;", new { uid });
                    if (urlItem is null)
                    {
                        Logger.Warn($"没有找到与 uid {uid} 对应的抽卡记录网址");
                        return;
                    }
                    Logger.Info($"Uid {uid} 的抽卡记录网址保存时间 {urlItem.Time:yyyy-MM-dd HH:mm:ss}，正在验证有效性。。。");
                    url = urlItem.GachaUrl;
                }
                else
                {
                    string? installPath = null;
                    if (server is "os")
                    {
                        if (!string.IsNullOrWhiteSpace(AppConfig.Instance.InstallPath_OS))
                        {
                            installPath = AppConfig.Instance.InstallPath_OS;
                        }
                        else
                        {
                            installPath = GachaLogClient.GetGameInstallPath(true);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(AppConfig.Instance.InstallPath_CN))
                        {
                            installPath = AppConfig.Instance.InstallPath_CN;
                        }
                        else
                        {
                            installPath = GachaLogClient.GetGameInstallPath(false);
                        }
                    }
                    Logger.Debug($"游戏本体所在的文件夹是：'{installPath}'");
                    if (Directory.Exists(installPath))
                    {
                        url = GachaLogClient.GetGachaUrlFromWebCache(installPath)!;
                        if (string.IsNullOrWhiteSpace(url))
                        {
                            Logger.Warn($"没有找到游戏中已缓存的抽卡记录网址");
                            return;
                        }
                        Logger.Info($"已找到抽卡记录网址，正在验证有效性。。。");
                    }
                    else
                    {
                        Logger.Warn($"没有找到游戏本体所在的文件夹，需要在 Config.json 文件中设置");
                        return;
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(lang))
            {
                _client.Language = lang;
            }
            var newUid = await _client.GetUidByGachaUrlAsync(url);
            if (newUid == 0)
            {
                Logger.Warn("该账号的抽卡记录为空");
            }
            else
            {
                con.Execute("INSERT OR REPLACE INTO GachaLogUrl (Uid, GachaUrl, Time) VALUES (@Uid, @GachaUrl, @Time);", new GachaLogUrl(newUid, url));
                Logger.Info($"正在获取 uid {newUid} 的抽卡记录");
                long endId = 0;
                if (!all)
                {
                    endId = con.QueryFirstOrDefault<long>("SELECT Id FROM GachaLogItem WHERE Uid = @Uid ORDER BY Id DESC LIMIT 1;", new { Uid = newUid });
                }
                var progress = new Progress<(GachaType GachaType, int Page)>((x) => Logger.Trace($"正在获取 {x.GachaType.ToDescription()} 第 {x.Page} 页"));
                var list = await _client.GetGachaLogAsync(url, endId, progress);
                var oldCount = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @Uid;", new { Uid = newUid });
                Logger.Trace("正在写入数据库");
                using var t = con.BeginTransaction();
                con.Execute("""
                    INSERT OR REPLACE INTO GachaLogItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, GachaId, Count, Lang)
                    VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @GachaId, @Count, @Lang);
                    """, list, t);
                t.Commit();
                var newCount = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @Uid;", new { Uid = newUid });
                Logger.Success($"Uid {newUid} 获取抽卡记录 {list.Count} 条，新增 {newCount - oldCount} 条");

                var rows = new List<List<object>> { new List<object> { "Uid", "总数", "群星跃迁", "始发跃迁", "角色跃迁", "光锥跃迁", "上次更新时间" }, GetStatsSummary(newUid) };
                ConsoleTableBuilder.From(rows).WithCharMapDefinition(FramePipDefinition).ExportAndWriteLine();
            }
        }
        catch (MihoyoApiException ex)
        {
            if (ex.ReturnCode == -101)
            {
                Logger.Warn("抽卡记录网址已过期");
            }
            else
            {
                Logger.Warn(ex.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
        }
    }





    public void StatsGachaLog(int uid)
    {
        try
        {
            using var con = DatabaseService.Instance.CreateConnection();
            var count = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid;", new { uid });
            if (uid != 0)
            {
                if (count > 0)
                {
                    var rows = new List<List<object>> { new List<object> { "跃迁类型", "数量", "5星", "4星" } };
                    foreach (int type in new[] { 1, 2, 11, 12 })
                    {
                        var obj = new { uid, type };
                        var c = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType=@type;", obj);
                        if (c == 0)
                        {
                            continue;
                        }
                        var cols = new List<object> { ((GachaType)type).ToDescription() };
                        var g_5 = con.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 5);
                            """, obj);
                        var g_4 = con.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 4);
                            """, obj);
                        cols.Add($"{c} ({g_5}-{g_4})");
                        var c_5 = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 5;", obj);
                        var c_4 = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 4;", obj);
                        cols.Add($"{c_5} ({(double)c_5 / (c - g_5):P3})");
                        cols.Add($"{c_4} ({(double)c_4 / (c - g_4):P3})");
                        rows.Add(cols);
                    }
                    ConsoleTableBuilder.From(rows).WithCharMapDefinition(FramePipDefinition).ExportAndWriteLine();
                    return;
                }
                else
                {
                    Logger.Warn($"Uid {uid} 的抽卡记录数为 0，下表是可输入的所有 uid：", true);
                }
            }
            {
                StatsAllUid();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
        }
    }




    private void StatsAllUid()
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var uids = con.Query<int>("SELECT DISTINCT Uid FROM GachaLogItem;").ToList();
        var rows = new List<List<object>> { new List<object> { "Uid", "总数", "群星跃迁", "始发跃迁", "角色跃迁", "光锥跃迁", "上次更新时间" } };
        foreach (var u in uids)
        {
            rows.Add(GetStatsSummary(u));
        }
        ConsoleTableBuilder.From(rows).WithCharMapDefinition(FramePipDefinition).ExportAndWriteLine();
    }




    private List<object> GetStatsSummary(int uid)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var count = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid;", new { uid });
        var cols = new List<object> { uid, count };
        foreach (int type in new[] { 1, 2, 11, 12 })
        {
            var obj = new { uid, type };
            var c = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type;", obj);
            var g_5 = con.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 5);
                            """, obj);
            var g_4 = con.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 4);
                            """, obj);
            cols.Add($"{c} ({g_5}-{g_4})");
        }
        var time = con.QueryFirstOrDefault<DateTime>("SELECT Time FROM GachaLogUrl WHERE Uid = @uid LIMIT 1;", new { uid });
        cols.Add(time.ToString("yyyy-MM-dd HH:mm:ss"));
        return cols;
    }




    public void ListGachaLog(int uid, int t, int rank, bool desc)
    {
        try
        {
            using var con = DatabaseService.Instance.CreateConnection();
            var count = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid;", new { uid });
            if (uid != 0)
            {
                if (count > 0)
                {
                    rank = Math.Clamp(rank, 3, 5);
                    if (Enum.IsDefined((GachaType)t))
                    {
                        var list = con.Query<GachaLogItemEx>("SELECT * FROM GachaLogItem WHERE Uid = @uid AND GachaType = @t ORDER BY Id;", new { uid, t }).ToList();
                        int index = 0;
                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i].Index = ++index;
                            if (list[i].RankType == 5)
                            {
                                index = 0;
                            }
                        }
                        if (desc)
                        {
                            list = list.OrderByDescending(x => x.Id).ToList();
                        }
                        list = list.Where(x => x.RankType >= rank).ToList();
                        if (list.Any())
                        {
                            var rows = new List<List<object>> { new List<object> { "跃迁类型", "Id", "时间", "名称", "保底内排序", "稀有度" } };
                            rows.AddRange(list.Select(x => new List<object> { x.GachaType.ToDescription(), x.Id, x.Time.ToString("yyyy-MM-dd HH:mm:ss"), x.Name, x.Index, GetRarityStar(x.RankType) }));
                            ConsoleTableBuilder.From(rows).WithCharMapDefinition(FramePipDefinition).ExportAndWriteLine();
                        }
                        else
                        {
                            Logger.Warn($"Uid {uid} 没有{((GachaType)t).ToDescription()}的相关记录", true);
                        }
                    }
                    else
                    {
                        var rows = new List<List<object>> { new List<object> { "跃迁类型", "Id", "时间", "名称", "保底内排序", "稀有度" } };
                        foreach (int type in new[] { 1, 2, 11, 12 })
                        {
                            var obj = new { uid, type };
                            var list = con.Query<GachaLogItemEx>("SELECT * FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type ORDER BY Id;", obj).ToList();
                            int index = 0;
                            for (int i = 0; i < list.Count; i++)
                            {
                                list[i].Index = ++index;
                                if (list[i].RankType == 5)
                                {
                                    index = 0;
                                }
                            }
                            if (desc)
                            {
                                list = list.OrderBy(x => x.Id).ToList();
                            }
                            list = list.Where(x => x.RankType >= rank).ToList();
                            if (list.Any())
                            {
                                rows.AddRange(list.Select(x => new List<object> { x.GachaType.ToDescription(), x.Id, x.Time.ToString("yyyy-MM-dd HH:mm:ss"), x.Name, x.Index, GetRarityStar(x.RankType) }));
                            }
                        }
                        ConsoleTableBuilder.From(rows).WithCharMapDefinition(FramePipDefinition).ExportAndWriteLine();
                    }
                    return;
                }
                else
                {
                    Logger.Warn($"Uid {uid} 的抽卡记录数为 0，下表是可输入的所有 uid：", true);
                }
            }
            {
                StatsAllUid();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
        }
    }




    private static string GetRarityStar(int rarity)
    {
        return rarity switch
        {
            1 => "●",
            2 => "● ●",
            3 => "● ● ●",
            4 => "● ● ● ●",
            5 => "● ● ● ● ●",
            _ => "",
        };
    }






    public void ExportGachaLog(int uid, bool all, string output, string format)
    {
        try
        {
            var time = DateTime.Now;
            if (Path.GetExtension(output) == ".json")
            {
                format = "json";
            }
            if (Path.GetExtension(output) == ".xlsx")
            {
                format = "excel";
            }
            using var con = DatabaseService.Instance.CreateConnection();
            if (all)
            {
                var uids = con.Query<int>("SELECT DISTINCT Uid FROM GachaLogItem;").ToList();
                if (uids.Count == 0)
                {
                    Logger.Warn("没有任何抽卡数据", true);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        output = Path.Combine(AppContext.BaseDirectory, "Export");
                    }
                    output = Path.GetFullPath(output);
                    Directory.CreateDirectory(output);
                    foreach (var u in uids)
                    {
                        string file = "";
                        if (format is "excel")
                        {
                            file = Path.Combine(output, $"export_gacha_{u}_{time:yyyyMMddHHmmss}.xlsx");
                            ExportAsExcel(u, file);
                        }
                        else
                        {
                            file = Path.Combine(output, $"export_gacha_{u}_{time:yyyyMMddHHmmss}.json");
                            ExportAsJson(u, file);
                        }
                        Logger.Success($"已导出 uid {u}: {file}");
                    }
                }
            }
            else
            {
                var list = con.Query<GachaLogItem>("SELECT * FROM GachaLogItem WHERE Uid = @uid;", new { uid }).ToList();
                if (list.Count == 0)
                {
                    Logger.Warn($"Uid {uid} 没有任何抽卡数据", true);
                }
                else
                {
                    string dir = "./";
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        output = null!;
                        dir = Path.Combine(AppContext.BaseDirectory, "Export");
                    }
                    else
                    {
                        output = Path.GetFullPath(output);
                        dir = Path.GetDirectoryName(output)!;
                    }
                    Directory.CreateDirectory(dir);

                    string file = "";
                    if (format is "excel")
                    {
                        file = output ?? Path.Combine(dir, $"export_gacha_{uid}_{time:yyyyMMddHHmmss}.xlsx");
                        ExportAsExcel(uid, file);
                    }
                    else
                    {
                        file = output ?? Path.Combine(dir, $"export_gacha_{uid}_{time:yyyyMMddHHmmss}.json");
                        ExportAsJson(uid, file);
                    }
                    Logger.Success($"已导出 uid {uid}: {file}", true);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex.Message);
        }
    }






    private void ExportAsJson(int uid, string output)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<GachaLogItem>("SELECT * FROM GachaLogItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new GachaLogExportFile(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        File.WriteAllText(output, str);
    }



    private void ExportAsExcel(int uid, string output)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<GachaLogItem>("SELECT * FROM GachaLogItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();

        var sheets = new DataSet();
        var table1 = new DataTable("原始数据");
        table1.Columns.Add("uid", typeof(string));
        table1.Columns.Add("id", typeof(string));
        table1.Columns.Add("time", typeof(string));
        table1.Columns.Add("name", typeof(string));
        table1.Columns.Add("item_type", typeof(string));
        table1.Columns.Add("rank_type", typeof(string));
        table1.Columns.Add("gacha_type", typeof(string));
        table1.Columns.Add("gacha_id", typeof(string));
        table1.Columns.Add("item_id", typeof(string));
        table1.Columns.Add("lang", typeof(string));
        table1.Columns.Add("count", typeof(string));
        foreach (var item in list)
        {
            table1.Rows.Add(item.Uid.ToString(),
                            item.Id.ToString(),
                            item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                            item.Name,
                            item.ItemType,
                            item.RankType.ToString(),
                            ((int)item.GachaType).ToString(),
                            item.GachaId.ToString(),
                            item.ItemId.ToString(),
                            item.Lang,
                            item.Count.ToString());
        }
        sheets.Tables.Add(table1);

        foreach (var type in new int[] { 1, 2, 11, 12 })
        {
            var table = new DataTable(((GachaType)type).ToDescription());
            table.Columns.Add("Uid", typeof(string));
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("时间", typeof(string));
            table.Columns.Add("名称", typeof(string));
            table.Columns.Add("物品类型", typeof(string));
            table.Columns.Add("稀有度", typeof(string));
            table.Columns.Add("跃迁类型", typeof(string));
            table.Columns.Add("保底内排序", typeof(string));
            var l = list.Where(x => x.GachaType == (GachaType)type).ToList();
            int index = 0;
            foreach (var item in l)
            {
                index++;
                table.Rows.Add(item.Uid.ToString(),
                            item.Id.ToString(),
                            item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                            item.Name,
                            item.ItemType,
                            item.RankType.ToString(),
                            ((int)item.GachaType).ToString(),
                            index.ToString());
                if (item.RankType == 5)
                {
                    index = 0;
                }
            }
            sheets.Tables.Add(table);
        }

        MiniExcel.SaveAs(output, sheets, configuration: new OpenXmlConfiguration { TableStyles = TableStyles.None, });
    }






    private class GachaLogItemEx : GachaLogItem
    {
        public int Index { get; set; }
    }



}
