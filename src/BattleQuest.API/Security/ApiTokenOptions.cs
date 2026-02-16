using Microsoft.AspNetCore.Authentication;

namespace BattleQuest.API.Security;

/// <summary>
/// Opções de configuração para autenticação via token no header.
/// </summary>
public sealed class ApiTokenOptions : AuthenticationSchemeOptions
{
	public string HeaderName { get; set; } = "X-API-TOKEN";
	public string Token { get; set; } = string.Empty;
}
