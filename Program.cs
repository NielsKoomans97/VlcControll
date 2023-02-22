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

    #region Public Tasks and Events

    public static async Task Main(string[] args)
    {
        DiscordToken = await File.ReadAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}\\token");
        httpClient = new HttpClient();
        discordClient = new DiscordClient(StandardConfig);

        var byteArray = Encoding.ASCII.GetBytes(":[password]");
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
                                var query = FromGroups(parts[3..parts.Length], "%20");
                                var results = await GetAsync<SearchResult[]>($"https://location.buienradar.nl/1.1/location/search?query={query}");

                                Console.WriteLine(query);

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

                    case "!request":
                        if (parts.Length > 2)
                            if (parts[3].Value != string.Empty)
                            {
                                Console.WriteLine(parts[3].Value);
                                var name = FromGroups(parts[3..parts.Length], " ");

                                embed = new DiscordEmbedBuilder();
                                embed = await MakeRequestAsync(name);

                                await internalMessage.Channel.SendMessageAsync(embed);
                            }
                        break;
                }
            }
        };

        await discordClient.ConnectAsync();

        await Task.Delay(-1);
    }

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

        Status = null;
        Status = await GetAsync<Status>(statusUrl);

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
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
            {
                Url = $"https://cdn.buienradar.nl/resources/images/icons/weather/116x116/{Observation.Iconcode}.png"
            },
            Description = WriteWeather()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> MakeRequestAsync(string name)
    {
        var requests = new Dictionary<int, string>();
        if (File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}\\requests.json"))
        {
            var text = await File.ReadAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}\\requests.json");
            requests = JsonConvert.DeserializeObject<Dictionary<int, string>>(text)
                ?? throw new JsonException("Could not deserialize requests file content to dictionary");
        }

        if (requests.ContainsValue(name))
        {
            var embed = new DiscordEmbedBuilder();
            var builder = new StringBuilder("This series or movie was already requested. Please try again with a different item that's not in the list yet. Or better yet, don't be a sad mindless idiot, idiot.");
            builder.AppendLine(" ");
            builder.AppendLine("Existing items in the request list:");
            builder.AppendLine(" ");
            foreach (var item in requests)
            {
                builder.AppendLine($"[{item.Key}]   {item.Value}");
            }

            embed.Description = builder.ToString();
            return embed;
        }

        requests.Add(requests.Count, name);
        await File.WriteAllTextAsync($"{AppDomain.CurrentDomain.BaseDirectory}\\requests.json", JsonConvert.SerializeObject(requests));

        return new DiscordEmbedBuilder() { Description = $"{name} was added to the requests list" };
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

    #endregion Public Tasks and Events

    #region Writing the actual message

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
        builder.AppendLine("**!request *[name]* ** __*//*__ Make a request for a series or movie to be added");
        builder.AppendLine("**!weather *[location]* ** __*//*__ Show observational, and forecasted weather data for the given location");
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

        var tsSunrise = DateTime.Parse(day0.Sunrise).TimeOfDay;
        var tsSunset = DateTime.Parse(day0.Sunset).TimeOfDay;

        builder.AppendLine($"Observations for **{SearchResult?.Name}**");
        builder.AppendLine();
        builder.AppendLine($"Sunrise: **{FixInt(tsSunrise.Hours)}:{FixInt(tsSunrise.Minutes)}** __*//*__ Sunset: **{FixInt(tsSunset.Hours)}:{FixInt(tsSunset.Minutes)}**");
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
            var tsHour = DateTime.Parse(hour.Datetimeutc).TimeOfDay;

            builder.AppendLine($"**{FixInt(tsHour.Hours)}:{FixInt(tsHour.Minutes)}** - **{GetWeatherText(hour.Iconcode)}** - Cloud cover: **{hour.Cloudcover}%**");
            builder.AppendLine($"Temp: **{hour.Temperature}°**, Wind: **{hour.Beaufort} Bft** from the **{hour.Winddirection}**, Precipitation: **{hour.Precipitationmm} mm**");
            builder.AppendLine();
        }
        builder.AppendLine();

        builder.AppendLine("*Data provided by **Buienradar** *");

        return builder.ToString();
    }

    #endregion Writing the actual message

    #region Other functionality

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

    private static string FromGroups(Group[] groups, string delimeter)
    {
        var builder = new StringBuilder();
        foreach (var group in groups)
        {
            if (!builder.ToString().Contains(group.Value))
            {
                builder.Append($"{delimeter}{group.Value}");
            }
        }

        return builder.ToString();
    }

    #endregion Other functionality
}
