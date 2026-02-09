# ANTLR-Based Parameter Extraction (Grammar-Driven)

## Overview

This implementation uses **ANTLR4 Runtime (v4.13.1)** and a **grammar** (`Grammar/PathExpression.g4`) to parse path expressions and extract values from:

- **CLR event objects** (via reflection)
- **datasource datasets** (`IDataRecord.Data` as `JObject`/`JToken`)

The ANTLR approach is one of two parallel implementations in this repo (the other is the FLUID approach in `FLUID_PARAMETER_RESOLUTION.md`).

## End-to-end flow (ANTLR path)

1. **Event arrives** (e.g. `InfoRequestEvent`, `SDTLemReplayEvent`, etc.)
2. **`LemDataSourceAdapter`** extracts required identifiers:
   - uses `AntlrParameterExtractor.DiscoverSemanticGuids(eventData)`
   - validates required IDs
3. Adapter returns a dataset: `List<IDataRecord>` (POC dummy data)
4. **Templates** are resolved using `ParameterTemplateResolver.ResolveTemplate(...)`:
   - `{InfoRequest.ProjectId}` resolves from eventData
   - `{LEM.EntityId}` resolves from dataset record where `Source == "LEM"`
   - `{Config.Some.Path}` resolves from rule config `JObject`
5. An `ActionItem` can be created using resolved strings.

## Key files and responsibilities

### 1. Grammar: `Grammar/PathExpression.g4`

Defines path syntax and semantic patterns (ProjectId/WorkAreaId/EntityId) used for auto-discovery.

**Token Types:**
- `IDENTIFIER` - Property names (e.g., `ProjectId`, `InfoRequest`)
- `INTEGER` - Array indices (e.g., `0` in `[0]`)
- `DOT` - Path separator (`.`)
- `LBRACKET` - Opening bracket (`[`)
- `RBRACKET` - Closing bracket (`]`)

**ANTLR4 Features Used:**
- `Antlr4.Runtime.Lexer` base class
- `ICharStream` for input
- `CommonTokenStream` for token buffering
- `IVocabulary` for token naming
- `CommonToken` for token creation

### 2. Extractor: `ParameterExtraction/AntlrParameterExtractor.cs`

Primary responsibilities:

- **Parse path expressions** using generated ANTLR lexer/parser from `PathExpression.g4`
- **Navigate values** across:
  - CLR objects (reflection)
  - `JObject` / `JToken` (dataset JSON)
- **Auto-discover GUIDs** with grammar-based semantic parsing:
  - `projectIdPattern()`, `workAreaIdPattern()`, `entityIdPattern()`

**Supported Path Syntax:**
- Simple property: `ProjectId`
- Nested property: `InfoRequest.ProjectId`
- Array indexing: `EntityIds[0]`
- Complex nesting: `WorkplanTask.Entities[0].WorkAreaEntityId`

### 3. **LemDataSourceAdapter** (`LemDataSourceAdapter.cs`)

Completely generic adapter with **zero event dependencies**:

```csharp
public async Task<IEnumerable<IDataRecord>> GetRecordsAsync(
    dynamic eventData,              // ANY event type
    dynamic dataSourceParams,       // Configuration from rule
    IEnumerable<IDataRecord> dataset,
    CancellationToken cancellationToken)
{
    // Extract parameters using ANTLR4 extractor
    var extractedParams = ExtractParameters(eventData, paramsObj);
    
    // Validate and call API
    // ...
}
```

## Template resolution (ANTLR)

Template resolution is implemented in `ParameterExtraction/ParameterTemplateResolver.cs`.

Supported placeholders:

- Event paths: `{InfoRequest.ProjectId}`
- Dataset paths: `{LEM.EntityId}` (where `LEM` matches `IDataRecord.Source`)
- Config paths: `{Config.Some.Path}`

Example:

```csharp
var resolved = ParameterTemplateResolver.ResolveTemplate(
    "A002IR_{LEM.EntityId}",
    eventData,
    ruleConfig,
    dataset);
```

