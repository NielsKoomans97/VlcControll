using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

internal class Program
{
    public static Playlist? Playlist;
    public static Status? Status;
    public static Observation? Observation;
    public static Forecast? Forecast;
    public static SearchResult? SearchResult;

    private static HttpClient? httpClient;
    private static DiscordClient? discordClient;

    private static string statusUrl = "http://192.168.2.161:8080/status.json";
    private static string playlistUrl = "http://192.168.2.161:8080/playlist.json";

    private static string DiscordToken = string.Empty;

    public static async Task Main(string[] args)
    {
        DiscordToken = await File.ReadAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}\\token");
        httpClient = new HttpClient();
        discordClient = new DiscordClient(StandardConfig);

        var byteArray = Encoding.ASCII.GetBytes(":F!nley19g7");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        await LoadStatusAsync();
        await LoadPlaylistAsync();

        discordClient.MessageCreated += async (sender, message) =>
        {
            var internalMessage = message.Message;
            var content = internalMessage.Content;
            var embed = new DiscordEmbedBuilder();

            //await GetStatusAsync();
            //await GetGuideAsync();

            if (content.StartsWith('!'))
            {
                await message.Channel.TriggerTypingAsync();

                var parts = ExtractCommand(content);

                switch (parts[1].Value)
                {
                    case "!weather":
                        Console.WriteLine(parts.Length);

                        foreach (Group part in parts)
                            Console.WriteLine($"{part.Index} {part.Value}");

                        if (parts.Length > 2)
                            if (parts[3].Value != string.Empty)
                            {
                                var query = parts[3].Value;
                                var results = await GetAsync<SearchResult[]>($"https://location.buienradar.nl/1.1/location/search?query={query}");

                                foreach (SearchResult result in results)
                                    Console.WriteLine($"{result.Name} {result.Weatherstationid}");

                                if (!results.Any())
                                {
                                    await internalMessage.Channel.SendMessageAsync("No location was found from the given query");
                                }

                                SearchResult = results.FirstOrDefault();

                                embed = new DiscordEmbedBuilder();
                                embed = await GetWeatherAsync();

                                await internalMessage.Channel.SendMessageAsync(embed);
                            }
                        break;

                    case "!skip":
                        //Console.WriteLine(parts.Length);

                        //foreach (Group part in parts)
                        //    Console.WriteLine($"{part.Index} {part.Value}");

                        if (parts.Length > 2)
                            if (parts[3].Value != string.Empty)
                            {
                                Console.WriteLine(parts[3].Value);

                                var index = Convert.ToInt32(parts[3].Value);
                                var item = Playlist.Items.FirstOrDefault(item => item.Value.Id == index);

                                Console.WriteLine(item.Value.Id);

                                if (item.Value != null)
                                {
                                    embed = await SkipAsync(item.Value);
                                    await internalMessage.Channel.SendMessageAsync(embed);
                                }
                                else
                                {
                                    await internalMessage.Channel.SendMessageAsync("No item was found with the given ID");
                                }
                            }
                            else
                            {
                                embed = new DiscordEmbedBuilder();
                                embed = await SkipAsync();
                                await internalMessage.Channel.SendMessageAsync(embed);
                            }
                        break;

                    case "!play":
                        embed = new DiscordEmbedBuilder();
                        embed = await PlayAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!pause":
                        embed = new DiscordEmbedBuilder();
                        embed = await PauseAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!status":
                        embed = new DiscordEmbedBuilder();
                        embed = await GetStatusAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!guide":
                        embed = new DiscordEmbedBuilder();
                        embed = await GetGuideAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!help":
                        embed = new DiscordEmbedBuilder();
                        embed = GetHelp();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;
                }
            }
        };

        await discordClient.ConnectAsync();

