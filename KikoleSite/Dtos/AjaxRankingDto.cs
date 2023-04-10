using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KikoleSite.Dtos
{
    public class AjaxRankingDto
    {
        internal const int UrlNamePosition = 1;
        internal const int ValuesCountByPlayer = 6;

        [JsonPropertyName("p")]
        public IReadOnlyList<JsonElement> PValue { get; set; }

        [JsonPropertyName("t")]
        public IReadOnlyList<JsonElement> TValue { get; set; }
    }
}