## DataSource Configuration (examples)

### Example: LEM DataSource for Rule A002IR

```javascript
const A002IR = {
    DataSources: [
        {
            "Key": "LEM",
            "Params": {
                // OR condition: tries each path until finding a valid GUID
                "projectIdPath": [
                    "InfoRequest.ProjectId",
                    "LemEvent.ProjectId",
                    "WorkplanTask.ProjectId",
                    "ProjectId"
                ],
                "workAreaIdPath": [
                    "InfoRequest.WorkareaId",
                    "LemEvent.WorkareaId",
                    "WorkplanTask.WorkAreaId",
                    "WorkplanTask.WorkareaId"
                ],
                "entityIdPath": [
                    "InfoRequest.EntityIds[0]",
                    "LemEvent.EntityId",
                    "WorkplanTask.Entities[0].WorkAreaEntityId",
                    "WorkplanTask.EntityIds[0]"
                ],
                "searchOption": "Active",
                "includeCollections": true
            }
        }
    ],
    Events: [
        "InfoRequestEvent",
        "SDTLemReplayEvent",
        "ExternalWorkplanTaskEvent",
        "SDTWorkplanReplayEvent"
    ]
};
```

## Features

### ? True ANTLR4 Integration
- Uses `Antlr4.Runtime.Lexer` base class
- Implements ANTLR4 lexer interface (`IToken`, `IVocabulary`, `ICharStream`)
- Processes token streams using `CommonTokenStream`
- Hand-written lexer for optimal performance (no parser generation needed)

### ? Lexical Analysis
The ANTLR4 lexer tokenizes path expressions:
```
Input:  "InfoRequest.EntityIds[0]"
Tokens: IDENTIFIER("InfoRequest"), DOT, IDENTIFIER("EntityIds"), LBRACKET, INTEGER("0"), RBRACKET
```

### ? Event-Agnostic Design
- **Zero** hard-coded event types in adapter
- **Zero** event-specific handlers
- Single generic extraction logic

### ? OR Condition Support
Path arrays provide fallback logic:
```json
{
  "projectIdPath": [
    "InfoRequest.ProjectId",    // Try this first
    "LemEvent.ProjectId",       // Then try this
    "WorkplanTask.ProjectId",   // Then try this
    "ProjectId"                 // Finally try this
  ]
}
```

### ? Complex Path Navigation
- Nested properties: `InfoRequest.ProjectId`
- Array indexing: `EntityIds[0]`
- Chained navigation: `WorkplanTask.Entities[0].WorkAreaEntityId`

### ? Auto-Discovery Fallback
When no paths are configured, the adapter automatically:
1. Recursively scans the event object
2. Finds all GUID properties
3. Intelligently matches them based on naming patterns

### ? Fallback Parsing
If ANTLR4 tokenization fails, falls back to simple string parsing

## Console Output Example

```
[Parameter Extraction] Starting ANTLR-based extraction...
  [ANTLR4] Matched path: InfoRequest.ProjectId = 11111111-1111-1111-1111-111111111111
  [ANTLR4] Matched path: InfoRequest.WorkareaId = 22222222-2222-2222-2222-222222222222
  [ANTLR4] Matched path: InfoRequest.EntityIds[0] = 33333333-3333-3333-3333-333333333333

[Extraction Result] 
  ProjectId=11111111-1111-1111-1111-111111111111
  WorkAreaId=22222222-2222-2222-2222-222222222222
  EntityId=33333333-3333-3333-3333-333333333333

[Validation] ? All required identifiers present

[API Call] GetWorkAreaEntityAsync
  ProjectId: 11111111-1111-1111-1111-111111111111
  WorkAreaId: 22222222-2222-2222-2222-222222222222
  EntityId: 33333333-3333-3333-3333-333333333333
```

Notice the **[ANTLR4]** prefix indicating actual ANTLR4 tokenization!

## Notes

