using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Subscriptions;

namespace SaveState.Infrastructure.Tests.Subscriptions;

public sealed class XboxGamePassProviderTests
{
    [Fact]
    public async Task GetGamesAsync_WithProductsObjectPayload_ParsesCatalog()
    {
        const string payload = """
            {
              "Products": [
                {
                  "ProductId": "halo-id",
                  "LocalizedProperties": [
                    {
                      "ProductTitle": "Halo Infinite"
                    }
                  ]
                }
              ]
            }
            """;

        var provider = CreateProvider(payload);

        var result = await provider.GetGamesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(g => g.Title == "Halo Infinite");
    }

    [Fact]
    public async Task GetGamesAsync_WithArrayPayload_ParsesCatalog()
    {
        const string payload = """
            [
              {
                "ProductId": "forza-id",
                "LocalizedProperties": [
                  {
                    "ProductTitle": "Forza Horizon 5"
                  }
                ]
              }
            ]
            """;

        var provider = CreateProvider(payload);

        var result = await provider.GetGamesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(g => g.Title == "Forza Horizon 5");
    }

    private static XboxGamePassProvider CreateProvider(string payload)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var httpClient = new HttpClient(new StubHttpMessageHandler(response));

        return new XboxGamePassProvider(
            NullLogger<XboxGamePassProvider>.Instance,
            httpClient,
            SystemTimeProvider.Instance);
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
