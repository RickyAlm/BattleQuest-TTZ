namespace BattleQuest.Application.Queries.Leaderboard;

/// <summary>
/// DTO representando uma entrada no ranking de jogadores.
/// </summary>
/// <param name="Rank">Posição do jogador no ranking (1 = primeiro lugar).</param>
/// <param name="PlayerId">Identificador único do jogador.</param>
/// <param name="Name">Nome do jogador (opcional).</param>
/// <param name="TotalScore">Pontuação total acumulada pelo jogador.</param>
/// <param name="LastKnownLevel">Último nível conhecido do jogador.</param>
public sealed record LeaderboardEntryDto(
	int Rank,
	string PlayerId,
	string? Name,
	long TotalScore,
	int? LastKnownLevel
);
