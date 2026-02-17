namespace BattleQuest.Application.Queries.Dashboard;

/// <summary>
/// DTO representando estatísticas de Gold acumulado de um jogador.
/// </summary>
/// <param name="PlayerId">Identificador do jogador.</param>
/// <param name="Name">Nome do jogador.</param>
/// <param name="TotalGold">Total de Gold acumulado.</param>
public sealed record PlayerGoldStatsDto(
	string PlayerId,
	string? Name,
	long TotalGold
);
