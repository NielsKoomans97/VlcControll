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

    private static HttpClient? httpClient;
    private static DiscordClient? discordClient;

    private static string statusUrl = "http://192.168.2.161:8080/status.json";
    private static string playlistUrl = "http://192.168.2.161:8080/playlist.json";

    public static async Task Main(string[] args)
    {
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
            var embed = default(DiscordEmbedBuilder);

            //await GetStatusAsync();
            //await GetGuideAsync();

            if (content.StartsWith('!'))
            {
                await message.Channel.TriggerTypingAsync();

                var parts = ExtractCommand(content);

                switch (parts[1].Value)
                {
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
                                embed = await SkipAsync();
                                await internalMessage.Channel.SendMessageAsync(embed);
                            }
                        break;

                    case "!play":
                        embed = await PlayAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!pause":
                        embed = await PauseAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!status":
                        embed = await GetStatusAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!guide":
                        embed = await GetGuideAsync();
                        await internalMessage.Channel.SendMessageAsync(embed);
                        break;

                    case "!help":
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
        Token = "MTA2MzEyMTk2NDA0ODMyMjU5Mg.G7txjC.1TtpnqneHypYO0gXC5f6E9nlYKHErKwDCq8BAI",
        TokenType = TokenType.Bot,
        AutoReconnect = true,
        MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
    };

    public static async Task<T> GetAsync<T>(string requestUri)
    {
        try
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
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return default(T);
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
        Status = await GetAsync<Status>($"{statusUrl}");

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteStatus()
        };

        return embedBuilder;
    }

    public static async Task<DiscordEmbedBuilder> GetGuideAsync()
    {
        var items = await GetAsync<Item>(playlistUrl);
        Playlist = new Playlist(items.Children);

        var embedBuilder = new DiscordEmbedBuilder()
        {
            Description = WriteGuide()
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

            builder.AppendLine($"**{FixInt(now.Hours)}:{FixInt(now.Minutes)}** - **{FixInt(left.Hours)}:{FixInt(left.Minutes)}**");

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

            builder.AppendLine($"**{FixInt(now.Hours)}:{FixInt(now.Minutes)}** - **{FixInt(left.Hours)}:{FixInt(left.Minutes)}**");

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

    //private static async Task Main(string[] args)
    //{
    //    var clientConfig = new DiscordConfiguration()
    //    {
    //        Intents = DiscordIntents.MessageContents | DiscordIntents.GuildMessages | DiscordIntents.Guilds,
    //        Token = "MTA2MzEyMTk2NDA0ODMyMjU5Mg.G7txjC.1TtpnqneHypYO0gXC5f6E9nlYKHErKwDCq8BAI",
    //        TokenType = TokenType.Bot,
    //        AutoReconnect = true,
    //        MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
    //    };

    //    var client = new DiscordClient(clientConfig);
    //    using var httpClient = new HttpClient();
    //    var byteArray = Encoding.ASCII.GetBytes(":F!nley19g7");
    //    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

    //    client.MessageCreated += async (s, e) =>
    //    {
    //        if (e.Message.Content.StartsWith('!'))
    //        {
    //            await e.Channel.TriggerTypingAsync();

    //            string _baseUrl = "http://192.168.2.161:8080/status.json?command=";
    //            string _baseUrl2 = "http://192.168.2.161:8080/status.json";
    //            string _playListUrl = "http://192.168.2.161:8080/playlist.json";

    //            var command = new Command(e.Message.Content.Split(' '));
    //            var response = default(HttpResponseMessage);
    //            var builder = new StringBuilder();
    //            switch (command.Name)
    //            {
    //                case "!help":
    //                    builder.Clear();
    //                    builder.AppendLine();
    //                    builder.AppendLine("\nVLCController (ofwel niels-bot) *v0.1*\n");
    //                    builder.AppendLine("Commands: ");
    //                    builder.AppendLine("**!skip** __*//*__ Skip to next item in the playlist");
    //                    builder.AppendLine("**!previous** __*//*__  Go to previous item in the playlist");
    //                    builder.AppendLine("**!pause** __*//*__  Pause playback");
    //                    builder.AppendLine("**!play** __*//*__ Start/resume playback");
    //                    builder.AppendLine("**!info** __*//*__ Show current playback information");
    //                    builder.AppendLine("**!guide** __*//*__ Shows a time guide for the coming 7 media after current playing media.");
    //                    builder.AppendLine("**!help** __*//*__ Shows this overview");
    //                    builder.AppendLine("**!weather [location name]** __*//*__ Shows basic weather information for [location name]");
    //                    //builder2.AppendLine("**!radar** __*//*__ Gets most recent radar image");
    //                    //builder2.AppendLine("**!satellite** __*//*__ Get most recent satellite image");
    //                    builder.AppendLine("\n\n");

    //                    var embedBuilder = new DiscordEmbedBuilder()
    //                    {
    //                        Description = builder.ToString()
    //                    };

    //                    await e.Channel.SendMessageAsync(embedBuilder);
    //                    break;

    //                case "!skip":

    //                    if (command.Parameters.Count > 0)
    //                    {
    //                        var id = command.Parameters[0];

    //                        builder.Clear();
    //                        response = await httpClient.GetAsync(_playListUrl);
    //                        var content = await response.Content.ReadAsStringAsync();
    //                        var playlist = JsonConvert.DeserializeObject<Playlist>(content);

    //                        response = await httpClient.GetAsync(_baseUrl2);
    //                        content = await response.Content.ReadAsStringAsync();
    //                        var obj = JsonConvert.DeserializeObject<Status>(content);

    //                        int index = 0;
    //                        foreach (var item in playlist.Children[0].Children)
    //                        {
    //                            if (item.Name == obj.Information.Category.Meta.Filename)
    //                            {
    //                                response = await httpClient.GetAsync($"{_baseUrl}pl_next&id={item.Id}");
    //                                content = await response.Content.ReadAsStringAsync();
    //                                obj = JsonConvert.DeserializeObject<Status>(content);

    //                                Group[] mediaInfo = ParseMediaInfo(obj.Information.Category.Meta.Filename);
    //                                var name = mediaInfo
    //                                   .FirstOrDefault(group =>
    //                                   {
    //                                       if (group.Name.Contains("ShowName") && group.Value != null)
    //                                       {
    //                                           return true;
    //                                       }

    //                                       return false;
    //                                   });
    //                                var year = mediaInfo
    //                                .FirstOrDefault(group =>
    //                                {
    //                                    if (group.Name.Contains("ShowYear") && group.Value != null)
    //                                        return true;

    //                                    return false;
    //                                });
    //                                var season = mediaInfo
    //                                .FirstOrDefault(group =>
    //                                {
    //                                    if (group.Name.Contains("Season") && group.Value != string.Empty)
    //                                        return true;

    //                                    return false;
    //                                });
    //                                var episode = mediaInfo
    //                                .FirstOrDefault(group =>
    //                                {
    //                                    if (group.Name.Contains("Episode") && group.Value != string.Empty)
    //                                    {
    //                                        return true;
    //                                    }

    //                                    return false;
    //                                });

    //                                builder.AppendLine();
    //                                builder.AppendLine($"{index}. {CleanShowName(name?.Value)}").Append(year?.Value != string.Empty ? $"(**{year?.Value}**)" : string.Empty);

    //                                if (episode != null)
    //                                    if (episode.Value != string.Empty)
    //                                    {
    //                                        builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
    //                                    }

    //                                var ts = obj.Position;
    //                                var tsl = obj.Length;
    //                                builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");

    //                                var embedbuilder = new DiscordEmbedBuilder()
    //                                {
    //                                    Description = builder.ToString(),
    //                                };

    //                                await e.Channel.SendMessageAsync(embedbuilder);

    //                                break;
    //                            }

    //                            index++;
    //                        }

    //                        response.Dispose();
    //                        break;
    //                    }
    //                    else
    //                    {
    //                        response = await httpClient.GetAsync($"{_baseUrl}pl_next");

    //                        if (response.IsSuccessStatusCode)
    //                        {
    //                            var content = await response.Content.ReadAsStringAsync();
    //                            var obj = JsonConvert.DeserializeObject<Status>(content);
    //                            builder.Clear();

    //                            response = await httpClient.GetAsync(_playListUrl);
    //                            content = await response.Content.ReadAsStringAsync();
    //                            var playlist = JsonConvert.DeserializeObject<Playlist>(content);

    //                            Group[] mediaInfo = ParseMediaInfo(obj.Information.Category.Meta.Filename);
    //                            var name = mediaInfo
    //                               .FirstOrDefault(group =>
    //                               {
    //                                   if (group.Name.Contains("ShowName") && group.Value != null)
    //                                   {
    //                                       return true;
    //                                   }

    //                                   return false;
    //                               });
    //                            var year = mediaInfo
    //                            .FirstOrDefault(group =>
    //                            {
    //                                if (group.Name.Contains("ShowYear") && group.Value != null)
    //                                    return true;

    //                                return false;
    //                            });
    //                            var season = mediaInfo
    //                            .FirstOrDefault(group =>
    //                            {
    //                                if (group.Name.Contains("Season") && group.Value != string.Empty)
    //                                    return true;

    //                                return false;
    //                            });
    //                            var episode = mediaInfo
    //                            .FirstOrDefault(group =>
    //                            {
    //                                if (group.Name.Contains("Episode") && group.Value != string.Empty)
    //                                {
    //                                    return true;
    //                                }

    //                                return false;
    //                            });

    //                            int index = 0;
    //                            foreach (var item in playlist.Children[0].Children)
    //                            {
    //                                if (item.Name == obj.Information.Category.Meta.Filename)
    //                                {
    //                                    break;
    //                                }

    //                                index++;
    //                            }

    //                            builder.AppendLine();
    //                            builder.AppendLine($"{index}. {CleanShowName(name?.Value)}").Append(year?.Value != string.Empty ? $"(**{year?.Value}**)" : string.Empty);

    //                            if (episode != null)
    //                                if (episode.Value != string.Empty)
    //                                {
    //                                    builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
    //                                }

    //                            var ts = obj.Position;
    //                            var tsl = obj.Length;
    //                            builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");

    //                            var embedbuilder = new DiscordEmbedBuilder()
    //                            {
    //                                Description = builder.ToString(),
    //                            };

    //                            await e.Channel.SendMessageAsync(embedbuilder);
    //                        }
    //                        response.Dispose();
    //                    }

    //                    break;

    //                case "!previous":
    //                    response = await httpClient.GetAsync($"{_baseUrl}pl_previous");

    //                    if (response.IsSuccessStatusCode)
    //                    {
    //                        await e.Channel.SendMessageAsync("Previous item");
    //                    }
    //                    response.Dispose();
    //                    break;

    //                case "!pause":
    //                    response = await httpClient.GetAsync($"{_baseUrl}pl_pause");

    //                    if (response.IsSuccessStatusCode)
    //                    {
    //                        await e.Channel.SendMessageAsync("Paused");
    //                    }
    //                    response.Dispose();
    //                    break;

    //                case "!play":

    //                    response = await httpClient.GetAsync($"{_baseUrl}pl_play");

    //                    if (response.IsSuccessStatusCode)
    //                    {
    //                        var content = await response.Content.ReadAsStringAsync();
    //                        var obj = JsonConvert.DeserializeObject<Status>(content);
    //                        builder.Clear();

    //                        response = await httpClient.GetAsync(_playListUrl);
    //                        content = await response.Content.ReadAsStringAsync();
    //                        var playlist = JsonConvert.DeserializeObject<Playlist>(content);

    //                        Group[] mediaInfo = ParseMediaInfo(obj.Information.Category.Meta.Filename);
    //                        var name = mediaInfo
    //                           .FirstOrDefault(group =>
    //                           {
    //                               if (group.Name.Contains("ShowName") && group.Value != null)
    //                               {
    //                                   return true;
    //                               }

    //                               return false;
    //                           });
    //                        var year = mediaInfo
    //                        .FirstOrDefault(group =>
    //                        {
    //                            if (group.Name.Contains("ShowYear") && group.Value != null)
    //                                return true;

    //                            return false;
    //                        });
    //                        var season = mediaInfo
    //                        .FirstOrDefault(group =>
    //                        {
    //                            if (group.Name.Contains("Season") && group.Value != string.Empty)
    //                                return true;

    //                            return false;
    //                        });
    //                        var episode = mediaInfo
    //                        .FirstOrDefault(group =>
    //                        {
    //                            if (group.Name.Contains("Episode") && group.Value != string.Empty)
    //                            {
    //                                return true;
    //                            }

    //                            return false;
    //                        });

    //                        int index = 0;
    //                        foreach (var item in playlist.Children[0].Children)
    //                        {
    //                            if (item.Name == obj.Information.Category.Meta.Filename)
    //                            {
    //                                break;
    //                            }

    //                            index++;
    //                        }

    //                        builder.AppendLine();
    //                        builder.AppendLine($"{index}. {CleanShowName(name?.Value)}").Append(year?.Value != string.Empty ? $"(**{year?.Value}**)" : string.Empty);

    //                        if (episode != null)
    //                            if (episode.Value != string.Empty)
    //                            {
    //                                builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
    //                            }

    //                        var ts = obj.Position;
    //                        var tsl = obj.Length;
    //                        builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");

    //                        var embedbuilder = new DiscordEmbedBuilder()
    //                        {
    //                            Description = builder.ToString(),
    //                        };

    //                        await e.Channel.SendMessageAsync(embedbuilder);
    //                    }
    //                    response.Dispose();

    //                    break;

    //                case "!info":
    //                    response = await httpClient.GetAsync(_baseUrl2);

    //                    if (response.IsSuccessStatusCode)
    //                    {
    //                        var content = await response.Content.ReadAsStringAsync();
    //                        var obj = JsonConvert.DeserializeObject<Status>(content);
    //                        builder.Clear();

    //                        response = await httpClient.GetAsync(_playListUrl);
    //                        content = await response.Content.ReadAsStringAsync();
    //                        var playlist = JsonConvert.DeserializeObject<Playlist>(content);

    //                        Group[] mediaInfo = ParseMediaInfo(obj.Information.Category.Meta.Filename);
    //                        var name = mediaInfo
    //                           .FirstOrDefault(group =>
    //                           {
    //                               if (group.Name.Contains("ShowName") && group.Value != null)
    //                               {
    //                                   return true;
    //                               }

    //                               return false;
    //                           });
    //                        var year = mediaInfo
    //                        .FirstOrDefault(group =>
    //                        {
    //                            if (group.Name.Contains("ShowYear") && group.Value != null)
    //                                return true;

    //                            return false;
    //                        });
    //                        var season = mediaInfo
    //                        .FirstOrDefault(group =>
    //                        {
    //                            if (group.Name.Contains("Season") && group.Value != string.Empty)
    //                                return true;

    //                            return false;
    //                        });
    //                        var episode = mediaInfo
    //                        .FirstOrDefault(group =>
    //                        {
    //                            if (group.Name.Contains("Episode") && group.Value != string.Empty)
    //                            {
    //                                return true;
    //                            }

    //                            return false;
    //                        });

    //                        int index = 0;
    //                        foreach (var item in playlist.Children[0].Children)
    //                        {
    //                            if (item.Name == obj.Information.Category.Meta.Filename)
    //                            {
    //                                break;
    //                            }

    //                            index++;
    //                        }

    //                        builder.AppendLine();
    //                        builder.AppendLine($"{index}. {CleanShowName(name?.Value)}").Append(year?.Value != string.Empty ? $"(**{year?.Value}**)" : string.Empty);

    //                        if (episode != null)
    //                            if (episode.Value != string.Empty)
    //                            {
    //                                builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
    //                            }

    //                        var ts = obj.Position;
    //                        var tsl = obj.Length;
    //                        builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");

    //                        var embedbuilder = new DiscordEmbedBuilder()
    //                        {
    //                            Description = builder.ToString(),
    //                        };

    //                        await e.Channel.SendMessageAsync(embedbuilder);
    //                    }

    //                    response.Dispose();
    //                    break;

    //                case "!guide":
    //                    response = await httpClient.GetAsync(_playListUrl);

    //                    if (response.IsSuccessStatusCode)
    //                    {
    //                        builder.Clear();
    //                        builder.AppendLine();

    //                        var content = response.Content;
    //                        var text = await content.ReadAsStringAsync();
    //                        var data = JsonConvert.DeserializeObject<Playlist>(text);

    //                        response = await httpClient.GetAsync($"{_baseUrl}");
    //                        content = response.Content;
    //                        text = await content.ReadAsStringAsync();
    //                        var info = JsonConvert.DeserializeObject<Status>(text);

    //                        var curLeaf = new Leaf();
    //                        int leafIndex = 0;

    //                        await e.Channel.SendMessageAsync(info.Information.Category.Meta.Filename);

    //                        for (int i = 0; i < data.Children[0].Children.Length; i++)
    //                        {
    //                            var leaf = data.Children[0].Children[i];

    //                            if (HttpUtility.UrlDecode(leaf.Uri).Contains(info.Information.Category.Meta.Filename))
    //                            {
    //                                leafIndex = i;
    //                                curLeaf = leaf;
    //                                break;
    //                            }
    //                        }

    //                        var name = info.Information.Category.Meta.Filename;
    //                        var mediainfo = ParseMediaInfo(name);
    //                        var title = mediainfo.FirstOrDefault(grp => grp.Name.Contains("ShowName") && grp.Value != string.Empty);
    //                        var year = mediainfo.FirstOrDefault(grp => grp.Name.Contains("ShowYear") && grp.Value != string.Empty);
    //                        var episode = mediainfo.FirstOrDefault(grp => grp.Name.Contains("Episode") && grp.Value != string.Empty);
    //                        var season = mediainfo.FirstOrDefault(grp => grp.Name.Contains("Season") && grp.Value != string.Empty);
    //                        var ts = DateTime.Now.TimeOfDay + info.TimeLeft;

    //                        builder.AppendLine();
    //                        builder.AppendLine($"**Now** - **{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}**");
    //                        builder.Append($"{leafIndex}. {CleanShowName(title.Value)} ");

    //                        if (year != null)
    //                        {
    //                            builder.AppendLine((year.Value != string.Empty) ? $"**({year.Value})**" : string.Empty);
    //                        }
    //                        else
    //                        {
    //                            builder.AppendLine();
    //                        }

    //                        if (episode != null)
    //                            if (episode.Value != string.Empty)
    //                            {
    //                                builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
    //                            }

    //                        builder.AppendLine();

    //                        int j = leafIndex;
    //                        foreach (var leaf in data.Children[0].Children[(leafIndex + 1)..(leafIndex + 10)])
    //                        {
    //                            if (j == data.Children[0].Children.Length)
    //                            {
    //                                break;
    //                            }

    //                            if (leaf != null)
    //                            {
    //                                var tsl = ts + TimeSpan.FromSeconds(leaf.Duration);
    //                                name = leaf.Name;
    //                                mediainfo = ParseMediaInfo(name);

    //                                title = mediainfo.FirstOrDefault(grp => grp.Name.Contains("ShowName") && grp.Value != string.Empty);
    //                                year = mediainfo.FirstOrDefault(grp => grp.Name.Contains("ShowYear") && grp.Value != string.Empty);
    //                                episode = mediainfo.FirstOrDefault(grp => grp.Name.Contains("Episode") && grp.Value != string.Empty);
    //                                season = mediainfo.FirstOrDefault(grp => grp.Name.Contains("Season") && grp.Value != string.Empty);

    //                                builder.AppendLine();
    //                                builder.AppendLine($"**{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}** - **{FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}**");
    //                                builder.Append($"{j}. {CleanShowName(title.Value)} ");

    //                                if (year != null)
    //                                {
    //                                    builder.AppendLine((year.Value != string.Empty) ? $"**({year.Value})**" : string.Empty);
    //                                }
    //                                else
    //                                {
    //                                    builder.AppendLine();
    //                                }

    //                                if (episode != null)
    //                                    if (episode.Value != string.Empty)
    //                                    {
    //                                        builder.AppendLine($"Season **{season.Value}** __*//*__ Episode **{episode.Value}**");
    //                                    }

    //                                builder.AppendLine();
    //                                j++;
    //                                ts = tsl;
    //                            }
    //                        }

    //                        var embedbuilder = new DiscordEmbedBuilder()
    //                        {
    //                            Description = builder.ToString()
    //                        };

    //                        await e.Channel.SendMessageAsync(embedbuilder);
    //                    }
    //                    break;

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

    //    await client.ConnectAsync();

    //    await Task.Delay(-1);

    //    client.Dispose();
    //    httpClient.Dispose();
    //}

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

    [JsonProperty("position", NullValueHandling = NullValueHandling.Ignore)]
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