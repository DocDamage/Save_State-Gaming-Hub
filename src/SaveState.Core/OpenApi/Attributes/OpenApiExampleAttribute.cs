namespace SaveState.Core.OpenApi.Attributes;

/// <summary>
/// Specifies an example value for an OpenAPI schema property.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public class OpenApiExampleAttribute : Attribute
{
    /// <summary>
    /// Gets the example value.
    /// </summary>
    public string Example { get; }

    /// <summary>
    /// Gets or sets the description for the property.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiExampleAttribute"/> class.
    /// </summary>
    public OpenApiExampleAttribute(string example)
    {
        Example = example;
    }
}

/// <summary>
/// Specifies an OpenAPI tag for an API operation or schema.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class OpenApiTagAttribute : Attribute
{
    /// <summary>
    /// Gets the tag name.
    /// </summary>
    public string Tag { get; }

    /// <summary>
    /// Gets or sets the tag description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenApiTagAttribute"/> class.
    /// </summary>
    public OpenApiTagAttribute(string tag)
    {
        Tag = tag;
    }
}

/// <summary>
/// Specifies that a class should be excluded from OpenAPI documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OpenApiExcludeAttribute : Attribute
{
}

/// <summary>
/// Specifies additional metadata for OpenAPI schema documentation.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class OpenApiSchemaAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the schema title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the schema description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the schema is deprecated.
    /// </summary>
    public bool Deprecated { get; set; }
}
