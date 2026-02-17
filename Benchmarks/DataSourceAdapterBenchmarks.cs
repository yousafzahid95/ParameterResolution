using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using AntlrTest1.AntlrExtraction;
using AntlrTest1.BespokeCustomExtraction;
using AntlrTest1.Events;
using AntlrTest1.Interfaces;
using Newtonsoft.Json.Linq;
using AntlrTest1;

namespace AntlrTest1.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [MarkdownExporter]
    public class DataSourceAdapterBenchmarks
    {
        private object _eventData = null!;
        private JObject _dataSourceConfig = null!;

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
        }

        [Benchmark(Baseline = true)]
        public async Task<List<IDataRecord>> Antlr_GetRecordsAsync()
        {
            var adapter = new LemDataSourceAdapter();
            var result = await adapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None);
            return result.ToList();
        }

        [Benchmark]
        public async Task<List<IDataRecord>> BespokeCustom_GetRecordsAsync()
        {
            var adapter = new BespokeDataSourceAdapter();
            var result = await adapter.GetRecordsAsync(
                _eventData,
                _dataSourceConfig,
                new List<IDataRecord>(),
                CancellationToken.None);
            return result.ToList();
        }
    }
}
