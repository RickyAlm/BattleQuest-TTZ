namespace BattleQuest.Application.Queries.Players;

/// <summary>
/// DTO representando estatísticas detalhadas de um jogador.
/// Usado no endpoint GET /api/players/:id/stats.
/// </summary>
/// <param name="PlayerId">Identificador único do jogador.</param>
/// <param name="Name">Nome do jogador (opcional).</param>
/// <param name="LastKnownLevel">Último nível conhecido do jogador.</param>
/// <param name="TotalScore">Pontuação total acumulada pelo jogador.</param>
/// <param name="TotalDeaths">Número total de vezes que o jogador morreu (como vítima).</param>
/// <param name="TotalKills">Número total de eliminações realizadas pelo jogador (como assassino).</param>
/// <param name="ItemsCollected">Quantidade total de itens coletados pelo jogador.</param>
/// <param name="QuestsCompleted">Número total de quests concluídas pelo jogador.</param>
/// <param name="TotalXpEarned">Experiência total acumulada pelo jogador.</param>
/// <param name="TotalGoldEarned">Ouro total acumulado pelo jogador.</param>
public sealed record PlayerStatsDto(
	string PlayerId,
	string? Name,
	int? LastKnownLevel,
	long TotalScore,
	int TotalDeaths,
	int TotalKills,
	long ItemsCollected,
	int QuestsCompleted,
	long TotalXpEarned,
	long TotalGoldEarned
);
