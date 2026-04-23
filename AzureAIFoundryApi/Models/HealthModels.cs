namespace AzureAIFoundryApi.Models;

public class HealthResponse
{
    public string Status { get; set; } = "Healthy";
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, SubsystemHealth> Subsystems { get; set; } = new();
}

public class SubsystemHealth
{
    public string Status { get; set; } = "Healthy";
    public string? Message { get; set; }
    public long? LatencyMs { get; set; }
}
