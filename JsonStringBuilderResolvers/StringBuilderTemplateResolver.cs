using System.Text;
using System.Text.Json;

namespace AntlrTest1.JsonStringBuilderResolvers
{
    /// <summary>
    /// Resolves JSON template definitions using StringBuilder and String.Format only.
    /// No regex is used. Placeholders {0}, {1}, {2}, ... are resolved by indexing into the params array.
    /// </summary>
    public static class StringBuilderTemplateResolver
    {
        /// <summary>
        /// Takes JSON array string in described format, resolves placeholders {0}, {1}, ...
        /// using StringBuilder for building and String.Format. Returns list of resolved strings (or null where resolution failed).
        /// </summary>
        public static List<string?> ResolveTemplates(string json)
        {
            var results = new List<string?>();

            var templates = JsonSerializer.Deserialize<List<TemplateDefinition>>(json);
            if (templates == null) return results;

            foreach (var item in templates)
            {
                var template = item.PropertyName ?? "";
                var parameters = item.Params?.ToArray() ?? Array.Empty<string>();

                try
                {
                    // Resolve using StringBuilder only (no regex): either String.Format when possible, or ResolveOne (manual scan + StringBuilder).
                    var resolved = parameters.Length == 0
                        ? template
                        : ResolveOneWithStringBuilder(template, parameters);
                    results.Add(resolved);
                }
                catch (FormatException fe)
                {
                    Console.Error.WriteLine($"Template format error for '{template}': {fe.Message}");
                    results.Add(null);
                }
            }

            return results;
        }

        /// <summary>
        /// Resolves a single template string with placeholders {0}, {1}, ... using only StringBuilder (no regex).
        /// Scans for placeholder indices manually and builds the result with StringBuilder.
        /// </summary>
        public static string ResolveOneWithStringBuilder(string template, string[] parameters)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            // No placeholders: return as-is
            var sb = new StringBuilder(template.Length);
            int i = 0;
            while (i < template.Length)
            {
                if (template[i] == '{' && i + 1 < template.Length)
                {
                    int start = i + 1;
                    int j = start;
                    while (j < template.Length && char.IsDigit(template[j]))
                        j++;
                    if (j < template.Length && template[j] == '}' && j > start)
                    {
                        var indexStr = template.Substring(start, j - start);
                        if (int.TryParse(indexStr, out int index) && index >= 0 && index < parameters.Length)
                        {
                            sb.Append(parameters[index]);
                            i = j + 1;
                            continue;
                        }
                    }
                }
                sb.Append(template[i]);
                i++;
            }

            return sb.ToString();
        }
    }
}
