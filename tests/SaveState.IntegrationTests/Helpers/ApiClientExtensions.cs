using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SaveState.IntegrationTests.Helpers;

/// <summary>
/// Extension methods for HttpClient to simplify API testing.
/// </summary>
public static class ApiClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Posts a JSON request and deserializes the response.
    /// </summary>
    public static async Task<T?> PostAsJsonAsync<T>(this HttpClient client, string url, object request)
    {
        var response = await client.PostAsJsonAsync(url, request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    /// <summary>
    /// Posts a JSON request and returns the response.
    /// </summary>
    public static async Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient client, string url, object request)
    {
        return await client.PostAsJsonAsync(url, request, JsonOptions);
    }

    /// <summary>
    /// Puts a JSON request and deserializes the response.
    /// </summary>
    public static async Task<T?> PutAsJsonAsync<T>(this HttpClient client, string url, object request)
    {
        var response = await client.PutAsJsonAsync(url, request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    /// <summary>
    /// Puts a JSON request and returns the response.
    /// </summary>
    public static async Task<HttpResponseMessage> PutAsJsonAsync(this HttpClient client, string url, object request)
    {
        return await client.PutAsJsonAsync(url, request, JsonOptions);
    }

    /// <summary>
    /// Patches a JSON request and deserializes the response.
    /// </summary>
    public static async Task<T?> PatchAsJsonAsync<T>(this HttpClient client, string url, object request)
    {
        var content = JsonContent.Create(request, options: JsonOptions);
        var response = await client.PatchAsync(url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    /// <summary>
    /// Gets a response and deserializes it.
    /// </summary>
    public static async Task<T?> GetFromJsonAsync<T>(this HttpClient client, string url)
    {
        return await client.GetFromJsonAsync<T>(url, JsonOptions);
    }

    /// <summary>
    /// Deletes a resource and ensures success.
    /// </summary>
    public static async Task DeleteAndEnsureSuccessAsync(this HttpClient client, string url)
    {
        var response = await client.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Asserts that the response has the expected status code.
    /// </summary>
    public static void ShouldHaveStatusCode(this HttpResponseMessage response, System.Net.HttpStatusCode expectedStatusCode)
    {
        response.StatusCode.Should().Be(expectedStatusCode);
    }

    /// <summary>
    /// Gets the response content as a string and asserts it's not null or empty.
    /// </summary>
    public static async Task<string> GetContentAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
        return content;
    }

    /// <summary>
    /// Deserializes the response content to the specified type.
    /// </summary>
    public static async Task<T?> DeserializeContentAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }
}
