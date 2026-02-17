using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using AntlrTest1.AntlrExtraction;
using AntlrTest1.BespokeCustomExtraction;
using AntlrTest1.FluidCoreExtraction;
using AntlrTest1.StringFormatExtraction;
using AntlrTest1.Events;
using AntlrTest1.Interfaces;
using Newtonsoft.Json.Linq;
using AntlrTest1;

namespace AntlrTest1.Benchmarks
{
    /// <summary>
    /// Comprehensive comparison benchmarks that run all approaches side-by-side
    /// for the same scenarios, making it easy to compare performance at a glance.
    /// </summary>
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    [RankColumn]
    public class ComparisonBenchmarks
    {
        private object _eventData = null!;
        private JObject _dataSourceConfig = null!;
        private JObject _templateConfig = null!;
        private List<IDataRecord> _antlrDataset = null!;
        private List<IDataRecord> _bespokeDataset = null!;
        private string _simpleTemplate = null!;
        private string _complexTemplate = null!;
        private string _multiPlaceholderTemplate = null!;

        // Pre-built resolvers to avoid setup overhead in benchmarks
        private BespokeParameterResolver _bespokeResolver = null!;
        private FluidCoreTemplateResolver _fluidCoreResolver = null!;
        private StringFormatTemplateResolver _stringFormatResolver = null!;

        [GlobalSetup]
        public void Setup()
        {
            // Setup event data
            _eventData = new InfoRequestEvent
            {
                InfoRequest = new InfoRequestData
                {
                    ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    WorkareaId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    EntityIds = new List<Guid> { Guid.Parse("33333333-3333-3333-3333-333333333333") }
                }
            };

            // Setup data source config
            _dataSourceConfig = new JObject
            {
                ["searchOption"] = "Active",
                ["includeOutOfScope"] = false,
                ["includeCollections"] = true,
                ["isProjectLevel"] = false
            };

            // Setup template config
            _templateConfig = new JObject
            {
                ["StaticValue"] = "Active",
                ["EmailSubject"] = "Entity {LEM.EntityId} for project {InfoRequest.ProjectId}",
                ["NotificationBody"] = "my value {LEM.EntityId} and another {InfoRequest.ProjectId}",
                ["ConfigOnly"] = "{Config.StaticValue}",
                ["SourceSystemKeyTemplate"] = "A002IR_{LEM.EntityId}",
                ["ActionDescriptionTemplate"] = "Action item for entity {LEM.EntityId} in project {InfoRequest.ProjectId}"
            };

            // Get datasets from adapters
            var antlrAdapter = new LemDataSourceAdapter();
            _antlrDataset = antlrAdapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None).Result.ToList();

            var bespokeAdapter = new BespokeDataSourceAdapter();
            _bespokeDataset = bespokeAdapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None).Result.ToList();

            // Setup templates
            _simpleTemplate = "A002IR_{LEM.EntityId}";
            _complexTemplate = "Entity {LEM.EntityId} for project {InfoRequest.ProjectId}";
            _multiPlaceholderTemplate = "my value {LEM.EntityId} and another {InfoRequest.ProjectId} and config {Config.StaticValue}";

            // Pre-build resolvers
            _bespokeResolver = BespokeParameterResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_bespokeDataset);

            _fluidCoreResolver = FluidCoreTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_bespokeDataset)
                .Build();

            _stringFormatResolver = StringFormatTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_bespokeDataset);
        }

        #region Simple Template Comparison

        [Benchmark(Baseline = true)]
        public string Antlr_SimpleTemplate() =>
            ParameterTemplateResolver.ResolveTemplate(
                _simpleTemplate,
                _eventData,
                _templateConfig,
                _antlrDataset) ?? string.Empty;

        [Benchmark]
        public string BespokeCustom_SimpleTemplate() =>
            _bespokeResolver.Resolve(_simpleTemplate) ?? string.Empty;

        [Benchmark]
        public string FluidCore_SimpleTemplate() =>
            _fluidCoreResolver.Render("A002IR_{{ LEM.EntityId }}");

        [Benchmark]
        public string StringFormat_SimpleTemplate() =>
            _stringFormatResolver.Resolve(_simpleTemplate) ?? string.Empty;

        #endregion

        #region Complex Template Comparison

        [Benchmark]
        public string Antlr_ComplexTemplate() =>
            ParameterTemplateResolver.ResolveTemplate(
                _complexTemplate,
                _eventData,
                _templateConfig,
                _antlrDataset) ?? string.Empty;

        [Benchmark]
        public string BespokeCustom_ComplexTemplate() =>
            _bespokeResolver.Resolve(_complexTemplate) ?? string.Empty;

        [Benchmark]
        public string FluidCore_ComplexTemplate() =>
            _fluidCoreResolver.Render("Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}");

        [Benchmark]
        public string StringFormat_ComplexTemplate() =>
            _stringFormatResolver.Resolve(_complexTemplate) ?? string.Empty;

        #endregion

        #region Multi-Placeholder Template Comparison

        [Benchmark]
        public string Antlr_MultiPlaceholderTemplate() =>
            ParameterTemplateResolver.ResolveTemplate(
                _multiPlaceholderTemplate,
                _eventData,
                _templateConfig,
                _antlrDataset) ?? string.Empty;

        [Benchmark]
        public string BespokeCustom_MultiPlaceholderTemplate() =>
            _bespokeResolver.Resolve(_multiPlaceholderTemplate) ?? string.Empty;

        [Benchmark]
        public string FluidCore_MultiPlaceholderTemplate() =>
            _fluidCoreResolver.Render("my value {{ LEM.EntityId }} and another {{ Event.InfoRequest.ProjectId }} and config {{ Config.StaticValue }}");

        [Benchmark]
        public string StringFormat_MultiPlaceholderTemplate() =>
            _stringFormatResolver.Resolve(_multiPlaceholderTemplate) ?? string.Empty;

        #endregion

        #region End-to-End Comparison (Adapter + Template Resolution)

        [Benchmark]
        public async Task<string> Antlr_EndToEnd()
        {
            var adapter = new LemDataSourceAdapter();
            var dataset = await adapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None);

            return ParameterTemplateResolver.ResolveTemplate(
                _complexTemplate,
                _eventData,
                _templateConfig,
                dataset) ?? string.Empty;
        }

        [Benchmark]
        public async Task<string> BespokeCustom_EndToEnd()
        {
            var adapter = new BespokeDataSourceAdapter();
            var dataset = await adapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None);

            var resolver = BespokeParameterResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(dataset);

            return resolver.Resolve(_complexTemplate) ?? string.Empty;
        }

        [Benchmark]
        public async Task<string> FluidCore_EndToEnd()
        {
            var adapter = new BespokeDataSourceAdapter();
            var dataset = await adapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None);

            var resolver = FluidCoreTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(dataset)
                .Build();

            return resolver.Render("Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}");
        }

        [Benchmark]
        public async Task<string> StringFormat_EndToEnd()
        {
            var adapter = new BespokeDataSourceAdapter();
            var dataset = await adapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None);

            var resolver = StringFormatTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(dataset);

            return resolver.Resolve(_complexTemplate) ?? string.Empty;
        }

        #endregion
    }
}
