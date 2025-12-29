namespace SaveState.Core.Common.Configuration;

using System.ComponentModel.DataAnnotations;

public class AiOptions : IValidatableObject
{
    public const string Section = "AI";

    public string PrimaryProvider { get; set; } = "OpenAI";
    public Dictionary<string, AiProviderOptions> Providers { get; set; } = new();
    public int MaxTokens { get; set; } = 2048;
    public float Temperature { get; set; } = 0.7f;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentRequests { get; set; } = 5;
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
    public bool EnableFallbackProviders { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (MaxTokens <= 0)
            results.Add(new ValidationResult("Max tokens must be positive", new[] { nameof(MaxTokens) }));

        if (Temperature < 0 || Temperature > 2)
            results.Add(new ValidationResult("Temperature must be between 0 and 2", new[] { nameof(Temperature) }));

        if (MaxConcurrentRequests <= 0)
            results.Add(new ValidationResult("Max concurrent requests must be positive", new[] { nameof(MaxConcurrentRequests) }));

        return results;
    }
}

public class AiProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public Dictionary<string, ModelOptions> Models { get; set; } = new();
}

public class ModelOptions
{
    public int MaxTokens { get; set; }
    public decimal CostPerToken { get; set; }
}

public class CircuitBreakerOptions
{
    public int Threshold { get; set; } = 5;
    public int DurationMs { get; set; } = 60000;
    public int TimeoutMs { get; set; } = 30000;
}
