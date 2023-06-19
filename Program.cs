using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using VLC_Media.MediaParser;

internal class Program
{
    public static PlaylistSearcher? PlaylistSearcher;
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

        var byteArray = Encoding.ASCII.GetBytes(":F!nley19g7");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        await LoadStatusAsync();
        await LoadPlaylistAsync();

        PlaylistSearcher = new PlaylistSearcher(Playlist);

        var embed = new DiscordEmbedBuilder();

        discordClient.MessageCreated += async (sender, message) =>
        {
            var internalMessage = message.Message;
            var content = internalMessage.Content;

            //await GetStatusAsync();
            //await GetGuideAsync();

            if (content.StartsWith('!'))
            {
                await message.Channel.TriggerTypingAsync();

                var parts = ExtractCommand(content);
                Console.WriteLine(parts.Length);

                foreach (Group part in parts)
                    Console.WriteLine($"{part.Index} {part.Value}");

                switch (parts[0].Value.ToLower())
                {
                    case "!weather":

                        if (parts.Length > 1)
                            if (parts[1].Value != string.Empty)
                            {
                                var query = FromGroups(parts[1..parts.Length], "%20");
                                var results = await GetAsync<SearchResult[]>($"https://location.buienradar.nl/1.1/location/search?query={query}");

                                if (!results.Any())
                                {
                                    await internalMessage.Channel.SendMessageAsync("No location was found from the given query");
                                }

                                SearchResult = results.FirstOrDefault();

                                embed.ClearFields();
                                embed = await GetWeatherAsync();

                                await internalMessage.Channel.SendMessageAsync(embed);
                            }
                        break;

                    case "!skip":
                        if (parts.Length > 1)
                        {
                            if (parts[1].Value != string.Empty)
                            {
                                Console.WriteLine(parts[1].Value);

                                var index = Convert.ToInt32(parts[1].Value);
                                var item = Playlist.Items.FirstOrDefault(item => item.Value.Id == index);

                                Console.WriteLine(item.Value.Id);

                                if (item.Value != null)
                                {
                                    embed.ClearFields();
                                    embed = await SkipAsync(item.Value);
                                    await internalMessage.Channel.SendMessageAsync(embed);
                                }
                                else
                                {
                                    await internalMessage.Channel.SendMessageAsync("No item was found with the given ID");
                                }
                            }
                        }
                        else
                        {
                            embed.ClearFields();
                            embed = await SkipAsync();
                            await internalMessage.Channel.SendMessageAsync(embed);
                        }
                        break;

                    case "!search":
                        embed.ClearFields();

                        var terms = parts[0];

                        var episode = parts
                            .FirstOrDefault(part => part.Value.StartsWith("episode"))?
                            .Value;
                        var season = parts
                            .FirstOrDefault(part => part.Value.StartsWith("season"))?
                            .Value;
                        var year = parts
                            .FirstOrDefault(part => part.Value.StartsWith("year"))?
                            .Value;

                        var yearValue = string.Empty;
                        var episodeValue = string.Empty;
                        var seasonEpisiode = string.Empty;

                        if (episode != null)
                        {
                            episodeValue = episode.Split('=')[1];
                        }

                        if (year != null)
                        {
                            yearValue = year.Split("=")[1];
                        }

                        if (season != null)
                        {
                            seasonEpisiode = season.Split("=")[1];
                        }

                        var squery = new SearchQuery(terms.Value);

                        if (squery == null)
                            await internalMessage.Channel.SendMessageAsync("Zoekveld was leeg");

                        await PlaylistSearcher.Search(squery);

                        PlaylistSearcher.SearchCompleted += async (s, e) =>
                        {
                            var builder = new StringBuilder();

                            foreach (var result in e.Results)
                            {
                                Group[] mediaInfo = ParseMediaInfo(result.Value.Name);
                                var name = mediaInfo.FirstOrDefault(group => group.Name == "title");
                                var year = mediaInfo.FirstOrDefault(group => group.Name == "year");
                                var episode = mediaInfo.FirstOrDefault(group => group.Name == "episode");
                                var season = mediaInfo.FirstOrDefault(group => group.Name == "season");

                                builder.AppendLine($"**{name.Value}**");
                                builder.AppendLine($"Index **{result.Key}**");
                                builder.AppendLine($"Episode **{episode.Value}**");
                                builder.AppendLine($"Season **{season.Value}**");
                                builder.AppendLine();
                            }

                            embed = new DiscordEmbedBuilder()
                            {
                                Description = builder.ToString()
                            };

                            await internalMessage.Channel.SendMessageAsync(embed);
                        };

                        break;

                    case "!play":
                        embed.ClearFields();
                        embed = await PlayAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!pause":
                        embed.ClearFields();
                        embed = await PauseAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!status":
                        embed.ClearFields();
                        embed = await GetStatusAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!guide":
                        embed.ClearFields();
                        embed = await GetGuideAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!help":
                        embed.ClearFields();
                        embed = GetHelp();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!request":
                        if (parts.Length > 1)
                            if (parts[1].Value != string.Empty)
                            {
                                Console.WriteLine(parts[1].Value);
                                var name = FromGroups(parts[1..parts.Length], " ");

                                embed.ClearFields();
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

    public static async Task<DiscordEmbedBuilder> SearchAsync(SearchQuery query)
    {
        if (query == null)
            return new DiscordEmbedBuilder()
            {
                Description = "Het zoekveld was leeg"
            };

        var builder = new StringBuilder();

        PlaylistSearcher.SearchCompleted += (s, e) =>
        {
            foreach (var result in e.Results)
            {
                Group[] mediaInfo = ParseMediaInfo(result.Value.Name);
                var name = mediaInfo.FirstOrDefault(group => group.Name == "title");
                var year = mediaInfo.FirstOrDefault(group => group.Name == "year");
                var episode = mediaInfo.FirstOrDefault(group => group.Name == "episode");
                var season = mediaInfo.FirstOrDefault(group => group.Name == "season");

                builder.AppendLine($"**{name.Value}**");
                builder.AppendLine($"Index **{result.Key}**");
                builder.AppendLine($"Episode **{episode.Value}**");
                builder.AppendLine($"Season **{season.Value}**");
                builder.AppendLine();
            }
        };

        await PlaylistSearcher.Search(query);

        return new DiscordEmbedBuilder()
        {
            Description = builder.ToString()
        };
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
        builder.AppendLine("**!skip *[index]* ** __*//*__ Skips to the next item in the playlist, you can also skip to a specific item when you also give an **index** parameter. Here you need to get the index number of the item you want to skip to, you can see this in the **Guide** function next to **Index: **.");
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
            var name = mediaInfo.FirstOrDefault(group => group.Name == "title");
            var year = mediaInfo.FirstOrDefault(group => group.Name == "year");
            var episode = mediaInfo.FirstOrDefault(group => group.Name == "episode");
            var season = mediaInfo.FirstOrDefault(group => group.Name == "season");

            var pos = TimeSpan.FromSeconds(Status.Position);
            var len = TimeSpan.FromSeconds(Status.Length);
            var now = DateTime.Now.TimeOfDay;
            var left = now + (len - pos);

            builder.AppendLine($"**Now** - **{FixInt(left.Hours)}:{FixInt(left.Minutes)}**");

            if (name != null)
            {
                if (name.Value != string.Empty)
                {
                    builder.AppendLine($"{CleanShowName(name.Value)}");
                    builder.AppendLine($"Index **{Status.Id}**");
                }
                else
                {
                    builder.AppendLine($"{Status.Information.Title}");
                    builder.AppendLine($"Index ** {Status.Id} **");
                }
            }
            else
            {
                builder.Append($"[{Status.Id}] {Status.Information.Title}");
            }

            if (year != null)
            {
                if (year.Value != string.Empty)
                    builder.AppendLine($" **({year.Value})**");
                //else
                //    builder.AppendLine();
            }

            if (episode != null)
            {
                if (episode.Value != string.Empty)
                {
                    builder.AppendLine($"Season **{season.Value}**");
                    builder.AppendLine($"Episode **{episode.Value}**");
                }
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
            var index = GetIndexFromItem();

            #region Write status

            Group[] mediaInfo = ParseMediaInfo(Status.Information.Category.Meta.Filename);
            var name = mediaInfo.FirstOrDefault(group => group.Name == "title");
            var year = mediaInfo.FirstOrDefault(group => group.Name == "year");
            var episode = mediaInfo.FirstOrDefault(group => group.Name == "episode");
            var season = mediaInfo.FirstOrDefault(group => group.Name == "season");

            var pos = TimeSpan.FromSeconds(Status.Position);
            var len = TimeSpan.FromSeconds(Status.Length);
            var now = DateTime.Now.TimeOfDay;
            var left = now + (len - pos);

            builder.AppendLine($"**Now** - **{FixInt(left.Hours)}:{FixInt(left.Minutes)}**");

            if (name != null)
            {
                if (name.Value != string.Empty)
                {
                    builder.AppendLine($"{CleanShowName(name.Value)}");
                    builder.AppendLine($"Index **{Status.Id}**");
                }
                else
                {
                    builder.AppendLine($"{Status.Information.Title}");
                    builder.AppendLine($"Index ** {Status.Id} **");
                }
            }
            else
            {
                builder.Append($"[{Status.Id}] {Status.Information.Title}");
            }

            if (year != null)
            {
                if (year.Value != string.Empty)
                    builder.AppendLine($" **({year.Value})**");
                //else
                //    builder.AppendLine();
            }

            if (episode != null)
            {
                if (episode.Value != string.Empty)
                {
                    builder.AppendLine($"Season **{season.Value}**");
                    builder.AppendLine($"Episode **{episode.Value}**");
                }
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
                    name = mediaInfo.FirstOrDefault(group => group.Name == "title");
                    year = mediaInfo.FirstOrDefault(group => group.Name == "year");
                    episode = mediaInfo.FirstOrDefault(group => group.Name == "episode");
                    season = mediaInfo.FirstOrDefault(group => group.Name == "season");

                    var itemPosPlusDuration = itemPos + TimeSpan.FromSeconds(item.Value.Duration);

                    builder.AppendLine($"**{FixInt(itemPos.Hours)}:{FixInt(itemPos.Minutes)}** - **{FixInt(itemPosPlusDuration.Hours)}:{FixInt(itemPosPlusDuration.Minutes)}**");

                    if (name != null)
                    {
                        if (name.Value != string.Empty)
                        {
                            builder.AppendLine($"{CleanShowName(name.Value)}");
                            builder.AppendLine($"Index **{item.Value.Id}**");
                        }
                        else
                        {
                            builder.AppendLine($"{item.Value.Name}");
                            builder.AppendLine($"Index **{item.Value.Id}**");
                        }
                    }
                    else
                    {
                        builder.Append($"[{item.Value.Id}] {item.Value.Name}");
                    }

                    if (year != null)
                    {
                        if (year.Value != string.Empty)
                            builder.AppendLine($" **({year.Value})**");
                        //else
                        //    builder.AppendLine();
                    }

                    if (episode != null)
                    {
                        if (episode.Value != string.Empty)
                        {
                            builder.AppendLine($"Season **{season.Value}**");
                            builder.AppendLine($"Episode **{episode.Value}**");
                        }
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

    private static int GetIndexFromItem()
    {
        if (Playlist != null)
        {
            for (int i = 0; i < Playlist.Items.Count; i++)
            {
                if (Playlist.Items[i].Id == Status.Id)
                    return i;
            }
        }

        Console.WriteLine("Playlist was null");

        throw new NullReferenceException(nameof(Status));
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
        var groups = new List<Group>();

        groups.AddRange(RegexData.ReportTitleRegex
            .Where(regex =>
            {
                if (regex.IsMatch(fileName))
                {
                    return true;
                }

                return false;
            })
            .OrderByDescending(regex => regex.Match(fileName).Groups.Count)
            .FirstOrDefault()
            .Match(fileName)
            .Groups);

        groups.AddRange(RegexData.YearInTitleRegex
            .Match(fileName).Groups);

        return groups.ToArray();
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
                    if (!groupList.Any(grp => grp.Value == group.Value))
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

public class SearchQuery
{
    public string SearchTerms { get; set; }
    public string Episode { get; set; }
    public string Season { get; set; }
    public string Year { get; set; }

    public SearchQuery(string searchTerms, string episode = "", string season = "", string year = "")
    {
        SearchTerms = searchTerms ?? throw new ArgumentNullException(nameof(searchTerms));
        Episode = episode ?? throw new ArgumentNullException(nameof(episode));
        Season = season ?? throw new ArgumentNullException(nameof(season));
        Year = year ?? throw new ArgumentNullException(nameof(year));
    }
}

public class PlaylistSearcher
{
    public Playlist? Playlist;

    public event EventHandler<SearchCompletedEventArgs>? SearchCompleted;

    public PlaylistSearcher(Playlist? playlist)
    {
        Playlist = playlist;
    }

    private List<KeyValuePair<int, Item>> results = new List<KeyValuePair<int, Item>>();

    public Task Search(SearchQuery query)
    {
        if (Playlist == null)
            return Task.CompletedTask;

        results.Clear();

        Array.ForEach(Playlist.Items.ToArray(), async item =>
        {
            await Task.Run(() => SearchChildren(item.Value, query));
        });

        return Task.CompletedTask;
    }

    private Group[] ParseMediaInfo(string fileName)
    {
        var groups = new List<Group>();

        groups.AddRange(RegexData.ReportTitleRegex
            .Where(regex =>
            {
                if (regex.IsMatch(fileName))
                {
                    return true;
                }

                return false;
            })
            .OrderByDescending(regex => regex.Match(fileName).Groups.Count)
            .FirstOrDefault()
            .Match(fileName)
            .Groups);

        groups.AddRange(RegexData.YearInTitleRegex
            .Match(fileName).Groups);

        return groups.ToArray();
    }

    private void SearchChildren(Item item, SearchQuery query)
    {
        if (item.Children != null && item.Children.Any())
        {
            for (int i = 0; i < item.Children.Length; i++)
            {
                var child = item.Children[i];
                if (child != null) continue;
                var info = ParseMediaInfo(child.Name);
                var title = info
                    .FirstOrDefault(grp => grp.Name == "title")
                    .Value;
                var episode = info
                    .FirstOrDefault(grp => grp.Name == "episode")
                    .Value;
                var season = info
                    .FirstOrDefault(grp => grp.Name == "season")
                    .Value;
                var year = info
                    .FirstOrDefault(grp => grp.Name == "year")
                    .Value;

                if (child.Name.Contains(title) && (episode == "01" && season == "01"))
                {
                    results.Add(new KeyValuePair<int, Item>(results.Count, child));
                }

                if (child.Children != null && child.Children.Any())
                {
                    SearchChildren(child, query);
                }
                else
                {
                    if (i == item.Children.Length)
                    {
                        SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(results));
                    }
                }
            }
        }
    }
}

public class SearchCompletedEventArgs
{
    public List<KeyValuePair<int, Item>> Results { get; }

    public SearchCompletedEventArgs(List<KeyValuePair<int, Item>> results)
    {
        Results = results ?? throw new ArgumentNullException(nameof(results));
    }
}