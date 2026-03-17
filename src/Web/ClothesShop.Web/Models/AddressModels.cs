using System.Text.Json.Serialization;

namespace ClothesShop.Web.Models
{
    public class Province
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("codename")]
        public string Codename { get; set; } = string.Empty;

        [JsonPropertyName("districts")]
        public List<District> Districts { get; set; } = new();
    }

    public class District
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("codename")]
        public string Codename { get; set; } = string.Empty;

        [JsonPropertyName("wards")]
        public List<Ward> Wards { get; set; } = new();
    }

    public class Ward
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("codename")]
        public string Codename { get; set; } = string.Empty;
    }
}
