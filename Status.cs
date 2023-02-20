using Newtonsoft.Json;

public partial class Status
{
    [JsonProperty("length", NullValueHandling = NullValueHandling.Ignore)]
    public long Length { get; set; }

    [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
    public long Position { get; set; }

    [JsonProperty("information", NullValueHandling = NullValueHandling.Ignore)]
    public Information Information { get; set; }

    [JsonProperty("currentplid", NullValueHandling = NullValueHandling.Ignore)]
    public long Id { get; set; }
}
