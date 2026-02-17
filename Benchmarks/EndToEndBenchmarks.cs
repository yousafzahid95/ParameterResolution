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
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class EndToEndBenchmarks
    {
        private object _eventData = null!;
        private JObject _dataSourceConfig = null!;
        private JObject _templateConfig = null!;
        private string _template = null!;

        [GlobalSetup]
        public void Setup()
        {
            _eventData = new InfoRequestEvent
            {
                InfoRequest = new InfoRequestData
                {
                    ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    WorkareaId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    EntityIds = new List<Guid> { Guid.Parse("33333333-3333-3333-3333-333333333333") }
                }
            };

            _dataSourceConfig = new JObject
            {
                ["searchOption"] = "Active",
                ["includeOutOfScope"] = false,
                ["includeCollections"] = true,
                ["isProjectLevel"] = false
            };

            _templateConfig = new JObject
            {
                ["StaticValue"] = "Active",
                ["SourceSystemKeyTemplate"] = "A002IR_{LEM.EntityId}",
                ["ActionDescriptionTemplate"] = "Action item for entity {LEM.EntityId} in project {InfoRequest.ProjectId}"
            };

            _template = "Entity {LEM.EntityId} for project {InfoRequest.ProjectId}";
        }

        [Benchmark(Baseline = true)]
        public async Task<string> Antlr_EndToEnd()
        {
            var adapter = new LemDataSourceAdapter();
            var dataset = await adapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None);

            return ParameterTemplateResolver.ResolveTemplate(
                _template,
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

            return resolver.Resolve(_template) ?? string.Empty;
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

            var liquidTemplate = "Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}";
            return resolver.Render(liquidTemplate);
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

            return resolver.Resolve(_template) ?? string.Empty;
        }
    }
}
