using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

internal class Program
{
    private static string FixInt(int value)
    {
        return value < 10 ? $"0{value}" : $"{value}";
    }

    private static async Task Main(string[] args)
    {
        var clientConfig = new DiscordConfiguration()
        {
            Intents = DiscordIntents.MessageContents | DiscordIntents.GuildMessages | DiscordIntents.Guilds,
            Token = "MTA2MzEyMTk2NDA0ODMyMjU5Mg.G7txjC.1TtpnqneHypYO0gXC5f6E9nlYKHErKwDCq8BAI",
            TokenType = TokenType.Bot,
            AutoReconnect = true,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
        };

        var client = new DiscordClient(clientConfig);
        using var httpClient = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes(":F!nley19g7");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        client.MessageCreated += async (s, e) =>
        {
            if (e.Message.Content.StartsWith('!'))
            {
                await e.Channel.TriggerTypingAsync();

                string _baseUrl = "http://192.168.2.161:8080/status.json?command=";

                if (e.Message.Content.StartsWith("!skip"))
                {
                    var response = await httpClient.GetAsync($"{_baseUrl}pl_next");

                    if (response.IsSuccessStatusCode)
                    {
                        await e.Channel.SendMessageAsync("Skipped");
                    }
                    response.Dispose();
                }
                if (e.Message.Content.StartsWith("!previous"))
                {
                    var response = await httpClient.GetAsync($"{_baseUrl}pl_previous");

                    if (response.IsSuccessStatusCode)
                    {
                        await e.Channel.SendMessageAsync("Previous item");
                    }
                    response.Dispose();
                }
                if (e.Message.Content.StartsWith("!pause"))
                {
                    var response = await httpClient.GetAsync($"{_baseUrl}pl_pause");

                    if (response.IsSuccessStatusCode)
                    {
                        await e.Channel.SendMessageAsync("Paused");
                    }
                    response.Dispose();
                }
                if (e.Message.Content.StartsWith("!play"))
                {
                    var response = await httpClient.GetAsync($"{_baseUrl}pl_play");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var obj = JsonConvert.DeserializeObject<dynamic>(content);
                        var builder = new StringBuilder();

                        if (obj?.information.category.meta.episodeNumber != null)
                        {
                            builder.AppendLine();
                            builder.AppendLine($"{obj?.information.category.meta.showName}");
                            builder.AppendLine($"Season **{obj?.information.category.meta.seasonNumber}** __*[]*__ Episode **{obj?.information.category.meta.episodeNumber}**");
                            var ts = TimeSpan.FromSeconds((long)obj?.time);
                            var tsl = TimeSpan.FromSeconds((long)obj?.length);
                            builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");
                        }
                        else
                        {
                            builder.AppendLine($"{obj?.information.category.meta.filename}");
                            var ts = TimeSpan.FromSeconds((long)obj?.time);
                            var tsl = TimeSpan.FromSeconds((long)obj?.length);
                            builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");
                        }

                        var embedbuilder = new DiscordEmbedBuilder()
                        {
                            Description = builder.ToString(),
                        };

                        await e.Channel.SendMessageAsync(embedbuilder);
                    }
                    response.Dispose();
                }
                if (e.Message.Content.StartsWith("!info"))
                {
                    var response = await httpClient.GetAsync($"{_baseUrl}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var obj = JsonConvert.DeserializeObject<dynamic>(content);
                        var builder = new StringBuilder();

                        if (obj?.information.category.meta.episodeNumber != null)
                        {
                            builder.AppendLine();
                            builder.AppendLine($"{obj?.information.category.meta.showName}");
                            builder.AppendLine($"Season **{obj?.information.category.meta.seasonNumber}** __*[]*__ Episode **{obj?.information.category.meta.episodeNumber}**");
                            var ts = TimeSpan.FromSeconds((long)obj?.time);
                            var tsl = TimeSpan.FromSeconds((long)obj?.length);
                            builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");
                        }
                        else
                        {
                            builder.AppendLine($"{obj?.information.category.meta.filename}");
                            var ts = TimeSpan.FromSeconds((long)obj?.time);
                            var tsl = TimeSpan.FromSeconds((long)obj?.length);
                            builder.AppendLine($"{FixInt(ts.Hours)}:{FixInt(ts.Minutes)}:{FixInt(ts.Seconds)} - {FixInt(tsl.Hours)}:{FixInt(tsl.Minutes)}:{FixInt(tsl.Seconds)}");
                        }

                        var embedbuilder = new DiscordEmbedBuilder()
                        {
                            Description = builder.ToString(),
                        };

                        await e.Channel.SendMessageAsync(embedbuilder);
                    }

                    response.Dispose();
                }
                if (e.Message.Content.StartsWith("!weather"))
                {
                    var parts = e.Message.Content.Split(" ");
                    if (parts.Length > 0)
                    {
                        Console.WriteLine(string.Join(" ", parts[1..parts.Length]));

                        var builder = new StringBuilder();
                        var response = await httpClient.GetAsync($"https://location.buienradar.nl/1.1/location/search?query={string.Join(" ", parts[1..parts.Length])}");
                        long? stationId = 0;
                        var locationName = string.Empty;
                        long? locationId = 0;

                        if (response.IsSuccessStatusCode)
                        {
                            var text = await response.Content.ReadAsStringAsync();
                            var obj = JsonConvert.DeserializeObject<SearchItem[]>(text);

                            stationId = obj?[0].StationId;

                            locationName = obj?[0].Name;

                            locationId = obj?[0].LocationId;
                        }
                        else
                        {
                            throw new Exception("**<!>** Could not find any weather stations close to given location");
                        }

                        response = await httpClient.GetAsync($"https://observations.buienradar.nl/1.0/actual/weatherstation/{stationId}");
                        if (response.IsSuccessStatusCode)
                        {
                            var text = await response.Content.ReadAsStringAsync();
                            var obj = JsonConvert.DeserializeObject<dynamic>(text);

                            response = await httpClient.GetAsync($"https://forecast.buienradar.nl/2.0/forecast/{locationId}");
                            text = await response.Content.ReadAsStringAsync();
                            var forecast = JsonConvert.DeserializeObject<Forecast>(text);
                            var day0 = forecast?.Items[0];

                            builder.AppendLine($"Observations for **{locationName}**");
                            builder.AppendLine();
                            builder.AppendLine($"Sunrise: **{FixInt(day0.Sunrise.Hour)}:{FixInt(day0.Sunrise.Minute)}** __*[]*__ Sunset: **{FixInt(day0.Sunset.Hour)}:{FixInt(day0.Sunset.Minute)}**");
                            builder.AppendLine();
                            builder.AppendLine("**Temperature (°C)**");
                            builder.AppendLine($"Temperature: **{obj?.temperature}°**");
                            builder.AppendLine($"Feel temperature: **{obj?.feeltemperature}°**");
                            builder.AppendLine($"Ground temperature: **{obj?.groundtemperature}°**");
                            builder.AppendLine();
                            builder.AppendLine("**Wind**");
                            builder.AppendLine($"Wind speed: **{obj?.windspeedBft} Bft**");
                            builder.AppendLine($"Wind direction: **{obj?.winddirection}**");
                            builder.AppendLine($"Wind gusts: **{obj?.windgusts} m/s**");
                            builder.AppendLine();
                            builder.AppendLine("**Other atmospheric properties**");
                            builder.AppendLine($"Air pressure: **{obj?.airpressure} hPa**");
                            builder.AppendLine($"Visibility: **{obj?.visibility} m**");
                            builder.AppendLine($"Humidity: **{obj?.humidity}%**");
                            builder.AppendLine();
                            builder.AppendLine("**Rain statistics**");
                            builder.AppendLine($"Precipitation: **{obj?.precipitation} mm**");
                            builder.AppendLine($"Precipation: **{obj?.precipation} mm**");
                            builder.AppendLine($"Rainfall last 24 hours: **{obj?.rainFallLast24Hour} mm**");
                            builder.AppendLine($"Rainfall last hour: **{obj?.rainFallLastHour} mm**");
                            builder.AppendLine();
                            builder.AppendLine("**Forecast per hour**");
                            foreach (var hour in day0.Hours)
                            {
                                builder.AppendLine($"**{FixInt(hour.DateTime.Hour)}:{FixInt(hour.DateTime.Minute)}** - **{GetWeatherText(hour.IconCode)}** - Cloud cover: **{hour.CloudCover}%**");
                                builder.AppendLine($"Temp: **{hour.Temperature}°**, Wind: **{hour.Beaufort} Bft** from the **{hour.WindDirection}**, Precipitation: **{hour.PrecipitationMm} mm**");
                                builder.AppendLine();
                            }
                            builder.AppendLine();

                            builder.AppendLine("*Data provided by **Buienradar** *");

                            var embedBuilder = new DiscordEmbedBuilder()
                            {
                                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                                {
                                    Url = $"https://cdn.buienradar.nl/resources/images/icons/weather/116x116/{obj?.iconcode}.png",
                                    Height = 117,
                                    Width = 117
                                },
                                Description = builder.ToString(),
                            };

                            await e.Channel.SendMessageAsync(embed: embedBuilder);
                        }

                        response.Dispose();
                    }
                }
                if (e.Message.Content.StartsWith("!radar"))
                {
                    var builder = new DiscordEmbedBuilder()
                    {
                        ImageUrl = "https://cdn.knmi.nl/knmi/map/page/weer/actueel-weer/neerslagradar/WWWRADAR_loop.gif"
                    };

                    await e.Channel.SendMessageAsync(embed: builder);
                }
                if (e.Message.Content.StartsWith("!satellite"))
                {
                    var builder = new DiscordEmbedBuilder()
                    {
                        ImageUrl = "https://cdn.knmi.nl/knmi/map/page/weer/actueel-weer/satelliet/satlast.jpg"
                    };

                    await e.Channel.SendMessageAsync(embed: builder);
                }
                if (e.Message.Content.StartsWith("!help"))
                {
                    var builder2 = new StringBuilder();
                    builder2.AppendLine();
                    builder2.AppendLine("\nVLCController (ofwel niels-bot) *v0.1*\n");
                    builder2.AppendLine("Commands: ");
                    builder2.AppendLine("**!skip** __*[]*__ Skip to next item in the playlist");
                    builder2.AppendLine("**!previous** __*[]*__  Go to previous item in the playlist");
                    builder2.AppendLine("**!pause** __*[]*__  Pause playback");
                    builder2.AppendLine("**!play** __*[]*__ Start/resume playback");
                    builder2.AppendLine("**!info** __*[]*__ Show current playback information");
                    builder2.AppendLine("**!help** __*[]*__ Shows this overview");
                    builder2.AppendLine("**!weather [location name]** __*[]*__ Shows basic weather information for [location name]");
                    builder2.AppendLine("**!radar** __*[]*__ Gets most recent radar image");
                    builder2.AppendLine("**!satellite** __*[]*__ Get most recent satellite image");
                    builder2.AppendLine("\n\n");

                    var embedBuilder = new DiscordEmbedBuilder()
                    {
                        Description = builder2.ToString()
                    };

                    await e.Channel.SendMessageAsync(embedBuilder);
                }
            }
        };

