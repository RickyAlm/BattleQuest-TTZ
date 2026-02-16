using BattleQuest.Application.Queries.Items;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Queries.Items;

/// <summary>
/// Implementação Entity Framework das consultas de itens.
/// Realiza agregações de eventos de coleta para calcular estatísticas de itens.
/// </summary>
public sealed class EfItemQueries : IItemQueries
{
	private const int DefaultLimit = 50;
	private const int MaxLimit = 500;

	private readonly BattleQuestDbContext _context;

	public EfItemQueries(BattleQuestDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Recupera os itens mais coletados com agregações de quantidade.
	/// Agrupa eventos por item, soma quantidades e ordena por total descendente.
	/// </summary>
	public async Task<IReadOnlyList<ItemStatsDto>> GetTopCollectedAsync(int limit = DefaultLimit, CancellationToken ct = default)
	{
		var normalizedLimit = NormalizeLimit(limit);

		return await _context.Events
			.AsNoTracking()
			.Where(e => e.ItemId != null && e.Quantity != null)
			.GroupBy(e => new
			{
				e.ItemId,
				ItemName = e.Item!.Name
			})
			.Select(g => new
			{
				ItemName = g.Key.ItemName,
				TotalCollected = g.Sum(e => (long)e.Quantity!.Value),
				CollectionCount = g.Count()
			})
			.OrderByDescending(x => x.TotalCollected)
			.Take(normalizedLimit)
			.Select(x => new ItemStatsDto(
				x.ItemName,
				x.TotalCollected,
				x.CollectionCount
			))
			.ToListAsync(ct);
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
