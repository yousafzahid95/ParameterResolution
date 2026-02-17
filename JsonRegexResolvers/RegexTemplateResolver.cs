using System.Text.Json;
using System.Text.RegularExpressions;

namespace AntlrTest1.JsonRegexResolvers
{
    /// <summary>
    /// Resolves JSON template definitions using regex to find and replace placeholders {0}, {1}, {2}, ...
    /// Bespoke implementation separate from the StringBuilder (no-regex) approach.
    /// </summary>
    public static class RegexTemplateResolver
    {
        // Matches {0}, {1}, {2}, ... (non-negative integer index only)
        private static readonly Regex PlaceholderRegex = new(@"\{(\d+)\}", RegexOptions.Compiled);

        /// <summary>
        /// Takes JSON array string in described format, resolves placeholders {0}, {1}, ...
        /// using regex replacement. Returns list of resolved strings (or null where resolution failed).
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
                    var resolved = ResolveOne(template, parameters);
                    results.Add(resolved);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Template resolution error for '{template}': {ex.Message}");
                    results.Add(null);
                }
            }

            return results;
        }

        /// <summary>
        /// Resolves a single template by replacing {0}, {1}, ... with the corresponding parameter using regex.
        /// </summary>
        public static string ResolveOne(string template, string[] parameters)
        {
            if (string.IsNullOrEmpty(template))
                return template;

            return PlaceholderRegex.Replace(template, match =>
            {
                if (!match.Success || match.Groups.Count < 2)
                    return match.Value;
                var indexStr = match.Groups[1].Value;
                if (int.TryParse(indexStr, out int index) && index >= 0 && index < parameters.Length)
                    return parameters[index];
                return match.Value;
            });
        }
    }
}
