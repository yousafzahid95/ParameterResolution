using AntlrTest1.Interfaces;
using Fluid;
using Newtonsoft.Json.Linq;

namespace AntlrTest1.FluidExtraction
{
    /// <summary>
    /// Template resolver using Fluid.Core NuGet package (Liquid template engine).
    /// This integrates the Fluid.Core package for template rendering.
    /// Uses Liquid syntax: {{ variable.property }} instead of {variable.property}
    /// 
    /// Example:
    ///   var resolver = FluidCoreTemplateResolver
    ///       .Create()
    ///       .WithEvent(eventData)
    ///       .WithConfig(ruleConfig)
    ///       .WithDataset(dataset)
    ///       .Build();
    ///   
    ///   var result = await resolver.RenderAsync("Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}");
    /// </summary>
    public class FluidCoreTemplateResolver
    {
        private static readonly FluidParser _parser = new();
        private readonly TemplateContext _context;

        private FluidCoreTemplateResolver(TemplateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new Fluid.Core template resolver builder.
        /// </summary>
        public static FluidCoreTemplateResolverBuilder Create()
        {
            return new FluidCoreTemplateResolverBuilder();
        }

        /// <summary>
        /// Renders a Liquid template using Fluid.Core.
        /// </summary>
        /// <param name="template">Liquid template string (e.g., "Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}")</param>
        /// <returns>Rendered string</returns>
        public async Task<string> RenderAsync(string template)
        {
            if (string.IsNullOrEmpty(template))
                return template ?? string.Empty;

            try
            {
                var fluidTemplate = _parser.Parse(template);
                return await fluidTemplate.RenderAsync(_context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fluid.Core] Template rendering error: {ex.Message}");
                return template; // Return original template on error
            }
        }

        /// <summary>
        /// Renders a Liquid template synchronously.
        /// </summary>
        public string Render(string template)
        {
            if (string.IsNullOrEmpty(template))
                return template ?? string.Empty;

            try
            {
                var fluidTemplate = _parser.Parse(template);
                return fluidTemplate.Render(_context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fluid.Core] Template rendering error: {ex.Message}");
                return template; // Return original template on error
            }
        }

        /// <summary>
        /// Builder for creating Fluid.Core template resolvers.
        /// </summary>
        public class FluidCoreTemplateResolverBuilder
        {
            private object? _eventData;
            private JObject? _ruleConfig;
            private IEnumerable<IDataRecord>? _dataset;

            /// <summary>
            /// Sets the event data source (accessible as "Event" in templates).
            /// </summary>
            public FluidCoreTemplateResolverBuilder WithEvent(object eventData)
            {
                _eventData = eventData;
                return this;
            }

            /// <summary>
            /// Sets the rule configuration (accessible as "Config" in templates).
            /// </summary>
            public FluidCoreTemplateResolverBuilder WithConfig(JObject ruleConfig)
            {
                _ruleConfig = ruleConfig;
                return this;
            }

            /// <summary>
            /// Sets the dataset (records accessible by their Source name, e.g., "LEM").
            /// </summary>
            public FluidCoreTemplateResolverBuilder WithDataset(IEnumerable<IDataRecord> dataset)
            {
                _dataset = dataset;
                return this;
            }

            /// <summary>
            /// Builds the Fluid.Core template resolver with configured context.
            /// </summary>
            public FluidCoreTemplateResolver Build()
            {
                var context = new TemplateContext();

                // Add event data to context as "Event"
                if (_eventData != null)
                {
                    var eventDict = ConvertToDictionary(_eventData);
                    context.SetValue("Event", eventDict);
                }

                // Add config to context as "Config"
                if (_ruleConfig != null)
                {
                    var configDict = ConvertJObjectToDictionary(_ruleConfig);
                    context.SetValue("Config", configDict);
                }

                // Add dataset records to context by their Source name
                if (_dataset != null)
                {
                    foreach (var record in _dataset)
                    {
                        if (!string.IsNullOrEmpty(record.Source))
                        {
                            var recordDict = ConvertJObjectToDictionary(record.Data);
                            context.SetValue(record.Source, recordDict);
                        }
                    }
                }

                return new FluidCoreTemplateResolver(context);
            }

            private Dictionary<string, object?> ConvertToDictionary(object obj)
            {
                var dict = new Dictionary<string, object?>();
                var type = obj.GetType();
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    var value = prop.GetValue(obj);
                    dict[prop.Name] = ConvertValue(value);
                }

                return dict;
            }

            private Dictionary<string, object?> ConvertJObjectToDictionary(JObject jObj)
            {
                var dict = new Dictionary<string, object?>();

                foreach (var prop in jObj.Properties())
                {
                    dict[prop.Name] = ConvertJToken(prop.Value);
                }

                return dict;
            }

            private object? ConvertValue(object? value)
            {
                if (value == null) return null;
                if (value is Guid guid) return guid.ToString();
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    var list = new List<object?>();
                    foreach (var item in enumerable)
                    {
                        list.Add(ConvertValue(item));
                    }
                    return list;
                }
                // Handle nested objects
                if (value.GetType().IsClass && value.GetType() != typeof(string))
                {
                    return ConvertToDictionary(value);
                }
                return value;
            }

            private object? ConvertJToken(JToken? token)
            {
                if (token == null) return null;

                return token.Type switch
                {
                    JTokenType.Object => ConvertJObjectToDictionary((JObject)token),
                    JTokenType.Array => token.Select(ConvertJToken).ToList(),
                    JTokenType.String => token.ToString(),
                    JTokenType.Integer => token.ToObject<long>(),
                    JTokenType.Float => token.ToObject<double>(),
                    JTokenType.Boolean => token.ToObject<bool>(),
                    JTokenType.Null => null,
                    JTokenType.Guid => token.ToString(),
                    _ => token.ToString()
                };
            }
        }
    }
}
