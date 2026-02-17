using BattleQuest.Application.Queries.Items;

namespace BattleQuest.Application.Queries.Dashboard;

/// <summary>
/// DTO representando métricas consolidadas para o dashboard do BattleQuest.
/// Agrega estatísticas sobre jogadores, pontuação, itens, mortes, chefes, XP, Gold e abates.
/// </summary>
/// <param name="TotalActivePlayers">Total de jogadores ativos no período.</param>
/// <param name="TotalScoreAccumulated">Pontuação total acumulada por todos os jogadores.</param>
/// <param name="TopCollectedItems">Lista dos itens mais coletados.</param>
/// <param name="TopPlayerDeaths">Lista dos jogadores com mais mortes.</param>
/// <param name="BossesDefeated">Lista dos chefes derrotados e contagem.</param>
/// <param name="TopPlayersByXp">Lista dos jogadores com mais XP acumulado.</param>
/// <param name="TopPlayersByGold">Lista dos jogadores com mais Gold acumulado.</param>
/// <param name="TopPlayersByKills">Lista dos jogadores com mais abates realizados.</param>
public sealed record DashboardMetricsDto(
	int TotalActivePlayers,
	long TotalScoreAccumulated,
	IReadOnlyList<ItemStatsDto> TopCollectedItems,
	IReadOnlyList<PlayerDeathStatsDto> TopPlayerDeaths,
	IReadOnlyList<BossDefeatStatsDto> BossesDefeated,
	IReadOnlyList<PlayerXpStatsDto> TopPlayersByXp,
	IReadOnlyList<PlayerGoldStatsDto> TopPlayersByGold,
	IReadOnlyList<PlayerKillStatsDto> TopPlayersByKills
);
