using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace SaveState.Infrastructure.OpenApi;

/// <summary>
/// Generates OpenAPI-style documentation from XML documentation comments.
/// </summary>
public class OpenApiDocumentationService
{
    private readonly ILogger<OpenApiDocumentationService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, XDocument> _xmlDocs = new();

    public OpenApiDocumentationService(ILogger<OpenApiDocumentationService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        LoadXmlDocumentation();
    }

    /// <summary>
    /// Generates API documentation for all services in an assembly.
    /// </summary>
    public ApiDocumentation GenerateDocumentation(Assembly assembly)
    {
        var documentation = new ApiDocumentation
        {
            Title = "SaveStateReborn API",
            Version = GetAssemblyVersion(assembly),
            Description = "Gaming management platform API",
            GeneratedAt = _timeProvider.UtcNow
        };

        var serviceTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Service") || t.Name.EndsWith("Provider"))
            .ToList();

        foreach (var serviceType in serviceTypes)
        {
            var serviceDoc = GenerateServiceDocumentation(serviceType);
            if (serviceDoc.Endpoints.Any())
            {
                documentation.Services.Add(serviceDoc);
            }
        }

        return documentation;
    }

    private ServiceDocumentation GenerateServiceDocumentation(Type serviceType)
    {
        var serviceDoc = new ServiceDocumentation
        {
            Name = serviceType.Name,
            Namespace = serviceType.Namespace ?? string.Empty
        };

        // Get XML summary for type
        serviceDoc.Description = GetTypeSummary(serviceType);

        var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName) // Exclude property accessors
            .Where(m => m.DeclaringType == serviceType)
            .ToList();

        foreach (var method in methods)
        {
            var endpointDoc = GenerateEndpointDocumentation(method);
            serviceDoc.Endpoints.Add(endpointDoc);
        }

