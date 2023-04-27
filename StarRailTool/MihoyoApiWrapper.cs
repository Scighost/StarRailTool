using System.Text.Json.Serialization;

namespace StarRailTool;

internal class MihoyoApiWrapper<T>
{

    [JsonPropertyName("retcode")]
    public int Retcode { get; set; }


    [JsonPropertyName("message")]
    public string Message { get; set; }


    [JsonPropertyName("data")]
    public T Data { get; set; }
}