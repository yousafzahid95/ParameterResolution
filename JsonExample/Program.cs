using AntlrTest1.JsonStringBuilderResolvers;
using AntlrTest1.JsonRegexResolvers;

namespace AntlrTest1.JsonExample
{
    /// <summary>
    /// Demo for both JSON template resolver implementations (StringBuilder-only and Regex).
    /// Run this project or call from main Program to see output.
    /// </summary>
    public static class JsonExampleProgram
    {
        public static void Run()
        {
            string json = """
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

            Console.WriteLine("=== StringBuilder implementation (no regex) ===");
            var resolvedSb = StringBuilderTemplateResolver.ResolveTemplates(json);
            foreach (var s in resolvedSb)
                Console.WriteLine(s ?? "(error)");

            Console.WriteLine("\n=== Regex implementation (bespoke) ===");
            var resolvedRegex = RegexTemplateResolver.ResolveTemplates(json);
            foreach (var s in resolvedRegex)
                Console.WriteLine(s ?? "(error)");
        }
    }
}
