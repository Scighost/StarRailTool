using StarRailTool;
using StarRailTool.Gacha;
using System.CommandLine;
using System.CommandLine.Parsing;


if (args.FirstOrDefault() != "complete")
{
    AppConfig.Load(Path.Combine(AppConfig.ConfigDirectory, "Config.json"));
}


#pragma warning disable CS0219 // 变量已被赋值，但从未使用过它的值
bool checkUpdate = true;
#pragma warning restore CS0219 // 变量已被赋值，但从未使用过它的值


var desc = $"""
    Star Rail Tool by Scighost v{AppConfig.AppVersion}
    星穹铁道小工具（仅有命令行版）
    项目地址和使用方法：https://github.com/Scighost/StarRailTool
    """;

#if DOTNET_TOOL
desc = $"""
    {desc}
    数据文件夹：{AppConfig.ConfigDirectory}
    """;
#endif

var root = new RootCommand(desc);

#if !DOTNET_TOOL
root.SetHandler(() =>
{
    Console.WriteLine(desc);
    Console.WriteLine();
    Console.WriteLine(@"输入 .\srtool.exe -h 查看帮助");
    Console.WriteLine("按回车键退出...");
    Console.ReadLine();
});
#endif




#region gacha
{


    var gacha = new Command("gacha", "抽卡相关功能");
    root.AddCommand(gacha);


    // 获取抽卡记录
    var gacha_get = new Command("get", "获取抽卡记录");
    var gacha_get_url = new Option<string>("-url", "抽卡记录网址");
    var gacha_get_uid = new Option<int>("-uid", "使用已保存的抽卡记录网址获取指定 uid 的抽卡记录");
    gacha_get_uid.AddCompletions((ctx) => GachaLogService.Instance.GetUidCompletions(ctx.WordToComplete));
    var gacha_get_server = new Option<string>("-server", () => "cn", "服务器优先级，cn 国服，os 国际服，默认国服").FromAmong("cn", "os");
    var gacha_get_all = new Option<bool>("-all", "获取全部抽卡记录，默认到本地已保存的最后一条时停止");
    var gacha_get_lang = new Option<string>("-lang", "代替游戏中选择的语言，简体中文是 'zh-cn'");

    gacha_get.AddOption(gacha_get_url);
    gacha_get.AddOption(gacha_get_uid);
    gacha_get.AddOption(gacha_get_server);
    gacha_get.AddOption(gacha_get_all);
    gacha_get.AddOption(gacha_get_lang);
    gacha_get.SetHandler(GachaLogService.Instance.GetGachaLogAsync, gacha_get_url, gacha_get_uid, gacha_get_server, gacha_get_all, gacha_get_lang);
    gacha.AddCommand(gacha_get);


    // 统计抽卡记录
    var gacha_stats = new Command("stats", "统计抽卡记录");
    var gacha_stats_uid = new Argument<int>("uid", () => 0, "统计指定 uid 的抽卡记录");
    gacha_stats_uid.AddCompletions((ctx) => GachaLogService.Instance.GetUidCompletions(ctx.WordToComplete));

    gacha_stats.AddArgument(gacha_stats_uid);
    gacha_stats.SetHandler(GachaLogService.Instance.StatsGachaLog, gacha_stats_uid);
    gacha.AddCommand(gacha_stats);


    // 列出抽卡记录
    var gacha_list = new Command("list", "列出抽卡记录");
    var gacha_list_uid = new Argument<int>("uid", "列出指定 uid 的抽卡记录");
    gacha_list_uid.AddCompletions((ctx) => GachaLogService.Instance.GetUidCompletions(ctx.WordToComplete));
    var gacha_list_type = new Option<int>("-type", "跃迁类型，1 群星，2 始发，11 角色，12 光锥");
    var gacha_list_rank = new Option<int>("-rank", () => 4, "最低稀有度，默认 4 星及以上");
    var gacha_list_asc = new Option<bool>("-desc", "时间降序，默认升序");

    gacha_list.AddArgument(gacha_list_uid);
    gacha_list.AddOption(gacha_list_type);
    gacha_list.AddOption(gacha_list_rank);
    gacha_list.AddOption(gacha_list_asc);
    gacha_list.SetHandler(GachaLogService.Instance.ListGachaLog, gacha_list_uid, gacha_list_type, gacha_list_rank, gacha_list_asc);
    gacha.AddCommand(gacha_list);



    // 导出抽卡记录
    var gacha_export = new Command("export", "导出抽卡记录");
    var gacha_export_uid = new Argument<int>("uid", () => 0, "导出指定 uid 的抽卡记录");
    gacha_export_uid.AddCompletions((ctx) => GachaLogService.Instance.GetUidCompletions(ctx.WordToComplete));
    var gacha_export_all = new Option<bool>("-all", "导出所有 uid 的抽卡记录");
    var gacha_export_output = new Option<string>("-output", "导出的文件名称或文件夹名称");
    var gacha_export_format = new Option<string>("-format", () => "excel", "导出格式，支持 Json(.json) 文件和 Excel(.xlsx) 文件").FromAmong("json", "excel");

    gacha_export.AddArgument(gacha_export_uid);
    gacha_export.AddOption(gacha_export_all);
    gacha_export.AddOption(gacha_export_output);
    gacha_export.AddOption(gacha_export_format);
    gacha_export.SetHandler(GachaLogService.Instance.ExportGachaLog, gacha_export_uid, gacha_export_all, gacha_export_output, gacha_export_format);
    gacha.AddCommand(gacha_export);




}
#endregion





#region update
{

    var update = new Command("update", "检查更新");
    update.SetHandler(async () => { checkUpdate = false; await GithubService.CheckUpdateAsync(true); });
    root.AddCommand(update);

}
#endregion




#region complete
{


    var complete = new Command("complete") { IsHidden = true };
    var complete_input = new Argument<string>("input");
    var complete_position = new Option<int>("--position");

    complete.AddArgument(complete_input);
    complete.AddOption(complete_position);
    complete.SetHandler(CompleteHandler, complete_input, complete_position);
    root.AddCommand(complete);


}
#endregion





var result = await root.InvokeAsync(args);



#if !DEBUG

if (checkUpdate && AppConfig.Instance.AutoCheckUpdate && args.FirstOrDefault() != "complete")
{
    await GithubService.CheckUpdateAsync();
}

#endif


if (result == 0 && args.FirstOrDefault() != "complete")
{
    DatabaseService.Instance.AutoBackupDatabase();
}




void CompleteHandler(string path, int position)
{
    if (position > path.Length)
    {
        path += " ";
    }
    var result = root.Parse(path).GetCompletions(position);
    foreach (var item in result)
    {
        Console.WriteLine(item.Label);
    }
}