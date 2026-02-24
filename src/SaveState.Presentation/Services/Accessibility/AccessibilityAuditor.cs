using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace SaveState.Presentation.Services.Accessibility;

/// <summary>
/// Automated accessibility auditing tool for WCAG 2.1 AA compliance.
/// </summary>
public class AccessibilityAuditor
{
    private readonly ILogger<AccessibilityAuditor> _logger;

    public AccessibilityAuditor(ILogger<AccessibilityAuditor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs a comprehensive accessibility audit of the UI.
    /// </summary>
    /// <param name="root">The root element to audit.</param>
    /// <returns>The audit results.</returns>
    public async Task<AccessibilityAuditResult> AuditAsync(Control root)
    {
        var result = new AccessibilityAuditResult
        {
            AuditDate = DateTime.UtcNow,
            TotalElements = 0
        };

        try
        {
            await AuditElementAsync(root, result);
            CalculateScore(result);
            
            _logger.LogInformation(
                "Accessibility audit complete: {Issues} issues found, score: {Score:F1}%",
                result.Issues.Count,
                result.ComplianceScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during accessibility audit");
            result.Issues.Add(new AccessibilityIssue
            {
                Severity = IssueSeverity.Error,
                Message = $"Audit failed: {ex.Message}",
                WcagGuideline = "N/A"
            });
        }

        return result;
    }

    /// <summary>
    /// Exports audit results to a file.
    /// </summary>
    public async Task ExportResultsAsync(AccessibilityAuditResult result, string outputPath)
    {
        try
        {
            var extension = Path.GetExtension(outputPath).ToLowerInvariant();
            var content = extension switch
            {
                ".json" => ExportToJson(result),
                ".html" => ExportToHtml(result),
                ".md" => ExportToMarkdown(result),
                _ => ExportToText(result)
            };

            await File.WriteAllTextAsync(outputPath, content);
            _logger.LogInformation("Exported accessibility audit to {Path}", outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export accessibility audit");
        }
    }

    #region Audit Checks

    private async Task AuditElementAsync(Control element, AccessibilityAuditResult result)
    {
        result.TotalElements++;

        // Check for missing alt text on images
        CheckMissingAltText(element, result);

        // Check contrast ratios
        CheckContrastRatio(element, result);

        // Check for missing labels
        CheckMissingLabels(element, result);

        // Check keyboard accessibility
        CheckKeyboardAccessibility(element, result);

        // Check focus order
        CheckFocusOrder(element, result);

        // Check ARIA violations
        CheckAriaViolations(element, result);

        // Check for keyboard traps
        await CheckKeyboardTrapsAsync(element, result);

        // Recurse into children
        foreach (var child in element.GetVisualChildren().OfType<Control>())
        {
            await AuditElementAsync(child, result);
        }
    }

    private void CheckMissingAltText(Control element, AccessibilityAuditResult result)
    {
        if (element is Image image)
        {
            var automationName = AutomationProperties.GetName(image);
            if (string.IsNullOrEmpty(automationName))
            {
                result.Issues.Add(new AccessibilityIssue
                {
                    ElementType = element.GetType().Name,
                    ElementName = element.Name,
                    Severity = IssueSeverity.Warning,
                    Message = "Image is missing alt text (accessibility name)",
                    WcagGuideline = "1.1.1 Non-text Content",
                    Remediation = "Add AutomationProperties.Name to the Image element"
                });
            }
        }
    }

    private void CheckContrastRatio(Control element, AccessibilityAuditResult result)
    {
        if (element is TextBlock textBlock)
        {
            var foreground = textBlock.Foreground;
            var background = GetBackgroundColor(element);

            if (foreground != null)
            {
                var validation = ColorContrastChecker.ValidateContrast(
                    GetColorFromBrush(foreground),
                    background,
                    textBlock.FontSize >= 18 || (textBlock.FontSize >= 14 && textBlock.FontWeight >= FontWeight.Bold));

                if (!validation.MeetsAA)
                {
                    result.Issues.Add(new AccessibilityIssue
                    {
                        ElementType = element.GetType().Name,
                        ElementName = element.Name,
                        Severity = IssueSeverity.Error,
                        Message = $"Insufficient color contrast: {validation.ContrastRatio:F1}:1 (requires {validation.RequiredForAA}:1)",
                        WcagGuideline = "1.4.3 Contrast (Minimum)",
                        Remediation = "Increase the contrast between text and background colors"
                    });
                }
                else if (!validation.MeetsAAA)
                {
                    result.Issues.Add(new AccessibilityIssue
                    {
                        ElementType = element.GetType().Name,
                        ElementName = element.Name,
                        Severity = IssueSeverity.Suggestion,
                        Message = $"Color contrast could be improved: {validation.ContrastRatio:F1}:1 (AAA requires {validation.RequiredForAAA}:1)",
                        WcagGuideline = "1.4.6 Contrast (Enhanced)",
                        Remediation = "Consider increasing contrast for better accessibility"
                    });
                }
            }
        }
    }

    private void CheckMissingLabels(Control element, AccessibilityAuditResult result)
    {
        if (element is TextBox textBox)
        {
            var automationName = AutomationProperties.GetName(textBox);
            var watermark = textBox.Watermark;
            
            if (string.IsNullOrEmpty(automationName) && string.IsNullOrEmpty(watermark))
            {
                result.Issues.Add(new AccessibilityIssue
                {
                    ElementType = element.GetType().Name,
                    ElementName = element.Name,
                    Severity = IssueSeverity.Error,
                    Message = "TextBox is missing a label or accessibility name",
                    WcagGuideline = "3.3.2 Labels or Instructions",
                    Remediation = "Add AutomationProperties.Name or Watermark to the TextBox"
                });
            }
        }

        if (element is ComboBox comboBox)
        {
            var automationName = AutomationProperties.GetName(comboBox);
            if (string.IsNullOrEmpty(automationName))
            {
                result.Issues.Add(new AccessibilityIssue
                {
                    ElementType = element.GetType().Name,
                    ElementName = element.Name,
                    Severity = IssueSeverity.Warning,
                    Message = "ComboBox is missing an accessibility name",
                    WcagGuideline = "3.3.2 Labels or Instructions",
                    Remediation = "Add AutomationProperties.Name to the ComboBox"
                });
            }
        }
    }

    private void CheckKeyboardAccessibility(Control element, AccessibilityAuditResult result)
    {
        if (element is Button button && !button.Focusable)
        {
            result.Issues.Add(new AccessibilityIssue
            {
                ElementType = element.GetType().Name,
                ElementName = element.Name,
                Severity = IssueSeverity.Error,
                Message = "Button is not focusable, making it inaccessible to keyboard users",
                WcagGuideline = "2.1.1 Keyboard",
                Remediation = "Ensure Focusable is true for interactive elements"
            });
        }

        // Check for elements that handle click but not keyboard
        // Check for interactive elements that might not have keyboard support
        if (element is InputElement input && input is not Button && input is not TextBox && input.Focusable)
        {
            // Elements with custom input handling should ensure keyboard accessibility
            // We flag focusable non-standard elements as potential keyboard accessibility issues
            result.Issues.Add(new AccessibilityIssue
            {
                ElementType = element.GetType().Name,
                ElementName = element.Name,
                Severity = IssueSeverity.Suggestion,
                Message = "Custom interactive element may not be accessible via keyboard",
                WcagGuideline = "2.1.1 Keyboard",
                Remediation = "Ensure keyboard equivalents exist for all interactive elements"
            });
        }
    }

    private void CheckFocusOrder(Control element, AccessibilityAuditResult result)
    {
        // Check for negative tab index (removed from tab order)
        if (element.TabIndex < 0 && element.Focusable)
        {
            result.Issues.Add(new AccessibilityIssue
            {
                ElementType = element.GetType().Name,
                ElementName = element.Name,
                Severity = IssueSeverity.Suggestion,
                Message = "Element has negative TabIndex and is removed from tab order",
                WcagGuideline = "2.4.3 Focus Order",
                Remediation = "Consider whether this element should be keyboard accessible"
            });
        }

        // Check for duplicate tab indices
        if (element.TabIndex > 0)
        {
            var parent = element.Parent as Control;
            if (parent != null)
            {
                var siblings = parent.GetVisualChildren().OfType<Control>();
                var duplicates = siblings.Where(s => s != element && s.TabIndex == element.TabIndex).ToList();
                
                if (duplicates.Any())
                {
                    result.Issues.Add(new AccessibilityIssue
                    {
                        ElementType = element.GetType().Name,
                        ElementName = element.Name,
                        Severity = IssueSeverity.Warning,
                        Message = $"Duplicate TabIndex ({element.TabIndex}) with {duplicates.Count} other element(s)",
                        WcagGuideline = "2.4.3 Focus Order",
                        Remediation = "Use unique TabIndex values or let the system calculate order automatically"
                    });
                }
            }
        }
    }

    private void CheckAriaViolations(Control element, AccessibilityAuditResult result)
    {
        var automationId = AutomationProperties.GetAutomationId(element);
        
        // Check for empty automation ID
        if (automationId != null && string.IsNullOrWhiteSpace(automationId))
        {
            result.Issues.Add(new AccessibilityIssue
            {
                ElementType = element.GetType().Name,
                ElementName = element.Name,
                Severity = IssueSeverity.Suggestion,
                Message = "Element has empty AutomationProperties.AutomationId",
                WcagGuideline = "4.1.2 Name, Role, Value",
                Remediation = "Provide a meaningful automation ID or remove the property"
            });
        }

        // Check for live regions without appropriate announcements
        var ariaLive = element.GetValue(AccessibilityService.AriaLiveProperty);
        if (ariaLive is AriaLiveMode liveMode && liveMode != AriaLiveMode.Off)
        {
            if (string.IsNullOrEmpty(AutomationProperties.GetName(element)))
            {
                result.Issues.Add(new AccessibilityIssue
                {
                    ElementType = element.GetType().Name,
                    ElementName = element.Name,
                    Severity = IssueSeverity.Warning,
                    Message = "ARIA live region is missing an accessible name",
                    WcagGuideline = "4.1.3 Status Messages",
                    Remediation = "Add AutomationProperties.Name to live regions"
                });
            }
        }
    }

    private async Task CheckKeyboardTrapsAsync(Control element, AccessibilityAuditResult result)
    {
        // This is a simplified check - full keyboard trap detection would require
        // actually navigating through the UI with simulated keyboard input
        
        if (element is TextBox textBox && textBox.AcceptsReturn)
        {
            // Multi-line text boxes that accept Enter might trap focus
            // unless there's a way to exit (like Tab or Ctrl+Enter)
            result.Issues.Add(new AccessibilityIssue
            {
                ElementType = element.GetType().Name,
                ElementName = element.Name,
                Severity = IssueSeverity.Suggestion,
                Message = "Multi-line TextBox may trap keyboard focus. Ensure Tab or Ctrl+Enter can exit the field.",
                WcagGuideline = "2.1.2 No Keyboard Trap",
                Remediation = "Provide a way to exit multi-line text fields with keyboard only"
            });
        }

        await Task.CompletedTask;
    }

    #endregion

    #region Helper Methods

    private Color GetBackgroundColor(Control element)
    {
        var current = element;
        while (current != null)
        {
            if (current is TemplatedControl tc && tc.Background is ISolidColorBrush brush1)
            {
                return brush1.Color;
            }
            if (current is Border border && border.Background is ISolidColorBrush brush2)
            {
                return brush2.Color;
            }
            if (current is Panel panel && panel.Background is ISolidColorBrush brush3)
            {
                return brush3.Color;
            }
            if (current is ContentControl cc && cc.Background is ISolidColorBrush brush4)
            {
                return brush4.Color;
            }
            current = current.Parent as Control;
        }
        return Colors.White; // Default assumption
    }

    private Color GetColorFromBrush(IBrush brush)
    {
        if (brush is ISolidColorBrush solidBrush)
        {
            return solidBrush.Color;
        }
        return Colors.Black;
    }

    private void CalculateScore(AccessibilityAuditResult result)
    {
        if (result.Issues.Count == 0)
        {
            result.ComplianceScore = 100;
            return;
        }

        // Weight issues by severity
        var errorWeight = result.Issues.Count(i => i.Severity == IssueSeverity.Error) * 10;
        var warningWeight = result.Issues.Count(i => i.Severity == IssueSeverity.Warning) * 5;
        var suggestionWeight = result.Issues.Count(i => i.Severity == IssueSeverity.Suggestion) * 1;

        var totalWeight = errorWeight + warningWeight + suggestionWeight;
        var maxWeight = result.TotalElements * 2; // Assume average 2 points per element

        result.ComplianceScore = Math.Max(0, 100 - (totalWeight * 100.0 / maxWeight));
    }

    #endregion

    #region Export Methods

    private string ExportToJson(AccessibilityAuditResult result)
    {
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private string ExportToHtml(AccessibilityAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><title>Accessibility Audit Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: sans-serif; margin: 40px; }");
        sb.AppendLine(".error { color: #dc2626; }");
        sb.AppendLine(".warning { color: #ea580c; }");
        sb.AppendLine(".suggestion { color: #2563eb; }");
        sb.AppendLine(".score { font-size: 48px; font-weight: bold; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
        sb.AppendLine("th, td { border: 1px solid #ccc; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background: #f3f4f6; }");
        sb.AppendLine("</style></head><body>");
        
        sb.AppendLine("<h1>Accessibility Audit Report</h1>");
        sb.AppendLine($"<p>Audit Date: {result.AuditDate:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine($"<p class=\"score\">Score: {result.ComplianceScore:F1}%</p>");
        sb.AppendLine($"<p>Total Elements: {result.TotalElements}</p>");
        sb.AppendLine($"<p>Issues Found: {result.Issues.Count}</p>");
        
        sb.AppendLine("<h2>Issues</h2><table>");
        sb.AppendLine("<tr><th>Severity</th><th>Element</th><th>Message</th><th>WCAG</th><th>Remediation</th></tr>");
        
        foreach (var issue in result.Issues.OrderByDescending(i => i.Severity))
        {
            var cssClass = issue.Severity.ToString().ToLowerInvariant();
            sb.AppendLine($"<tr class=\"{cssClass}\">");
            sb.AppendLine($"<td>{issue.Severity}</td>");
            sb.AppendLine($"<td>{issue.ElementType}</td>");
            sb.AppendLine($"<td>{issue.Message}</td>");
            sb.AppendLine($"<td>{issue.WcagGuideline}</td>");
            sb.AppendLine($"<td>{issue.Remediation}</td>");
            sb.AppendLine("</tr>");
        }
        
        sb.AppendLine("</table></body></html>");
        return sb.ToString();
    }

    private string ExportToMarkdown(AccessibilityAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Accessibility Audit Report");
        sb.AppendLine();
        sb.AppendLine($"**Audit Date:** {result.AuditDate:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Compliance Score:** {result.ComplianceScore:F1}%");
        sb.AppendLine($"**Total Elements:** {result.TotalElements}");
        sb.AppendLine($"**Issues Found:** {result.Issues.Count}");
        sb.AppendLine();
        
        sb.AppendLine("## Issues");
        sb.AppendLine();
        sb.AppendLine("| Severity | Element | Message | WCAG | Remediation |");
        sb.AppendLine("|----------|---------|---------|------|-------------|");
        
        foreach (var issue in result.Issues.OrderByDescending(i => i.Severity))
        {
            sb.AppendLine($"| {issue.Severity} | {issue.ElementType} | {issue.Message} | {issue.WcagGuideline} | {issue.Remediation} |");
        }
        
        return sb.ToString();
    }

    private string ExportToText(AccessibilityAuditResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ACCESSIBILITY AUDIT REPORT");
        sb.AppendLine("==========================");
        sb.AppendLine();
        sb.AppendLine($"Audit Date: {result.AuditDate:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Compliance Score: {result.ComplianceScore:F1}%");
        sb.AppendLine($"Total Elements: {result.TotalElements}");
        sb.AppendLine($"Issues Found: {result.Issues.Count}");
        sb.AppendLine();
        
        if (result.Issues.Count > 0)
        {
            sb.AppendLine("ISSUES:");
            sb.AppendLine("------");
            
            foreach (var issue in result.Issues.OrderByDescending(i => i.Severity))
            {
                sb.AppendLine($"[{issue.Severity}] {issue.ElementType}: {issue.Message}");
                sb.AppendLine($"  WCAG: {issue.WcagGuideline}");
                sb.AppendLine($"  Remediation: {issue.Remediation}");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }

    #endregion
}

/// <summary>
/// Results of an accessibility audit.
/// </summary>
public class AccessibilityAuditResult
{
    public DateTime AuditDate { get; set; }
    public int TotalElements { get; set; }
    public double ComplianceScore { get; set; }
    public List<AccessibilityIssue> Issues { get; set; } = new();

    public int ErrorCount => Issues.Count(i => i.Severity == IssueSeverity.Error);
    public int WarningCount => Issues.Count(i => i.Severity == IssueSeverity.Warning);
    public int SuggestionCount => Issues.Count(i => i.Severity == IssueSeverity.Suggestion);
}

/// <summary>
/// Individual accessibility issue.
/// </summary>
public class AccessibilityIssue
{
    public string ElementType { get; set; } = string.Empty;
    public string? ElementName { get; set; }
    public IssueSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string WcagGuideline { get; set; } = string.Empty;
    public string Remediation { get; set; } = string.Empty;
}

/// <summary>
/// Issue severity levels.
/// </summary>
public enum IssueSeverity
{
    Suggestion,
    Warning,
    Error
}
