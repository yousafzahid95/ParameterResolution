using System.Text.Json.Serialization;

namespace AntlrTest1.JsonStringBuilderResolvers
{
    /// <summary>
    /// JSON-serializable template definition with propertyName (template with {0}, {1}, ...) and params array.
    /// </summary>
    public class TemplateDefinition
    {
        [JsonPropertyName("propertyName")]
        public string? PropertyName { get; set; }

        [JsonPropertyName("params")]
        public List<string>? Params { get; set; }
    }
}
