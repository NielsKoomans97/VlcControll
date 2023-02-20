using Newtonsoft.Json;

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
