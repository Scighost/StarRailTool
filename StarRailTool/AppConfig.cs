using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarRailTool;

internal class AppConfig
{


    [JsonIgnore]
    public static AppConfig Instance { get; set; }

    [JsonIgnore]
    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    [JsonIgnore]
    public static string? AppVersion { get; private set; }


    static AppConfig()
    {
        try
        {
            var file = Process.GetCurrentProcess().MainModule?.FileName;
            if (File.Exists(file))
            {
                AppVersion = FileVersionInfo.GetVersionInfo(file).FileVersion;
            }
        }
        catch { }
    }



    public string InstallPath_CN { get; set; } = "";


    public string InstallPath_OS { get; set; } = "";


    public bool EnableAutoBackupDatabase { get; set; } = true;


    public int BackupIntervalInDays { get; set; } = 21;


    public bool AutoCheckUpdate { get; set; } = true;



    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }


    public static AppConfig Load(string path)
    {
        try
        {
            AppConfig? config = null;
            if (File.Exists(path))
            {
                var str = File.ReadAllText(path);
                config = JsonSerializer.Deserialize<AppConfig>(str);
            }
            return config ??= new AppConfig();
        }
        catch (Exception ex)
        {
            Logger.Warn($"无法解析配置文件 {path}：{ex.Message}");
            return new AppConfig();
        }
    }




    public void Save(string path)
    {
        try
        {
            var str = JsonSerializer.Serialize(this, JsonSerializerOptions);
            File.WriteAllText(path, str);
        }
        catch (Exception ex)
        {
            Logger.Warn($"保存配置文件 {path} 出错：{ex.Message}");
        }
    }





}