        await client.ConnectAsync();

        await Task.Delay(-1);

        client.Dispose();
        httpClient.Dispose();
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

public class SearchItem
{
    [JsonProperty("weatherstationId")] public long StationId { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("id")] public long LocationId { get; set; }
}

public class Forecast
{
    [JsonProperty("days")]
    public ForecastItem[] Items { get; set; }
}

public class ForecastItem
{
    [JsonProperty("hours")]
    public Hour[] Hours { get; set; }

    [JsonProperty("sunset")]
    public DateTime Sunset { get; set; }

    [JsonProperty("sunrise")]
    public DateTime Sunrise { get; set; }
}

public class Hour
{
    [JsonProperty("cloudcover")]
    public int CloudCover { get; set; }

    [JsonProperty("datetime")]
    public DateTimeOffset DateTime { get; set; }

    [JsonProperty("temperature")]
    public float Temperature { get; set; }

    [JsonProperty("winddirection")]
    public string WindDirection { get; set; }

    [JsonProperty("beaufort")]
    public int Beaufort { get; set; }

    [JsonProperty("precipitationmm")]
    public float PrecipitationMm { get; set; }

    [JsonProperty("iconcode")]
    public string IconCode { get; set; }

    [JsonProperty("sunshine")]
    public int Sunshine { get; set; }
}