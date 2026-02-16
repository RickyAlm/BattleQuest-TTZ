namespace BattleQuest.Application.Queries.Players;

/// <summary>
/// Interface para consultas relacionadas a jogadores.
/// Define operações de leitura para recuperar informações e estatísticas de jogadores.
/// </summary>
public interface IPlayerQueries
{
	/// <summary>
	/// Recupera a lista de todos os jogadores com informações básicas.
	/// </summary>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Lista de jogadores ordenados por PlayerId.</returns>
	Task<IReadOnlyList<PlayerDto>> GetAllAsync(CancellationToken ct = default);

	/// <summary>
	/// Recupera estatísticas detalhadas de um jogador específico.
	/// Inclui pontuação total, mortes, eliminações, itens coletados, quests concluídas, XP e ouro.
	/// </summary>
	/// <param name="playerId">Identificador único do jogador.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>
	/// Estatísticas do jogador se encontrado;
	/// null se o jogador não existir.
	/// </returns>
	Task<PlayerStatsDto?> GetStatsByIdAsync(string playerId, CancellationToken ct = default);
}
