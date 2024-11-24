using Newtonsoft.Json;

namespace SiegeLibTests;

[JsonObject]
public class TestConfig
{
    [JsonProperty("ds1_path")] public string DungeonSiege1Path { get; set; }
    [JsonProperty("ds1_user_path")] public string DungeonSiege1UserPath { get; set; }
    [JsonProperty("ds2_path")] public string DungeonSiege2Path { get; set; }
    [JsonProperty("ds2_user_path")] public string DungeonSiege2UserPath { get; set; }
}