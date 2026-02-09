using System.Text.Json;
using AntlrTest1.Events;
using AntlrTest1.Interfaces;
using AntlrTest1.FluidExtraction;
using Newtonsoft.Json.Linq;

namespace AntlrTest1
{
    /// <summary>
    /// Data source adapter using FLUID approach for parameter extraction.
    /// This is a parallel implementation to LemDataSourceAdapter (ANTLR approach).
    /// Both achieve the same result but use different extraction methods.
    /// </summary>
    public class FluidDataSourceAdapter
    {
        public async Task<IEnumerable<IDataRecord>> GetRecordsAsync(
            dynamic eventData,
            dynamic dataSourceParams,
            IEnumerable<IDataRecord> dataset,
            CancellationToken cancellationToken)
        {
            Console.WriteLine($"DataSource adapter {nameof(FluidDataSourceAdapter)} execution starts (FLUID approach).");

            if (eventData is null)
            {
                throw new InvalidOperationException("Event data cannot be null.");
            }

            // Convert dataSourceParams to JObject
            JObject? paramsObj = dataSourceParams is JObject jObj ? jObj : 
                                 dataSourceParams != null ? JObject.FromObject(dataSourceParams) : 
                                 new JObject();

            // Extract parameters using FLUID-based extractor (NO ANTLR)
            var extractedParams = ExtractParameters(eventData, paramsObj);

            // Validate extracted parameters
            ValidateParameters(extractedParams);

            // Call appropriate API based on level
            return extractedParams.IsProjectLevel
                ? await GetProjectEntityAsync(extractedParams)
                : await GetWorkAreaEntityAsync(extractedParams);
        }

        private ExtractedParameters ExtractParameters(object eventData, JObject dataSourceParams)
        {
            Console.WriteLine("\n[Parameter Extraction] Starting FLUID-based extraction (NO ANTLR)...");

            var result = new ExtractedParameters
            {
                // Get configuration from dataSourceParams
                EffectiveDate = dataSourceParams["effectiveDate"]?.ToObject<DateTime?>(),
                SearchOption = dataSourceParams["searchOption"]?.ToString() ?? "Active",
                IncludeOutOfScope = dataSourceParams["includeOutOfScope"]?.ToObject<bool>() ?? false,
                IncludeCollections = dataSourceParams["includeCollections"]?.ToObject<bool>() ?? true,
                IsProjectLevel = dataSourceParams["isProjectLevel"]?.ToObject<bool>() ?? false
            };

            // Use FLUID-based auto-discovery for semantic pattern matching
            Console.WriteLine("\n[Auto-Discovery] Using FLUID-based semantic patterns...");
            AutoDiscoverParameters(eventData, result);

            Console.WriteLine($"\n[Extraction Result] ProjectId={result.ProjectId}, WorkAreaId={result.WorkAreaId}, EntityId={result.EntityId}\n");

            return result;
        }

        private void AutoDiscoverParameters(object eventData, ExtractedParameters result)
        {
            // Use FLUID-based semantic pattern matching (NO ANTLR)
            var (projectId, workAreaId, entityId) = FluidParameterExtractor.DiscoverSemanticGuids(eventData);

            if (projectId != Guid.Empty)
            {
                result.ProjectId = projectId;
            }

            if (workAreaId != Guid.Empty)
            {
                result.WorkAreaId = workAreaId;
            }

            if (entityId != Guid.Empty)
            {
                result.EntityId = entityId;
            }

            // Determine if project level
            if (result.WorkAreaId == Guid.Empty && result.ProjectId != Guid.Empty && result.EntityId != Guid.Empty)
            {
                result.IsProjectLevel = true;
                Console.WriteLine($"  [Auto-Discovery] Detected as Project Level");
            }
        }

        private void ValidateParameters(ExtractedParameters parameters)
        {
            if (parameters.IsProjectLevel)
            {
                if (parameters.ProjectId == Guid.Empty || parameters.EntityId == Guid.Empty)
                {
                    Console.WriteLine($"[Validation Error] ProjectLevel: Required identifiers missing. ProjectId: {parameters.ProjectId}, EntityId: {parameters.EntityId}");
                    throw new InvalidOperationException("ProjectId and EntityId are required for project-level queries.");
                }
            }
            else
            {
                if (parameters.ProjectId == Guid.Empty || parameters.WorkAreaId == Guid.Empty || parameters.EntityId == Guid.Empty)
                {
                    Console.WriteLine($"[Validation Error] WorkAreaLevel: Required identifiers missing. ProjectId: {parameters.ProjectId}, WorkAreaId: {parameters.WorkAreaId}, EntityId: {parameters.EntityId}");
                    throw new InvalidOperationException("ProjectId, WorkAreaId, and EntityId are required for workarea-level queries.");
                }
            }

            Console.WriteLine("[Validation] ✓ All required identifiers present");
        }

