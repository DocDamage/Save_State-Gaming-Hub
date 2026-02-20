using Microsoft.Extensions.Logging;
using NSwag;
using NSwag.Generation;

namespace SaveState.Presentation.OpenApi;

/// <summary>
/// Handles OpenAPI/Swagger endpoint requests.
/// </summary>
public class OpenApiEndpoint
{
    private readonly IOpenApiDocumentGenerator _documentGenerator;
    private readonly ILogger<OpenApiEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiEndpoint"/> class.
    /// </summary>
    public OpenApiEndpoint(
        IOpenApiDocumentGenerator documentGenerator,
        ILogger<OpenApiEndpoint> logger)
    {
        _documentGenerator = documentGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Generates the OpenAPI JSON document.
    /// </summary>
    public async Task<string> GenerateOpenApiJsonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Generating OpenAPI JSON document");
            
            var document = await _documentGenerator.GenerateAsync("v2.5");
            var json = document.ToJson();

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate OpenAPI document");
            throw;
        }
    }

    /// <summary>
    /// Gets the Swagger UI HTML.
    /// </summary>
    public string GetSwaggerUiHtml()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>SaveStateReborn API - Swagger UI</title>
    <link rel=""stylesheet"" type=""text/css"" href=""https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"" />
</head>
<body>
    <div id=""swagger-ui""></div>
    <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js""></script>
    <script>
        SwaggerUIBundle({
            url: '/openapi/v2.5.json',
            dom_id: '#swagger-ui',
            presets: [
                SwaggerUIBundle.presets.apis,
                SwaggerUIBundle.presets.standalone
            ]
        });
    </script>
</body>
</html>";
    }

    /// <summary>
    /// Saves the OpenAPI JSON document to a file.
    /// </summary>
    public async Task SaveOpenApiJsonAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var json = await GenerateOpenApiJsonAsync(cancellationToken);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("OpenAPI documentation saved to {FilePath}", filePath);
    }
}
