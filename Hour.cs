using Newtonsoft.Json;

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
