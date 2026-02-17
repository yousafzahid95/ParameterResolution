using System.Text.Json.Serialization;

namespace AntlrTest1.JsonRegexResolvers
{
    /// <summary>
    /// JSON-serializable template definition (same contract as JsonStringBuilderResolvers).
    /// </summary>
    public class TemplateDefinition
    {
        [JsonPropertyName("propertyName")]
        public string? PropertyName { get; set; }

        [JsonPropertyName("params")]
        public List<string>? Params { get; set; }
    }
}
