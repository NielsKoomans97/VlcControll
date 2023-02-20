using Newtonsoft.Json;

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
