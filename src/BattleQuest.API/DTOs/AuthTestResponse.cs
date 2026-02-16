namespace BattleQuest.API.DTOs;

/// <summary>
/// Response retornado pelo endpoint de diagnóstico de autenticação.
/// Usado para validar se o token está funcionando corretamente.
/// </summary>
public sealed record AuthTestResponse
{
	public required bool Authenticated { get; init; }
	public required string AuthenticationScheme { get; init; }
	public required DateTimeOffset TimestampUtc { get; init; }
}
