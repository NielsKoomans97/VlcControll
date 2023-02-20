using Newtonsoft.Json;

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
