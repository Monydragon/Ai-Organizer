using System;
using System.Collections.Generic;

namespace Ai_Organizer.Models;

public sealed class ModelPerformanceMetrics
{
    public string ModelName { get; set; } = "";
    public string ProviderName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public long ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Quality score from 0 to 1 (based on user feedback or automated evaluation).
    /// </summary>
    public double QualityScore { get; set; }
    
    /// <summary>
    /// Cost of this request (if applicable).
    /// </summary>
    public decimal CostUsd { get; set; }
    
    /// <summary>
    /// Input token count (if applicable).
    /// </summary>
    public int? InputTokens { get; set; }
    
    /// <summary>
    /// Output token count (if applicable).
    /// </summary>
    public int? OutputTokens { get; set; }
    
    /// <summary>
    /// Task type (e.g., "FileOrganization", "MetadataExtraction").
    /// </summary>
    public string TaskType { get; set; } = "";
    
    /// <summary>
    /// Optional details about what was processed.
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Whether the request succeeded.
    /// </summary>
    public bool Success { get; set; } = true;
    
    /// <summary>
    /// Error message if request failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

public sealed class ModelPerformanceSummary
{
    public string ModelName { get; set; } = "";
    public string ProviderName { get; set; } = "";
    
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public double AverageQualityScore { get; set; }
    public decimal TotalCostUsd { get; set; }
    public DateTime FirstUsed { get; set; }
    public DateTime LastUsed { get; set; }
}


