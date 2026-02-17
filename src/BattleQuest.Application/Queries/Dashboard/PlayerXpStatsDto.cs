namespace BattleQuest.Application.Queries.Dashboard;

/// <summary>
/// DTO representando estatísticas de XP acumulado de um jogador.
/// </summary>
/// <param name="PlayerId">Identificador do jogador.</param>
/// <param name="Name">Nome do jogador.</param>
/// <param name="TotalXp">Total de XP acumulado.</param>
public sealed record PlayerXpStatsDto(
	string PlayerId,
	string? Name,
	long TotalXp
);
