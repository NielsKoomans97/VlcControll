using Newtonsoft.Json;

public partial class Category
{
    [JsonProperty("meta", NullValueHandling = NullValueHandling.Ignore)]
    public Meta Meta { get; set; }
}
