using System.Text;
using System.Text.RegularExpressions;
using AntlrTest1.Interfaces;
using Newtonsoft.Json.Linq;

namespace AntlrTest1.StringFormatExtraction
{
    /// <summary>
    /// Template resolver using StringBuilder and String.Format approach.
    /// This approach extracts placeholders, resolves them to values, then uses String.Format
    /// with indexed placeholders {0}, {1}, etc. for efficient string building.
    /// 
    /// Example:
    ///   var resolver = StringFormatTemplateResolver
    ///       .Create()
    ///       .WithEvent(eventData)
    ///       .WithConfig(ruleConfig)
    ///       .WithDataset(dataset);
    ///   
    ///   var result = resolver.Resolve("Entity {LEM.EntityId} for project {InfoRequest.ProjectId}");
    /// </summary>
    public class StringFormatTemplateResolver
    {
        private object? _eventData;
        private JObject? _ruleConfig;
        private IEnumerable<IDataRecord>? _dataset;
        private static readonly Regex PlaceholderRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

        private StringFormatTemplateResolver() { }

        /// <summary>
        /// Creates a new StringFormat template resolver builder.
        /// </summary>
        public static StringFormatTemplateResolver Create()
        {
            return new StringFormatTemplateResolver();
        }

        /// <summary>
        /// Sets the event data source for path expressions.
        /// </summary>
        public StringFormatTemplateResolver WithEvent(object eventData)
        {
            _eventData = eventData;
            return this;
        }

        /// <summary>
        /// Sets the rule configuration JObject for Config.* placeholders.
        /// </summary>
        public StringFormatTemplateResolver WithConfig(JObject ruleConfig)
        {
            _ruleConfig = ruleConfig;
            return this;
        }

        /// <summary>
        /// Sets the dataset for dataset-based placeholders (e.g., {LEM.EntityId}).
        /// </summary>
        public StringFormatTemplateResolver WithDataset(IEnumerable<IDataRecord> dataset)
        {
            _dataset = dataset;
            return this;
        }

        /// <summary>
        /// Resolves all placeholders in the template string using StringBuilder and String.Format.
        /// </summary>
        /// <param name="template">Template string with optional {placeholder} syntax</param>
        /// <returns>Resolved string with all placeholders substituted</returns>
        public string? Resolve(string? template)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            // Find all placeholders in the template
            var matches = PlaceholderRegex.Matches(template);
            if (matches.Count == 0)
                return template; // No placeholders, return as-is

            // Build a dictionary of unique placeholders and their resolved values
            var placeholderMap = new Dictionary<string, string>();
            var resolvedValues = new List<string>();
            var formatStringBuilder = new StringBuilder();
            
            int lastIndex = 0;
            int placeholderIndex = 0;

            foreach (Match match in matches)
            {
                // Add text before this placeholder
                formatStringBuilder.Append(template, lastIndex, match.Index - lastIndex);
                
                var expression = match.Groups[1].Value.Trim();
                
                // Check if we've seen this placeholder before
                if (!placeholderMap.ContainsKey(expression))
                {
                    // Resolve the expression
                    var resolved = ResolveExpression(expression);
                    var resolvedValue = resolved ?? string.Empty;
                    
                    // Store in map and add to values list
                    placeholderMap[expression] = resolvedValue;
                    resolvedValues.Add(resolvedValue);
                    placeholderIndex = resolvedValues.Count - 1;
                }
                else
                {
                    // Reuse existing resolved value
                    placeholderIndex = resolvedValues.IndexOf(placeholderMap[expression]);
                }
                
                // Add indexed placeholder {0}, {1}, etc.
                formatStringBuilder.Append('{').Append(placeholderIndex).Append('}');
                
                lastIndex = match.Index + match.Length;
            }

            // Add remaining text after last placeholder
            formatStringBuilder.Append(template, lastIndex, template.Length - lastIndex);

            // Use String.Format to replace indexed placeholders with resolved values
            try
            {
                var formatString = formatStringBuilder.ToString();
                return string.Format(formatString, resolvedValues.ToArray());
            }
            catch (FormatException fe)
            {
                // Log or handle error — here we return the original template
                Console.Error.WriteLine($"StringFormat template error for '{template}': {fe.Message}");
                return template;
            }
        }

        /// <summary>
        /// Resolves a single expression (without curly braces).
        /// Uses the same extraction logic as FluidParameterExtractor for consistency.
        /// </summary>
        private string? ResolveExpression(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return null;

            expression = expression.Trim();

            // Config.* -> rule configuration JObject
            if (expression.StartsWith("Config.", StringComparison.OrdinalIgnoreCase))
            {
                if (_ruleConfig == null)
                    return null;

                var path = expression.Substring("Config.".Length);
                var token = _ruleConfig.SelectToken(path);
                return token?.Type == JTokenType.Null ? null : token?.ToString();
            }

            // Dataset prefix: Prefix.Path -> IDataRecord.Source == Prefix
            var firstDot = expression.IndexOf('.');
            if (firstDot > 0 && _dataset != null)
            {
                var prefix = expression.Substring(0, firstDot);
                var path = expression.Substring(firstDot + 1);

                var record = _dataset.FirstOrDefault(r =>
                    r.Source.Equals(prefix, StringComparison.OrdinalIgnoreCase));

                if (record != null)
                {
                    // Use BespokeCustomExtraction for consistency with other approaches
                    var value = BespokeCustomExtraction.BespokeParameterExtractor.Extract(record.Data, path);

                    // If extraction didn't work, fall back to JObject.SelectToken
                    if (value == null || ReferenceEquals(value, record.Data))
                    {
                        if (record.Data is JObject jObj)
                        {
                            var token = jObj.SelectToken(path);
                            if (token is JValue jValue)
                            {
                                return jValue.Value?.ToString();
                            }

                            return token?.ToString();
                        }
                    }

                    return value?.ToString();
                }
            }

            // Fallback: treat whole expression as a path on eventData
            if (_eventData != null)
            {
                // Use BespokeCustomExtraction for consistency
                var eventValue = BespokeCustomExtraction.BespokeParameterExtractor.Extract(_eventData, expression);
                return eventValue?.ToString();
            }

            return null;
        }

        /// <summary>
        /// Builds an ActionItem from templates and resolved values.
        /// </summary>
        public ActionItem BuildActionItem(
            string sourceSystemKeyTemplate,
            string descriptionTemplate,
            string status = "Open",
            Guid? taskId = null)
        {
            var firstRecord = _dataset?.FirstOrDefault();
            
            var actionItem = new ActionItem
            {
                TaskId = taskId ?? Guid.NewGuid(),
                SourceSystemKey = Resolve(sourceSystemKeyTemplate) ?? string.Empty,
                Description = Resolve(descriptionTemplate) ?? string.Empty,
                Status = status
            };

            // Extract EntityId and WorkAreaId from dataset if available
            if (firstRecord != null)
            {
                var entityIdToken = firstRecord.Data["EntityId"];
                var workAreaIdToken = firstRecord.Data["WorkAreaId"];

                if (entityIdToken != null && Guid.TryParse(entityIdToken.ToString(), out var entityId))
                {
                    actionItem.EntityId = entityId;
                }

                if (workAreaIdToken != null && Guid.TryParse(workAreaIdToken.ToString(), out var workAreaId))
                {
                    actionItem.WorkAreaId = workAreaId;
                }
            }

            return actionItem;
        }
    }
}
