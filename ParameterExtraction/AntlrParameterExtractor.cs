using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Newtonsoft.Json.Linq;

namespace AntlrTest1.ParameterExtraction
{
    /// <summary>
    /// ANTLR4-powered parameter extractor for parsing path expressions
    /// Uses generated PathExpression grammar for tokenization and parsing
    /// Supports: PropertyName, Nested.Property, Array[0], Nested.Array[0].Property
    /// </summary>
    public class AntlrParameterExtractor
    {
        /// <summary>
        /// Extract a value from an object using a path expression
        /// </summary>
        /// <param name="source">Source object to navigate</param>
        /// <param name="pathExpression">Path like "InfoRequest.EntityIds[0]"</param>
        /// <returns>Extracted value or null</returns>
        public static object? Extract(object source, string pathExpression)
        {
            if (source == null || string.IsNullOrEmpty(pathExpression))
                return null;

            // Use generated ANTLR4 parser
            var tokens = ParseWithGeneratedGrammar(pathExpression);
            return NavigateTokens(source, tokens);
        }

        /// <summary>
        /// Extract a GUID from an object using a path expression
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
        /// Try multiple path expressions (OR logic) and return first valid GUID
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
                    Console.WriteLine($"  [ANTLR4] Matched path: {path} = {guid}");
                    return guid;
                }
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Parse path expression using the generated PathExpression grammar
        /// </summary>
        private static List<PathToken> ParseWithGeneratedGrammar(string pathExpression)
        {
            try
            {
                // Create ANTLR4 input stream
                var inputStream = new AntlrInputStream(pathExpression);
                
                // Create lexer from generated grammar
                var lexer = new PathExpressionLexer(inputStream);
                
                // Create token stream
                var tokenStream = new CommonTokenStream(lexer);
                
                // Create parser from generated grammar
                var parser = new PathExpressionParser(tokenStream);
                
                // Disable error output for cleaner console
                parser.RemoveErrorListeners();
                
                // Parse the path expression
                var context = parser.pathExpression();
                
                // Check if parsing was successful
                if (context == null || context.exception != null)
                {
                    return FallbackTokenize(pathExpression);
                }
                
                // Visit the parse tree to extract tokens
                var visitor = new PathExpressionVisitor();
                return visitor.Visit(context) ?? FallbackTokenize(pathExpression);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ANTLR4] Parsing failed, using fallback: {ex.Message}");
                return FallbackTokenize(pathExpression);
            }
        }

        /// <summary>
        /// Visitor to extract path tokens from the parse tree
        /// </summary>
        private class PathExpressionVisitor : PathExpressionBaseVisitor<List<PathToken>>
        {
            public override List<PathToken> VisitPathExpression(PathExpressionParser.PathExpressionContext context)
            {
                var tokens = new List<PathToken>();
                
                if (context == null)
                    return tokens;
                
                // Visit each segment
                var segments = context.segment();
                if (segments == null)
                    return tokens;
                    
                foreach (var segment in segments)
                {
                    if (segment != null)
                    {
                        var token = ExtractSegment(segment);
                        if (token != null)
                            tokens.Add(token);
                    }
                }
                
                return tokens;
            }

            private PathToken? ExtractSegment(PathExpressionParser.SegmentContext context)
            {
                if (context == null || context.IDENTIFIER() == null)
                    return null;
                    
                var token = new PathToken
                {
                    PropertyName = context.IDENTIFIER().GetText()
                };
                
                // Check for array access
                if (context.arrayAccess() != null && context.arrayAccess().INTEGER() != null)
                {
                    var indexText = context.arrayAccess().INTEGER().GetText();
                    if (int.TryParse(indexText, out var index))
                    {
                        token.ArrayIndex = index;
                    }
                }
                
                return token;
            }
        }

        /// <summary>
        /// Fallback tokenization if ANTLR parsing fails
        /// </summary>
        private static List<PathToken> FallbackTokenize(string pathExpression)
        {
            var tokens = new List<PathToken>();
            var segments = pathExpression.Split('.');

            foreach (var segment in segments)
            {
                var token = new PathToken();
                
                // Check for array indexing
                var bracketIndex = segment.IndexOf('[');
                if (bracketIndex >= 0)
                {
                    token.PropertyName = segment.Substring(0, bracketIndex);
                    
                    var closeBracket = segment.IndexOf(']');
                    if (closeBracket > bracketIndex)
                    {
                        var indexStr = segment.Substring(bracketIndex + 1, closeBracket - bracketIndex - 1);
                        if (int.TryParse(indexStr, out var index))
                        {
                            token.ArrayIndex = index;
                        }
                    }
                }
                else
                {
                    token.PropertyName = segment;
                }

                if (!string.IsNullOrEmpty(token.PropertyName))
                {
                    tokens.Add(token);
                }
            }

            return tokens;
        }

        /// <summary>
        /// Navigate through tokens to extract value
        /// </summary>
        private static object? NavigateTokens(object current, List<PathToken> tokens)
        {
            foreach (var token in tokens)
            {
                if (current == null)
                    return null;

                // Get property value
                current = GetPropertyValue(current, token.PropertyName);

                if (current == null)
                    return null;

                // Handle array indexing if needed
                if (token.ArrayIndex.HasValue)
                {
                    current = GetArrayElement(current, token.ArrayIndex.Value);
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
        /// Get element from array or list
        /// </summary>
        private static object? GetArrayElement(object obj, int index)
        {
            if (obj == null)
                return null;

            if (obj is System.Collections.IList list && index >= 0 && index < list.Count)
            {
                return list[index];
            }

            if (obj is System.Collections.IEnumerable enumerable)
            {
                return enumerable.Cast<object>().Skip(index).FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Auto-discover all GUID properties in an object
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
        /// Discover and categorize GUIDs based on grammar semantic patterns
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

                // Check if path matches ProjectId pattern
                if (projectId == Guid.Empty && MatchesProjectIdPattern(path))
                {
                    projectId = guid;
                    Console.WriteLine($"  [Grammar Pattern] ProjectId matched: {path} = {guid}");
                }
                // Check if path matches WorkAreaId pattern
                else if (workAreaId == Guid.Empty && MatchesWorkAreaIdPattern(path))
                {
                    workAreaId = guid;
                    Console.WriteLine($"  [Grammar Pattern] WorkAreaId matched: {path} = {guid}");
                }
                // Check if path matches EntityId pattern
                else if (entityId == Guid.Empty && MatchesEntityIdPattern(path))
                {
                    entityId = guid;
                    Console.WriteLine($"  [Grammar Pattern] EntityId matched: {path} = {guid}");
                }
            }

            return (projectId, workAreaId, entityId);
        }

        /// <summary>
        /// Check if a path matches the ProjectId semantic pattern from grammar
        /// </summary>
        private static bool MatchesProjectIdPattern(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                var inputStream = new AntlrInputStream(path);
                var lexer = new PathExpressionLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);
                var parser = new PathExpressionParser(tokenStream);
                
                parser.RemoveErrorListeners();
                
                // Try to parse as projectIdPattern
                var context = parser.projectIdPattern();
                
                // If parsing succeeded without errors, it matches the pattern
                return context != null && context.exception == null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a path matches the WorkAreaId semantic pattern from grammar
        /// </summary>
        private static bool MatchesWorkAreaIdPattern(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                var inputStream = new AntlrInputStream(path);
                var lexer = new PathExpressionLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);
                var parser = new PathExpressionParser(tokenStream);
                
                parser.RemoveErrorListeners();
                
                // Try to parse as workAreaIdPattern
                var context = parser.workAreaIdPattern();
                
                // If parsing succeeded without errors, it matches the pattern
                return context != null && context.exception == null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a path matches the EntityId semantic pattern from grammar
        /// </summary>
        private static bool MatchesEntityIdPattern(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                var inputStream = new AntlrInputStream(path);
                var lexer = new PathExpressionLexer(inputStream);
                var tokenStream = new CommonTokenStream(lexer);
                var parser = new PathExpressionParser(tokenStream);
                
                parser.RemoveErrorListeners();
                
                // Try to parse as entityIdPattern
                var context = parser.entityIdPattern();
                
                // If parsing succeeded without errors, it matches the pattern
                return context != null && context.exception == null;
            }
            catch
            {
                return false;
            }
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

        private class PathToken
        {
            public string PropertyName { get; set; } = string.Empty;
            public int? ArrayIndex { get; set; }
        }
    }
}
