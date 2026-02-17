using System.Reflection;
using Newtonsoft.Json.Linq;

namespace AntlrTest1.BespokeCustomExtraction
{
    /// <summary>
    /// Standalone parameter extractor using reflection and JObject navigation.
    /// This is a Bespoke Custom approach implementation that does NOT use ANTLR.
    /// Supports: PropertyName, Nested.Property, Array[0], Nested.Array[0].Property
    /// </summary>
    public static class BespokeParameterExtractor
    {
        /// <summary>
        /// Extract a value from an object using a path expression (Bespoke Custom approach - no ANTLR).
        /// </summary>
        /// <param name="source">Source object to navigate</param>
        /// <param name="pathExpression">Path like "InfoRequest.EntityIds[0]"</param>
        /// <returns>Extracted value or null</returns>
        public static object? Extract(object source, string pathExpression)
        {
            if (source == null || string.IsNullOrEmpty(pathExpression))
                return null;

            // Parse path expression into segments
            var segments = ParsePathExpression(pathExpression);
            return NavigatePath(source, segments);
        }

        /// <summary>
        /// Extract a GUID from an object using a path expression.
        /// </summary>
        public static Guid ExtractGuid(object source, string pathExpression)
        {
            var value = Extract(source, pathExpression);

            if (value is Guid guid)
                return guid;

            if (value != null && Guid.TryParse(value.ToString(), out var parsedGuid))
                return parsedGuid;

            return Guid.Empty;
        }

