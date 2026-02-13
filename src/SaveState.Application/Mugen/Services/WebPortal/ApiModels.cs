namespace SaveState.Application.Mugen.Services.WebPortal;

/// <summary>
/// API endpoint definition.
/// </summary>
public class ApiEndpoint
{
    public string EndpointId { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string Method { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ApiEndpointType Type { get; set; }
    public IReadOnlyList<string> RequiredScopes { get; set; } = default!;
    public int RateLimitRequests { get; set; }
    public TimeSpan RateLimitWindow { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
}

/// <summary>
/// API request tracking.
/// </summary>
public class ApiRequest
{
    public string RequestId { get; set; } = default!;
    public string EndpointId { get; set; } = default!;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public int StatusCode { get; set; }
    public TimeSpan Duration { get; set; }
    public long RequestSize { get; set; }
    public long ResponseSize { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// API rate limit status.
/// </summary>
public class RateLimitStatus
{
    public string EndpointId { get; set; } = default!;
    public string? UserId { get; set; }
    public int Limit { get; set; }
    public int Remaining { get; set; }
    public DateTime ResetAt { get; set; }
    public TimeSpan Window { get; set; }
}

/// <summary>
/// API documentation entry.
/// </summary>
public class ApiDocumentation
{
    public string EndpointId { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string Method { get; set; } = default!;
    public string Summary { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IReadOnlyList<ApiParameter> Parameters { get; set; } = default!;
    public IReadOnlyList<ApiResponse> Responses { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// API parameter definition.
/// </summary>
public class ApiParameter
{
    public string Name { get; set; } = default!;
    public string In { get; set; } = default!; // query, path, header, body
    public string Type { get; set; } = default!;
    public bool Required { get; set; }
    public string Description { get; set; } = default!;
    public object? DefaultValue { get; set; }
}

/// <summary>
/// API response definition.
/// </summary>
public class ApiResponse
{
    public int StatusCode { get; set; }
    public string Description { get; set; } = default!;
    public string? ContentType { get; set; }
    public string? Schema { get; set; }
}
