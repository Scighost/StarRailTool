using System.Reflection;
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


    [JsonIgnore]
    public static string ConfigDirectory { get; private set; }



    static AppConfig()
    {
        try
        {
            AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
#if DOTNET_TOOL
            ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".srtool");
            Directory.CreateDirectory(ConfigDirectory);
#else
            ConfigDirectory = AppContext.BaseDirectory;
#endif
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


    public static void Load(string path)
    {
        try
        {
            AppConfig? config = null;
            if (File.Exists(path))
            {
                var str = File.ReadAllText(path);
                config = JsonSerializer.Deserialize<AppConfig>(str);
            }
            if (config is null)
            {
                config = new AppConfig();
                var str = JsonSerializer.Serialize(config, JsonSerializerOptions);
                File.WriteAllText(path, str);
            }
            Instance = config ??= new AppConfig();
        }
        catch (Exception ex)
        {
            Logger.Warn($"无法解析配置文件 {path}：{ex.Message}");
            Instance = new AppConfig();
            var str = JsonSerializer.Serialize(Instance, JsonSerializerOptions);
            File.WriteAllText(path, str);
        }
    }





}
