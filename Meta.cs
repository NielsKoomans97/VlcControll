using Newtonsoft.Json;

public partial class Meta
{
    [JsonProperty("showName", NullValueHandling = NullValueHandling.Ignore)]
    public string ShowName { get; set; }

    [JsonProperty("filename", NullValueHandling = NullValueHandling.Ignore)]
    public string Filename { get; set; }

    [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
    public string Title { get; set; }

    [JsonProperty("episodeNumber", NullValueHandling = NullValueHandling.Ignore)]
    public string EpisodeNumber { get; set; }

    [JsonProperty("seasonNumber", NullValueHandling = NullValueHandling.Ignore)]
    public string SeasonNumber { get; set; }
}
