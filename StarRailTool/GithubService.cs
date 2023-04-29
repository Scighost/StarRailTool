using System.Text.Json;
using System.Text.Json.Serialization;

namespace StarRailTool;

internal abstract class GithubService
{


    private static readonly HttpClient _httpClient;


    private const string url = "https://api.github.com/repos/Scighost/StarRailTool/releases/latest";


    static GithubService()
    {
        _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "StarRailTool");
    }


    public static async Task<GithubRelease?> GetLatestReleaseAsync(bool disableCache = false, bool throwException = false)
    {
        try
        {
            var release = DatabaseService.Instance.GetValue<string>("NewVersion", out var time);
            if (release != null && DateTime.Now - time < TimeSpan.FromDays(1) && !disableCache)
            {
                return JsonSerializer.Deserialize<GithubRelease>(release);
            }
            else
            {
                var str = await _httpClient.GetStringAsync(url);
                DatabaseService.Instance.SetValue("NewVersion", str);
                return JsonSerializer.Deserialize<GithubRelease>(str);
            }
        }
        catch
        {
            if (throwException)
            {
                throw;
            }
            return null;
        }
    }





    public static async Task CheckUpdateAsync(bool manual = false)
    {
        if (!manual)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(1);
        }
        try
        {
            var release = await GetLatestReleaseAsync(disableCache: manual, throwException: manual);
            if (release != null)
            {
                if (Version.TryParse(release.TagName, out var newVersion))
                {
                    if (Version.TryParse(AppConfig.AppVersion, out var oldVeriosn))
                    {
                        if (newVersion > oldVeriosn)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine($"有新版本 v{release.TagName}:{release.Name}");
                            Console.WriteLine(release.HtmlUrl);
#if DOTNET_TOOL
                            Console.WriteLine("使用命令 'dotnet tool update srtool -g' 更新");
#endif
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        return;
                    }
                }
                throw new ArgumentException("检查更新失败");
            }
            if (manual)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"已是最新版本");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        catch (Exception ex)
        {
            if (manual)
            {
                Logger.Error(ex.Message);
            }
        }
    }








    public class GithubAsset
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonPropertyName("uploader")]
        public GithubUploader Uploader { get; set; }
    }

    public class GithubAuthor
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("gravatar_id")]
        public string GravatarId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("followers_url")]
        public string FollowersUrl { get; set; }

        [JsonPropertyName("following_url")]
        public string FollowingUrl { get; set; }

        [JsonPropertyName("gists_url")]
        public string GistsUrl { get; set; }

        [JsonPropertyName("starred_url")]
        public string StarredUrl { get; set; }

        [JsonPropertyName("subscriptions_url")]
        public string SubscriptionsUrl { get; set; }

        [JsonPropertyName("organizations_url")]
        public string OrganizationsUrl { get; set; }

        [JsonPropertyName("repos_url")]
        public string ReposUrl { get; set; }

        [JsonPropertyName("events_url")]
        public string EventsUrl { get; set; }

        [JsonPropertyName("received_events_url")]
        public string ReceivedEventsUrl { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("site_admin")]
        public bool SiteAdmin { get; set; }
    }

    public class GithubRelease
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("assets_url")]
        public string AssetsUrl { get; set; }

        [JsonPropertyName("upload_url")]
        public string UploadUrl { get; set; }

        [JsonPropertyName("tarball_url")]
        public string TarballUrl { get; set; }

        [JsonPropertyName("zipball_url")]
        public string ZipballUrl { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("target_commitish")]
        public string TargetCommitish { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; set; }

        [JsonPropertyName("author")]
        public GithubAuthor Author { get; set; }

        [JsonPropertyName("assets")]
        public List<GithubAsset> Assets { get; set; }
    }

    public class GithubUploader
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public string NodeId { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("gravatar_id")]
        public string GravatarId { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("followers_url")]
        public string FollowersUrl { get; set; }

        [JsonPropertyName("following_url")]
        public string FollowingUrl { get; set; }

        [JsonPropertyName("gists_url")]
        public string GistsUrl { get; set; }

        [JsonPropertyName("starred_url")]
        public string StarredUrl { get; set; }

        [JsonPropertyName("subscriptions_url")]
        public string SubscriptionsUrl { get; set; }

        [JsonPropertyName("organizations_url")]
        public string OrganizationsUrl { get; set; }

        [JsonPropertyName("repos_url")]
        public string ReposUrl { get; set; }

        [JsonPropertyName("events_url")]
        public string EventsUrl { get; set; }

        [JsonPropertyName("received_events_url")]
        public string ReceivedEventsUrl { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("site_admin")]
        public bool SiteAdmin { get; set; }
    }





}
