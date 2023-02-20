using Newtonsoft.Json;

public partial class Information
{
    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public long? Title { get; set; }

    [JsonProperty("category", NullValueHandling = NullValueHandling.Ignore)]
    public Category Category { get; set; }

    [JsonProperty("titles", NullValueHandling = NullValueHandling.Ignore)]
    public object[] Titles { get; set; }
}
