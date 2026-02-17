using BattleQuest.Application.Queries.Leaderboard;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Queries.Leaderboard;

/// <summary>
/// Implementação Entity Framework das consultas de leaderboard.
/// Realiza agregações de pontuação por jogador e atribui ranking sequencial.
/// </summary>
public sealed class EfLeaderboardQueries : ILeaderboardQueries
{
	private const int DefaultLimit = 50;
	private const int MaxLimit = 500;

	private readonly BattleQuestDbContext _context;

	public EfLeaderboardQueries(BattleQuestDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Recupera o ranking de jogadores ordenado por pontuação total.
	/// Agrega pontos de eventos, faz join com Players para dados adicionais,
	/// ordena descendente e atribui rank sequencial.
	/// </summary>
	/// <param name="limit">Número de jogadores a retornar (normalizado entre 1 e 500).</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Lista de entradas do leaderboard com rank, nome e pontuação.</returns>
	public async Task<IReadOnlyList<LeaderboardEntryDto>> GetTopPlayersAsync(int limit = DefaultLimit, CancellationToken ct = default)
	{
		var normalizedLimit = NormalizeLimit(limit);

		// Agregar pontos por jogador
		var playerScores = await _context.Events
			.AsNoTracking()
			.Where(e => e.PlayerId != null && e.Points != null)
			.GroupBy(e => e.PlayerId)
			.Select(g => new
			{
				PlayerId = g.Key!,
				TotalScore = g.Sum(e => (long)e.Points!.Value)
			})
			.OrderByDescending(x => x.TotalScore)
			.Take(normalizedLimit)
			.ToListAsync(ct);

		if (playerScores.Count == 0)
			return Array.Empty<LeaderboardEntryDto>();

		// Obter dados dos jogadores
		var playerIds = playerScores.Select(x => x.PlayerId).ToList();
		var players = await _context.Players
			.AsNoTracking()
			.Where(p => playerIds.Contains(p.PlayerId))
			.ToDictionaryAsync(p => p.PlayerId, ct);

		// Construir leaderboard com rank sequencial
		var leaderboard = new List<LeaderboardEntryDto>();
		int rank = 1;

		foreach (var score in playerScores)
		{
			players.TryGetValue(score.PlayerId, out var player);

			leaderboard.Add(new LeaderboardEntryDto(
				rank++,
				score.PlayerId,
				player?.Name,
				score.TotalScore,
				player?.LastKnownLevel
			));
		}

		return leaderboard;
	}

	/// <summary>
	/// Normaliza o limite de resultados para estar dentro dos bounds permitidos.
	/// Limites <= 0 são ajustados para o padrão (50).
	/// Limites > 500 são limitados ao máximo (500).
	/// </summary>
	private static int NormalizeLimit(int limit)
	{
		if (limit <= 0) return DefaultLimit;
		if (limit > MaxLimit) return MaxLimit;
		return limit;
	}
}
