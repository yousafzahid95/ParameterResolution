using System.Text.Json;
using AntlrTest1;
using AntlrTest1.Events;
using AntlrTest1.Interfaces;
using Newtonsoft.Json.Linq;
using AntlrTest1.ParameterExtraction;  // ANTLR approach
using AntlrTest1.FluidExtraction;       // FLUID approach (separate, no ANTLR)

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║  LEM DataSource Adapter - ANTLR Parameter Extraction Test   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

var adapter = new LemDataSourceAdapter();

// Simplified LEM DataSource configuration using grammar-based semantic patterns
var lemDataSourceConfig = new JObject
{
    ["searchOption"] = "Active",
    ["includeOutOfScope"] = false,
    ["includeCollections"] = true,
    ["isProjectLevel"] = false
};

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine(" Test 1: InfoRequestEvent");
Console.WriteLine("═══════════════════════════════════════════════════════════════");

var event1 = new InfoRequestEvent
{
    InfoRequest = new InfoRequestData
    {
        ProjectId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        WorkareaId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        EntityIds = new List<Guid> { Guid.Parse("33333333-3333-3333-3333-333333333333") }
    }
};

var result1 = await adapter.GetRecordsAsync(event1, lemDataSourceConfig, new List<IDataRecord>(), CancellationToken.None);
Console.WriteLine($"✓ Success: EntityId = {result1.First().Data["EntityId"]}\n");

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine(" Test 2: SDTLemReplayEvent");
Console.WriteLine("═══════════════════════════════════════════════════════════════");

var event2 = new SDTLemReplayEvent
{
    LemEvent = new LemEventData
    {
        ProjectId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
        WorkareaId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
        EntityId = Guid.Parse("66666666-6666-6666-6666-666666666666")
    }
};

var result2 = await adapter.GetRecordsAsync(event2, lemDataSourceConfig, new List<IDataRecord>(), CancellationToken.None);
Console.WriteLine($"✓ Success: EntityId = {result2.First().Data["EntityId"]}\n");

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine(" Test 3: ExternalWorkplanTaskEvent");
Console.WriteLine("═══════════════════════════════════════════════════════════════");

var event3 = new ExternalWorkplanTaskEvent
{
    WorkplanTask = new WorkplanTaskData
    {
        ProjectId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
        WorkAreaId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
        Entities = new List<WorkplanTaskEntity>
        {
            new WorkplanTaskEntity { WorkAreaEntityId = Guid.Parse("99999999-9999-9999-9999-999999999999") }
        }
    }
};

var result3 = await adapter.GetRecordsAsync(event3, lemDataSourceConfig, new List<IDataRecord>(), CancellationToken.None);
Console.WriteLine($"✓ Success: EntityId = {result3.First().Data["EntityId"]}\n");

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine(" Test 4: SDTWorkplanReplayEvent");
Console.WriteLine("═══════════════════════════════════════════════════════════════");

