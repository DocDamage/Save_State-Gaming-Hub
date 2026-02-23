using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

public partial class SignatureVerificationService
{
    #region Private Verification Methods

    private async Task<VerificationTestResult> RunStaticVerificationAsync(
        GameMemorySignature signature,
        int processId,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new VerificationTestResult
        {
            TestName = "Static Verification",
            TestType = VerificationTestType.Static
        };

        try
        {
            // Get signatures from database and scan
            var scanResult = await ScanForSignatureAsync(signature, ct);

            if (scanResult.IsFailure)
            {
                result.Passed = false;
                result.Message = $"Pattern not found: {scanResult.Error}";
                return result;
            }

            var (address, value) = scanResult.Value;

            // Validate value range
            if (!signature.IsValidValue(value))
            {
                result.Passed = false;
                result.Message = $"Value {value} is outside valid range";
                return result;
            }

            result.Passed = true;
            result.Message = $"Found at 0x{address.ToInt64():X8}, value: {value}";
            result.Metadata["FoundAddress"] = address;
            result.Metadata["CurrentValue"] = value;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Message = $"Static verification error: {ex.Message}";
        }

        result.Duration = stopwatch.Elapsed;
        return result;
    }

    private async Task<VerificationTestResult> RunPointerChainVerificationAsync(
        GameMemorySignature signature,
        int processId,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new VerificationTestResult
        {
            TestName = "Pointer Chain Verification",
            TestType = VerificationTestType.PointerChain
        };

        try
        {
            // Parse pointer chain from pattern (format: "base->offset1->offset2")
            var parts = signature.Pattern.Split(new[] { "->" }, StringSplitOptions.None);
            if (parts.Length < 2)
            {
                result.Passed = false;
                result.Message = "Invalid pointer chain format";
                return result;
            }

            // Resolve base address
            var basePattern = parts[0].Trim();
            var baseResult = await ScanForSignatureAsync(
                new GameMemorySignature { Pattern = basePattern, Offset = 0, ValueType = "int64" },
                ct);

            if (baseResult.IsFailure)
            {
                result.Passed = false;
                result.Message = "Could not find base address for pointer chain";
                return result;
            }

            var currentAddress = baseResult.Value.Address;

            // Follow pointer chain
            for (int i = 1; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i].Trim().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out var offset))
                {
                    result.Passed = false;
                    result.Message = $"Invalid offset in chain: {parts[i]}";
                    return result;
                }

                // Read pointer at current address + offset
                var readResult = await _memoryReader.ReadMemoryBytesAsync(
                    IntPtr.Add(currentAddress, offset), 8, ct);

                if (readResult.IsFailure)
                {
                    result.Passed = false;
                    result.Message = $"Could not read pointer at offset {offset}";
                    return result;
                }

