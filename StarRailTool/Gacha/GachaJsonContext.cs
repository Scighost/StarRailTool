using System.Text.Json.Serialization;

namespace StarRailTool.Gacha;

[JsonSerializable(typeof(MihoyoApiWrapper<GachaLogResult>))]
[JsonSerializable(typeof(GachaLogItem[]))]
[JsonSerializable(typeof(GachaLogItem))]
internal partial class GachaJsonContext : JsonSerializerContext
{

}