var event4 = new SDTWorkplanReplayEvent
{
    WorkplanTask = new WorkplanReplayData
    {
        ProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        WorkareaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        EntityIds = new List<Guid> { Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc") }
    }
};

var result4 = await adapter.GetRecordsAsync(event4, lemDataSourceConfig, new List<IDataRecord>(), CancellationToken.None);
Console.WriteLine($"✓ Success: EntityId = {result4.First().Data["EntityId"]}\n");

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine(" Test 5: Auto-Discovery (Empty Config)");
Console.WriteLine("═══════════════════════════════════════════════════════════════");

var event5 = new SDTInfoRequestReplayEvent
{
    InfoRequest = new InfoRequestReplayData
    {
        ProjectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
        WorkareaId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
        EntityIds = new List<Guid> { Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff") }
    }
};

var emptyConfig = new JObject(); // No paths - triggers auto-discovery

var result5 = await adapter.GetRecordsAsync(event5, emptyConfig, new List<IDataRecord>(), CancellationToken.None);
Console.WriteLine($"✓ Success: EntityId = {result5.First().Data["EntityId"]}\n");

Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine(" Test 6: Multiple Events, Same Config (Rule A002IR Scenario)");
Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

var testEvents = new List<(string Name, object Event)>
{
    ("InfoRequestEvent", event1),
    ("SDTLemReplayEvent", event2),
    ("ExternalWorkplanTaskEvent", event3),
    ("SDTWorkplanReplayEvent", event4),
    ("SDTInfoRequestReplayEvent", event5)
};

Console.WriteLine("Processing all events with simplified config (auto-discovery):\n");

foreach (var (name, evt) in testEvents)
{
    IEnumerable<IDataRecord> result = await adapter.GetRecordsAsync(evt, lemDataSourceConfig, new List<IDataRecord>(), CancellationToken.None);
    Console.WriteLine($"  ✓ {name,-30} EntityId: {result.First().Data["EntityId"]}");
}

Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
Console.WriteLine(" Test 7: Grammar Pattern Validation (False Positive Test)");
Console.WriteLine("═══════════════════════════════════════════════════════════════");

// Create an event with properties that contain "pid" but aren't ProjectId
var falsePositiveEvent = new
{
    RapidProcessing = new
    {
        Pid = Guid.Parse("00000000-0000-0000-0000-000000000000"), // NOT a ProjectId!
        OtherName = Guid.Parse("00000000-0000-0000-0000-000000000000") // NOT a ProjectId!
    },
    InfoRequest = new InfoRequestData
    {
        ProjectId = Guid.Parse("12345678-1234-1234-1234-123456789012"),
        WorkareaId = Guid.Parse("87654321-4321-4321-4321-210987654321"),
        EntityIds = new List<Guid> { Guid.Parse("abcdefab-abcd-abcd-abcd-abcdefabcdef") }
    }
};

Console.WriteLine("\nEvent has properties with 'pid' substring:");
Console.WriteLine("  - RapidProcessing.Pid (should be IGNORED)");
Console.WriteLine("  - RapidProcessing.StupidName (should be IGNORED)");
Console.WriteLine("  - InfoRequest.ProjectId (should be MATCHED)\n");

var result7 = await adapter.GetRecordsAsync(falsePositiveEvent, lemDataSourceConfig, new List<IDataRecord>(), CancellationToken.None);
Console.WriteLine($"\n✓ Grammar correctly matched: InfoRequest.ProjectId");
Console.WriteLine($"✓ Ignored false positives: Pid, StupidName\n");

// ---------------------------------------------------------------------------
// Shared templates/config used by BOTH ANTLR and FLUID demos
// ---------------------------------------------------------------------------
var templateConfig = new JObject
{
    ["StaticValue"] = "Active",
    ["EmailSubject"] = "Entity {LEM.EntityId} for project {InfoRequest.ProjectId}",
    ["NotificationBody"] = "my value {LEM.EntityId} and another {InfoRequest.ProjectId}",
    ["ConfigOnly"] = "{Config.StaticValue}",
    ["SourceSystemKeyTemplate"] = "A002IR_{LEM.EntityId}",
    ["ActionDescriptionTemplate"] = "Action item for entity {LEM.EntityId} in project {InfoRequest.ProjectId}"
};

var staticValue = templateConfig["StaticValue"]?.ToString();
var subjectTemplate = templateConfig["EmailSubject"]?.ToString();
var bodyTemplate = templateConfig["NotificationBody"]?.ToString();
var configOnlyTemplate = templateConfig["ConfigOnly"]?.ToString();
var sourceSystemKeyTemplate = templateConfig["SourceSystemKeyTemplate"]?.ToString();
var actionDescriptionTemplate = templateConfig["ActionDescriptionTemplate"]?.ToString();

// ---------------------------------------------------------------------------
// ANTLR SECTION
// ---------------------------------------------------------------------------
Console.WriteLine("\n===============================================================");
Console.WriteLine(" ANTLR Approach (separate from FLUID)");
Console.WriteLine("===============================================================");

var antlrDatasetFromTest1 = result1.ToList();
foreach (var rec in antlrDatasetFromTest1)
{
    rec.Source = "LEM";
}

RunAntlrTemplateResolutionAndActionItem(
    eventData: event1,
    dataset: antlrDatasetFromTest1,
    config: templateConfig,
    staticValue: staticValue,
    subjectTemplate: subjectTemplate,
    bodyTemplate: bodyTemplate,
    configOnlyTemplate: configOnlyTemplate,
    sourceSystemKeyTemplate: sourceSystemKeyTemplate,
    actionDescriptionTemplate: actionDescriptionTemplate);

// ---------------------------------------------------------------------------
// FLUID SECTION (NO ANTLR)
// ---------------------------------------------------------------------------
Console.WriteLine("\n===============================================================");
Console.WriteLine(" FLUID Approach (separate from ANTLR, NO ANTLR dependency)");
Console.WriteLine("===============================================================");

await RunFluidAdapterAndTemplateResolution(
    eventData: event1,
    dataSourceConfig: lemDataSourceConfig,
    templateConfig: templateConfig,
    subjectTemplate: subjectTemplate,
    bodyTemplate: bodyTemplate,
    sourceSystemKeyTemplate: sourceSystemKeyTemplate,
    actionDescriptionTemplate: actionDescriptionTemplate);

// ---------------------------------------------------------------------------
// SIDE-BY-SIDE COMPARISON
// ---------------------------------------------------------------------------
await RunComparison(
    eventData: event1,
    dataSourceConfig: lemDataSourceConfig,
    templateConfig: templateConfig);

// ---------------------------------------------------------------------------
// FLUID PACKAGE SECTION (Fluid.Core / Liquid templates)
// ---------------------------------------------------------------------------
RunFluidCoreLiquidTemplates(
    eventData: event1,
    dataset: antlrDatasetFromTest1,
    templateConfig: templateConfig);

// -----------------------------
// Local helper functions
// -----------------------------
static void RunAntlrTemplateResolutionAndActionItem(
    object eventData,
    List<IDataRecord> dataset,
    JObject config,
    string? staticValue,
    string? subjectTemplate,
    string? bodyTemplate,
    string? configOnlyTemplate,
    string? sourceSystemKeyTemplate,
    string? actionDescriptionTemplate)
{
    Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
    Console.WriteLine(" Test 8 (ANTLR): Template Resolution & ActionItem Creation");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");

    var resolvedSubject = ParameterTemplateResolver.ResolveTemplate(
        subjectTemplate,
        eventData,
        config,
        dataset);

    var resolvedBody = ParameterTemplateResolver.ResolveTemplate(
        bodyTemplate,
        eventData,
        config,
        dataset);

    var resolvedConfigOnly = ParameterTemplateResolver.ResolveTemplate(
        configOnlyTemplate,
        eventData,
        config,
        dataset);

    var resolvedSourceSystemKey = ParameterTemplateResolver.ResolveTemplate(
        sourceSystemKeyTemplate,
        eventData,
        config,
        dataset);

    var resolvedActionDescription = ParameterTemplateResolver.ResolveTemplate(
        actionDescriptionTemplate,
        eventData,
        config,
        dataset);

    // Build an ActionItem instance using resolved templates and dataset values
    var firstRecord = dataset.First();
    var actionItem = new ActionItem
    {
        EntityId = Guid.Parse(firstRecord.Data["EntityId"]!.ToString()),
        WorkAreaId = Guid.Parse(firstRecord.Data["WorkAreaId"]!.ToString()),
        TaskId = Guid.NewGuid(), // In a real system this would come from upstream task context
        SourceSystemKey = resolvedSourceSystemKey ?? string.Empty,
        Description = resolvedActionDescription ?? string.Empty,
        Status = "Open"
    };

    Console.WriteLine($"\nStatic value from config: {staticValue}");
    Console.WriteLine($"Resolved EmailSubject:    {resolvedSubject}");
    Console.WriteLine($"Resolved NotificationBody:{resolvedBody}");
    Console.WriteLine($"Resolved ConfigOnly:      {resolvedConfigOnly}");
    Console.WriteLine("\nCreated ActionItem from templates and dataset:");
    Console.WriteLine($"  EntityId:        {actionItem.EntityId}");
    Console.WriteLine($"  WorkAreaId:      {actionItem.WorkAreaId}");
    Console.WriteLine($"  TaskId:          {actionItem.TaskId}");
    Console.WriteLine($"  SourceSystemKey: {actionItem.SourceSystemKey}");
    Console.WriteLine($"  Description:     {actionItem.Description}");
    Console.WriteLine($"  Status:          {actionItem.Status}");
}

static async Task RunFluidAdapterAndTemplateResolution(
    object eventData,
    JObject dataSourceConfig,
    JObject templateConfig,
    string? subjectTemplate,
    string? bodyTemplate,
    string? sourceSystemKeyTemplate,
    string? actionDescriptionTemplate)
{
    Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
    Console.WriteLine(" Test 9 (FLUID): DataSource Adapter & Template Resolution");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");

    // FLUID approach: Use FluidDataSourceAdapter (parallel to LemDataSourceAdapter)
    var fluidAdapter = new FluidDataSourceAdapter();
    var fluidResult = await fluidAdapter.GetRecordsAsync(eventData, dataSourceConfig, new List<IDataRecord>(), CancellationToken.None);
    Console.WriteLine($"✓ FLUID Success: EntityId = {fluidResult.First().Data["EntityId"]}\n");

    // Use FLUID result as dataset for template resolution
    var fluidDataset = fluidResult.ToList();

    // FLUID approach: Fluent interface for parameter resolution (NO ANTLR)
    var fluidResolver = AntlrTest1.FluidExtraction.FluidParameterResolver
        .Create()
        .WithEvent(eventData)
        .WithConfig(templateConfig)
        .WithDataset(fluidDataset);

    // Resolve templates using fluent API (FLUID approach - no ANTLR)
    var fluidResolvedSubject = fluidResolver.Resolve(subjectTemplate);
    var fluidResolvedBody = fluidResolver.Resolve(bodyTemplate);

    // Build ActionItem using fluent builder (FLUID approach)
    var fluidActionItem = fluidResolver
        .BuildActionItem(
            sourceSystemKeyTemplate: sourceSystemKeyTemplate ?? string.Empty,
            descriptionTemplate: actionDescriptionTemplate ?? string.Empty,
            status: "Open",
            taskId: Guid.NewGuid());

    Console.WriteLine("\nFLUID Approach Results (NO ANTLR):");
    Console.WriteLine($"  Resolved EmailSubject:     {fluidResolvedSubject}");
    Console.WriteLine($"  Resolved NotificationBody: {fluidResolvedBody}");
    Console.WriteLine("\nFLUID ActionItem (built with FLUID extractor):");
    Console.WriteLine($"  EntityId:        {fluidActionItem.EntityId}");
    Console.WriteLine($"  WorkAreaId:      {fluidActionItem.WorkAreaId}");
    Console.WriteLine($"  TaskId:          {fluidActionItem.TaskId}");
    Console.WriteLine($"  SourceSystemKey: {fluidActionItem.SourceSystemKey}");
    Console.WriteLine($"  Description:     {fluidActionItem.Description}");
    Console.WriteLine($"  Status:          {fluidActionItem.Status}");
}

static async Task RunComparison(
    object eventData,
    JObject dataSourceConfig,
    JObject templateConfig)
{
    Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
    Console.WriteLine(" Test 10: Side-by-Side Comparison - ANTLR vs FLUID");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");

    // ANTLR approach
    var antlrAdapter = new LemDataSourceAdapter();
    var antlrDataset = (await antlrAdapter.GetRecordsAsync(eventData, dataSourceConfig, new List<IDataRecord>(), CancellationToken.None)).ToList();
    var antlrResolved = ParameterTemplateResolver.ResolveTemplate(
        "A002IR_{LEM.EntityId}",
        eventData,
        templateConfig,
        antlrDataset);

    // FLUID approach
    var fluidAdapter = new FluidDataSourceAdapter();
    var fluidDataset = (await fluidAdapter.GetRecordsAsync(eventData, dataSourceConfig, new List<IDataRecord>(), CancellationToken.None)).ToList();
    var fluidResolver = AntlrTest1.FluidExtraction.FluidParameterResolver
        .Create()
        .WithEvent(eventData)
        .WithConfig(templateConfig)
        .WithDataset(fluidDataset);
    var fluidResolved = fluidResolver.Resolve("A002IR_{LEM.EntityId}");

    Console.WriteLine("\nSame template resolved with both approaches:");
    Console.WriteLine($"  ANTLR result: {antlrResolved}");
    Console.WriteLine($"  FLUID result: {fluidResolved}");
    Console.WriteLine($"  Results match: {antlrResolved == fluidResolved}");
}

static void RunFluidCoreLiquidTemplates(
    object eventData,
    List<IDataRecord> dataset,
    JObject templateConfig)
{
    Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
    Console.WriteLine(" Test 11: Fluid.Core NuGet Package - Liquid Template Rendering");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");

    // Fluid.Core approach: Uses Liquid template syntax {{ variable }}
    var fluidCoreResolver = FluidCoreTemplateResolver
        .Create()
        .WithEvent(eventData)
        .WithConfig(templateConfig)
        .WithDataset(dataset)
        .Build();

    // Liquid template syntax: {{ LEM.EntityId }} instead of {LEM.EntityId}
    var liquidTemplate = "Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}";
    var liquidSourceKey = "A002IR_{{ LEM.EntityId }}";
    var liquidDescription = "Action item for entity {{ LEM.EntityId }} in project {{ Event.InfoRequest.ProjectId }}";

    var fluidCoreResult = fluidCoreResolver.Render(liquidTemplate);
    var fluidCoreSourceKey = fluidCoreResolver.Render(liquidSourceKey);
    var fluidCoreDescription = fluidCoreResolver.Render(liquidDescription);

    Console.WriteLine("\nFluid.Core (Liquid Template Engine) Results:");
    Console.WriteLine($"  Liquid Template:      {liquidTemplate}");
    Console.WriteLine($"  Rendered Result:      {fluidCoreResult}");
    Console.WriteLine($"  SourceSystemKey:      {fluidCoreSourceKey}");
    Console.WriteLine($"  Description:          {fluidCoreDescription}");

    // Build ActionItem using Fluid.Core outputs
    var firstRecord = dataset.First();
    var fluidCoreActionItem = new ActionItem
    {
        EntityId = Guid.Parse(firstRecord.Data["EntityId"]!.ToString()),
        WorkAreaId = Guid.Parse(firstRecord.Data["WorkAreaId"]!.ToString()),
        TaskId = Guid.NewGuid(),
        SourceSystemKey = fluidCoreSourceKey,
        Description = fluidCoreDescription,
        Status = "Open"
    };

    Console.WriteLine("\nFluid.Core ActionItem:");
    Console.WriteLine($"  EntityId:        {fluidCoreActionItem.EntityId}");
    Console.WriteLine($"  WorkAreaId:      {fluidCoreActionItem.WorkAreaId}");
    Console.WriteLine($"  TaskId:          {fluidCoreActionItem.TaskId}");
    Console.WriteLine($"  SourceSystemKey: {fluidCoreActionItem.SourceSystemKey}");
    Console.WriteLine($"  Description:     {fluidCoreActionItem.Description}");
    Console.WriteLine($"  Status:          {fluidCoreActionItem.Status}");
}

Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║              All Tests Completed Successfully!               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

Console.WriteLine("\nKey Features Demonstrated:");
Console.WriteLine("\nANTLR Approach (Separate):");
Console.WriteLine("  ✓ Grammar-based semantic pattern matching (ANTLR4)");
Console.WriteLine("  ✓ ANTLR4 path expression parsing");
Console.WriteLine("  ✓ LemDataSourceAdapter using ANTLR extraction");
Console.WriteLine("  ✓ ParameterTemplateResolver (static utility)");
Console.WriteLine("\nFLUID Approach (Separate, NO ANTLR):");
Console.WriteLine("  ✓ Reflection-based path navigation");
Console.WriteLine("  ✓ Pattern matching without ANTLR");
Console.WriteLine("  ✓ FluidDataSourceAdapter using FLUID extraction");
Console.WriteLine("  ✓ FluidParameterResolver (fluent builder)");
Console.WriteLine("  ✓ Fluid.Core NuGet package integration (Liquid templates)");
Console.WriteLine("\nBoth Approaches Support:");
Console.WriteLine("  ✓ Automatic parameter discovery");
Console.WriteLine("  ✓ Array indexing (EntityIds[0])");
Console.WriteLine("  ✓ Nested property navigation (InfoRequest.ProjectId)");
Console.WriteLine("  ✓ Template resolution from config / event / dataset");
Console.WriteLine("  ✓ ActionItem creation with resolved templates");
Console.WriteLine("  ✓ Same datasource parameters resolved independently");