        /// <summary>
        /// Try multiple path expressions (OR logic) and return first valid GUID.
        /// </summary>
        public static Guid ExtractGuidWithAlternatives(object source, IEnumerable<string> pathExpressions)
        {
            foreach (var path in pathExpressions)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var guid = ExtractGuid(source, path);
                if (guid != Guid.Empty)
                {
                    Console.WriteLine($"  [Bespoke Custom] Matched path: {path} = {guid}");
                    return guid;
                }
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Parse path expression into segments (Bespoke Custom approach - simple string parsing).
        /// </summary>
        private static List<PathSegment> ParsePathExpression(string pathExpression)
        {
            var segments = new List<PathSegment>();
            var parts = pathExpression.Split('.');

            foreach (var part in parts)
            {
                var segment = new PathSegment();

                // Check for array indexing: PropertyName[0]
                var bracketIndex = part.IndexOf('[');
                if (bracketIndex >= 0)
                {
                    segment.PropertyName = part.Substring(0, bracketIndex);
                    
                    var closeBracket = part.IndexOf(']');
                    if (closeBracket > bracketIndex)
                    {
                        var indexStr = part.Substring(bracketIndex + 1, closeBracket - bracketIndex - 1);
                        if (int.TryParse(indexStr, out var index))
                        {
                            segment.ArrayIndex = index;
                        }
                    }
                }
                else
                {
                    segment.PropertyName = part;
                }

                if (!string.IsNullOrEmpty(segment.PropertyName))
                {
                    segments.Add(segment);
                }
            }

            return segments;
        }

        /// <summary>
        /// Navigate through path segments to extract value.
        /// </summary>
        private static object? NavigatePath(object current, List<PathSegment> segments)
        {
            foreach (var segment in segments)
            {
                if (current == null)
                    return null;

                // Get property value
                current = GetPropertyValue(current, segment.PropertyName);

                if (current == null)
                    return null;

                // Handle array indexing if needed
                if (segment.ArrayIndex.HasValue)
                {
                    current = GetArrayElement(current, segment.ArrayIndex.Value);
                }
            }

            return current;
        }

        /// <summary>
        /// Get property value from a CLR object, JObject, or JToken using case-insensitive matching where appropriate.
        /// </summary>
        private static object? GetPropertyValue(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;

            // Handle JObject directly
            if (obj is JObject jObject)
            {
                // JObject property lookup is case-sensitive; try exact then case-insensitive
                JToken? token = jObject[propertyName];
                if (token == null)
                {
                    // Case-insensitive search over children
                    token = jObject.Properties()
                        .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        ?.Value;
                }

                if (token == null)
                    return null;

                if (token is JValue jValue)
                {
                    return jValue.Value;
                }

                return token;
            }

            // Handle generic JToken that might wrap an object
            if (obj is JToken jToken && jToken.Type == JTokenType.Object)
            {
                var child = jToken[propertyName];
                if (child == null && jToken is JObject jObj)
                {
                    child = jObj.Properties()
                        .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                        ?.Value;
                }

                if (child == null)
                    return null;

                if (child is JValue jChildValue)
                {
                    return jChildValue.Value;
                }

                return child;
            }

            // Use reflection for CLR objects
            var type = obj.GetType();
            
            // Try exact match first
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            
            // Try case-insensitive match
            if (property == null)
            {
                property = type.GetProperty(propertyName, 
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }

            return property?.GetValue(obj);
        }

        /// <summary>
        /// Get element from array or list.
        /// </summary>
        private static object? GetArrayElement(object obj, int index)
        {
            if (obj == null)
                return null;

            // Handle JArray
            if (obj is JArray jArray && index >= 0 && index < jArray.Count)
            {
                return jArray[index];
            }

            // Handle IList
            if (obj is System.Collections.IList list && index >= 0 && index < list.Count)
            {
                return list[index];
            }

            // Handle IEnumerable
            if (obj is System.Collections.IEnumerable enumerable)
            {
                return enumerable.Cast<object>().Skip(index).FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Auto-discover all GUID properties in an object (Bespoke Custom approach).
        /// </summary>
        public static Dictionary<string, Guid> DiscoverGuids(object obj, string prefix = "")
        {
            var results = new Dictionary<string, Guid>();

            if (obj == null)
                return results;

            DiscoverGuidsRecursive(obj, prefix, results, 0);

            return results;
        }

        /// <summary>
        /// Discover and categorize GUIDs based on naming patterns (Bespoke Custom approach).
        /// </summary>
        public static (Guid ProjectId, Guid WorkAreaId, Guid EntityId) DiscoverSemanticGuids(object obj)
        {
            var discoveredGuids = DiscoverGuids(obj);
            
            Guid projectId = Guid.Empty;
            Guid workAreaId = Guid.Empty;
            Guid entityId = Guid.Empty;

            foreach (var kvp in discoveredGuids)
            {
                var path = kvp.Key;
                var guid = kvp.Value;

                // Check if path matches ProjectId pattern (Bespoke Custom pattern matching)
                if (projectId == Guid.Empty && MatchesProjectIdPattern(path))
                {
                    projectId = guid;
                    Console.WriteLine($"  [Bespoke Custom Pattern] ProjectId matched: {path} = {guid}");
                }
                // Check if path matches WorkAreaId pattern
                else if (workAreaId == Guid.Empty && MatchesWorkAreaIdPattern(path))
                {
                    workAreaId = guid;
                    Console.WriteLine($"  [Bespoke Custom Pattern] WorkAreaId matched: {path} = {guid}");
                }
                // Check if path matches EntityId pattern
                else if (entityId == Guid.Empty && MatchesEntityIdPattern(path))
                {
                    entityId = guid;
                    Console.WriteLine($"  [Bespoke Custom Pattern] EntityId matched: {path} = {guid}");
                }
            }

            return (projectId, workAreaId, entityId);
        }

        /// <summary>
        /// Check if a path matches the ProjectId semantic pattern (Bespoke Custom pattern matching).
        /// </summary>
        private static bool MatchesProjectIdPattern(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Bespoke Custom pattern matching: check for ProjectId in path
            var lowerPath = path.ToLowerInvariant();
            return lowerPath.Contains("projectid") && 
                   (lowerPath.EndsWith(".projectid") || lowerPath == "projectid" ||
                    lowerPath.Contains("inforequest.projectid") ||
                    lowerPath.Contains("lemevent.projectid") ||
                    lowerPath.Contains("workplantask.projectid"));
        }

        /// <summary>
        /// Check if a path matches the WorkAreaId semantic pattern (Bespoke Custom pattern matching).
        /// </summary>
        private static bool MatchesWorkAreaIdPattern(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var lowerPath = path.ToLowerInvariant();
            return (lowerPath.Contains("workareaid") || lowerPath.Contains("workareaid")) &&
                   (lowerPath.EndsWith(".workareaid") || lowerPath.EndsWith(".workareaid") ||
                    lowerPath == "workareaid" || lowerPath == "workareaid" ||
                    lowerPath.Contains("inforequest.workareaid") ||
                    lowerPath.Contains("lemevent.workareaid") ||
                    lowerPath.Contains("workplantask.workareaid"));
        }

        /// <summary>
        /// Check if a path matches the EntityId semantic pattern (Bespoke Custom pattern matching).
        /// </summary>
        private static bool MatchesEntityIdPattern(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var lowerPath = path.ToLowerInvariant();
            return (lowerPath.Contains("entityid") || lowerPath.Contains("entityids")) &&
                   (lowerPath.EndsWith(".entityid") || lowerPath.Contains("entityids[0]") ||
                    lowerPath == "entityid" ||
                    lowerPath.Contains("inforequest.entityids") ||
                    lowerPath.Contains("lemevent.entityid") ||
                    lowerPath.Contains("workplantask.entities") ||
                    lowerPath.Contains("workplantask.entityids"));
        }

        private static void DiscoverGuidsRecursive(object obj, string currentPath, Dictionary<string, Guid> results, int depth)
        {
            if (obj == null || depth > 5) // Max depth to prevent infinite recursion
                return;

            var type = obj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var propValue = prop.GetValue(obj);
                var propPath = string.IsNullOrEmpty(currentPath) ? prop.Name : $"{currentPath}.{prop.Name}";

                if (propValue == null)
                    continue;

                // If it's a Guid
                if (prop.PropertyType == typeof(Guid))
                {
                    var guidValue = (Guid)propValue;
                    if (guidValue != Guid.Empty)
                    {
                        results[propPath] = guidValue;
                    }
                }
                // If it's a List<Guid>
                else if (propValue is IEnumerable<Guid> guidList)
                {
                    var firstGuid = guidList.FirstOrDefault();
                    if (firstGuid != Guid.Empty)
                    {
                        results[$"{propPath}[0]"] = firstGuid;
                    }
                }
                // If it's a complex object (not primitive, not string)
                else if (!prop.PropertyType.IsPrimitive && 
                         prop.PropertyType != typeof(string) &&
                         prop.PropertyType != typeof(DateTime) &&
                         !prop.PropertyType.IsEnum &&
                         prop.Name != "Body") // Skip Body to avoid deserialization
                {
                    // Check if it's a collection
                    if (propValue is System.Collections.IEnumerable enumerable && prop.PropertyType != typeof(string))
                    {
                        var index = 0;
                        foreach (var item in enumerable)
                        {
                            if (item != null && !item.GetType().IsPrimitive)
                            {
                                DiscoverGuidsRecursive(item, $"{propPath}[{index}]", results, depth + 1);
                            }
                            index++;
                            if (index > 0) break; // Only process first element
                        }
                    }
                    else
                    {
                        // Recurse into nested object
                        DiscoverGuidsRecursive(propValue, propPath, results, depth + 1);
                    }
                }
            }
        }

        private class PathSegment
        {
            public string PropertyName { get; set; } = string.Empty;
            public int? ArrayIndex { get; set; }
        }
    }
}
