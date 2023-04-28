using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarRailTool.Gacha;

[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult>))]
[JsonSerializable(typeof(GachaLogItem[]))]
[JsonSerializable(typeof(GachaLogItem))]
[JsonSerializable(typeof(GachaLogExportFile))]
[JsonSerializable(typeof(GachaLogExportFile.GachaLogExportInfo))]
internal partial class GachaJsonContext : JsonSerializerContext
{

    public static GachaJsonContext Config { get; private set; } = new(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

}
