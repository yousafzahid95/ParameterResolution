using System.Text.RegularExpressions;
using AntlrTest1.Interfaces;
using Newtonsoft.Json.Linq;

namespace AntlrTest1.FluidExtraction
{
    /// <summary>
    /// Fluent interface builder for parameter resolution (FLUID approach - NO ANTLR).
    /// Provides a declarative, chainable API for resolving template strings.
    /// 
    /// This is a standalone implementation that does NOT use ANTLR.
    /// 
    /// Example:
    ///   var resolver = FluidParameterResolver
    ///       .Create()
    ///       .WithEvent(eventData)
    ///       .WithConfig(ruleConfig)
    ///       .WithDataset(dataset);
    ///   
    ///   var result = resolver.Resolve("Entity {LEM.EntityId} for project {InfoRequest.ProjectId}");
    /// </summary>
    public class FluidParameterResolver
    {
        private object? _eventData;
        private JObject? _ruleConfig;
        private IEnumerable<IDataRecord>? _dataset;
        private static readonly Regex PlaceholderRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

        private FluidParameterResolver() { }

        /// <summary>
        /// Creates a new fluid parameter resolver builder.
        /// </summary>
        public static FluidParameterResolver Create()
        {
            return new FluidParameterResolver();
        }

        /// <summary>
        /// Sets the event data source for path expressions.
        /// </summary>
        public FluidParameterResolver WithEvent(object eventData)
        {
            _eventData = eventData;
            return this;
        }

        /// <summary>
        /// Sets the rule configuration JObject for Config.* placeholders.
        /// </summary>
        public FluidParameterResolver WithConfig(JObject ruleConfig)
        {
            _ruleConfig = ruleConfig;
            return this;
        }

        /// <summary>
        /// Sets the dataset for dataset-based placeholders (e.g., {LEM.EntityId}).
        /// </summary>
        public FluidParameterResolver WithDataset(IEnumerable<IDataRecord> dataset)
        {
            _dataset = dataset;
            return this;
        }

        /// <summary>
        /// Resolves all placeholders in the template string.
        /// </summary>
        /// <param name="template">Template string with optional {placeholder} syntax</param>
        /// <returns>Resolved string with all placeholders substituted</returns>
        public string? Resolve(string? template)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            return PlaceholderRegex.Replace(template, match =>
            {
                var expression = match.Groups[1].Value;
                var resolved = ResolveExpression(expression);
                return resolved ?? string.Empty;
            });
        }

        /// <summary>
        /// Resolves a single expression (without curly braces).
        /// </summary>
        public string? ResolveExpression(string expression)
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
                    // Use FLUID extractor (NOT ANTLR)
                    var value = FluidParameterExtractor.Extract(record.Data, path);

                    // If FLUID navigation didn't move off the root JObject, or returned null,
                    // fall back to JObject.SelectToken for scalar extraction.
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
                // Use FLUID extractor (NOT ANTLR)
                var eventValue = FluidParameterExtractor.Extract(_eventData, expression);
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
