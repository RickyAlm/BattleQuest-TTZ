namespace BattleQuest.Application.Queries.Dashboard;

/// <summary>
/// DTO representando estatísticas de mortes de um jogador.
/// </summary>
/// <param name="PlayerId">Identificador do jogador.</param>
/// <param name="Name">Nome do jogador.</param>
/// <param name="TotalDeaths">Total de mortes sofridas.</param>
public sealed record PlayerDeathStatsDto(
	string PlayerId,
	string? Name,
	int TotalDeaths
);
