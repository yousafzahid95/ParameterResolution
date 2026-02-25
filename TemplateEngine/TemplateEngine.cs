using AntlrTest1.Interfaces;
using AntlrTest1.Models;

namespace AntlrTest1.TemplateEngine
{
    /// <summary>
    /// Resolves a TemplateParam against a dataset keyed by DataSourceKey.
    /// Any key present in the dataset is supported: LEM, EventMessage, EventData, Event, or custom keys.
    /// Uses String.Format and bracket expressions like [LEM.Property], [EventMessage.MessageId], [EventData.ProjectId], or [SourceKey[index].Property].
    /// </summary>
    public static class RuleTemplateEngine
    {
        /// <summary>
        /// Resolves a TemplateParam by resolving each param expression against the dataset,
        /// then calling String.Format(template, ...values).
        /// </summary>
        public static string Resolve(TemplateParam param, IReadOnlyDictionary<string, IReadOnlyList<IDataRecord>> dataset)
        {
            if (param == null || string.IsNullOrEmpty(param.Template))
                return string.Empty;

            if (param.Params == null || param.Params.Count == 0)
                return param.Template;

            var rawValues = new object?[param.Params.Count];
            for (var i = 0; i < param.Params.Count; i++)
            {
                rawValues[i] = ExpressionResolver.Resolve(param.Params[i], dataset);
            }

            if (string.Equals(param.Template, "{0}", StringComparison.Ordinal) && rawValues.Length > 1)
            {
                var firstNonEmpty = rawValues.FirstOrDefault(v =>
                    v != null && !string.IsNullOrWhiteSpace(v.ToString()));

                if (firstNonEmpty == null)
                    return string.Empty;

                return string.Format(param.Template, firstNonEmpty);
            }

            var values = rawValues
                .Select(v => (object?)(v ?? string.Empty))
                .ToArray();

            try
            {
                return string.Format(param.Template, values);
            }
            catch (FormatException)
            {
                return param.Template;
            }
        }
    }
}
