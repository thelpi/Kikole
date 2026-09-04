using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KikoleSite.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace KikoleSiteUnitTests.Identity;

/// <summary>
/// Verifie l'appel en k-anonymity (prefixe de 5 caracteres envoye, jamais le mot de passe)
/// et le repli tolerant : une API indisponible ou en erreur ne doit jamais bloquer un
/// changement de mot de passe.
/// </summary>
public class HibpPasswordValidatorTests
{
    private const string Password = "TestPassword123";

    private static (string prefix, string suffix) Sha1Parts(string value)
    {
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(value)));
        return (hash[..5], hash[5..]);
    }

    private static HibpPasswordValidator CreateValidator(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.pwnedpasswords.com/") };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(_ => _.CreateClient(nameof(HibpPasswordValidator))).Returns(httpClient);

        return new HibpPasswordValidator(factory.Object);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        internal StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("API injoignable");
    }

    [Fact]
    public async Task ValidateAsync_WhenTheSuffixIsInTheResponse_Fails()
    {
        var (_, suffix) = Sha1Parts(Password);
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"OTHERSUFFIX0000000000000000000000:3\r\n{suffix}:42\r\n")
        });

        var result = await CreateValidator(handler).ValidateAsync(null!, null!, Password);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == HibpPasswordValidator.PwnedPasswordErrorCode);
    }

    [Fact]
    public async Task ValidateAsync_WhenTheSuffixIsNotInTheResponse_Succeeds()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:1\r\nBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB:2\r\n")
        });

        var result = await CreateValidator(handler).ValidateAsync(null!, null!, Password);

        result.Should().Be(IdentityResult.Success);
    }

    [Fact]
    public async Task ValidateAsync_OnlySendsThePrefixOfTheHash_NeverThePasswordItself()
    {
        var (prefix, _) = Sha1Parts(Password);
        string? requestedPath = null;

        var handler = new StubHandler(request =>
        {
            requestedPath = request.RequestUri!.AbsolutePath;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };
        });

        await CreateValidator(handler).ValidateAsync(null!, null!, Password);

        requestedPath.Should().Be($"/range/{prefix}");
        requestedPath.Should().NotContain(Password);
    }

    [Fact]
    public async Task ValidateAsync_WhenTheApiThrows_FailsOpen()
    {
        var result = await CreateValidator(new ThrowingHandler()).ValidateAsync(null!, null!, Password);

        result.Should().Be(IdentityResult.Success);
    }

    [Fact]
    public async Task ValidateAsync_WhenTheApiReturnsAnErrorStatus_FailsOpen()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await CreateValidator(handler).ValidateAsync(null!, null!, Password);

        result.Should().Be(IdentityResult.Success);
    }

    [Fact]
    public async Task ValidateAsync_WithAnEmptyPassword_Succeeds()
    {
        var result = await CreateValidator(new ThrowingHandler()).ValidateAsync(null!, null!, string.Empty);

        result.Should().Be(IdentityResult.Success);
    }
}
