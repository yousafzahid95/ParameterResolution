# Benchmark Suite

This folder contains comprehensive benchmarks for all four template resolution approaches.

## Benchmark Classes

### 0. `ComparisonBenchmarks.cs` ⭐ **RECOMMENDED FOR SIDE-BY-SIDE COMPARISONS**
**This is the main benchmark class for comparing all approaches at once!**

Provides side-by-side performance comparisons for all 4 approaches:
- **Simple Template**: All 4 approaches resolving `A002IR_{LEM.EntityId}`
- **Complex Template**: All 4 approaches resolving `Entity {LEM.EntityId} for project {InfoRequest.ProjectId}`
- **Multi-Placeholder Template**: All 4 approaches with multiple placeholders
- **End-to-End**: Complete pipeline (adapter + template resolution) for all approaches

**Results Format**: BenchmarkDotNet displays all approaches in a single table with:
- Execution time (Mean, Median, Min, Max)
- Memory allocation (Gen0, Gen1, Gen2, Allocated)
- **Relative performance** (compared to baseline - ANTLR)
- **Rank column** showing which approach is fastest

**Example Output:**
```
| Method                          | Mean      | Ratio | Rank | Gen0 | Allocated |
|-------------------------------- |----------:|------:|-----:|-----:|----------:|
| Antlr_SimpleTemplate            | 1.234 μs  | 1.00  | 2    | 0.01 | 1.2 KB    |
| BespokeCustom_SimpleTemplate     | 0.987 μs  | 0.80  | 1    | 0.01 | 1.0 KB    |
| FluidCore_SimpleTemplate         | 2.456 μs  | 1.99  | 4    | 0.02 | 2.5 KB    |
| StringFormat_SimpleTemplate       | 1.567 μs  | 1.27  | 3    | 0.01 | 1.5 KB    |
```

### 1. `TemplateResolutionBenchmarks.cs`
Benchmarks template resolution performance for all approaches:
- **Simple Template**: Single placeholder resolution (`A002IR_{LEM.EntityId}`)
- **Complex Template**: Multiple placeholders (`Entity {LEM.EntityId} for project {InfoRequest.ProjectId}`)
- **Multi-Placeholder Template**: Multiple placeholders including config (`my value {LEM.EntityId} and another {InfoRequest.ProjectId} and config {Config.StaticValue}`)

### 2. `DataSourceAdapterBenchmarks.cs`
Benchmarks data source adapter performance:
- ANTLR approach (`LemDataSourceAdapter`)
- Bespoke Custom approach (`BespokeDataSourceAdapter`)

### 3. `EndToEndBenchmarks.cs`
Benchmarks complete end-to-end workflows:
- Full pipeline: DataSource adapter → Template resolution
- Tests all 4 approaches in realistic scenarios

### 4. `ParameterExtractionBenchmarks.cs`
Benchmarks parameter extraction performance:
- Simple path extraction (`InfoRequest.ProjectId`)
- Nested path extraction (`InfoRequest.WorkareaId`)
- Array path extraction (`InfoRequest.EntityIds[0]`)
- Semantic GUID discovery (auto-discovery)

### 5. `JsonResolverBenchmarks.cs`
Benchmarks the **two JSON template resolver approaches only** (same JSON input for fair comparison):
- **StringBuilder** (`JsonStringBuilderResolvers.StringBuilderTemplateResolver`) – no regex, manual scan + StringBuilder
- **Regex** (`JsonRegexResolvers.RegexTemplateResolver`) – bespoke compiled regex for `{0}`, `{1}`, …
- **Small**: 3 templates (matches JsonExample)
- **Medium**: 20 templates with 2 placeholders each
- **ManyPlaceholders**: 1 template with 10 placeholders

Run only these: `dotnet run -c Release -- --benchmark --filter "*JsonResolverBenchmarks*"`

## Running Benchmarks

**⚠️ IMPORTANT: Benchmarks MUST be run in RELEASE configuration for accurate results!**

### Option 1: Run Comparison Benchmarks (Recommended - Side-by-Side Results)
```bash
dotnet run -c Release -- --benchmark --filter "*ComparisonBenchmarks*"
```

This will show all 4 approaches compared side-by-side in a single results table!

### Option 2: Run from main program (All benchmarks)
```bash
dotnet run -c Release -- --benchmark
```

### Option 3: Run specific benchmark class
```bash
dotnet run -c Release -- --benchmark --filter "*TemplateResolutionBenchmarks*"
dotnet run -c Release -- --benchmark --filter "*DataSourceAdapterBenchmarks*"
dotnet run -c Release -- --benchmark --filter "*EndToEndBenchmarks*"
dotnet run -c Release -- --benchmark --filter "*ParameterExtractionBenchmarks*"
dotnet run -c Release -- --benchmark --filter "*JsonResolverBenchmarks*"
```

### Option 4: Build in Release first, then run
```bash
dotnet build -c Release
dotnet run -c Release -- --benchmark
```

**Note**: The `-c Release` flag ensures the code is optimized, which BenchmarkDotNet requires for accurate performance measurements.

## Benchmark Results

Results are displayed in:
- **Console output** - Summary statistics
- **Markdown files** - Detailed reports (generated in `BenchmarkDotNet.Artifacts/`)

## What Gets Measured

- **Execution Time**: Mean, median, min, max execution times
- **Memory Allocation**: Gen0, Gen1, Gen2 collections, allocated bytes
- **Throughput**: Operations per second
- **Relative Performance**: Comparison against baseline (ANTLR)

## Notes

- All `Task.Delay()` calls have been removed from adapters for accurate benchmarking
- Benchmarks use realistic data structures matching production scenarios
- Each benchmark runs multiple iterations for statistical accuracy
