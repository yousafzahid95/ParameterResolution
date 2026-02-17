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
    public class TemplateResolutionBenchmarks
    {
        private object _eventData = null!;
        private JObject _templateConfig = null!;
        private List<IDataRecord> _dataset = null!;
        private string _simpleTemplate = null!;
        private string _complexTemplate = null!;
        private string _multiPlaceholderTemplate = null!;

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

            // Setup dataset
            var dummyData = new JObject
            {
                ["EntityId"] = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ["ProjectId"] = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ["WorkAreaId"] = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ["EntityName"] = "Test Entity",
                ["Status"] = "Active"
            };
            _dataset = new List<IDataRecord> { new DataRecord { Data = dummyData, Source = "LEM" } };

            // Setup templates
            _simpleTemplate = "A002IR_{LEM.EntityId}";
            _complexTemplate = "Entity {LEM.EntityId} for project {InfoRequest.ProjectId}";
            _multiPlaceholderTemplate = "my value {LEM.EntityId} and another {InfoRequest.ProjectId} and config {Config.StaticValue}";
        }

        [Benchmark(Baseline = true)]
        public string Antlr_SimpleTemplate()
        {
            return ParameterTemplateResolver.ResolveTemplate(
                _simpleTemplate,
                _eventData,
                _templateConfig,
                _dataset) ?? string.Empty;
        }

        [Benchmark]
        public string BespokeCustom_SimpleTemplate()
        {
            var resolver = BespokeParameterResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset);
            return resolver.Resolve(_simpleTemplate) ?? string.Empty;
        }

        [Benchmark]
        public string FluidCore_SimpleTemplate()
        {
            var resolver = FluidCoreTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset)
                .Build();
            var liquidTemplate = "A002IR_{{ LEM.EntityId }}";
            return resolver.Render(liquidTemplate);
        }

        [Benchmark]
        public string StringFormat_SimpleTemplate()
        {
            var resolver = StringFormatTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset);
            return resolver.Resolve(_simpleTemplate) ?? string.Empty;
        }

        [Benchmark]
        public string Antlr_ComplexTemplate()
        {
            return ParameterTemplateResolver.ResolveTemplate(
                _complexTemplate,
                _eventData,
                _templateConfig,
                _dataset) ?? string.Empty;
        }

        [Benchmark]
        public string BespokeCustom_ComplexTemplate()
        {
            var resolver = BespokeParameterResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset);
            return resolver.Resolve(_complexTemplate) ?? string.Empty;
        }

        [Benchmark]
        public string FluidCore_ComplexTemplate()
        {
            var resolver = FluidCoreTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset)
                .Build();
            var liquidTemplate = "Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}";
            return resolver.Render(liquidTemplate);
        }

        [Benchmark]
        public string StringFormat_ComplexTemplate()
        {
            var resolver = StringFormatTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset);
            return resolver.Resolve(_complexTemplate) ?? string.Empty;
        }

        [Benchmark]
        public string Antlr_MultiPlaceholderTemplate()
        {
            return ParameterTemplateResolver.ResolveTemplate(
                _multiPlaceholderTemplate,
                _eventData,
                _templateConfig,
                _dataset) ?? string.Empty;
        }

        [Benchmark]
        public string BespokeCustom_MultiPlaceholderTemplate()
        {
            var resolver = BespokeParameterResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset);
            return resolver.Resolve(_multiPlaceholderTemplate) ?? string.Empty;
        }

        [Benchmark]
        public string FluidCore_MultiPlaceholderTemplate()
        {
            var resolver = FluidCoreTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset)
                .Build();
            var liquidTemplate = "my value {{ LEM.EntityId }} and another {{ Event.InfoRequest.ProjectId }} and config {{ Config.StaticValue }}";
            return resolver.Render(liquidTemplate);
        }

        [Benchmark]
        public string StringFormat_MultiPlaceholderTemplate()
        {
            var resolver = StringFormatTemplateResolver
                .Create()
                .WithEvent(_eventData)
                .WithConfig(_templateConfig)
                .WithDataset(_dataset);
            return resolver.Resolve(_multiPlaceholderTemplate) ?? string.Empty;
        }
    }
}
