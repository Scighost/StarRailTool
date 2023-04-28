using StarRailTool;
using StarRailTool.Gacha;
using System.CommandLine;

AppConfig.Instance = AppConfig.Load(Path.Combine(AppContext.BaseDirectory, "Config.json"));

bool checkUpdate = true;


var root = new RootCommand();



#region gacha
{


    var gacha = new Command("gacha", "抽卡相关功能");
    root.AddCommand(gacha);


    // 获取抽卡记录
    var gacha_get = new Command("get", "获取抽卡记录");
    var gacha_get_url = new Option<string>("-url", "[可选] 抽卡记录网址");
    var gacha_get_uid = new Option<int>("-uid", "[可选] 使用已保存的抽卡记录网址获取指定 uid 的抽卡记录");
    var gacha_get_server = new Option<string>("-server", () => "cn", "[可选] 服务器优先级，cn 国服，os 国际服，默认国服").FromAmong("cn", "os");
    var gacha_get_all = new Option<bool>("-all", "[可选] 获取全部抽卡记录，否则到本地已保存的最后一条时停止");

    gacha_get.AddOption(gacha_get_url);
    gacha_get.AddOption(gacha_get_uid);
    gacha_get.AddOption(gacha_get_server);
    gacha_get.AddOption(gacha_get_all);
    gacha_get.SetHandler(GachaLogService.Instance.GetGachaLogAsync, gacha_get_url, gacha_get_uid, gacha_get_server, gacha_get_all);
    gacha.AddCommand(gacha_get);


    // 统计抽卡记录
    var gacha_stats = new Command("stats", "统计抽卡记录");
    var gacha_stats_uid = new Argument<int>("uid", () => 0, "[可选] 统计指定 uid 的抽卡记录");

    gacha_stats.AddArgument(gacha_stats_uid);
    gacha_stats.SetHandler(GachaLogService.Instance.StatsGachaLog, gacha_stats_uid);
    gacha.AddCommand(gacha_stats);


    // 列出抽卡记录
    var gacha_list = new Command("list", "列出抽卡记录");
    var gacha_list_uid = new Argument<int>("uid", "列出指定 uid 的抽卡记录");
    var gacha_list_type = new Option<int>("-t", "[可选] 跃迁类型，1 群星，2 始发，11 角色，12 光锥");
    var gacha_list_rank = new Option<int>("-r", () => 4, "[可选] 最低稀有度，默认 4 星及以上");
    var gacha_list_asc = new Option<bool>("-asc", "[可选] 时间升序，默认降序");

    gacha_list.AddArgument(gacha_list_uid);
    gacha_list.AddOption(gacha_list_type);
    gacha_list.AddOption(gacha_list_rank);
    gacha_list.AddOption(gacha_list_asc);
    gacha_list.SetHandler(GachaLogService.Instance.ListGachaLog, gacha_list_uid, gacha_list_type, gacha_list_rank, gacha_list_asc);
    gacha.AddCommand(gacha_list);



}
#endregion








#region update
{

    var update = new Command("update", "检查更新");
    update.SetHandler(async () => { checkUpdate = false; await GithubService.CheckUpdateAsync(true); });
    root.AddCommand(update);

}
#endregion

root.Invoke(args);



await root.InvokeAsync(args);


AppConfig.Instance.Save(Path.Combine(AppContext.BaseDirectory, "Config.json"));


#if !DEBUG

if (checkUpdate)
{
    await GithubService.CheckUpdateAsync();
}

#endif

