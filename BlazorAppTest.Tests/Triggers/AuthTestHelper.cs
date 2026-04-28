using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BlazorAppTest.Tests.Triggers;

public static class AuthTestHelper
{
    public static IServiceProvider CreateServiceProviderWithAuth(string userName)
    {
        var services = new ServiceCollection();
        var authMock = new Mock<AuthenticationStateProvider>();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], "TestAuth"));

        authMock.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(user));

        services.AddSingleton(authMock.Object);
        return services.BuildServiceProvider();
    }
}