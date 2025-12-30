namespace SaveState.Core.AiGaming;

using SaveState.Core.Common.Base;

/// <summary>
/// AiGaming Bounded Context - handles AI-assisted gaming features, cheat detection, trainer generation, and memory analysis.
/// This context owns entities related to AI models, cheat patterns, trainers, and memory scans.
/// </summary>
public static class AiGamingContext
{
    public const string Name = "AiGaming";

    // Entities owned by this context
    public static readonly Type[] Entities = {
        typeof(AiModel),
        typeof(CheatPattern),
        typeof(Trainer),
        typeof(MemoryScan)
    };

    // Domain services
    public interface ICheatDetectionService { /* Implementation in Application layer */ }
    public interface ITrainerGenerationService { /* Implementation in Application layer */ }
    public interface IMemoryAnalysisService { /* Implementation in Application layer */ }

    // Value objects for this context
    public class MemoryAddress : ValueObject
    {
        public long Value { get; }

        public MemoryAddress(long value)
        {
            if (value < 0)
                throw new ArgumentException("Memory address cannot be negative", nameof(value));
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public static MemoryAddress operator +(MemoryAddress left, int offset)
            => new(left.Value + offset);

        public static MemoryAddress operator +(MemoryAddress left, long offset)
            => new(left.Value + offset);

        public override string ToString() => $"0x{Value:X}";

        public static implicit operator long(MemoryAddress address) => address.Value;
        public static explicit operator MemoryAddress(long value) => new(value);
    }

    public class ConfidenceScore : ValueObject
    {
        public float Value { get; }

        public ConfidenceScore(float value)
        {
            if (value < 0 || value > 1)
                throw new ArgumentException("Confidence score must be between 0 and 1", nameof(value));
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public string ToPercentage() => $"{Value:P1}";

        public static implicit operator float(ConfidenceScore score) => score.Value;
        public static explicit operator ConfidenceScore(float value) => new(value);
    }

    public class CheatSignature : ValueObject
    {
        public byte[] Pattern { get; }
        public string? Mask { get; }

        public CheatSignature(byte[] pattern, string? mask = null)
        {
            Pattern = Guard.Against.Null(pattern, nameof(pattern));
            if (pattern.Length == 0)
                throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

            Mask = mask;
            if (mask != null && mask.Length != pattern.Length)
                throw new ArgumentException("Mask length must match pattern length", nameof(mask));
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Convert.ToBase64String(Pattern);
            yield return Mask ?? string.Empty;
        }

        public override string ToString()
        {
            return $"Pattern: {BitConverter.ToString(Pattern)}{(Mask != null ? $", Mask: {Mask}" : "")}";
        }
    }

    public class ProcessId : ValueObject
    {
        public int Value { get; }

        public ProcessId(int value)
        {
            if (value <= 0)
                throw new ArgumentException("Process ID must be positive", nameof(value));
            Value = value;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();

        public static implicit operator int(ProcessId pid) => pid.Value;
        public static explicit operator ProcessId(int value) => new(value);
    }

    /// <summary>
    /// Represents a memory value that can be of various types used in cheat training and memory scanning.
    /// Provides type-safe access to different value types while maintaining flexibility for memory operations.
    /// </summary>
    public abstract class MemoryValue
    {
        public abstract Type ValueType { get; }

        public static MemoryValue From<T>(T value) where T : notnull
        {
            return value switch
            {
                int intValue => new IntMemoryValue(intValue),
                float floatValue => new FloatMemoryValue(floatValue),
                bool boolValue => new BoolMemoryValue(boolValue),
                byte[] byteArray => new ByteArrayMemoryValue(byteArray),
                string stringValue => new StringMemoryValue(stringValue),
                long longValue => new LongMemoryValue(longValue),
                double doubleValue => new DoubleMemoryValue(doubleValue),
                _ => throw new ArgumentException($"Unsupported memory value type: {typeof(T)}", nameof(value))
            };
        }

        public bool TryGetValue<T>(out T value) where T : notnull
        {
            if (this is TypedMemoryValue<T> typedValue)
            {
                value = typedValue.Value;
                return true;
            }

            value = default!;
            return false;
        }

        public T GetValue<T>() where T : notnull
        {
            if (TryGetValue(out T value))
            {
                return value;
            }

            throw new InvalidCastException($"Cannot convert {ValueType} to {typeof(T)}");
        }

        public override string ToString()
        {
            return this switch
            {
                IntMemoryValue intVal => intVal.Value.ToString(),
                FloatMemoryValue floatVal => floatVal.Value.ToString(),
                BoolMemoryValue boolVal => boolVal.Value.ToString(),
                ByteArrayMemoryValue byteArr => BitConverter.ToString(byteArr.Value),
                StringMemoryValue strVal => strVal.Value,
                LongMemoryValue longVal => longVal.Value.ToString(),
                DoubleMemoryValue doubleVal => doubleVal.Value.ToString(),
                _ => "Unknown"
            };
        }
    }

    public abstract class TypedMemoryValue<T> : MemoryValue where T : notnull
    {
        public T Value { get; }

