namespace BattleQuest.Application.Queries.Players;

/// <summary>
/// DTO representando um jogador com informações básicas.
/// </summary>
/// <param name="PlayerId">Identificador único do jogador.</param>
/// <param name="Name">Nome do jogador (opcional).</param>
/// <param name="LastKnownLevel">Último nível conhecido do jogador.</param>
/// <param name="LastKnownZone">Nome da última zona conhecida onde o jogador esteve.</param>
public sealed record PlayerDto(
	string PlayerId,
	string? Name,
	int? LastKnownLevel,
	string? LastKnownZone
);
