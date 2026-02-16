using System.Security.Claims;
using System.Text.Encodings.Web;
using BattleQuest.API.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BattleQuest.API.Tests.Security;

/// <summary>
/// Testes unitários para o ApiTokenAuthenticationHandler.
/// Valida cenários de autenticação com token válido, inválido, ausente e respostas 401.
/// </summary>
public sealed class ApiTokenAuthenticationHandlerTests
{
	private readonly Mock<IOptionsMonitor<ApiTokenOptions>> _optionsMonitorMock;
	private readonly Mock<ILoggerFactory> _loggerFactoryMock;
	private readonly Mock<ILogger<ApiTokenAuthenticationHandler>> _loggerMock;
	private readonly UrlEncoder _urlEncoder;

	public ApiTokenAuthenticationHandlerTests()
	{
		_optionsMonitorMock = new Mock<IOptionsMonitor<ApiTokenOptions>>();
		_loggerFactoryMock = new Mock<ILoggerFactory>();
		_loggerMock = new Mock<ILogger<ApiTokenAuthenticationHandler>>();
		_urlEncoder = UrlEncoder.Default;

		_loggerFactoryMock
			.Setup(x => x.CreateLogger(It.IsAny<string>()))
			.Returns(_loggerMock.Object);
	}

	/// <summary>
	/// Valida que um token válido resulta em autenticação bem-sucedida.
	/// </summary>
	[Fact]
	public async Task HandleAuthenticateAsync_ValidToken_ShouldReturnSuccess()
	{
		// Arrange
		const string validToken = "NANDATE-dev-token";
		var options = new ApiTokenOptions
		{
			Token = validToken,
			HeaderName = "X-API-TOKEN"
		};

		_optionsMonitorMock
			.Setup(x => x.Get(ApiTokenDefaults.Scheme))
			.Returns(options);

		var context = CreateHttpContext();
		context.Request.Headers["X-API-TOKEN"] = validToken;

		var handler = CreateHandler(context, options);
		await InitializeHandlerAsync(handler, context);

		// Act
		var result = await handler.AuthenticateAsync();

		// Assert
		result.Succeeded.Should().BeTrue();
		result.Principal.Should().NotBeNull();
		result.Principal!.Identity.Should().NotBeNull();
		result.Principal.Identity!.IsAuthenticated.Should().BeTrue();
		result.Principal.Identity.AuthenticationType.Should().Be(ApiTokenDefaults.Scheme);
		result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("api-token");
		result.Principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be("ApiTokenClient");
	}

	/// <summary>
	/// Valida que um token inválido resulta em falha de autenticação.
	/// </summary>
	[Fact]
	public async Task HandleAuthenticateAsync_InvalidToken_ShouldReturnFail()
	{
		// Arrange
		const string validToken = "NANDATE-dev-token";
		const string invalidToken = "token-invalido";

		var options = new ApiTokenOptions
		{
			Token = validToken,
			HeaderName = "X-API-TOKEN"
		};

		_optionsMonitorMock
			.Setup(x => x.Get(ApiTokenDefaults.Scheme))
			.Returns(options);

		var context = CreateHttpContext();
		context.Request.Headers["X-API-TOKEN"] = invalidToken;

		var handler = CreateHandler(context, options);
		await InitializeHandlerAsync(handler, context);

		// Act
		var result = await handler.AuthenticateAsync();

		// Assert
		result.Succeeded.Should().BeFalse();
		result.Failure.Should().NotBeNull();
		result.Failure!.Message.Should().Contain("inválido");
	}

	/// <summary>
	/// Valida que a ausência do header de token resulta em falha de autenticação.
	/// </summary>
	[Fact]
	public async Task HandleAuthenticateAsync_MissingToken_ShouldReturnFail()
	{
		// Arrange
		var options = new ApiTokenOptions
		{
			Token = "NANDATE-dev-token",
			HeaderName = "X-API-TOKEN"
		};

		_optionsMonitorMock
			.Setup(x => x.Get(ApiTokenDefaults.Scheme))
			.Returns(options);

		var context = CreateHttpContext(); // Sem header

		var handler = CreateHandler(context, options);
		await InitializeHandlerAsync(handler, context);

		// Act
		var result = await handler.AuthenticateAsync();

		// Assert
		result.Succeeded.Should().BeFalse();
		result.Failure.Should().NotBeNull();
		result.Failure!.Message.Should().Contain("ausente");
	}