        protected TypedMemoryValue(T value)
        {
            Value = value;
        }

        public override Type ValueType => typeof(T);
    }

    public class IntMemoryValue : TypedMemoryValue<int>
    {
        public IntMemoryValue(int value) : base(value) { }
    }

    public class FloatMemoryValue : TypedMemoryValue<float>
    {
        public FloatMemoryValue(float value) : base(value) { }
    }

    public class BoolMemoryValue : TypedMemoryValue<bool>
    {
        public BoolMemoryValue(bool value) : base(value) { }
    }

    public class ByteArrayMemoryValue : TypedMemoryValue<byte[]>
    {
        public ByteArrayMemoryValue(byte[] value) : base(value) { }
    }

    public class StringMemoryValue : TypedMemoryValue<string>
    {
        public StringMemoryValue(string value) : base(value) { }
    }

    public class LongMemoryValue : TypedMemoryValue<long>
    {
        public LongMemoryValue(long value) : base(value) { }
    }

    public class DoubleMemoryValue : TypedMemoryValue<double>
    {
        public DoubleMemoryValue(double value) : base(value) { }
    }
}

// Placeholder entities for the context - will be fully implemented in Phase 1
public class AiModel : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string ModelType { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public DateTime TrainedAt { get; private set; }
    public Dictionary<string, string> Parameters { get; private set; } = new();

    private AiModel() { }

    public static AiModel Create(string name, string modelType, string version)
    {
        return new AiModel
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            ModelType = Guard.Against.NullOrWhiteSpace(modelType, nameof(modelType)),
            Version = Guard.Against.NullOrWhiteSpace(version, nameof(version)),
            TrainedAt = DateTime.UtcNow,
            Parameters = new Dictionary<string, string>()
        };
    }
}

public class CheatPattern : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AiGamingContext.CheatSignature Signature { get; private set; } = null!;
    public string CheatType { get; private set; } = string.Empty;
    public AiGamingContext.ConfidenceScore DetectionConfidence { get; private set; }

    private CheatPattern() { }

    public static CheatPattern Create(string name, string description, AiGamingContext.CheatSignature signature, string cheatType, AiGamingContext.ConfidenceScore confidence)
    {
        return new CheatPattern
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = Guard.Against.NullOrWhiteSpace(description, nameof(description)),
            Signature = Guard.Against.Null(signature, nameof(signature)),
            CheatType = Guard.Against.NullOrWhiteSpace(cheatType, nameof(cheatType)),
            DetectionConfidence = Guard.Against.Null(confidence, nameof(confidence))
        };
    }
}

public class Trainer : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string TargetProcess { get; private set; } = string.Empty;
    public List<TrainerCheat> Cheats { get; private set; } = new();
    public DateTime GeneratedAt { get; private set; }

    private Trainer() { }

    public static Trainer Create(string name, string targetProcess)
    {
        return new Trainer
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            TargetProcess = Guard.Against.NullOrWhiteSpace(targetProcess, nameof(targetProcess)),
            Cheats = new List<TrainerCheat>(),
            GeneratedAt = DateTime.UtcNow
        };
    }
}

public class TrainerCheat
{
    public string Description { get; set; } = string.Empty;
    public AiGamingContext.MemoryAddress Address { get; set; } = null!;
    public AiGamingContext.MemoryValue? DefaultValue { get; set; }
    public Type ValueType => DefaultValue?.ValueType ?? typeof(int);
}

public class MemoryScan : EntityBase
{
    public AiGamingContext.ProcessId ProcessId { get; private set; }
    public string ProcessName { get; private set; } = string.Empty;
    public AiGamingContext.MemoryAddress StartAddress { get; private set; }
    public AiGamingContext.MemoryAddress EndAddress { get; private set; }
    public string ScanType { get; private set; } = string.Empty;
    public List<MemoryScanResult> Results { get; private set; } = new();
    public DateTime ScannedAt { get; private set; }

    private MemoryScan() { }

    public static MemoryScan Create(AiGamingContext.ProcessId processId, string processName, AiGamingContext.MemoryAddress startAddress, AiGamingContext.MemoryAddress endAddress, string scanType)
    {
        if (endAddress <= startAddress)
            throw new ArgumentException("End address must be greater than start address");

        return new MemoryScan
        {
            ProcessId = Guard.Against.Null(processId, nameof(processId)),
            ProcessName = Guard.Against.NullOrWhiteSpace(processName, nameof(processName)),
            StartAddress = Guard.Against.Null(startAddress, nameof(startAddress)),
            EndAddress = Guard.Against.Null(endAddress, nameof(endAddress)),
            ScanType = Guard.Against.NullOrWhiteSpace(scanType, nameof(scanType)),
            Results = new List<MemoryScanResult>(),
            ScannedAt = DateTime.UtcNow
        };
    }
}

public class MemoryScanResult
{
    public AiGamingContext.MemoryAddress Address { get; set; } = null!;
    public AiGamingContext.MemoryValue? Value { get; set; }
    public AiGamingContext.ConfidenceScore Confidence { get; set; } = null!;
}
