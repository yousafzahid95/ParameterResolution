using System.Text.RegularExpressions;
using AntlrTest1.Interfaces;
using Newtonsoft.Json.Linq;

namespace AntlrTest1.AntlrExtraction
{
    /// <summary>
    /// Resolves template strings that may contain placeholders which are dynamically
    /// resolved from event data, rule configuration, or IDataRecord datasets.
    ///
    /// Supported placeholder syntax:
    /// - {InfoRequest.ProjectId}      -> eventData path
    /// - {WorkplanTask.Entities[0].WorkAreaEntityId} -> eventData path
    /// - {LEM.EntityId}              -> dataset record with Source == "LEM"
    /// - {LEM.Attributes.TaxId}      -> dataset record + path into JObject Data
    /// - {Config.Some.Path}          -> rule config JObject path
    ///
    /// Any string without curly braces is treated as a static value.
    /// </summary>
    public static class ParameterTemplateResolver
    {
        // Matches {expression} where expression does not contain nested braces
        private static readonly Regex PlaceholderRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

        public static string? ResolveTemplate(
            string? template,
            object eventData,
            JObject ruleConfig,
            IEnumerable<IDataRecord> dataset)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            return PlaceholderRegex.Replace(template, match =>
            {
                var expression = match.Groups[1].Value;
                var resolved = ResolveExpression(expression, eventData, ruleConfig, dataset);
                return resolved ?? string.Empty;
            });
        }

        private static string? ResolveExpression(
            string expression,
            object eventData,
            JObject ruleConfig,
            IEnumerable<IDataRecord> dataset)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return null;

            expression = expression.Trim();

            // Config.* -> rule configuration JObject
            if (expression.StartsWith("Config.", StringComparison.OrdinalIgnoreCase))
            {
                var path = expression.Substring("Config.".Length);
                var token = ruleConfig.SelectToken(path);
                return token?.Type == JTokenType.Null ? null : token?.ToString();
            }

            // Dataset prefix: Prefix.Path -> IDataRecord.Source == Prefix
            var firstDot = expression.IndexOf('.');
            if (firstDot > 0)
            {
                var prefix = expression.Substring(0, firstDot);
                var path = expression.Substring(firstDot + 1);

                var record = dataset.FirstOrDefault(r =>
                    r.Source.Equals(prefix, StringComparison.OrdinalIgnoreCase));

                if (record != null)
                {
                    var value = AntlrParameterExtractor.Extract(record.Data, path);

                    // If ANTLR navigation didn't move off the root JObject, or returned null,
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
            var eventValue = AntlrParameterExtractor.Extract(eventData, expression);
            return eventValue?.ToString();
        }
    }
}
