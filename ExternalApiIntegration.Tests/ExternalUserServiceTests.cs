using ExternalApiIntegration.Configuration;
using ExternalApiIntegration.Models;
using ExternalApiIntegration.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ExternalUserServiceTests
{
    /// <summary>
    /// Creates an ExternalUserService with mocked HttpClient, config, and in-memory cache.
    /// </summary>
    private ExternalUserService CreateService(string jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var apiOptions = new ReqresApiOptions
        {
            BaseUrl = "https://reqres.in/api",
            ApiKey = "reqres-free-v1"
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonResponse)
            });

        var client = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(apiOptions.BaseUrl)
        };

        if (!string.IsNullOrWhiteSpace(apiOptions.ApiKey))
        {
            client.DefaultRequestHeaders.Add("x-api-key", apiOptions.ApiKey);
        }

        var options = Options.Create(apiOptions);
        var cache = new MemoryCache(new MemoryCacheOptions());

        return new ExternalUserService(client, options, cache);
    }

    [Fact]
    //Verifies user is correctly deserialized from a valid JSON response
    public async Task GetUserByIdAsync_Returns_User_When_Found()
    {
        string fake = "{\"data\":{\"id\":1,\"email\":\"a@b.com\",\"first_name\":\"A\",\"last_name\":\"B\",\"avatar\":\"url\"}}";
        var svc = CreateService(fake);
        var user = await svc.GetUserByIdAsync(1);

        Assert.Equal(1, user.Id);
        Assert.Equal("A", user.First_Name);
    }

    [Fact]
    //Confirms that 404 response throws KeyNotFoundException
    public async Task GetUserByIdAsync_Throws_On_NotFound()
    {
        var svc = CreateService("User not found", HttpStatusCode.NotFound);
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetUserByIdAsync(999));
        Assert.Contains("User 999 not found", ex.Message);
    }

    [Fact]
    //Ensures pagination works and all pages are fetched
    public async Task GetAllUsersAsync_Fetches_All_Pages()
    {
        string p1 = "{\"page\":1,\"total_pages\":2,\"data\":[{\"id\":1,\"email\":\"a@b.com\",\"first_name\":\"A\",\"last_name\":\"B\",\"avatar\":\"u\"}]}";
        string p2 = "{\"page\":2,\"total_pages\":2,\"data\":[{\"id\":2,\"email\":\"c@d.com\",\"first_name\":\"C\",\"last_name\":\"D\",\"avatar\":\"v\"}]}";

        var responses = new Queue<HttpResponseMessage>(new[]
        {
            new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(p1) },
            new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(p2) }
        });

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responses.Dequeue());

        var svc = new ExternalUserService(
            new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://reqres.in/api") },
            Options.Create(new ReqresApiOptions { BaseUrl = "https://reqres.in/api" }),
            new MemoryCache(new MemoryCacheOptions()));

        var allUsers = (await svc.GetAllUsersAsync()).ToList();

        Assert.Equal(2, allUsers.Count);
        Assert.Contains(allUsers, u => u.Id == 2);
    }

    [Fact]
    //Ensures that repeated calls use cache instead of hitting API multiple times
    public async Task Caches_User_ById()
    {
        string fake = "{\"data\":{\"id\":3,\"email\":\"x@y.com\",\"first_name\":\"X\",\"last_name\":\"Y\",\"avatar\":\"z\"}}";
        int callCount = 0;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fake)
                };
            });

        var svc = new ExternalUserService(
            new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://reqres.in/api") },
            Options.Create(new ReqresApiOptions { BaseUrl = "https://reqres.in/api" }),
            new MemoryCache(new MemoryCacheOptions()));

        var user1 = await svc.GetUserByIdAsync(3);
        var user2 = await svc.GetUserByIdAsync(3);

        Assert.Equal(1, callCount);
    }
}