        return serviceDoc;
    }

    private EndpointDocumentation GenerateEndpointDocumentation(MethodInfo method)
    {
        var doc = new EndpointDocumentation
        {
            Name = method.Name,
            ReturnType = FormatTypeName(method.ReturnType)
        };

        // Get XML documentation
        var xmlKey = $"M:{method.DeclaringType?.FullName}.{method.Name}";
        var xmlDoc = GetXmlDocumentation(xmlKey);

        if (!string.IsNullOrEmpty(xmlDoc))
        {
            var xdoc = XDocument.Parse($"<root>{xmlDoc}</root>");
            doc.Description = xdoc.Descendants("summary").FirstOrDefault()?.Value.Trim();
            doc.Remarks = xdoc.Descendants("remarks").FirstOrDefault()?.Value.Trim();
            doc.Example = xdoc.Descendants("example").FirstOrDefault()?.Value.Trim();
        }

        // Parameters
        var parameters = method.GetParameters();
        foreach (var param in parameters)
        {
            var paramDoc = new ParameterDocumentation
            {
                Name = param.Name ?? string.Empty,
                Type = FormatTypeName(param.ParameterType),
                IsOptional = param.IsOptional,
                DefaultValue = param.DefaultValue?.ToString()
            };

            doc.Parameters.Add(paramDoc);
        }

        // Determine if it uses Result pattern
        doc.UsesResultPattern = method.ReturnType.Name.Contains("Result") ||
                                (method.ReturnType.IsGenericType &&
                                 method.ReturnType.GetGenericTypeDefinition().Name.Contains("Result"));

        return doc;
    }

    private string FormatTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericArgs = type.GetGenericArguments();
            var genericName = type.Name.Split('`')[0];
            var argNames = string.Join(", ", genericArgs.Select(FormatTypeName));
            return $"{genericName}<{argNames}>";
        }

        if (type == typeof(void)) return "void";
        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "int";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(long)) return "long";
        if (type == typeof(double)) return "double";
        if (type == typeof(DateTime)) return "DateTime";
        if (type == typeof(Guid)) return "Guid";
        if (type == typeof(Task)) return "Task";

        return type.Name;
    }

    private string? GetTypeSummary(Type type)
    {
        var xmlKey = $"T:{type.FullName}";
        var xml = GetXmlDocumentation(xmlKey);
        
        if (string.IsNullOrEmpty(xml)) return null;

        try
        {
            var xdoc = XDocument.Parse($"<root>{xml}</root>");
            return xdoc.Descendants("summary").FirstOrDefault()?.Value.Trim();
        }
        catch
        {
            return null;
        }
    }

    private string? GetXmlDocumentation(string key)
    {
        foreach (var xmlDoc in _xmlDocs.Values)
        {
            var member = xmlDoc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == key);
            
            if (member != null)
            {
                return member.ToString();
            }
        }
        
        return null;
    }

    private void LoadXmlDocumentation()
    {
        var assemblies = new[]
        {
            "SaveState.Core",
            "SaveState.Application",
            "SaveState.Infrastructure"
        };

        foreach (var assemblyName in assemblies)
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);
                var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
                
                if (File.Exists(xmlPath))
                {
                    _xmlDocs[assemblyName] = XDocument.Load(xmlPath);
                    _logger.LogDebug("Loaded XML documentation from {Path}", xmlPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load XML documentation for {Assembly}", assemblyName);
            }
        }
    }

    private static string GetAssemblyVersion(Assembly assembly)
    {
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    /// <summary>
    /// Serializes documentation to JSON.
    /// </summary>
    public string ToJson(ApiDocumentation documentation)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        
        return JsonSerializer.Serialize(documentation, options);
    }

    /// <summary>
    /// Generates Markdown documentation.
    /// </summary>
    public string ToMarkdown(ApiDocumentation documentation)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"# {documentation.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Version:** {documentation.Version}");
        sb.AppendLine();
        sb.AppendLine($"**Description:** {documentation.Description}");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {documentation.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        foreach (var service in documentation.Services)
        {
            sb.AppendLine($"## {service.Name}");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(service.Description))
            {
                sb.AppendLine(service.Description);
                sb.AppendLine();
            }

            foreach (var endpoint in service.Endpoints)
            {
                sb.AppendLine($"### {endpoint.Name}");
                sb.AppendLine();
                
                if (!string.IsNullOrEmpty(endpoint.Description))
                {
                    sb.AppendLine(endpoint.Description);
                    sb.AppendLine();
                }

                if (endpoint.Parameters.Any())
                {
                    sb.AppendLine("**Parameters:**");
                    sb.AppendLine();
                    sb.AppendLine("| Name | Type | Optional | Default |");
                    sb.AppendLine("|------|------|----------|----------|");
                    
                    foreach (var param in endpoint.Parameters)
                    {
                        var defaultValue = param.DefaultValue ?? "-";
                        sb.AppendLine($"| {param.Name} | {param.Type} | {param.IsOptional} | {defaultValue} |");
                    }
                    
                    sb.AppendLine();
                }

                sb.AppendLine($"**Returns:** {endpoint.ReturnType}");
                
                if (endpoint.UsesResultPattern)
                {
                    sb.AppendLine();
                    sb.AppendLine("> ℹ️ Uses Result<T> pattern for error handling");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// API documentation model.
/// </summary>
public class ApiDocumentation
{
    public string Title { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<ServiceDocumentation> Services { get; set; } = new();
}

/// <summary>
/// Service documentation model.
/// </summary>
public class ServiceDocumentation
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<EndpointDocumentation> Endpoints { get; set; } = new();
}

/// <summary>
/// Endpoint documentation model.
/// </summary>
public class EndpointDocumentation
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Remarks { get; set; }
    public string? Example { get; set; }
    public string ReturnType { get; set; } = string.Empty;
    public bool UsesResultPattern { get; set; }
    public List<ParameterDocumentation> Parameters { get; set; } = new();
}

/// <summary>
/// Parameter documentation model.
/// </summary>
public class ParameterDocumentation
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
}
