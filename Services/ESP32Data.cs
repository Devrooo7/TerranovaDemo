using System.Text.Json.Serialization;

namespace TerranovaDemo.Services
{
    public class ESP32Data
    {
        [JsonPropertyName("temperatura")]
        public float temperatura { get; set; }

        [JsonPropertyName("humedadSuelo")]
        public int humedadSuelo { get; set; }

        // Captura ambos posibles nombres de campo desde JSON
        [JsonPropertyName("bomba")]
        public bool? bombaJson { get; set; }

        [JsonPropertyName("bombaStatus")]
        public bool? bombaStatusJson { get; set; }

        // Propiedad pública unificada que siempre devuelve bool
        [JsonIgnore]
        public bool bombaStatus => (bombaJson ?? false) || (bombaStatusJson ?? false);
    }
}