	/// <summary>
	/// Valida que um token vazio (apenas espaços) resulta em falha de autenticação.
	/// </summary>
	[Fact]
	public async Task HandleAuthenticateAsync_EmptyToken_ShouldReturnFail()
	{
		// Arrange
		var options = new ApiTokenOptions
		{
			Token = "NANDATE-dev-token",
			HeaderName = "X-API-TOKEN"
		};

		_optionsMonitorMock
			.Setup(x => x.Get(ApiTokenDefaults.Scheme))
			.Returns(options);

		var context = CreateHttpContext();
		context.Request.Headers["X-API-TOKEN"] = "   ";

		var handler = CreateHandler(context, options);
		await InitializeHandlerAsync(handler, context);

		// Act
		var result = await handler.AuthenticateAsync();

		// Assert
		result.Succeeded.Should().BeFalse();
		result.Failure.Should().NotBeNull();
		result.Failure!.Message.Should().Contain("vazio");
	}

	/// <summary>
	/// Valida que o challenge retorna status 401 com ProblemDetails JSON estruturado.
	/// </summary>
	[Fact]
	public async Task HandleChallengeAsync_ShouldReturn401WithProblemDetails()
	{
		// Arrange
		var options = new ApiTokenOptions
		{
			Token = "NANDATE-dev-token",
			HeaderName = "X-API-TOKEN"
		};

		_optionsMonitorMock
			.Setup(x => x.Get(ApiTokenDefaults.Scheme))
			.Returns(options);

		var context = CreateHttpContext();
		var handler = CreateHandler(context, options);
		await InitializeHandlerAsync(handler, context);

		// Simula falha de autenticação
		await handler.AuthenticateAsync();

		// Act
		await handler.ChallengeAsync(new AuthenticationProperties());

		// Assert
		context.Response.StatusCode.Should().Be(401);
		context.Response.ContentType.Should().Be("application/problem+json");
		context.Response.Headers.Should().ContainKey("WWW-Authenticate");
		context.Response.Headers["WWW-Authenticate"].ToString().Should().Contain(ApiTokenDefaults.Scheme);
		context.Response.Headers["WWW-Authenticate"].ToString().Should().Contain("realm=");
	}

	/// <summary>
	/// Valida que o challenge inclui o header WWW-Authenticate conforme RFC 7235.
	/// </summary>
	[Fact]
	public async Task HandleChallengeAsync_ShouldIncludeWWWAuthenticateHeader()
	{
		// Arrange
		var options = new ApiTokenOptions
		{
			Token = "NANDATE-dev-token",
			HeaderName = "X-API-TOKEN"
		};

		_optionsMonitorMock
			.Setup(x => x.Get(ApiTokenDefaults.Scheme))
			.Returns(options);

		var context = CreateHttpContext();
		var handler = CreateHandler(context, options);
		await InitializeHandlerAsync(handler, context);

		await handler.AuthenticateAsync();

		// Act
		await handler.ChallengeAsync(new AuthenticationProperties());

		// Assert
		context.Response.Headers["WWW-Authenticate"].ToString()
			.Should().Be("ApiToken realm=\"BattleQuest API\"");
	}

	private DefaultHttpContext CreateHttpContext()
	{
		var context = new DefaultHttpContext();
		context.Request.Scheme = "http";
		context.Request.Host = new HostString("localhost");
		context.Request.Path = "/api/events";
		context.Response.Body = new MemoryStream();
		return context;
	}

	private ApiTokenAuthenticationHandler CreateHandler(HttpContext context, ApiTokenOptions options)
	{
		_optionsMonitorMock
			.Setup(x => x.Get(ApiTokenDefaults.Scheme))
			.Returns(options);

		return new ApiTokenAuthenticationHandler(
			_optionsMonitorMock.Object,
			_loggerFactoryMock.Object,
			_urlEncoder);
	}

	private static async Task InitializeHandlerAsync(ApiTokenAuthenticationHandler handler, HttpContext context)
	{
		await handler.InitializeAsync(
			new AuthenticationScheme(ApiTokenDefaults.Scheme, null, typeof(ApiTokenAuthenticationHandler)),
			context);
	}
}
