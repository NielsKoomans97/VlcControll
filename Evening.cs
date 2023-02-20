using Newtonsoft.Json;

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
