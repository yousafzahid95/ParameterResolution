# FLUID Parameter Resolution Approach (No ANTLR)

## Overview

The **FLUID (Fluent Interface)** approach provides a **completely independent** parameter resolution implementation that **does NOT use ANTLR**.

It uses:
- simple string parsing for path expressions (split on `.` + optional `[index]`)
- reflection for CLR objects
- `JObject/JToken` navigation for dataset records
- naming/pattern heuristics to auto-discover GUIDs

## What FLUID is (and what it is not)

**FLUID is NOT a wrapper around ANTLR** - it is a **parallel, standalone implementation**:
- ✅ Uses reflection and JObject navigation (no ANTLR dependency)
- ✅ Uses pattern matching for semantic discovery (no grammar parsing)
- ✅ Provides fluent builder API for better readability
- ✅ Achieves the same results as ANTLR approach independently

### Note: Fluid.Core (NuGet) is optional and separate

This repo also includes an optional integration with the **Fluid.Core** NuGet package (Liquid templates) via `FluidExtraction/FluidCoreTemplateResolver.cs`.

- `Fluid.Core` uses **Liquid syntax** like `{{ LEM.EntityId }}`
- The custom FLUID resolver uses `{LEM.EntityId}`

Both are available; they are different template syntaxes.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              FLUID Approach (NO ANTLR)                        │
│                                                               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  FluidDataSourceAdapter                               │  │
│  │  • Uses FluidParameterExtractor                       │  │
│  │  • Reflection-based path navigation                   │  │
│  │  • Pattern matching for semantic discovery            │  │
│  └───────────────────────────────────────────────────────┘  │
│                       │                                       │
│                       ▼                                       │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  FluidParameterExtractor                               │  │
│  │  • Simple string parsing (no ANTLR)                    │  │
│  │  • Reflection for CLR objects                        │  │
│  │  • JObject.SelectToken for JSON                       │  │
│  │  • Pattern matching for GUID discovery                │  │
│  └───────────────────────────────────────────────────────┘  │
│                       │                                       │
│                       ▼                                       │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  FluidParameterResolver                                │  │
│  │  • Fluent builder API                                  │  │
│  │  • Template resolution                                │  │
│  │  • ActionItem builder                                 │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│              ANTLR Approach (Separate)                      │
│                                                               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  LemDataSourceAdapter                                   │  │
│  │  • Uses AntlrParameterExtractor                        │  │
│  │  • ANTLR4 grammar parsing                              │  │
│  │  • Grammar-based semantic patterns                     │  │
│  └───────────────────────────────────────────────────────┘  │
│                       │                                       │
│                       ▼                                       │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  AntlrParameterExtractor                               │  │
│  │  • ANTLR4 PathExpression.g4 grammar                   │  │
│  │  • Generated lexer/parser                             │  │
│  │  • Grammar-based pattern matching                      │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### FluidParameterExtractor

Standalone path extractor using reflection and simple string parsing (NO ANTLR):

```csharp
public static class FluidParameterExtractor
{
    public static object? Extract(object source, string pathExpression)
    public static Guid ExtractGuid(object source, string pathExpression)
    public static Guid ExtractGuidWithAlternatives(object source, IEnumerable<string> pathExpressions)
    public static (Guid ProjectId, Guid WorkAreaId, Guid EntityId) DiscoverSemanticGuids(object obj)
}
```

**How it works:**
- Parses path expressions using simple string splitting (no ANTLR lexer/parser)
- Uses reflection for CLR object navigation
- Uses `JObject.SelectToken()` for JSON navigation
- Pattern matching (string contains/ends with) for semantic discovery

### FluidParameterResolver

Fluent builder for template resolution:

```csharp
public class FluidParameterResolver
{
    public static FluidParameterResolver Create()
    public FluidParameterResolver WithEvent(object eventData)
    public FluidParameterResolver WithConfig(JObject ruleConfig)
    public FluidParameterResolver WithDataset(IEnumerable<IDataRecord> dataset)
    public string? Resolve(string? template)
    public ActionItem BuildActionItem(...)
}
```

### FluidDataSourceAdapter

Data source adapter using FLUID extraction (parallel to `LemDataSourceAdapter`):

```csharp
public class FluidDataSourceAdapter
{
    public async Task<IEnumerable<IDataRecord>> GetRecordsAsync(
        dynamic eventData,
        dynamic dataSourceParams,
        IEnumerable<IDataRecord> dataset,
        CancellationToken cancellationToken)
}
```

## Usage Examples

### Using FluidDataSourceAdapter

```csharp
// FLUID approach - completely independent from ANTLR
var fluidAdapter = new FluidDataSourceAdapter();
var dataset = await fluidAdapter.GetRecordsAsync(
    eventData,
    dataSourceConfig,
    new List<IDataRecord>(),
    CancellationToken.None);
```

### Using FluidParameterResolver

