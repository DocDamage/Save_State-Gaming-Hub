using Microsoft.Extensions.DependencyInjection;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace SaveState.Infrastructure.OpenApi;

/// <summary>
/// Configures OpenAPI document generation for the SaveStateReborn API.
/// </summary>
public static class OpenApiConfiguration
{
    /// <summary>
    /// Configures OpenAPI document generation with NSwag.
    /// </summary>
    public static void ConfigureOpenApiDocument(IServiceCollection services)
    {
        services.AddOpenApiDocument(config =>
        {
            config.DocumentName = "v2.5";
            config.Title = "SaveStateReborn API";
            config.Description = "Gaming management platform API with memory intelligence, cloud sync, and MUGEN integration. Version 2.5.1";
            config.Version = "2.5.1";

            // Add security definition
            config.AddSecurity("Bearer", new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            });

            // Add security requirement
            config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));

            // Add CQRS document processor
            config.DocumentProcessors.Add(new DocumentProcessors.CqrsDocumentProcessor());
        });
    }
}
