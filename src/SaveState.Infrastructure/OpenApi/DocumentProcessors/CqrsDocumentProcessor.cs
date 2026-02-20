using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using System.Reflection;
using MediatR;

namespace SaveState.Infrastructure.OpenApi.DocumentProcessors;

/// <summary>
/// Custom document processor to include CQRS commands and queries in OpenAPI documentation.
/// </summary>
public class CqrsDocumentProcessor : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        // Find all CQRS types from Application assembly
        var assembly = typeof(SaveState.Application.AssemblyMarker).Assembly;
        
        var commandTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Where(t => t.Name.EndsWith("Command"))
            .Take(100) // Limit to prevent memory issues
            .ToList();

        var queryTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => 
                i.IsGenericType && 
                i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Where(t => t.Name.EndsWith("Query"))
            .Take(100) // Limit to prevent memory issues
            .ToList();

        // Add schemas for commands
        foreach (var commandType in commandTypes)
        {
            AddTypeSchema(context, commandType, "Command");
        }

        // Add schemas for queries
        foreach (var queryType in queryTypes)
        {
            AddTypeSchema(context, queryType, "Query");
        }

        // Add CQRS operations to document
        AddCqrsOperations(context, commandTypes, queryTypes);
    }

    private void AddTypeSchema(DocumentProcessorContext context, Type type, string typeCategory)
    {
        var schemaName = type.Name;
        
        if (!context.Document.Components.Schemas.ContainsKey(schemaName))
        {
            var schema = CreateSchemaForType(type, typeCategory);
            context.Document.Components.Schemas[schemaName] = schema;
        }
    }

    private JsonSchema CreateSchemaForType(Type type, string typeCategory)
    {
        var schema = new JsonSchema
        {
            Title = type.Name,
            Description = $"{typeCategory}: {type.Name}",
            Type = JsonObjectType.Object
        };

        // Add properties from type
        foreach (var prop in type.GetProperties().Where(p => p.CanRead))
        {
            var propertySchema = CreatePropertySchema(prop.PropertyType);
            
            // Check for OpenApiExampleAttribute
            var exampleAttr = prop.GetCustomAttribute<Core.OpenApi.Attributes.OpenApiExampleAttribute>();
            if (exampleAttr?.Description != null)
            {
                propertySchema.Description = exampleAttr.Description;
            }
            else
            {
                propertySchema.Description = $"Property: {prop.Name}";
            }

            schema.Properties[prop.Name] = propertySchema;
        }

        return schema;
    }

    private JsonSchemaProperty CreatePropertySchema(Type propertyType)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        
        var schema = new JsonSchemaProperty();

        if (underlyingType == typeof(string))
        {
            schema.Type = JsonObjectType.String;
        }
        else if (underlyingType == typeof(int) || underlyingType == typeof(long))
        {
            schema.Type = JsonObjectType.Integer;
        }
        else if (underlyingType == typeof(double) || underlyingType == typeof(float) || underlyingType == typeof(decimal))
        {
            schema.Type = JsonObjectType.Number;
        }
        else if (underlyingType == typeof(bool))
        {
            schema.Type = JsonObjectType.Boolean;
        }
        else if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
        {
            schema.Type = JsonObjectType.String;
            schema.Format = "date-time";
        }
        else if (underlyingType == typeof(Guid))
        {
            schema.Type = JsonObjectType.String;
            schema.Format = "uuid";
        }
        else
        {
            schema.Type = JsonObjectType.Object;
        }

        return schema;
    }

    private void AddCqrsOperations(DocumentProcessorContext context, List<Type> commands, List<Type> queries)
    {
        // Add path for command execution
        var commandPath = new OpenApiPathItem();
        var commandOperation = new OpenApiOperation
        {
            Summary = "Execute Command",
            Description = "Execute a CQRS command. Supports: " + string.Join(", ", commands.Take(10).Select(c => c.Name)),
            Tags = { "Commands" }
        };
        
        commandOperation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = new JsonSchema
                    {
                        Type = JsonObjectType.Object,
                        Description = "Command to execute"
                    }
                }
            }
        };
        
        commandPath["post"] = commandOperation;
        
        if (!context.Document.Paths.ContainsKey("/api/commands"))
        {
            context.Document.Paths["/api/commands"] = commandPath;
        }

        // Add path for query execution
        var queryPath = new OpenApiPathItem();
        var queryOperation = new OpenApiOperation
        {
            Summary = "Execute Query",
            Description = "Execute a CQRS query. Supports: " + string.Join(", ", queries.Take(10).Select(q => q.Name)),
            Tags = { "Queries" }
        };
        
        queryPath["get"] = queryOperation;
        
        if (!context.Document.Paths.ContainsKey("/api/queries"))
        {
            context.Document.Paths["/api/queries"] = queryPath;
        }
    }
}
