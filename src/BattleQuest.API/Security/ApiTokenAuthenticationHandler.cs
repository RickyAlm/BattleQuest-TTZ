using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BattleQuest.API.Security;

/// <summary>
/// Handler responsável pela autenticação via token no header HTTP.
/// Valida o token enviado no header configurado (padrão: X-API-TOKEN) 
/// contra o token armazenado nas configurações da aplicação.
/// </summary>
public sealed class ApiTokenAuthenticationHandler(
	IOptionsMonitor<ApiTokenOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder)
	: AuthenticationHandler<ApiTokenOptions>(options, logger, encoder)
{
	/// <summary>
	/// Executa a autenticação do usuário validando o token no header da requisição.
	/// Utiliza comparação constant-time para prevenir timing attacks.
	/// </summary>
	/// <returns>
	/// AuthenticateResult.Success se o token for válido,
	/// AuthenticateResult.Fail caso contrário.
	/// </returns>
	protected override Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		// Se não tiver token configurado, falha explícita (evita rodar “sem auth” por acidente)
		if (string.IsNullOrWhiteSpace(Options.Token))
			return Task.FromResult(AuthenticateResult.Fail("ApiTokenAuth:Token não configurado."));

		var headerName = string.IsNullOrWhiteSpace(Options.HeaderName) ? "X-API-TOKEN" : Options.HeaderName;

		if (!Request.Headers.TryGetValue(headerName, out var headerValues))
			return Task.FromResult(AuthenticateResult.Fail($"Header '{headerName}' ausente."));

		var provided = headerValues.ToString()?.Trim();
		if (string.IsNullOrWhiteSpace(provided))
			return Task.FromResult(AuthenticateResult.Fail($"Header '{headerName}' vazio."));

		// Comparação constant-time para prevenir timing attacks
		if (!CryptographicOperations.FixedTimeEquals(
			Encoding.UTF8.GetBytes(provided),
			Encoding.UTF8.GetBytes(Options.Token)))
		{
			return Task.FromResult(AuthenticateResult.Fail("Token inválido."));
		}

		var claims = new[]
		{
			new Claim(ClaimTypes.NameIdentifier, "api-token"),
			new Claim(ClaimTypes.Name, "ApiTokenClient")
		};

		var identity = new ClaimsIdentity(claims, ApiTokenDefaults.Scheme);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, ApiTokenDefaults.Scheme);

		return Task.FromResult(AuthenticateResult.Success(ticket));
	}

	/// <summary>
	/// Adiciona o header WWW-Authenticate na resposta 401 Unauthorized.
	/// Retorna ProblemDetails JSON estruturado com mensagem descritiva do erro.
	/// Implementa conformidade com RFC 7235 e RFC 7807.
	/// </summary>
	protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		Response.Headers.Append("WWW-Authenticate", $"{ApiTokenDefaults.Scheme} realm=\"BattleQuest API\"");
		Response.StatusCode = 401;
		Response.ContentType = "application/problem+json";

		// Determina mensagem específica com base no contexto de falha
		var failureMessage = "Token de autenticação ausente ou inválido.";
		var headerName = string.IsNullOrWhiteSpace(Options.HeaderName) ? "X-API-TOKEN" : Options.HeaderName;

		if (Context.Features.Get<IAuthenticateResultFeature>()?.AuthenticateResult?.Failure?.Message is { } message)
		{
			if (message.Contains("ausente", StringComparison.OrdinalIgnoreCase))
				failureMessage = $"Token de autenticação ausente. Forneça um token válido no header '{headerName}'.";
			else if (message.Contains("vazio", StringComparison.OrdinalIgnoreCase))
				failureMessage = $"Token de autenticação vazio. Forneça um token válido no header '{headerName}'.";
			else if (message.Contains("inválido", StringComparison.OrdinalIgnoreCase))
				failureMessage = $"Token de autenticação inválido. Verifique se o token fornecido no header '{headerName}' está correto.";
		}

		var problemDetails = new ProblemDetails
		{
			Type = "https://httpstatuses.com/401",
			Title = "Unauthorized",
			Status = 401,
			Detail = failureMessage,
			Instance = Request.Path
		};

		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};

		await Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
	}
}
