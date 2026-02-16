using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
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
	/// Implementa conformidade com RFC 7235.
	/// </summary>
	protected override Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		Response.Headers.Append("WWW-Authenticate", $"{ApiTokenDefaults.Scheme} realm=\"BattleQuest API\"");
		Response.StatusCode = 401;
		return Task.CompletedTask;
	}
}
