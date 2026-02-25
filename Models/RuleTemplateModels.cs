using Newtonsoft.Json;

namespace AntlrTest1.Models
{
    /// <summary>
    /// Root rule definition with events, action item template, and data source checks.
    /// </summary>
    public class RuleTemplate
    {
        [JsonProperty("RuleName")]
        public string RuleName { get; set; } = string.Empty;

        [JsonProperty("Events")]
        public List<string> Events { get; set; } = new();

        [JsonProperty("ActionItemTemplate")]
        public ActionItemTemplateDefinition ActionItemTemplate { get; set; } = new();

        [JsonProperty("Checks")]
        public RuleChecks? Checks { get; set; }
    }

    /// <summary>
    /// Action item output template: each property has a format template and param expressions.
    /// </summary>
    public class ActionItemTemplateDefinition
    {
        [JsonProperty("SourceSystem")]
        public int SourceSystem { get; set; }

        [JsonProperty("ItemDefinitionId")]
        public int ItemDefinitionId { get; set; }

        [JsonProperty("Description")]
        public TemplateParam? Description { get; set; }

        [JsonProperty("TaskId")]
        public TemplateParam? TaskId { get; set; }

        [JsonProperty("EntityId")]
        public TemplateParam? EntityId { get; set; }

        [JsonProperty("SourceSystemKey")]
        public TemplateParam? SourceSystemKey { get; set; }
    }

    /// <summary>
    /// A single template: format string (e.g. "{0}") and param expressions (e.g. "[AllWorkplan.TaskId]").
    /// Resolved via String.Format(template, ...resolvedParams).
    /// </summary>
    public class TemplateParam
    {
        [JsonProperty("template")]
        public string Template { get; set; } = string.Empty;

        [JsonProperty("params")]
        public List<string> Params { get; set; } = new();
    }

    /// <summary>
    /// Checks section containing data source definitions.
    /// </summary>
    public class RuleChecks
    {
        [JsonProperty("DataSources")]
        public List<DataSourceDefinition> DataSources { get; set; } = new();
    }

    /// <summary>
    /// One data source: Key (e.g. "AllWorkplan") and params as template/params resolved from event.
    /// Adapter receives IDictionary&lt;string, object&gt; built from resolved params.
    /// </summary>
    public class DataSourceDefinition
    {
        [JsonProperty("Key")]
        public string Key { get; set; } = string.Empty;

        [JsonProperty("params")]
        public Dictionary<string, TemplateParam> Params { get; set; } = new();
    }
}
