using Newtonsoft.Json;

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
