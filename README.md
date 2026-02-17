# Dynamic Parameter Resolution POC (C# / .NET 8)

This project is a **POC** for resolving **datasource parameters** and **ActionItem attributes** dynamically from:

- **event payloads** (C# objects)
- **datasource datasets** (`IEnumerable<IDataRecord>` where each record has a `Source` and `JObject Data`)
- **rule config** (a `JObject` loaded from JSON)
- **mixed templates** (static text + dynamic placeholders)

It contains **four separate, parallel implementations** that achieve the same functional result:

1. **ANTLR approach** (grammar-based parsing)
2. **Bespoke (Custom) approach** (reflection + string parsing; *no ANTLR dependency*)
3. **FLUID (NuGet Core) approach** (Fluid.Core package for Liquid templates)
4. **StringFormat approach** (StringBuilder + String.Format with indexed placeholders)

---

## What the POC demonstrates (end-to-end)

### Goal

Given:
- **Event**: e.g. `InfoRequestEvent` containing `ProjectId`, `WorkareaId`, `EntityIds[0]`
- **Datasource config**: e.g. `{ "Key": "LEM", "Params": { ... } }`

We:
1. **Resolve datasource parameters** from the event (ProjectId/WorkAreaId/EntityId)
2. Call a datasource adapter which returns a dataset: `List<IDataRecord>` (e.g. `Source="LEM"` + `Data={ EntityId, WorkAreaId, Attributes... }`)
3. **Resolve ActionItem fields** from templates such as:
   - `SourceSystemKey = "A002IR_{LEM.EntityId}"`
   - `Description = "Action item for entity {LEM.EntityId} in project {InfoRequest.ProjectId}"`

## Project Structure

### Event Classes (Events folder)
All event classes that the adapter can handle:

1. **ExternalGenericEntityEventMessage.cs** - Base message wrapper with Label and Body
2. **WorkAreaEntityExternalEvent.cs** - WorkArea entity events
3. **ProjectEntityExternalEvent.cs** - Project-level entity events
4. **InfoRequestEvent.cs** - Information request events
5. **ExternalWorkplanTaskEvent.cs** - Workplan task events
6. **SDTLemReplayEvent.cs** - LEM replay events
7. **SDTWorkplanReplayEvent.cs** - Workplan replay events
8. **SDTInfoRequestReplayEvent.cs** - Info request replay events
9. **ExternalMemberRelationEventMessage.cs** - Member relation events
10. **ActionItemManualClosedEvent.cs** - Action item closed events

### Interfaces (Interfaces folder)
- **IDataRecord.cs** - Interface for data records with JObject data and source string
- **DataRecord.cs** - Concrete implementation of IDataRecord

### ANTLR approach (grammar-based)
- **Grammar/PathExpression.g4**
  - ANTLR grammar for path expressions like `InfoRequest.ProjectId`, `EntityIds[0]`, `WorkplanTask.Entities[0].WorkAreaEntityId`
- **ParameterExtraction/AntlrParameterExtractor.cs**
  - Parses path expressions using generated ANTLR lexer/parser
  - Navigates CLR objects *and* `JObject/JToken` trees
  - Provides auto-discovery for `Guid` values (semantic pattern matching using grammar rules)
- **ParameterExtraction/ParameterTemplateResolver.cs**
  - Resolves templates with `{...}` placeholders using `AntlrParameterExtractor`
- **LemDataSourceAdapter.cs**
  - Uses ANTLR auto-discovery to resolve required IDs
  - Returns dummy `IDataRecord` dataset (POC)

### Bespoke (Custom) approach (no ANTLR)
- **BespokeCustomExtraction/BespokeParameterExtractor.cs**
  - Standalone extractor (reflection + string parsing)
  - Navigates CLR objects *and* `JObject/JToken` trees
  - Provides GUID auto-discovery using naming/pattern rules (no grammar)
- **BespokeCustomExtraction/BespokeParameterResolver.cs**
  - Fluent API for `{...}` template resolution and ActionItem creation
- **BespokeCustomExtraction/BespokeDataSourceAdapter.cs**
  - Parallel datasource adapter to `LemDataSourceAdapter` but uses Bespoke Custom extraction (no ANTLR)

### Fluid.Core (NuGet) integration (Liquid templates)
- **FluidCoreExtraction/FluidCoreTemplateResolver.cs**
  - Uses **Fluid.Core** package to render Liquid templates (e.g. `{{ LEM.EntityId }}`)
  - This is *not* the same syntax as `{LEM.EntityId}`

### StringFormat approach (StringBuilder + String.Format)
- **StringFormatExtraction/StringFormatTemplateResolver.cs**
  - Uses `StringBuilder` for efficient string building
  - Converts template placeholders to indexed format (`{0}`, `{1}`, etc.)
  - Uses `String.Format` to resolve templates with resolved values
  - Reuses resolved values for duplicate placeholders
  - Fluent API similar to Bespoke Custom approach

### Domain Models
- **ActionItem.cs** - Action item model with EntityId, WorkAreaId, TaskId, SourceSystemKey, Description, and Status

### Demo runner
- **Program.cs**
  - Runs a series of console demos
  - Contains clearly separated sections:
    - ANTLR section
    - Bespoke (Custom) section
    - Side-by-side comparison
    - Fluid.Core (Liquid) demo
    - StringFormat approach demo
    - Comprehensive comparison of all 4 approaches

## Features

### Placeholder syntax (`{...}` templates)

Both ANTLR and Bespoke (Custom) resolvers support the same placeholder syntax:

- **Event paths**: `{InfoRequest.ProjectId}`, `{WorkplanTask.Entities[0].WorkAreaEntityId}`
- **Dataset paths**: `{LEM.EntityId}`, `{LEM.Attributes.TaxId}`
  - the first segment (`LEM`) matches `IDataRecord.Source`
- **Config paths**: `{Config.Some.Path}` reads from the rule config `JObject`
- **Static values**: any string with no `{}` is returned as-is

### Liquid syntax (Fluid.Core)

When using `FluidCoreTemplateResolver`, use **Liquid** syntax:

- Dataset: `{{ LEM.EntityId }}`
- Event: `{{ Event.InfoRequest.ProjectId }}` (event is exposed as `Event`)
- Config: `{{ Config.StaticValue }}`

### Parameter resolution approaches

#### 1. ANTLR Approach (Separate Implementation)
- Uses **ANTLR4 grammar** (`PathExpression.g4`) for parsing path expressions
- Supports complex nested paths: `InfoRequest.ProjectId`, `WorkplanTask.Entities[0].WorkAreaEntityId`
- Grammar-based semantic pattern matching for auto-discovery
- Static utility class: `ParameterTemplateResolver.ResolveTemplate()`
- Data source adapter: `LemDataSourceAdapter`

**Example:**
```csharp
// ANTLR approach - uses AntlrParameterExtractor
var adapter = new LemDataSourceAdapter();
var dataset = await adapter.GetRecordsAsync(eventData, config, new List<IDataRecord>(), token);

var resolved = ParameterTemplateResolver.ResolveTemplate(
    "Entity {LEM.EntityId} for project {InfoRequest.ProjectId}",
    eventData,
    ruleConfig,
    dataset);
```

#### 2. Bespoke (Custom) Approach (Separate Implementation - NO ANTLR)
- **Completely independent** - does NOT use ANTLR
- Uses **reflection and pattern matching** for path navigation
- **Declarative, chainable API** for building resolvers
- Method chaining: `.WithEvent()`, `.WithConfig()`, `.WithDataset()`
- Built-in `BuildActionItem()` helper for creating action items
- Data source adapter: `BespokeDataSourceAdapter`

**Example (Bespoke Custom with {placeholder} syntax):**
```csharp
// Bespoke Custom approach - uses BespokeParameterExtractor (NO ANTLR)
var adapter = new BespokeDataSourceAdapter();
var dataset = await adapter.GetRecordsAsync(eventData, config, new List<IDataRecord>(), token);

var resolver = AntlrTest1.BespokeCustomExtraction.BespokeParameterResolver
    .Create()
    .WithEvent(eventData)
    .WithConfig(ruleConfig)
    .WithDataset(dataset);

var resolved = resolver.Resolve("Entity {LEM.EntityId} for project {InfoRequest.ProjectId}");
var actionItem = resolver.BuildActionItem(
    sourceSystemKeyTemplate: "A002IR_{LEM.EntityId}",
    descriptionTemplate: "Action item for entity {LEM.EntityId}",
    status: "Open");
```

**Example (Fluid.Core NuGet package with Liquid {{ }} syntax):**
```csharp
// Fluid.Core approach - uses Fluid.Core NuGet package (Liquid templates)
var fluidCoreResolver = FluidCoreTemplateResolver
    .Create()
    .WithEvent(eventData)
    .WithConfig(ruleConfig)
    .WithDataset(dataset)
    .Build();

// Liquid syntax: {{ variable.property }}
var result = fluidCoreResolver.Render("Entity {{ LEM.EntityId }} for project {{ Event.InfoRequest.ProjectId }}");
```

#### 3. StringFormat Approach (StringBuilder + String.Format)
- Uses **StringBuilder** for efficient string building
- Converts `{placeholder}` syntax to indexed `{0}`, `{1}`, etc. format
- Uses **String.Format** to resolve templates with resolved values
- Reuses resolved values for duplicate placeholders (optimization)
- Fluent API similar to FLUID approach
- Data source adapter: Uses `BespokeDataSourceAdapter` (same as Bespoke Custom)

**Example:**
```csharp
// StringFormat approach - uses StringBuilder and String.Format
var adapter = new FluidDataSourceAdapter();
var dataset = await adapter.GetRecordsAsync(eventData, config, new List<IDataRecord>(), token);

var resolver = StringFormatTemplateResolver
    .Create()
    .WithEvent(eventData)
    .WithConfig(ruleConfig)
    .WithDataset(dataset);

var resolved = resolver.Resolve("Entity {LEM.EntityId} for project {InfoRequest.ProjectId}");
var actionItem = resolver.BuildActionItem(
    sourceSystemKeyTemplate: "A002IR_{LEM.EntityId}",
    descriptionTemplate: "Action item for entity {LEM.EntityId}",
    status: "Open");
```

**How it works:**
1. Extracts all unique placeholders from the template
2. Resolves each placeholder to a value (using same extraction logic as FLUID)
3. Builds a format string with indexed placeholders (`{0}`, `{1}`, etc.)
4. Uses `String.Format(formatString, resolvedValues)` to produce final result
5. Reuses resolved values when the same placeholder appears multiple times

### Dummy Data Generation
The adapter returns mock JObject data with:
- Entity information (IDs, names, types)
- Status and dates
- Attributes (TaxId, Jurisdiction, etc.)
- Different data for WorkArea vs Project entities

### Event Handling
The adapter handles 9 different event types:
- WorkAreaEntity
- ProjectEntity
- InfoRequest
- ExternalWorkplanTask
- SDTLemReplay
- SDTWorkplanReplay
- SDTInfoRequestReplay
- MemberRelation
- ActionItemManualClosed

### Validation
- Validates required GUIDs are not empty
- Different validation logic for Project-level vs WorkArea-level entities
- Detailed error messages with event context

## Testing

Run the application with:
```bash
dotnet run
```

The `Program.cs` file contains multiple console demos, including:
1. InfoRequestEvent handling (ANTLR)
2. SDTLemReplayEvent handling (ANTLR)
3. ExternalWorkplanTaskEvent handling (ANTLR)
4. SDTWorkplanReplayEvent handling (ANTLR)
5. Auto-discovery with empty config (ANTLR)
6. Multiple events with same config (Rule A002IR scenario) (ANTLR)
7. Grammar pattern validation (false positive test) (ANTLR)
8. Template Resolution & ActionItem Creation (ANTLR approach)
9. Bespoke Custom Approach - DataSource Adapter & Template Resolution (Bespoke Custom, NO ANTLR)
10. Side-by-Side Comparison - ANTLR vs FLUID (both approaches with same data)
11. Fluid.Core NuGet Package - Liquid Template Rendering
12. StringFormat Approach - StringBuilder + String.Format
13. Comprehensive Comparison - All 4 Approaches

Each test creates an event with GUIDs and demonstrates parameter extraction and template resolution.

## Dependencies
- .NET 8
- Newtonsoft.Json (v13.0.4) - For JObject support
- Antlr4.Runtime.Standard (v4.13.1) - For ANTLR-based path expression parsing
- Antlr4BuildTasks (v12.8.0) - For grammar compilation during build
- **Fluid.Core (v2.24.0)** - For FLUID approach Liquid template rendering

## Which doc to read next

- If you want the ANTLR deep dive: `ANTLR_PARAMETER_EXTRACTION.md`
- If you want the FLUID deep dive: `FLUID_PARAMETER_RESOLUTION.md`

## Output
Each test displays:
- Console logs showing which adapter and event type is being processed
- Source identifier ("LEM")
- Complete JSON data with entity information and attributes
