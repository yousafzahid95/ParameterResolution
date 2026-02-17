using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using AntlrTest1.AntlrExtraction;
using AntlrTest1.BespokeCustomExtraction;
using AntlrTest1.Events;

namespace AntlrTest1.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class ParameterExtractionBenchmarks
    {
        private object _eventData = null!;
        private string _simplePath = null!;
        private string _nestedPath = null!;
        private string _arrayPath = null!;

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

            _simplePath = "InfoRequest.ProjectId";
            _nestedPath = "InfoRequest.WorkareaId";
            _arrayPath = "InfoRequest.EntityIds[0]";
        }

        [Benchmark(Baseline = true)]
        public object? Antlr_ExtractSimplePath()
        {
            return AntlrParameterExtractor.Extract(_eventData, _simplePath);
        }

        [Benchmark]
        public object? BespokeCustom_ExtractSimplePath()
        {
            return BespokeParameterExtractor.Extract(_eventData, _simplePath);
        }

        [Benchmark]
        public object? Antlr_ExtractNestedPath()
        {
            return AntlrParameterExtractor.Extract(_eventData, _nestedPath);
        }

        [Benchmark]
        public object? BespokeCustom_ExtractNestedPath()
        {
            return BespokeParameterExtractor.Extract(_eventData, _nestedPath);
        }

        [Benchmark]
        public object? Antlr_ExtractArrayPath()
        {
            return AntlrParameterExtractor.Extract(_eventData, _arrayPath);
        }

        [Benchmark]
        public object? BespokeCustom_ExtractArrayPath()
        {
            return BespokeParameterExtractor.Extract(_eventData, _arrayPath);
        }

        [Benchmark]
        public (Guid, Guid, Guid) Antlr_DiscoverSemanticGuids()
        {
            return AntlrParameterExtractor.DiscoverSemanticGuids(_eventData);
        }

        [Benchmark]
        public (Guid, Guid, Guid) BespokeCustom_DiscoverSemanticGuids()
        {
            return BespokeParameterExtractor.DiscoverSemanticGuids(_eventData);
        }
    }
}
