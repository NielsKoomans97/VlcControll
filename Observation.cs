using Newtonsoft.Json;

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
