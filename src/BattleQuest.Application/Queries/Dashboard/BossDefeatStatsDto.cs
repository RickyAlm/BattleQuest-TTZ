namespace BattleQuest.Application.Queries.Dashboard;

/// <summary>
/// DTO representando estatísticas de derrotas de um chefe.
/// </summary>
/// <param name="BossName">Nome do chefe.</param>
/// <param name="DefeatCount">Número de vezes que o chefe foi derrotado.</param>
public sealed record BossDefeatStatsDto(
	string BossName,
	int DefeatCount
);
