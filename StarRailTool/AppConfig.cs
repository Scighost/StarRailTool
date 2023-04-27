using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarRailTool;

internal class AppConfig
{


    [JsonIgnore]
    public static AppConfig Instance { get; set; }


    private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };



    public string InstallPath_CN { get; set; }


    public string InstallPath_OS { get; set; }





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
            var str = JsonSerializer.Serialize(this, jsonSerializerOptions);
            File.WriteAllText(path, str);
        }
        catch (Exception ex)
        {
            Logger.Warn($"保存配置文件 {path} 出错：{ex.Message}");
        }
    }





}
