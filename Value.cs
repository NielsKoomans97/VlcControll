using Newtonsoft.Json;

public partial class Value
{
    [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
    public double? ValueValue { get; set; }
}
