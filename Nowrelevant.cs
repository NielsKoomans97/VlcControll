using Newtonsoft.Json;

public partial class Nowrelevant
{
    [JsonProperty("values", NullValueHandling = NullValueHandling.Ignore)]
    public Value[] Values { get; set; }
}