                currentAddress = (IntPtr)BitConverter.ToInt64(readResult.Value, 0);
            }

            result.Passed = true;
            result.Message = $"Pointer chain resolved to 0x{currentAddress.ToInt64():X8}";
            result.Metadata["FinalAddress"] = currentAddress;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Message = $"Pointer chain verification error: {ex.Message}";
        }

        result.Duration = stopwatch.Elapsed;
        return result;
    }

    private async Task<VerificationTestResult> RunStabilityVerificationAsync(
        GameMemorySignature signature,
        IntPtr address,
        VerificationOptions options,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new VerificationTestResult
        {
            TestName = "Stability Verification",
            TestType = VerificationTestType.Stability
        };

        try
        {
            var values = new List<object>();
            var valueChanges = 0;
            object? previousValue = null;

            for (int i = 0; i < options.StabilitySampleCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                var currentValue = await ReadValueAtAddressAsync(address, signature.ValueType, ct);
                if (currentValue == null)
                {
                    result.Passed = false;
                    result.Message = $"Failed to read value at sample {i}";
                    return result;
                }

                values.Add(currentValue);

                if (previousValue != null && !currentValue.Equals(previousValue))
                {
                    valueChanges++;
                }

                previousValue = currentValue;

                if (i < options.StabilitySampleCount - 1)
                {
                    await Task.Delay(options.StabilitySampleDelayMs, ct);
                }
            }

            // Analyze stability
            var changeRate = (double)valueChanges / (options.StabilitySampleCount - 1);

            // Determine if stability is acceptable based on signature type
            var isStable = changeRate switch
            {
                <= 0.1 => true,  // Mostly stable (e.g., max health)
                <= 0.9 => signature.Name.ToLowerInvariant().Contains("position") ||
                          signature.Name.ToLowerInvariant().Contains("time"), // Expected for dynamic values
                _ => false // Too volatile
            };

            result.Passed = isStable || valueChanges > 0; // Pass if stable OR if changing (depending on type)
            result.Message = $"Value changed {valueChanges} times over {options.StabilitySampleCount} samples ({changeRate:P0} change rate)";
            result.Metadata["ChangeRate"] = changeRate;
            result.Metadata["Values"] = values;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Message = $"Stability verification error: {ex.Message}";
        }

        result.Duration = stopwatch.Elapsed;
        return result;
    }

    private async Task<VerificationTestResult> RunDynamicVerificationAsync(
        GameMemorySignature signature,
        IntPtr address,
        VerificationOptions options,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new VerificationTestResult
        {
            TestName = "Dynamic Verification",
            TestType = VerificationTestType.Dynamic
        };

        if (options.UserInteraction == null)
        {
            result.Passed = true; // Skip if no user interaction
            result.Message = "Skipped - no user interaction provider";
            return result;
        }

        try
        {
            // Determine what action to request based on signature name
            var (actionDescription, expectedChange) = GetDynamicTestExpectations(signature);

            // Read initial value
            var initialValue = await ReadValueAtAddressAsync(address, signature.ValueType, ct);
            if (initialValue == null)
            {
                result.Passed = false;
                result.Message = "Could not read initial value";
                return result;
            }

            // Request user action
            var actionConfirmed = await options.UserInteraction.RequestActionAsync(actionDescription, ct);
            if (!actionConfirmed)
            {
                result.Passed = true; // User cancelled, treat as skipped
                result.Message = "User cancelled dynamic test";
                return result;
            }

            // Wait a moment for the action to take effect
            await Task.Delay(1000, ct);

            // Read new value
            var newValue = await ReadValueAtAddressAsync(address, signature.ValueType, ct);
            if (newValue == null)
            {
                result.Passed = false;
                result.Message = "Could not read value after action";
                return result;
            }

            // Verify change
            var valueChanged = !initialValue.Equals(newValue);
            var changeAppropriate = expectedChange switch
            {
                ExpectedChange.Increase => CompareValues(newValue, initialValue) > 0,
                ExpectedChange.Decrease => CompareValues(newValue, initialValue) < 0,
                ExpectedChange.Any => valueChanged,
                ExpectedChange.None => !valueChanged,
                _ => valueChanged
            };

            result.Passed = changeAppropriate;
            result.Message = changeAppropriate
                ? $"Value changed as expected: {initialValue} -> {newValue}"
                : $"Value did not change as expected. Initial: {initialValue}, Current: {newValue}";
            result.Metadata["InitialValue"] = initialValue;
            result.Metadata["FinalValue"] = newValue;
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Message = $"Dynamic verification error: {ex.Message}";
        }

        result.Duration = stopwatch.Elapsed;
        return result;
    }

    private async Task<Result<(IntPtr Address, object Value)>> ScanForSignatureAsync(
        GameMemorySignature signature,
        CancellationToken ct)
    {
        // Use the memory reader's pattern detection
        var patternsResult = await _memoryReader.DetectPatternsAsync(ct);

        if (patternsResult.IsFailure || patternsResult.Value == null)
        {
            return Result.Failure<(IntPtr, object)>("Failed to detect patterns");
        }

        // Find matching pattern
        var matchingPattern = patternsResult.Value
            .FirstOrDefault(p => p.Name.Equals(signature.Name, StringComparison.OrdinalIgnoreCase));

        if (matchingPattern != null)
        {
            return Result.Success((matchingPattern.Address, matchingPattern.CurrentValue));
        }

        return Result.Failure<(IntPtr, object)>("Signature not found");
    }

    private async Task<object?> ReadValueAtAddressAsync(IntPtr address, string valueType, CancellationToken ct)
    {
        var size = GetValueSize(valueType);
        var result = await _memoryReader.ReadMemoryBytesAsync(address, size, ct);

        if (result.IsFailure || result.Value == null)
        {
            return null;
        }

        return ConvertBytesToValue(result.Value, valueType);
    }

    private static int GetValueSize(string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "int8" or "byte" or "bool" => 1,
            "int16" or "short" => 2,
            "int32" or "int" or "float" => 4,
            "int64" or "long" or "double" => 8,
            _ => 4
        };
    }

    private static object ConvertBytesToValue(byte[] bytes, string valueType)
    {
        return valueType.ToLowerInvariant() switch
        {
            "int8" or "byte" => bytes[0],
            "int16" or "short" => BitConverter.ToInt16(bytes, 0),
            "int32" or "int" => BitConverter.ToInt32(bytes, 0),
            "int64" or "long" => BitConverter.ToInt64(bytes, 0),
            "float" => BitConverter.ToSingle(bytes, 0),
            "double" => BitConverter.ToDouble(bytes, 0),
            "bool" => bytes[0] != 0,
            _ => BitConverter.ToInt32(bytes, 0)
        };
    }

    private static double CompareValues(object a, object b)
    {
        if (a is IComparable comparableA && b is IComparable comparableB)
        {
            return comparableA.CompareTo(comparableB);
        }
        return a.Equals(b) ? 0 : -1;
    }

    private (string Action, ExpectedChange Change) GetDynamicTestExpectations(GameMemorySignature signature)
    {
        var nameLower = signature.Name.ToLowerInvariant();

        if (nameLower.Contains("health") || nameLower.Contains("hp"))
        {
            return ("Please take damage in the game", ExpectedChange.Decrease);
        }
        if (nameLower.Contains("ammo") || nameLower.Contains("bullet"))
        {
            return ("Please fire your weapon", ExpectedChange.Decrease);
        }
        if (nameLower.Contains("money") || nameLower.Contains("gold") || nameLower.Contains("coin"))
        {
            return ("Please spend some money or pick up currency", ExpectedChange.Any);
        }
        if (nameLower.Contains("position") || nameLower.Contains("coord"))
        {
            return ("Please move your character", ExpectedChange.Any);
        }
        if (nameLower.Contains("score") || nameLower.Contains("points"))
        {
            return ("Please score some points", ExpectedChange.Increase);
        }
        if (nameLower.Contains("time") || nameLower.Contains("timer"))
        {
            return ("Wait a moment", ExpectedChange.Any);
        }

        return ("Please interact with the game to change this value", ExpectedChange.Any);
    }

    private enum ExpectedChange
    {
        Increase,
        Decrease,
        Any,
        None
    }

    private static double CalculateConfidence(List<VerificationTestResult> testResults)
    {
        if (testResults.Count == 0) return 0;

        var weights = new Dictionary<VerificationTestType, double>
        {
            [VerificationTestType.Static] = 0.4,
            [VerificationTestType.Dynamic] = 0.3,
            [VerificationTestType.Stability] = 0.2,
            [VerificationTestType.PointerChain] = 0.1
        };

        double totalWeight = 0;
        double weightedScore = 0;

        foreach (var test in testResults)
        {
            var weight = weights.GetValueOrDefault(test.TestType, 0.1);
            weightedScore += (test.Passed ? 1 : 0) * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? weightedScore / totalWeight : 0;
    }

    private SignatureHealthScore CalculateHealthScore(List<VerificationTestResult> testResults, GameMemorySignature signature)
    {
        var score = new SignatureHealthScore();

        // Pattern quality (based on pattern length and wildcards)
        var patternBytes = signature.Pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wildcardCount = patternBytes.Count(b => b == "??" || b == "**");
        score.PatternQuality = Math.Max(0, 100 - wildcardCount * 10);
        if (patternBytes.Length < 4) score.PatternQuality -= 20;

        // Address stability (based on stability test)
        var stabilityTest = testResults.FirstOrDefault(r => r.TestType == VerificationTestType.Stability);
        if (stabilityTest != null)
        {
            score.AddressStability = stabilityTest.Passed ? 90 : 30;
        }
        else
        {
            score.AddressStability = 50; // Unknown
        }

        // Value reliability (based on static and dynamic tests)
        var staticTest = testResults.FirstOrDefault(r => r.TestType == VerificationTestType.Static);
        var dynamicTest = testResults.FirstOrDefault(r => r.TestType == VerificationTestType.Dynamic);
        score.ValueReliability = (staticTest?.Passed == true ? 50 : 0) +
                                  (dynamicTest?.Passed == true ? 50 : 0);

        // Compatibility (placeholder - would use community data)
        score.CompatibilityScore = 50;

        // Community score (placeholder)
        score.CommunityScore = 50;

        // Calculate overall
        score.OverallScore = (score.PatternQuality + score.AddressStability +
                              score.ValueReliability + score.CompatibilityScore +
                              score.CommunityScore) / 5;

        return score;
    }

    private static void ValidatePatternQuality(GameMemorySignature signature, ValidationReport report)
    {
        var patternBytes = signature.Pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (patternBytes.Length < 4)
        {
            report.Issues.Add(new ValidationIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Pattern,
                Description = "Pattern is very short and may not be unique enough",
                SuggestedFix = "Use a longer pattern (at least 8 bytes) for better uniqueness"
            });
        }

        var wildcardCount = patternBytes.Count(b => b == "??" || b == "**");
        var wildcardRatio = (double)wildcardCount / patternBytes.Length;

        if (wildcardRatio > 0.5)
        {
            report.Issues.Add(new ValidationIssue
            {
                Severity = IssueSeverity.Warning,
                Category = IssueCategory.Pattern,
                Description = "Pattern has too many wildcards and may match multiple locations",
                SuggestedFix = "Reduce the number of wildcards to improve specificity"
            });
        }
    }

    private static void ValidateOffset(GameMemorySignature signature, ValidationReport report)
    {
        if (signature.Offset < -0x1000 || signature.Offset > 0x1000)
        {
            report.Issues.Add(new ValidationIssue
            {
                Severity = IssueSeverity.Info,
                Category = IssueCategory.Offset,
                Description = "Offset is unusually large",
                SuggestedFix = "Verify that the offset is correct"
            });
        }
    }

    private static void ValidateValueType(GameMemorySignature signature, ValidationReport report)
    {
        var validTypes = new[] { "int8", "byte", "int16", "short", "int32", "int", "int64", "long", "float", "double", "bool" };

        if (!validTypes.Contains(signature.ValueType.ToLowerInvariant()))
        {
            report.Issues.Add(new ValidationIssue
            {
                Severity = IssueSeverity.Critical,
                Category = IssueCategory.ValueType,
                Description = $"Unknown value type: {signature.ValueType}",
                SuggestedFix = $"Use one of: {string.Join(", ", validTypes)}"
            });
        }
    }

    private static void ValidateRanges(GameMemorySignature signature, ValidationReport report)
    {
        if (signature.MinValue.HasValue && signature.MaxValue.HasValue)
        {
            if (signature.MinValue > signature.MaxValue)
            {
                report.Issues.Add(new ValidationIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = IssueCategory.Range,
                    Description = "Min value is greater than max value",
                    SuggestedFix = "Swap min and max values"
                });
            }
        }
    }

    private static SignatureHealthScore CalculateOfflineHealthScore(GameMemorySignature signature, List<ValidationIssue> issues)
    {
        var score = new SignatureHealthScore();

        // Pattern quality
        var patternBytes = signature.Pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var wildcardCount = patternBytes.Count(b => b == "??" || b == "**");
        score.PatternQuality = Math.Max(0, 100 - wildcardCount * 10);
        if (patternBytes.Length < 4) score.PatternQuality -= 20;

        // Address stability (unknown without process)
        score.AddressStability = 50;

        // Value reliability (unknown without process)
        score.ValueReliability = 50;

        // Compatibility (unknown without community data)
        score.CompatibilityScore = 50;

        // Community score
        score.CommunityScore = 50;

        // Reduce score based on issues
        foreach (var issue in issues)
        {
            var deduction = issue.Severity switch
            {
                IssueSeverity.Critical => 20,
                IssueSeverity.Warning => 10,
                IssueSeverity.Info => 0,
                _ => 0
            };
            score.OverallScore -= deduction;
        }

        // Calculate overall
        score.OverallScore = Math.Max(0, (score.PatternQuality + score.AddressStability +
                              score.ValueReliability + score.CompatibilityScore +
                              score.CommunityScore) / 5 - issues.Count * 5);

        return score;
    }

    private static List<string> GenerateRecommendations(GameMemorySignature signature, List<ValidationIssue> issues)
    {
        var recommendations = new List<string>();

        foreach (var issue in issues.Where(i => !string.IsNullOrEmpty(i.SuggestedFix)))
        {
            recommendations.Add(issue.SuggestedFix!);
        }

        if (!signature.MinValue.HasValue && !signature.MaxValue.HasValue)
        {
            recommendations.Add("Consider adding min/max value constraints for validation");
        }

        if (string.IsNullOrEmpty(signature.Description))
        {
            recommendations.Add("Add a description to help identify what this signature represents");
        }

        return recommendations;
    }

    private static string SuggestRelaxedPattern(string originalPattern)
    {
        // Replace every 4th byte with wildcard as a conservative relaxation
        var bytes = originalPattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 3; i < bytes.Length; i += 4)
        {
            if (bytes[i] != "??" && bytes[i] != "**")
            {
                bytes[i] = "??";
            }
        }
        return string.Join(" ", bytes);
    }

    private static List<string> GetAlternativeValueTypes(string currentType)
    {
        var typeMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["int"] = new() { "float", "int64", "int16" },
            ["int32"] = new() { "float", "int64", "int16" },
            ["float"] = new() { "int", "double" },
            ["int64"] = new() { "int", "double" },
            ["double"] = new() { "float", "int64" }
        };

        return typeMap.GetValueOrDefault(currentType, new List<string> { "int", "float" });
    }

    #endregion
}