        #region API Calls

        private async Task<IEnumerable<IDataRecord>> GetWorkAreaEntityAsync(ExtractedParameters parameters)
        {
            try
            {
                Console.WriteLine($"\n[API Call] GetWorkAreaEntityAsync (FLUID approach)");
                Console.WriteLine($"  ProjectId: {parameters.ProjectId}");
                Console.WriteLine($"  WorkAreaId: {parameters.WorkAreaId}");
                Console.WriteLine($"  EntityId: {parameters.EntityId}");
                
                await Task.Delay(10);

                var dummyData = new JObject
                {
                    ["EntityId"] = parameters.EntityId,
                    ["ProjectId"] = parameters.ProjectId,
                    ["WorkAreaId"] = parameters.WorkAreaId,
                    ["EntityName"] = "Test WorkArea Entity (FLUID)",
                    ["EntityType"] = "Corporation",
                    ["CategoryName"] = "Legal Entity",
                    ["TaxClassificationTypeName"] = "Partnership",
                    ["Status"] = parameters.SearchOption,
                    ["EffectiveDate"] = parameters.EffectiveDate?.ToString() ?? DateTime.UtcNow.ToString(),
                    ["IncludeOutOfScope"] = parameters.IncludeOutOfScope,
                    ["IncludeCollections"] = parameters.IncludeCollections,
                    ["CreatedDate"] = DateTime.UtcNow.AddDays(-30).ToString(),
                    ["ModifiedDate"] = DateTime.UtcNow.ToString(),
                    ["Attributes"] = new JObject
                    {
                        ["TaxId"] = "12-3456789",
                        ["Jurisdiction"] = "Delaware",
                        ["IncorporationDate"] = "2020-01-01"
                    }
                };

                return new List<IDataRecord> { new DataRecord { Data = dummyData, Source = "LEM" } };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to fetch WorkArea entity data: {ex.Message}");
                throw new InvalidOperationException("Error while fetching WorkArea entity data: " + ex.Message, ex);
            }
        }

        private async Task<IEnumerable<IDataRecord>> GetProjectEntityAsync(ExtractedParameters parameters)
        {
            try
            {
                Console.WriteLine($"\n[API Call] GetProjectEntityAsync (FLUID approach)");
                Console.WriteLine($"  ProjectId: {parameters.ProjectId}");
                Console.WriteLine($"  EntityId: {parameters.EntityId}");
                
                await Task.Delay(10);

                var dummyData = new JObject
                {
                    ["EntityId"] = parameters.EntityId,
                    ["ProjectId"] = parameters.ProjectId,
                    ["EntityName"] = "Test Project Entity (FLUID)",
                    ["EntityType"] = "Partnership",
                    ["CategoryName"] = "Legal Entity",
                    ["TaxClassificationTypeName"] = "Partnership",
                    ["Status"] = parameters.SearchOption,
                    ["EffectiveDate"] = parameters.EffectiveDate?.ToString() ?? DateTime.UtcNow.ToString(),
                    ["IncludeOutOfScope"] = parameters.IncludeOutOfScope,
                    ["CreatedDate"] = DateTime.UtcNow.AddDays(-60).ToString(),
                    ["ModifiedDate"] = DateTime.UtcNow.ToString(),
                    ["Attributes"] = new JObject
                    {
                        ["TaxId"] = "98-7654321",
                        ["FormationType"] = "LLC",
                        ["StateOfFormation"] = "California"
                    }
                };

                return new List<IDataRecord> { new DataRecord { Data = dummyData, Source = "LEM" } };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to fetch Project entity data: {ex.Message}");
                throw new InvalidOperationException("Error while fetching Project entity data: " + ex.Message, ex);
            }
        }

        #endregion

        #region Helper Classes

        private class ExtractedParameters
        {
            public Guid ProjectId { get; set; }
            public Guid WorkAreaId { get; set; }
            public Guid EntityId { get; set; }
            public DateTime? EffectiveDate { get; set; }
            public string SearchOption { get; set; } = "Active";
            public bool IncludeOutOfScope { get; set; }
            public bool IncludeCollections { get; set; }
            public bool IsProjectLevel { get; set; }
        }

        #endregion
    }
}
