using BattleQuest.Application.Queries.Events;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Queries.Events;

/// <summary>
/// Implementação Entity Framework Core das consultas de eventos.
/// Utiliza projeção direta para otimizar performance (sem carregar entidades completas).
/// </summary>
public sealed class EfEventQueries : IEventQueries
{
	private const int DefaultLimit = 50;
	private const int MaxLimit = 500;

	private readonly BattleQuestDbContext _db;

	public EfEventQueries(BattleQuestDbContext db) => _db = db;

	/// <summary>
	/// Retorna os últimos eventos do jogo ordenados por data de ocorrência (mais recente primeiro).
	/// Implementa normalização do limite e utiliza AsNoTracking() para otimização.
	/// </summary>
	/// <param name="limit">Número de eventos solicitados (é normalizado entre 1 e 500).</param>
	/// <param name="includeRaw">Se true, inclui a linha original do log no resultado.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Lista de eventos ordenados cronologicamente (mais recentes primeiro).</returns>
	public async Task<IReadOnlyList<EventDto>> GetLatestAsync(int limit, bool includeRaw, CancellationToken ct)
	{
		limit = NormalizeLimit(limit);

		// Projeção direta (sem carregar entidades)
		return await _db.Events
			.AsNoTracking()
			.OrderByDescending(e => e.OccurredAt)
			.ThenByDescending(e => e.EventId)
			.Take(limit)
			.Select(e => new EventDto(
				e.EventId,
				e.OccurredAt,
				e.Channel.Name,
				e.ActionType.Name,

				e.PlayerId,
				e.VictimPlayerId,
				e.KillerPlayerId,
				e.QuestId,

				e.Zone != null ? e.Zone.Name : null,
				e.Item != null ? e.Item.Name : null,
				e.Boss != null ? e.Boss.Name : null,

				e.Quantity,
				e.Xp,
				e.Gold,
				e.Hp,
				e.Damage,
				e.Method,
				e.PlayerLevel,
				e.Points,
				e.Reason,
				e.LocationX,
				e.LocationY,
				e.MessageText,

				includeRaw ? e.Raw : null
			))
			.ToListAsync(ct);
	}

	/// <summary>
	/// Normaliza o limite de eventos solicitados dentro dos bounds permitidos.
	/// </summary>
	/// <param name="limit">Limite solicitado.</param>
	/// <returns>
	/// DefaultLimit (50) se limit <= 0,
	/// MaxLimit (500) se limit > MaxLimit,
	/// caso contrário retorna o próprio limit.
	/// </returns>
	private static int NormalizeLimit(int limit)
	{
		if (limit <= 0) return DefaultLimit;
		return limit > MaxLimit ? MaxLimit : limit;
	}
}
