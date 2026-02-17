namespace BattleQuest.Application.Queries.Leaderboard;

/// <summary>
/// Interface para consultas relacionadas ao ranking de jogadores.
/// Define operações de leitura para recuperar o leaderboard ordenado por pontuação.
/// </summary>
public interface ILeaderboardQueries
{
	/// <summary>
	/// Recupera o ranking de jogadores ordenado por pontuação total.
	/// Agrega pontos de todos os eventos de cada jogador e ordena descendente.
	/// Atribui rank sequencial (1 = maior pontuação).
	/// </summary>
	/// <param name="limit">Número máximo de entradas a retornar. Padrão: 50, Máximo: 500.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>
	/// Lista de entradas do leaderboard ordenadas por pontuação total (descendente).
	/// Cada entrada inclui rank, playerId, nome, pontuação total e nível.
	/// </returns>
	Task<IReadOnlyList<LeaderboardEntryDto>> GetTopPlayersAsync(int limit = 50, CancellationToken ct = default);
}
