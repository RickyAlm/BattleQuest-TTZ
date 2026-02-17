namespace BattleQuest.Application.Queries.Dashboard;

/// <summary>
/// DTO representando estatísticas de abates (kills) de um jogador.
/// </summary>
/// <param name="PlayerId">Identificador do jogador.</param>
/// <param name="Name">Nome do jogador.</param>
/// <param name="TotalKills">Total de abates realizados.</param>
public sealed record PlayerKillStatsDto(
	string PlayerId,
	string? Name,
	int TotalKills
);