        await Task.Delay(-1);
    }

    private static Group[] ExtractCommand(string content)
    {
        var regex = new Regex("([\\S]+)");
        var groupList = new List<Group>();

        if (regex.IsMatch(content))
        {
            var mts = regex.Matches(content);
            foreach (Match match in mts)
            {
                foreach (Group group in match.Groups)
                {
                    if (!groupList.Contains(group))
                    {
                        groupList.Add(group);
                    }
                }
            }

            return groupList.ToArray();
        }

        throw new Exception("Couldn't extract parts from command");
    }

    private static DiscordConfiguration StandardConfig => new DiscordConfiguration()
    {
        Intents = DiscordIntents.MessageContents | DiscordIntents.GuildMessages | DiscordIntents.Guilds,
        Token = DiscordToken,
        TokenType = TokenType.Bot,
        AutoReconnect = true,
        MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
    };

    public static async Task<T> GetAsync<T>(string requestUri)
    {
        using (var response = await httpClient.GetAsync(requestUri))
        using (var content = response.EnsureSuccessStatusCode().Content)
        using (var streamReader = new StreamReader(await content.ReadAsStreamAsync()))
        using (var jsonTextReader = new JsonTextReader(streamReader))
        {
            var js = new JsonSerializer();
            return js.Deserialize<T>(jsonTextReader)
                ?? throw new Exception(nameof(requestUri));
        }
    }

    public static async Task<DiscordEmbedBuilder> SkipAsync(Item item)
    {
        if (item.Id != 0)
        {
            Console.WriteLine($"{item.Name} = {item.Id}");

            //var item = Playlist.Items.Where(item => item.Value.Id == id).FirstOrDefault().Value;

            Status = await GetAsync<Status>($"{statusUrl}?command=pl_play&id={item.Id}");

            var embedBuilder = new DiscordEmbedBuilder()
            {
                Description = WriteStatus()
            };

            return embedBuilder;
        }

        return new DiscordEmbedBuilder()
        {
            Description = "** No information **"
        };
    }

    public static async Task<DiscordEmbedBuilder> SkipAsync()
    {
        Status = await GetAsync<Status>($"{statusUrl}?command=pl_next");

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteStatus()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> PlayAsync()
    {
        Status = await GetAsync<Status>($"{statusUrl}?command=pl_play");

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteStatus()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> PreviousAsync()
    {
        Status = await GetAsync<Status>($"{statusUrl}?command=pl_previous");

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteStatus()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> PauseAsync()
    {
        Status = await GetAsync<Status>($"{statusUrl}?command=pl_pause");

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteStatus()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> GetStatusAsync()
    {
        Status = null;
        Status = await GetAsync<Status>(statusUrl);

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteStatus()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> GetGuideAsync()
    {
        Playlist = null;
        var items = await GetAsync<Item>(playlistUrl);
        Playlist = new Playlist(items.Children);

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteGuide()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> GetWeatherAsync()
    {
        Console.WriteLine($"https://observations.buienradar.nl/1.0/actual/weatherstation/{SearchResult?.Weatherstationid}");
        Console.WriteLine($"https://forecast.buienradar.nl/2.0/forecast/{SearchResult?.Id}");

        Observation = await GetAsync<Observation>($"https://observations.buienradar.nl/1.0/actual/weatherstation/{SearchResult?.Weatherstationid}");
        Forecast = await GetAsync<Forecast>($"https://forecast.buienradar.nl/2.0/forecast/{SearchResult?.Id}");

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteWeather()
        };

        return embedBuilder;
    }

    public static async Task LoadStatusAsync()
    {
        Status = await GetAsync<Status>(statusUrl);
    }

    public static async Task LoadPlaylistAsync()
    {
        var items = await GetAsync<Item>(playlistUrl);
        Playlist = new Playlist(items.Children);
    }

    public static DiscordEmbedBuilder GetHelp()
    {
        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteHelp()
        };

        return embedBuilder;
    }

    private static string WriteHelp()
    {
        var builder = new StringBuilder();
        builder.AppendLine("VLCController (ofwel niels-bot) v0.2");
        builder.AppendLine();
        builder.AppendLine("**!help** __*//*__ Shows this overview");
        builder.AppendLine("**!play** __*//*__ Starts/resumes video playback");
        builder.AppendLine("**!pause** __*//*__ Pauses video playback");
        builder.AppendLine("**!skip *[id]* ** __*//*__ Skips to the next item in the playlist, or the optionally given [id] index");
        builder.AppendLine("**!previous** __*//*__ Goes back to previous item in the playlist");
        builder.AppendLine("**!guide** __*//*__ Shows tv-style guide for the next items in the queue");
        builder.AppendLine();

        return builder.ToString();
    }

    private static string WriteStatus()
    {
        var builder = new StringBuilder();
        builder.AppendLine();

        if (Status != null)
        {
            Group[] mediaInfo = ParseMediaInfo(Status.Information.Category.Meta.Filename);
            var name = mediaInfo
               .FirstOrDefault(group =>
               {
                   if (group.Name.Contains("ShowName") && group.Value != null)
                   {
                       return true;
                   }

                   return false;
               });
            var year = mediaInfo
            .FirstOrDefault(group =>
            {
                if (group.Name.Contains("ShowYear") && group.Value != null)
                    return true;

                return false;
            });
            var season = mediaInfo
            .FirstOrDefault(group =>
            {
                if (group.Name.Contains("Season") && group.Value != string.Empty)
                    return true;

                return false;
            });
            var episode = mediaInfo
            .FirstOrDefault(group =>
            {
                if (group.Name.Contains("Episode") && group.Value != string.Empty)
                {
                    return true;
                }

                return false;
            });

            var pos = TimeSpan.FromSeconds(Status.Position);
            var len = TimeSpan.FromSeconds(Status.Length);
            var now = DateTime.Now.TimeOfDay;
            var left = now + (len - pos);

            builder.AppendLine($"**Now** - **{FixInt(left.Hours)}:{FixInt(left.Minutes)}**");

            if (name != null)
            {
                if (name.Value != string.Empty)
                    builder.Append($"[{Status.Id}] {CleanShowName(name.Value)}");
                else
                    builder.Append($"[{Status.Id}] {Status.Information.Title}");
            }
            else
            {
                builder.Append($"[{Status.Id}] {Status.Information.Title}");
            }

            if (year != null)
            {
                if (year.Value != string.Empty)
                    builder.AppendLine($" **({year.Value})**");
                else
                    builder.AppendLine();
            }
            else
            {
                builder.AppendLine();
            }

            if (episode != null)
            {
                if (episode.Value != string.Empty)
                    builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
            }

            builder.AppendLine($"{FixInt(pos.Hours)}:{FixInt(pos.Minutes)}:{FixInt(pos.Seconds)} - {FixInt(len.Hours)}:{FixInt(len.Minutes)}:{FixInt(len.Seconds)}");

            return builder.ToString();
        }

        builder.AppendLine("** No Information **");
        builder.AppendLine();

        return builder.ToString();
    }

    private static string WriteGuide()
    {
        var builder = new StringBuilder();
        builder.AppendLine();

        if (Playlist != null)
        {
            var index = GetIndexFromItem(Status.Information.Category.Meta.Filename);

            #region Write status

            Group[] mediaInfo = ParseMediaInfo(Status.Information.Category.Meta.Filename);
            var name = mediaInfo
               .FirstOrDefault(group =>
               {
                   if (group.Name.Contains("ShowName") && group.Value != null)
                   {
                       return true;
                   }

                   return false;
               });
            var year = mediaInfo
            .FirstOrDefault(group =>
            {
                if (group.Name.Contains("ShowYear") && group.Value != null)
                    return true;

                return false;
            });
            var season = mediaInfo
            .FirstOrDefault(group =>
            {
                if (group.Name.Contains("Season") && group.Value != string.Empty)
                    return true;

                return false;
            });
            var episode = mediaInfo
            .FirstOrDefault(group =>
            {
                if (group.Name.Contains("Episode") && group.Value != string.Empty)
                {
                    return true;
                }

                return false;
            });

            var pos = TimeSpan.FromSeconds(Status.Position);
            var len = TimeSpan.FromSeconds(Status.Length);
            var now = DateTime.Now.TimeOfDay;
            var left = now + (len - pos);

            builder.AppendLine($"**Now** - **{FixInt(left.Hours)}:{FixInt(left.Minutes)}**");

            if (name != null)
            {
                if (name.Value != string.Empty)
                    builder.Append($"[{Status.Id}] {CleanShowName(name.Value)}");
                else
                    builder.Append($"[{Status.Id}] {Status.Information.Title}");
            }
            else
            {
                builder.Append($"[{Status.Id}] {Status.Information.Title}");
            }

            if (year != null)
            {
                if (year.Value != string.Empty)
                    builder.AppendLine($" **({year.Value})**");
                else
                    builder.AppendLine();
            }
            else
            {
                builder.AppendLine();
            }

            if (episode != null)
            {
                if (episode.Value != string.Empty)
                    builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
            }

            builder.AppendLine($"{FixInt(pos.Hours)}:{FixInt(pos.Minutes)}:{FixInt(pos.Seconds)} - {FixInt(len.Hours)}:{FixInt(len.Minutes)}:{FixInt(len.Seconds)}");

            builder.AppendLine();

            #endregion Write status

            #region Write guide

            var itemPos = left;

            foreach (var item in Playlist.Items.ToArray()[(index + 1)..(index + 10)])
            {
                if (item.Value.Uri != null)
                {
                    mediaInfo = ParseMediaInfo(HttpUtility.UrlDecode(Path.GetFileName(item.Value.Uri)));
                    name = mediaInfo
                      .FirstOrDefault(group =>
                      {
                          if (group.Name.Contains("ShowName") && group.Value != null)
                          {
                              return true;
                          }

                          return false;
                      });
                    year = mediaInfo
                   .FirstOrDefault(group =>
                   {
                       if (group.Name.Contains("ShowYear") && group.Value != null)
                           return true;

                       return false;
                   });
                    season = mediaInfo
                   .FirstOrDefault(group =>
                   {
                       if (group.Name.Contains("Season") && group.Value != string.Empty)
                           return true;

                       return false;
                   });
                    episode = mediaInfo
                   .FirstOrDefault(group =>
                   {
                       if (group.Name.Contains("Episode") && group.Value != string.Empty)
                       {
                           return true;
                       }

                       return false;
                   });

                    var itemPosPlusDuration = itemPos + TimeSpan.FromSeconds(item.Value.Duration);

                    builder.AppendLine($"**{FixInt(itemPos.Hours)}:{FixInt(itemPos.Minutes)}** - **{FixInt(itemPosPlusDuration.Hours)}:{FixInt(itemPosPlusDuration.Minutes)}**");

                    if (name != null)
                    {
                        if (name.Value != string.Empty)
                            builder.Append($"[{item.Value.Id}] {CleanShowName(name.Value)}");
                        else
                            builder.Append($"[{item.Value.Id}] {item.Value.Name}");
                    }
                    else
                    {
                        builder.Append($"[{item.Value.Id}] {item.Value.Name}");
                    }

                    if (year != null)
                    {
                        if (year.Value != string.Empty)
                            builder.AppendLine($" **({year.Value})**");
                        else
                            builder.AppendLine();
                    }
                    else
                    {
                        builder.AppendLine();
                    }

                    if (episode != null)
                    {
                        if (episode.Value != string.Empty)
                            builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
                    }

                    builder.AppendLine();

                    itemPos = itemPosPlusDuration;
                }
            }

            #endregion Write guide

            return builder.ToString();
        }

        builder.AppendLine("** No information **");
        return builder.ToString();
    }

    private static string WriteWeather()
    {
        var day0 = Forecast?.Days[0];

        var builder = new StringBuilder();
        builder.AppendLine();

        builder.AppendLine($"Observations for **{SearchResult?.Name}**");
        builder.AppendLine();
        builder.AppendLine($"Sunrise: **{day0?.Sunrise}** __*//*__ Sunset: **{day0?.Sunset}**");
        builder.AppendLine();
        builder.AppendLine("**Temperature (°C)**");
        builder.AppendLine($"Temperature: **{Observation?.Temperature}°**");
        builder.AppendLine($"Feel temperature: **{Observation?.Feeltemperature}°**");
        builder.AppendLine($"Ground temperature: **{Observation?.Groundtemperature}°**");
        builder.AppendLine();
        builder.AppendLine("**Wind**");
        builder.AppendLine($"Wind speed: **{Observation?.WindspeedBft} Bft**");
        builder.AppendLine($"Wind direction: **{Observation?.Winddirection}**");
        builder.AppendLine($"Wind gusts: **{Observation?.Windgusts} m/s**");
        builder.AppendLine();
        builder.AppendLine("**Other atmospheric properties**");
        builder.AppendLine($"Air pressure: **{Observation?.Airpressure} hPa**");
        builder.AppendLine($"Visibility: **{Observation?.Visibility} m**");
        builder.AppendLine($"Humidity: **{Observation?.Humidity}%**");
        builder.AppendLine();
        builder.AppendLine("**Rain statistics**");
        builder.AppendLine($"Precipitation: **{Observation?.Precipitation} mm**");
        builder.AppendLine($"Precipation: **{Observation?.Precipation} mm**");
        builder.AppendLine($"Rainfall last 24 hours: **{Observation?.RainFallLast24Hour} mm**");
        builder.AppendLine($"Rainfall last hour: **{Observation?.RainFallLastHour} mm**");
        builder.AppendLine();
        builder.AppendLine("**Forecast per hour**");
        foreach (Hour hour in day0?.Hours)
        {
            builder.AppendLine($"**{hour.Datetime}** - **{GetWeatherText(hour.Iconcode)}** - Cloud cover: **{hour.Cloudcover}%**");
            builder.AppendLine($"Temp: **{hour.Temperature}°**, Wind: **{hour.Beaufort} Bft** from the **{hour.Winddirection}**, Precipitation: **{hour.Precipitationmm} mm**");
            builder.AppendLine();
        }
        builder.AppendLine();

        builder.AppendLine("*Data provided by **Buienradar** *");

        return builder.ToString();
    }

    private static int GetIndexFromItem(string filename)
    {
        if (Playlist != null)
        {
            Console.WriteLine(filename);

            for (int i = 0; i < Playlist.Items.Count; i++)
            {
                if (Playlist.Items[i].Uri != null)
                {
                    var path = HttpUtility.UrlDecode(Path.GetFileName(Playlist.Items[i].Uri));

                    Console.WriteLine(path);

                    if (path.Contains(filename))
                    {
                        Console.WriteLine($"Found! {path}");

                        return i;
                    }
                }
            }
        }

        Console.WriteLine("Playlist was null");

        throw new NullReferenceException(nameof(filename));
    }

    private static string FixInt(int value)
    {
        return value < 10 ? $"0{value}" : $"{value}";
    }

    public static string GetFileName(string uri)
    {
        return HttpUtility.UrlDecode(uri.Substring(uri.LastIndexOf('/'), uri.Length - uri.LastIndexOf("/")));
    }

    public static Group[] ParseMediaInfo(string fileName)
    {
        Console.WriteLine(fileName);
        string pattern = @"^((?<ShowNameA>.*[^ (_.]) [ (_.]+ (?!720p|1080p|x264|x265)
(
# Shows with preceding Year
#(?<ShowYearA>\d{4}) ([ (_.]+ (
(?<ShowYearA>(?:19|20)\d{2}) ([ (_.]+ (
(?<SeasonA>\d{1,2})x(?<EpisodeA>\d{1,2})
|(?<SeasonB>[0-3]?[0-9])(?<EpisodeB>\d{2})
|S(?<SeasonC>\d{1,2})E(?<EpisodeC>\d{1,2}) ) )?

| # Shows without preceding Year
 (?<!\d{4})(
S(?<SeasonD>\d{1,2})E(?<EpisodeD>\d{1,2})
|(?<SeasonF>[0-3]?[0-9])(?<EpisodeF>\d{2})
|(?<SeasonE>\d{1,2})x(?<EpisodeE>\d{1,2}))

)
|(?<ShowNameB>.+))
";
        RegexOptions options = RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase;

        if (Regex.IsMatch(fileName, pattern, options))
        {
            var mts = Regex.Match(fileName, pattern, options);
            return mts.Groups.Values.ToArray();
        }

        throw new Exception("No valid media filename inserted");
    }

    public static string CleanShowName(string showName)
    {
        var rgx = new Regex("([^\\w])");

        if (rgx.IsMatch(showName))
        {
            return rgx.Replace(showName, " ");
        }

        return showName;
    }

    public static string GetWeatherText(string icon)
    {
        switch (icon)
        {
            case "a": return "Sunny";
            case "aa": return "Clear";
            case "b": case "bb": case "o": case "oo": case "r": case "rr": return "Lightly Cloudy";
            case "c": case "cc": return "Cloudy";
            case "d": case "dd": return "Misty";
            case "f": case "ff": case "k": case "kk": return "Light shower";
            case "g": case "gg": return "Thundershower";
            case "h": case "hh": return "Moderate/heavy shower";
            case "i": case "ii": return "Snow shower";
            case "j": case "jj": return "Mostly clear";
            case "l": case "ll": case "q": case "qq": return "Moderate/heavy rain";
            case "m": case "mm": return "Light rain";
            case "n": case "nn": return "The world is invisible";
            case "p": case "pp": return "Cloudy";
            case "s": case "ss": return "Heavy (thunder)storm";
            case "t:": case "tt": return "Moderate/heavy snow";
            case "u": case "uu": return "Light snow shower";
            case "v": case "vv": return "Light snow";
            case "w": case "ww": return "Moderate/heavy rain/snow";
            default: return "Empty";
        }
    }
}

public partial class Playlist
{
    public ItemDict Items { get; set; }

    public Playlist(Item[] items)
    {
        Items = new ItemDict();
        Populate(items);
    }

    private void Populate(Item[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            Items.Add(Items.Count, items[i]);

            if (items[i].Children != null)
                if (items[i].Children.Any())
                {
                    Populate(items[i].Children);
                }
        }
    }

    public class ItemDict : Dictionary<int, Item>
    {
        public Item this[string key]
        {
            get
            {
                foreach (var value in Values)
                {
                    if (value.Name == key)
                        return value;
                }

                throw new NullReferenceException(nameof(key));
            }
        }
    }
}

public partial class Item
{
    [JsonProperty("ro")]
    public string ReadOnly { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
    public long Duration { get; set; }

    [JsonProperty("uri", NullValueHandling = NullValueHandling.Ignore)]
    public string Uri { get; set; }

    [JsonProperty("children")]
    public Item[] Children { get; set; }
}

public partial class Status
{
    [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
    public long Length { get; set; }

    [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
    public long Position { get; set; }

    [JsonProperty("information", NullValueHandling = NullValueHandling.Ignore)]
    public Information Information { get; set; }

    [JsonProperty("currentplid", NullValueHandling = NullValueHandling.Ignore)]
    public long Id { get; set; }
}

public partial class Information
{
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public long? Title { get; set; }

    [JsonProperty("category", NullValueHandling = NullValueHandling.Ignore)]
    public Category Category { get; set; }

    [JsonProperty("titles", NullValueHandling = NullValueHandling.Ignore)]
    public object[] Titles { get; set; }
}

public partial class Category
{
    [JsonProperty("meta", NullValueHandling = NullValueHandling.Ignore)]
    public Meta Meta { get; set; }
}

public partial class Meta
{
    [JsonProperty("showName", NullValueHandling = NullValueHandling.Ignore)]
    public string ShowName { get; set; }

    [JsonProperty("filename", NullValueHandling = NullValueHandling.Ignore)]
    public string Filename { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string Title { get; set; }

    [JsonProperty("episodeNumber", NullValueHandling = NullValueHandling.Ignore)]
    public string EpisodeNumber { get; set; }

    [JsonProperty("seasonNumber", NullValueHandling = NullValueHandling.Ignore)]
    public string SeasonNumber { get; set; }
}

//                case "!weather":
//                    var parts = command.Parameters[0];

//                    builder.Clear();
//                    response = await httpClient.GetAsync($"https://location.buienradar.nl/1.1/location/search?query={parts}");
//                    long? stationId = 0;
//                    var locationName = string.Empty;
//                    long? locationId = 0;

//                    if (response.IsSuccessStatusCode)
//                    {
//                        var text = await response.Content.ReadAsStringAsync();
//                        var obj = JsonConvert.DeserializeObject<SearchItem[]>(text);

//                        stationId = obj?[0].StationId;

//                        locationName = obj?[0].Name;

//                        locationId = obj?[0].LocationId;
//                    }
//                    else
//                    {
//                        throw new Exception("**<!>** Could not find any weather stations close to given location");
//                    }

//                    response = await httpClient.GetAsync($"https://observations.buienradar.nl/1.0/actual/weatherstation/{stationId}");
//                    if (response.IsSuccessStatusCode)
//                    {
//                        var text = await response.Content.ReadAsStringAsync();
//                        var obj = JsonConvert.DeserializeObject<dynamic>(text);

//                        response = await httpClient.GetAsync($"https://forecast.buienradar.nl/2.0/forecast/{locationId}");
//                        text = await response.Content.ReadAsStringAsync();
//                        var forecast = JsonConvert.DeserializeObject<Forecast>(text);
//                        var day0 = forecast?.Items[0];

//                        builder.AppendLine($"Observations for **{locationName}**");
//                        builder.AppendLine();
//                        builder.AppendLine($"Sunrise: **{FixInt(day0.Sunrise.Hour)}:{FixInt(day0.Sunrise.Minute)}** __*//*__ Sunset: **{FixInt(day0.Sunset.Hour)}:{FixInt(day0.Sunset.Minute)}**");
//                        builder.AppendLine();
//                        builder.AppendLine("**Temperature (°C)**");
//                        builder.AppendLine($"Temperature: **{obj?.temperature}°**");
//                        builder.AppendLine($"Feel temperature: **{obj?.feeltemperature}°**");
//                        builder.AppendLine($"Ground temperature: **{obj?.groundtemperature}°**");
//                        builder.AppendLine();
//                        builder.AppendLine("**Wind**");
//                        builder.AppendLine($"Wind speed: **{obj?.windspeedBft} Bft**");
//                        builder.AppendLine($"Wind direction: **{obj?.winddirection}**");
//                        builder.AppendLine($"Wind gusts: **{obj?.windgusts} m/s**");
//                        builder.AppendLine();
//                        builder.AppendLine("**Other atmospheric properties**");
//                        builder.AppendLine($"Air pressure: **{obj?.airpressure} hPa**");
//                        builder.AppendLine($"Visibility: **{obj?.visibility} m**");
//                        builder.AppendLine($"Humidity: **{obj?.humidity}%**");
//                        builder.AppendLine();
//                        builder.AppendLine("**Rain statistics**");
//                        builder.AppendLine($"Precipitation: **{obj?.precipitation} mm**");
//                        builder.AppendLine($"Precipation: **{obj?.precipation} mm**");
//                        builder.AppendLine($"Rainfall last 24 hours: **{obj?.rainFallLast24Hour} mm**");
//                        builder.AppendLine($"Rainfall last hour: **{obj?.rainFallLastHour} mm**");
//                        builder.AppendLine();
//                        builder.AppendLine("**Forecast per hour**");
//                        foreach (var hour in day0.Hours)
//                        {
//                            builder.AppendLine($"**{FixInt(hour.DateTime.Hour)}:{FixInt(hour.DateTime.Minute)}** - **{GetWeatherText(hour.IconCode)}** - Cloud cover: **{hour.CloudCover}%**");
//                            builder.AppendLine($"Temp: **{hour.Temperature}°**, Wind: **{hour.Beaufort} Bft** from the **{hour.WindDirection}**, Precipitation: **{hour.PrecipitationMm} mm**");
//                            builder.AppendLine();
//                        }
//                        builder.AppendLine();

//                        builder.AppendLine("*Data provided by **Buienradar** *");

//                        embedBuilder = new DiscordEmbedBuilder()
//                        {
//                            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
//                            {
//                                Url = $"https://cdn.buienradar.nl/resources/images/icons/weather/116x116/{obj?.iconcode}.png",
//                                Height = 117,
//                                Width = 117
//                            },
//                            Description = builder.ToString(),
//                        };

//                        await e.Channel.SendMessageAsync(embed: embedBuilder);
//                    }

//                    response.Dispose();

//                    break;
//            }
//        }
//    };

public partial class Observation
{
    [JsonProperty("stationid", NullValueHandling = NullValueHandling.Ignore)]
    public long? Stationid { get; set; }

    [JsonProperty("stationname", NullValueHandling = NullValueHandling.Ignore)]
    public string Stationname { get; set; }

    [JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
    public double? Lat { get; set; }

    [JsonProperty("lon", NullValueHandling = NullValueHandling.Ignore)]
    public double? Lon { get; set; }

    [JsonProperty("regio", NullValueHandling = NullValueHandling.Ignore)]
    public string Regio { get; set; }

    [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
    public string Timestamp { get; set; }

    [JsonProperty("iconcode", NullValueHandling = NullValueHandling.Ignore)]
    public string Iconcode { get; set; }

    [JsonProperty("winddirection", NullValueHandling = NullValueHandling.Ignore)]
    public string Winddirection { get; set; }

    [JsonProperty("airpressure", NullValueHandling = NullValueHandling.Ignore)]
    public double? Airpressure { get; set; }

    [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Temperature { get; set; }

    [JsonProperty("groundtemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Groundtemperature { get; set; }

    [JsonProperty("feeltemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Feeltemperature { get; set; }

    [JsonProperty("visibility", NullValueHandling = NullValueHandling.Ignore)]
    public long? Visibility { get; set; }

    [JsonProperty("windgusts", NullValueHandling = NullValueHandling.Ignore)]
    public double? Windgusts { get; set; }

    [JsonProperty("windspeed", NullValueHandling = NullValueHandling.Ignore)]
    public long? Windspeed { get; set; }

    [JsonProperty("windspeedBft", NullValueHandling = NullValueHandling.Ignore)]
    public long? WindspeedBft { get; set; }

    [JsonProperty("humidity", NullValueHandling = NullValueHandling.Ignore)]
    public long? Humidity { get; set; }

    [JsonProperty("precipitation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipitation { get; set; }

    [JsonProperty("precipation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipation { get; set; }

    [JsonProperty("precipitationmm", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipitationmm { get; set; }

    [JsonProperty("rainFallLast24Hour", NullValueHandling = NullValueHandling.Ignore)]
    public long? RainFallLast24Hour { get; set; }

    [JsonProperty("rainFallLastHour", NullValueHandling = NullValueHandling.Ignore)]
    public long? RainFallLastHour { get; set; }

    [JsonProperty("winddirectiondegrees", NullValueHandling = NullValueHandling.Ignore)]
    public long? Winddirectiondegrees { get; set; }
}

public partial class Forecast
{
    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public Location Location { get; set; }

    [JsonProperty("timeOffset", NullValueHandling = NullValueHandling.Ignore)]
    public long? TimeOffset { get; set; }

    [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
    public string Timestamp { get; set; }

    [JsonProperty("altitude", NullValueHandling = NullValueHandling.Ignore)]
    public long? Altitude { get; set; }

    [JsonProperty("elevation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Elevation { get; set; }

    [JsonProperty("machinename", NullValueHandling = NullValueHandling.Ignore)]
    public string Machinename { get; set; }

    [JsonProperty("elapsedms", NullValueHandling = NullValueHandling.Ignore)]
    public long? Elapsedms { get; set; }

    [JsonProperty("pollenindexNowDay", NullValueHandling = NullValueHandling.Ignore)]
    public long? PollenindexNowDay { get; set; }

    [JsonProperty("pollenindexNowHour", NullValueHandling = NullValueHandling.Ignore)]
    public long? PollenindexNowHour { get; set; }

    [JsonProperty("nowrelevant", NullValueHandling = NullValueHandling.Ignore)]
    public Nowrelevant Nowrelevant { get; set; }

    [JsonProperty("days", NullValueHandling = NullValueHandling.Ignore)]
    public Day[] Days { get; set; }
}

public partial class Day
{
    [JsonProperty("date", NullValueHandling = NullValueHandling.Ignore)]
    public string Date { get; set; }

    [JsonProperty("sunrise", NullValueHandling = NullValueHandling.Ignore)]
    public string Sunrise { get; set; }

    [JsonProperty("sunset", NullValueHandling = NullValueHandling.Ignore)]
    public string Sunset { get; set; }

    [JsonProperty("afternoon", NullValueHandling = NullValueHandling.Ignore)]
    public Afternoon Afternoon { get; set; }

    [JsonProperty("evening", NullValueHandling = NullValueHandling.Ignore)]
    public Evening Evening { get; set; }

    [JsonProperty("hours", NullValueHandling = NullValueHandling.Ignore)]
    public Hour[] Hours { get; set; }

    [JsonProperty("datetime", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetime { get; set; }

    [JsonProperty("datetimeutc", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetimeutc { get; set; }

    [JsonProperty("timetype", NullValueHandling = NullValueHandling.Ignore)]
    public string Timetype { get; set; }

    [JsonProperty("precipitationmm", NullValueHandling = NullValueHandling.Ignore)]
    public double? Precipitationmm { get; set; }

    [JsonProperty("mintemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Mintemperature { get; set; }

    [JsonProperty("maxtemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Maxtemperature { get; set; }

    [JsonProperty("cloudcover", NullValueHandling = NullValueHandling.Ignore)]
    public long? Cloudcover { get; set; }

    [JsonProperty("iconcode", NullValueHandling = NullValueHandling.Ignore)]
    public string Iconcode { get; set; }

    [JsonProperty("iconid", NullValueHandling = NullValueHandling.Ignore)]
    public long? Iconid { get; set; }

    [JsonProperty("sources", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sources { get; set; }

    [JsonProperty("winddirection", NullValueHandling = NullValueHandling.Ignore)]
    public string Winddirection { get; set; }

    [JsonProperty("winddirectiondegrees", NullValueHandling = NullValueHandling.Ignore)]
    public long? Winddirectiondegrees { get; set; }

    [JsonProperty("windspeedms", NullValueHandling = NullValueHandling.Ignore)]
    public double? Windspeedms { get; set; }

    [JsonProperty("visibility", NullValueHandling = NullValueHandling.Ignore)]
    public long? Visibility { get; set; }

    [JsonProperty("precipitation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipitation { get; set; }

    [JsonProperty("beaufort", NullValueHandling = NullValueHandling.Ignore)]
    public long? Beaufort { get; set; }

    [JsonProperty("humidity", NullValueHandling = NullValueHandling.Ignore)]
    public long? Humidity { get; set; }

    [JsonProperty("sunshine", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshine { get; set; }

    [JsonProperty("uvindex", NullValueHandling = NullValueHandling.Ignore)]
    public long? Uvindex { get; set; }

    [JsonProperty("bbqindex", NullValueHandling = NullValueHandling.Ignore)]
    public long? Bbqindex { get; set; }

    [JsonProperty("mosquitoindex", NullValueHandling = NullValueHandling.Ignore)]
    public long? Mosquitoindex { get; set; }

    [JsonProperty("pollenindex", NullValueHandling = NullValueHandling.Ignore)]
    public long? Pollenindex { get; set; }

    [JsonProperty("airqualityindex", NullValueHandling = NullValueHandling.Ignore)]
    public long? Airqualityindex { get; set; }

    [JsonProperty("mintemp", NullValueHandling = NullValueHandling.Ignore)]
    public double? Mintemp { get; set; }

    [JsonProperty("maxtemp", NullValueHandling = NullValueHandling.Ignore)]
    public double? Maxtemp { get; set; }

    [JsonProperty("windspeed", NullValueHandling = NullValueHandling.Ignore)]
    public long? Windspeed { get; set; }

    [JsonProperty("sunshinepower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshinepower { get; set; }

    [JsonProperty("sunpower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunpower { get; set; }

    [JsonProperty("morning", NullValueHandling = NullValueHandling.Ignore)]
    public Afternoon Morning { get; set; }

    [JsonProperty("night", NullValueHandling = NullValueHandling.Ignore)]
    public Afternoon Night { get; set; }

    [JsonProperty("icescrapeindex", NullValueHandling = NullValueHandling.Ignore)]
    public long? Icescrapeindex { get; set; }
}

public partial class Afternoon
{
    [JsonProperty("datetime", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetime { get; set; }

    [JsonProperty("datetimeutc", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetimeutc { get; set; }

    [JsonProperty("timetype", NullValueHandling = NullValueHandling.Ignore)]
    public string Timetype { get; set; }

    [JsonProperty("precipitationmm", NullValueHandling = NullValueHandling.Ignore)]
    public double? Precipitationmm { get; set; }

    [JsonProperty("mintemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Mintemperature { get; set; }

    [JsonProperty("maxtemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Maxtemperature { get; set; }

    [JsonProperty("cloudcover", NullValueHandling = NullValueHandling.Ignore)]
    public long? Cloudcover { get; set; }

    [JsonProperty("iconcode", NullValueHandling = NullValueHandling.Ignore)]
    public string Iconcode { get; set; }

    [JsonProperty("iconid", NullValueHandling = NullValueHandling.Ignore)]
    public long? Iconid { get; set; }

    [JsonProperty("sources", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sources { get; set; }

    [JsonProperty("winddirection", NullValueHandling = NullValueHandling.Ignore)]
    public string Winddirection { get; set; }

    [JsonProperty("winddirectiondegrees", NullValueHandling = NullValueHandling.Ignore)]
    public long? Winddirectiondegrees { get; set; }

    [JsonProperty("windspeedms", NullValueHandling = NullValueHandling.Ignore)]
    public double? Windspeedms { get; set; }

    [JsonProperty("visibility", NullValueHandling = NullValueHandling.Ignore)]
    public long? Visibility { get; set; }

    [JsonProperty("precipitation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipitation { get; set; }

    [JsonProperty("beaufort", NullValueHandling = NullValueHandling.Ignore)]
    public long? Beaufort { get; set; }

    [JsonProperty("humidity", NullValueHandling = NullValueHandling.Ignore)]
    public long? Humidity { get; set; }

    [JsonProperty("sunshine", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshine { get; set; }

    [JsonProperty("hour", NullValueHandling = NullValueHandling.Ignore)]
    public long? Hour { get; set; }

    [JsonProperty("mintemp", NullValueHandling = NullValueHandling.Ignore)]
    public double? Mintemp { get; set; }

    [JsonProperty("maxtemp", NullValueHandling = NullValueHandling.Ignore)]
    public double? Maxtemp { get; set; }

    [JsonProperty("windspeed", NullValueHandling = NullValueHandling.Ignore)]
    public long? Windspeed { get; set; }

    [JsonProperty("sunshinepower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshinepower { get; set; }

    [JsonProperty("sunpower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunpower { get; set; }
}

public partial class Evening
{
    [JsonProperty("datetime", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetime { get; set; }

    [JsonProperty("datetimeutc", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetimeutc { get; set; }

    [JsonProperty("timetype", NullValueHandling = NullValueHandling.Ignore)]
    public string Timetype { get; set; }

    [JsonProperty("precipitationmm", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipitationmm { get; set; }

    [JsonProperty("mintemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Mintemperature { get; set; }

    [JsonProperty("maxtemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Maxtemperature { get; set; }

    [JsonProperty("cloudcover", NullValueHandling = NullValueHandling.Ignore)]
    public long? Cloudcover { get; set; }

    [JsonProperty("iconcode", NullValueHandling = NullValueHandling.Ignore)]
    public string Iconcode { get; set; }

    [JsonProperty("iconid", NullValueHandling = NullValueHandling.Ignore)]
    public long? Iconid { get; set; }

    [JsonProperty("sources", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sources { get; set; }

    [JsonProperty("winddirection", NullValueHandling = NullValueHandling.Ignore)]
    public string Winddirection { get; set; }

    [JsonProperty("winddirectiondegrees", NullValueHandling = NullValueHandling.Ignore)]
    public long? Winddirectiondegrees { get; set; }

    [JsonProperty("windspeedms", NullValueHandling = NullValueHandling.Ignore)]
    public double? Windspeedms { get; set; }

    [JsonProperty("visibility", NullValueHandling = NullValueHandling.Ignore)]
    public long? Visibility { get; set; }

    [JsonProperty("precipitation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipitation { get; set; }

    [JsonProperty("beaufort", NullValueHandling = NullValueHandling.Ignore)]
    public long? Beaufort { get; set; }

    [JsonProperty("humidity", NullValueHandling = NullValueHandling.Ignore)]
    public long? Humidity { get; set; }

    [JsonProperty("sunshine", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshine { get; set; }

    [JsonProperty("hour", NullValueHandling = NullValueHandling.Ignore)]
    public long? Hour { get; set; }

    [JsonProperty("mintemp", NullValueHandling = NullValueHandling.Ignore)]
    public double? Mintemp { get; set; }

    [JsonProperty("maxtemp", NullValueHandling = NullValueHandling.Ignore)]
    public double? Maxtemp { get; set; }

    [JsonProperty("windspeed", NullValueHandling = NullValueHandling.Ignore)]
    public long? Windspeed { get; set; }

    [JsonProperty("sunshinepower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshinepower { get; set; }

    [JsonProperty("sunpower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunpower { get; set; }
}

public partial class Hour
{
    [JsonProperty("datetime", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetime { get; set; }

    [JsonProperty("datetimeutc", NullValueHandling = NullValueHandling.Ignore)]
    public string Datetimeutc { get; set; }

    [JsonProperty("timetype", NullValueHandling = NullValueHandling.Ignore)]
    public string Timetype { get; set; }

    [JsonProperty("precipitationmm", NullValueHandling = NullValueHandling.Ignore)]
    public double? Precipitationmm { get; set; }

    [JsonProperty("temperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Temperature { get; set; }

    [JsonProperty("feeltemperature", NullValueHandling = NullValueHandling.Ignore)]
    public double? Feeltemperature { get; set; }

    [JsonProperty("cloudcover", NullValueHandling = NullValueHandling.Ignore)]
    public long? Cloudcover { get; set; }

    [JsonProperty("iconcode", NullValueHandling = NullValueHandling.Ignore)]
    public string Iconcode { get; set; }

    [JsonProperty("iconid", NullValueHandling = NullValueHandling.Ignore)]
    public long? Iconid { get; set; }

    [JsonProperty("sources", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sources { get; set; }

    [JsonProperty("winddirection", NullValueHandling = NullValueHandling.Ignore)]
    public string Winddirection { get; set; }

    [JsonProperty("winddirectiondegrees", NullValueHandling = NullValueHandling.Ignore)]
    public long? Winddirectiondegrees { get; set; }

    [JsonProperty("windspeedms", NullValueHandling = NullValueHandling.Ignore)]
    public double? Windspeedms { get; set; }

    [JsonProperty("visibility", NullValueHandling = NullValueHandling.Ignore)]
    public long? Visibility { get; set; }

    [JsonProperty("precipitation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Precipitation { get; set; }

    [JsonProperty("beaufort", NullValueHandling = NullValueHandling.Ignore)]
    public long? Beaufort { get; set; }

    [JsonProperty("humidity", NullValueHandling = NullValueHandling.Ignore)]
    public long? Humidity { get; set; }

    [JsonProperty("sunshine", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshine { get; set; }

    [JsonProperty("hour", NullValueHandling = NullValueHandling.Ignore)]
    public long? HourHour { get; set; }

    [JsonProperty("pollenindex", NullValueHandling = NullValueHandling.Ignore)]
    public long? Pollenindex { get; set; }

    [JsonProperty("windspeed", NullValueHandling = NullValueHandling.Ignore)]
    public long? Windspeed { get; set; }

    [JsonProperty("sunshinepower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunshinepower { get; set; }

    [JsonProperty("sunpower", NullValueHandling = NullValueHandling.Ignore)]
    public long? Sunpower { get; set; }
}

public partial class Location
{
    [JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
    public double? Lat { get; set; }

    [JsonProperty("lon", NullValueHandling = NullValueHandling.Ignore)]
    public double? Lon { get; set; }
}

public partial class Nowrelevant
{
    [JsonProperty("values", NullValueHandling = NullValueHandling.Ignore)]
    public Value[] Values { get; set; }
}

public partial class Value
{
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    public double? ValueValue { get; set; }
}

public partial class SearchResult
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public long? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("asciiname", NullValueHandling = NullValueHandling.Ignore)]
    public string Asciiname { get; set; }

    [JsonProperty("alternatenames", NullValueHandling = NullValueHandling.Ignore)]
    public string[] Alternatenames { get; set; }

    [JsonProperty("countrycode", NullValueHandling = NullValueHandling.Ignore)]
    public string Countrycode { get; set; }

    [JsonProperty("country", NullValueHandling = NullValueHandling.Ignore)]
    public string Country { get; set; }

    [JsonProperty("featurecode", NullValueHandling = NullValueHandling.Ignore)]
    public string Featurecode { get; set; }

    [JsonProperty("location", NullValueHandling = NullValueHandling.Ignore)]
    public Location Location { get; set; }

    [JsonProperty("boundaries")]
    public object Boundaries { get; set; }

    [JsonProperty("altitude", NullValueHandling = NullValueHandling.Ignore)]
    public long? Altitude { get; set; }

    [JsonProperty("elevation", NullValueHandling = NullValueHandling.Ignore)]
    public long? Elevation { get; set; }

    [JsonProperty("weatherstationid", NullValueHandling = NullValueHandling.Ignore)]
    public long? Weatherstationid { get; set; }

    [JsonProperty("weatherstationdistance", NullValueHandling = NullValueHandling.Ignore)]
    public double? Weatherstationdistance { get; set; }

    [JsonProperty("continent", NullValueHandling = NullValueHandling.Ignore)]
    public string Continent { get; set; }

    [JsonProperty("foad", NullValueHandling = NullValueHandling.Ignore)]
    public Foad Foad { get; set; }

    [JsonProperty("hidefromsearch", NullValueHandling = NullValueHandling.Ignore)]
    public string Hidefromsearch { get; set; }
}

public partial class Foad
{
    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty("code", NullValueHandling = NullValueHandling.Ignore)]
    public string Code { get; set; }
}