```csharp
// FLUID approach - fluent API, no ANTLR dependency
var resolver = AntlrTest1.FluidExtraction.FluidParameterResolver
    .Create()
    .WithEvent(eventData)
    .WithConfig(ruleConfig)
    .WithDataset(dataset);

var resolved = resolver.Resolve("Entity {LEM.EntityId} for project {InfoRequest.ProjectId}");
```

### Building ActionItem

```csharp
var actionItem = resolver.BuildActionItem(
    sourceSystemKeyTemplate: "A002IR_{LEM.EntityId}",
    descriptionTemplate: "Action item for entity {LEM.EntityId}",
    status: "Open",
    taskId: Guid.NewGuid());
```

## Path Expression Parsing

### FLUID Approach (Simple String Parsing)

```csharp
// FLUID: Simple string split and reflection
var segments = pathExpression.Split('.');
foreach (var segment in segments)
{
    // Handle array indexing: PropertyName[0]
    // Use reflection or JObject navigation
}
```

### ANTLR Approach (Grammar-Based)

```csharp
// ANTLR: Grammar-based parsing
var inputStream = new AntlrInputStream(pathExpression);
var lexer = new PathExpressionLexer(inputStream);
var parser = new PathExpressionParser(tokenStream);
var context = parser.pathExpression();
```

## Semantic Pattern Matching

### FLUID Approach (Pattern Matching)

```csharp
// FLUID: String pattern matching
private static bool MatchesProjectIdPattern(string path)
{
    var lowerPath = path.ToLowerInvariant();
    return lowerPath.Contains("projectid") && 
           (lowerPath.EndsWith(".projectid") || ...);
}
```

### ANTLR Approach (Grammar Rules)

```csharp
// ANTLR: Grammar-based pattern matching
var context = parser.projectIdPattern();
return context != null && context.exception == null;
```

## Comparison: FLUID vs ANTLR

| Aspect | FLUID Approach | ANTLR Approach |
|--------|---------------|----------------|
| **Dependencies** | None for custom FLUID; optional `Fluid.Core` for Liquid templates | Antlr4.Runtime.Standard |
| **Path Parsing** | String splitting | ANTLR4 grammar |
| **Pattern Matching** | String contains/ends | Grammar rules |
| **Semantic Discovery** | Pattern matching | Grammar patterns |
| **Performance** | Fast (simple parsing) | Fast (optimized lexer) |
| **Complexity** | Lower (no grammar) | Higher (grammar file) |
| **Maintainability** | Easy (C# only) | Medium (grammar + C#) |
| **API Style** | Fluent builder | Static utility |

## When to Use FLUID vs ANTLR

### Use FLUID When:
- ✅ You want zero ANTLR dependencies
- ✅ You prefer reflection-based navigation
- ✅ You want simpler codebase (no grammar files)
- ✅ You prefer fluent builder API
- ✅ You need quick implementation without grammar setup

### Use ANTLR When:
- ✅ You need formal grammar validation
- ✅ You want grammar-based pattern matching
- ✅ You plan to extend with complex parsing rules
- ✅ You prefer static utility methods
- ✅ You want grammar-driven semantic discovery

## Both Approaches Achieve Same Results

Both FLUID and ANTLR approaches:
- ✅ Resolve datasource parameters from event data
- ✅ Resolve action item attributes from templates
- ✅ Support static values, event paths, dataset values
- ✅ Support mixed templates with multiple placeholders
- ✅ Auto-discover GUIDs using semantic patterns
- ✅ Handle nested paths and array indexing

## Integration Example

```csharp
// Same event data, same config - both approaches work
var eventData = new InfoRequestEvent { ... };
var config = new JObject { ... };

// ANTLR approach
var antlrAdapter = new LemDataSourceAdapter();
var antlrDataset = await antlrAdapter.GetRecordsAsync(eventData, config, ...);
var antlrResult = ParameterTemplateResolver.ResolveTemplate("...", eventData, config, antlrDataset);

// FLUID approach (completely separate)
var fluidAdapter = new FluidDataSourceAdapter();
var fluidDataset = await fluidAdapter.GetRecordsAsync(eventData, config, ...);
var fluidResolver = FluidParameterResolver.Create().WithEvent(eventData).WithConfig(config).WithDataset(fluidDataset);
var fluidResult = fluidResolver.Resolve("...");

// Both produce the same results!
```

## Summary

The FLUID approach is a **completely independent implementation** that:
- ✅ Does NOT use ANTLR
- ✅ Uses reflection and pattern matching
- ✅ Provides fluent builder API
- ✅ Achieves the same results as ANTLR
- ✅ Can be used as a drop-in alternative
- ✅ Has zero ANTLR dependencies

Both approaches are **parallel implementations** that solve the same problem using different techniques.

- Choose **FLUID** if you want no grammar/tooling and prefer C#-only logic.
- Choose **ANTLR** if you want grammar-driven parsing and semantic rules.
