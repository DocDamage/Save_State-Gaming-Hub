using SaveState.Core.Common.Base;

namespace SaveState.Core.AiGaming.Entities;

public class AiModel : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public string ModelId { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int MaxTokens { get; private set; }
    public float Temperature { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    protected AiModel() { } // EF Core

    public AiModel(string name, string provider, string modelId, int maxTokens = 2048, float temperature = 0.7f)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Provider = Guard.Against.NullOrWhiteSpace(provider, nameof(provider));
        ModelId = Guard.Against.NullOrWhiteSpace(modelId, nameof(modelId));
        MaxTokens = Guard.Against.Negative(maxTokens, nameof(maxTokens));
        Temperature = Guard.Against.OutOfRange(temperature, nameof(temperature), 0f, 2f);
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(int? maxTokens = null, float? temperature = null)
    {
        if (maxTokens.HasValue)
        {
            MaxTokens = Guard.Against.Negative(maxTokens.Value, nameof(maxTokens));
        }

        if (temperature.HasValue)
        {
            Temperature = Guard.Against.OutOfRange(temperature.Value, nameof(temperature), 0f, 2f);
        }
    }

    public void UpdateDescription(string description)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }

    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
