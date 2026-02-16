using BattleQuest.Application.Queries.Players;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Queries.Players;

/// <summary>
/// Implementação Entity Framework das consultas de jogadores.
/// Utiliza projeção direta para DTOs e AsNoTracking para otimização de leitura.
/// </summary>
public sealed class EfPlayerQueries : IPlayerQueries
{
	private readonly BattleQuestDbContext _context;

	public EfPlayerQueries(BattleQuestDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Recupera todos os jogadores com informações básicas.
	/// </summary>
	public async Task<IReadOnlyList<PlayerDto>> GetAllAsync(CancellationToken ct = default)
	{
		return await _context.Players
			.AsNoTracking()
			.OrderBy(p => p.PlayerId)
			.Select(p => new PlayerDto(
				p.PlayerId,
				p.Name,
				p.LastKnownLevel,
				p.LastKnownZone != null ? p.LastKnownZone.Name : null
			))
			.ToListAsync(ct);
	}

	/// <summary>
	/// Recupera estatísticas detalhadas de um jogador com agregações de eventos.
	/// Realiza joins e agregações para calcular métricas como pontuação, mortes, eliminações, etc.
	/// </summary>
	public async Task<PlayerStatsDto?> GetStatsByIdAsync(string playerId, CancellationToken ct = default)
	{
		var player = await _context.Players
			.AsNoTracking()
			.Where(p => p.PlayerId == playerId)
			.Select(p => new
			{
				p.PlayerId,
				p.Name,
				p.LastKnownLevel
			})
			.FirstOrDefaultAsync(ct);

		if (player == null)
			return null;

		// Agregações de eventos relacionados ao jogador
		var stats = await _context.Events
			.AsNoTracking()
			.Where(e => e.PlayerId == playerId)
			.GroupBy(e => e.PlayerId)
			.Select(g => new
			{
				TotalScore = g.Sum(e => (long?)e.Points) ?? 0L,
				TotalXp = g.Sum(e => (long?)e.Xp) ?? 0L,
				TotalGold = g.Sum(e => (long?)e.Gold) ?? 0L,
				ItemsCollected = g.Where(e => e.Quantity != null).Sum(e => (long?)e.Quantity) ?? 0L,
				QuestsCompleted = g.Count(e => e.QuestId != null && e.ActionType.Name == "QUEST_COMPLETE")
			})
			.FirstOrDefaultAsync(ct);

		// Conta mortes (como vítima)
		var deaths = await _context.Events
			.AsNoTracking()
			.Where(e => e.VictimPlayerId == playerId)
			.CountAsync(ct);

		// Conta eliminações (como assassino)
		var kills = await _context.Events
			.AsNoTracking()
			.Where(e => e.KillerPlayerId == playerId)
			.CountAsync(ct);

		return new PlayerStatsDto(
			player.PlayerId,
			player.Name,
			player.LastKnownLevel,
			stats?.TotalScore ?? 0L,
			deaths,
			kills,
			stats?.ItemsCollected ?? 0L,
			stats?.QuestsCompleted ?? 0,
			stats?.TotalXp ?? 0L,
			stats?.TotalGold ?? 0L
		);
	}
}
