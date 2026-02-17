using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using AntlrTest1.JsonStringBuilderResolvers;
using AntlrTest1.JsonRegexResolvers;

namespace AntlrTest1.Benchmarks
{
    /// <summary>
    /// Benchmarks for the two JSON template resolver approaches only:
    /// StringBuilder (no regex) and Regex (bespoke). Same JSON input for fair comparison.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class JsonResolverBenchmarks
    {
        private string _jsonSmall = null!;
        private string _jsonMedium = null!;
        private string _jsonManyPlaceholders = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Small: 3 templates (matches JsonExample)
            _jsonSmall = """
                [
                  {
                    "propertyName": "my value {0} and another value {1} and other {2}",
                    "params": ["param0", "param1", "param2"]
                  },
                  {
                    "propertyName": "static text",
                    "params": []
                  },
                  {
                    "propertyName": "{0}",
                    "params": ["param0"]
                  }
                ]
                """;

            // Medium: more templates
            var mediumItems = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                mediumItems.Add($$"""
                    {
                      "propertyName": "Template {{i}}: {0} and {1}",
                      "params": ["val0", "val1"]
                    }
                    """);
            }
            _jsonMedium = "[\n  " + string.Join(",\n  ", mediumItems) + "\n]";

            // Many placeholders per template
            var placeholders = string.Join(" ", Enumerable.Range(0, 10).Select(i => $"{{{i}}}"));
            var paramList = string.Join(", ", Enumerable.Range(0, 10).Select(i => $"\"p{i}\""));
            _jsonManyPlaceholders = "[\n  {\n    \"propertyName\": \"" + placeholders + "\",\n    \"params\": [" + paramList + "]\n  }\n]";
        }

        [Benchmark(Baseline = true)]
        public List<string?> StringBuilder_Small()
        {
            return StringBuilderTemplateResolver.ResolveTemplates(_jsonSmall);
        }

        [Benchmark]
        public List<string?> Regex_Small()
        {
            return RegexTemplateResolver.ResolveTemplates(_jsonSmall);
        }

        [Benchmark]
        public List<string?> StringBuilder_Medium()
        {
            return StringBuilderTemplateResolver.ResolveTemplates(_jsonMedium);
        }

        [Benchmark]
        public List<string?> Regex_Medium()
        {
            return RegexTemplateResolver.ResolveTemplates(_jsonMedium);
        }

        [Benchmark]
        public List<string?> StringBuilder_ManyPlaceholders()
        {
            return StringBuilderTemplateResolver.ResolveTemplates(_jsonManyPlaceholders);
        }

        [Benchmark]
        public List<string?> Regex_ManyPlaceholders()
        {
            return RegexTemplateResolver.ResolveTemplates(_jsonManyPlaceholders);
        }
    }
}