- The project uses `Antlr4BuildTasks` so the grammar is compiled during build.
- Generated ANTLR classes are produced under `obj/...` during build.

## Dependencies

```xml
<PackageReference Include="Antlr4.Runtime.Standard" Version="4.13.1" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
```

## Summary

This implementation provides a **true ANTLR4-based** parameter extractor that:
- ? Uses **actual ANTLR4 Runtime** classes (`Lexer`, `ICharStream`, `CommonTokenStream`)
- ? Implements **custom hand-written lexer** for path expressions
- ? Provides **lexical tokenization** (not regex-based)
- ? Supports **multiple path alternatives (OR logic)**
- ? Includes **auto-discovery fallback**
- ? Handles **complex nested structures**
- ? Is **highly maintainable and testable**

The same DataSource configuration works for **all events** in a rule, eliminating the need for event-specific handlers and reducing code complexity by ~70%.

---

## Parallel implementation: FLUID approach (separate, no ANTLR)

This solution also provides a **FLUID (Fluent Interface)** approach as a **completely separate, independent implementation**. The FLUID approach does NOT use ANTLR - it uses reflection-based path navigation and pattern matching to achieve the same results.

### FLUID Approach Overview

The `FluidParameterResolver` class provides a fluent builder pattern for resolving templates:

```csharp
var resolver = FluidParameterResolver
    .Create()
    .WithEvent(eventData)
    .WithConfig(ruleConfig)
    .WithDataset(dataset);

var resolved = resolver.Resolve("Entity {LEM.EntityId} for project {InfoRequest.ProjectId}");
```

### Key Benefits of FLUID Approach

1. **No ANTLR Dependency** - Completely independent implementation
2. **Declarative Syntax** - More readable and self-documenting code
3. **Method Chaining** - Fluent API allows building resolvers step-by-step
4. **Built-in Helpers** - `BuildActionItem()` method for common use cases
5. **Reflection-Based** - Uses reflection and JObject navigation (no grammar parsing)
6. **Reusable Resolvers** - Build once, resolve multiple templates

### FLUID vs ANTLR Approach

| Feature | ANTLR Approach | FLUID Approach |
|---------|---------------|----------------|
| **Syntax** | Static method calls | Fluent method chaining |
| **Readability** | Good | Excellent (more declarative) |
| **Reusability** | Pass all params each time | Build resolver once, reuse |
| **ActionItem Support** | Manual construction | Built-in `BuildActionItem()` |
| **Path Extraction** | ANTLR4 grammar parsing | Reflection + string parsing |
| **Pattern Matching** | Grammar rules | String pattern matching |
| **Dependencies** | Antlr4.Runtime.Standard | None (reflection only) |
| **Independence** | Uses ANTLR | Completely separate (NO ANTLR) |

### Example: Creating ActionItem with FLUID

```csharp
var resolver = FluidParameterResolver
    .Create()
    .WithEvent(eventData)
    .WithConfig(ruleConfig)
    .WithDataset(dataset);

var actionItem = resolver.BuildActionItem(
    sourceSystemKeyTemplate: "A002IR_{LEM.EntityId}",
    descriptionTemplate: "Action item for entity {LEM.EntityId} in project {InfoRequest.ProjectId}",
    status: "Open",
    taskId: Guid.NewGuid());
```

### When to Use Which Approach?

- **Use ANTLR Static (`ParameterTemplateResolver`)** when:
  - You need a simple, one-off template resolution
  - You prefer functional/static utility style
  - You're resolving a single template string

- **Use FLUID (`FluidParameterResolver`)** when:
  - You need to resolve multiple templates with the same context
  - You prefer declarative, builder-style APIs
  - You're building complex objects like `ActionItem`
  - You want more readable, self-documenting code

Both approaches support the same `{...}` placeholder syntax and achieve the same results, but use different implementations:

- **ANTLR**: grammar parsing + semantic pattern rules
- **FLUID**: reflection + string parsing + pattern matching (no grammar)
