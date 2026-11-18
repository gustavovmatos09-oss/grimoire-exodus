using Newtonsoft.Json;

namespace Grimoire.Game.Data
{
    /// <summary>
    /// A clickable item exposed by the AQW map's mapItem component.
    /// </summary>
    public class MapItem
    {
        [JsonProperty("ID")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("qID")]
        public int QuestId { get; set; }

        [JsonProperty("itemName")]
        public string ItemName { get; set; }

        [JsonProperty("collectMsg")]
        public string CollectMessage { get; set; }
    }
}
