using BattleQuest.Application.Queries.Dashboard;
using BattleQuest.Application.Queries.Items;
using BattleQuest.Domain.Entities;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Queries.Dashboard;

/// <summary>
/// Implementação com Entity Framework Core para consultas de métricas do dashboard.
/// Realiza agregações de eventos para fornecer estatísticas consolidadas.
/// </summary>
public sealed class EfDashboardQueries : IDashboardQueries
{
	private readonly BattleQuestDbContext _context;

	private const int DefaultTopLimit = 10;
	private const string ActionBossDefeat = "BOSS_DEFEAT";

	public EfDashboardQueries(BattleQuestDbContext context) => _context = context;

	/// <summary>
	/// Recupera métricas consolidadas do dashboard com filtros opcionais de data.
	/// </summary>
	/// <param name="startDate">Data inicial opcional para filtro de eventos.</param>
	/// <param name="endDate">Data final opcional para filtro de eventos.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Métricas consolidadas incluindo jogadores ativos, pontuação, top itens, mortes, chefes derrotados, XP, gold e kills.</returns>
	public async Task<DashboardMetricsDto> GetMetricsAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default)
	{
		var eventsQuery = BuildEventsQuery(startDate, endDate);

		var totalActivePlayers = await GetTotalActivePlayersAsync(eventsQuery, ct);
		var totalScore = await GetTotalScoreAsync(eventsQuery, ct);

		var topItems = await GetTopCollectedItemsAsync(eventsQuery, ct);
		var topDeaths = await GetTopPlayerDeathsAsync(eventsQuery, ct);
		var bossesDefeated = await GetBossesDefeatedAsync(eventsQuery, ct);

		var topXpPlayers = await GetTopPlayersByXpAsync(eventsQuery, ct);
		var topGoldPlayers = await GetTopPlayersByGoldAsync(eventsQuery, ct);
		var topKillsPlayers = await GetTopPlayersByKillsAsync(eventsQuery, ct);

		return new DashboardMetricsDto(
			TotalActivePlayers: totalActivePlayers,
			TotalScoreAccumulated: totalScore,
			TopCollectedItems: topItems,
			TopPlayerDeaths: topDeaths,
			BossesDefeated: bossesDefeated,
			TopPlayersByXp: topXpPlayers,
			TopPlayersByGold: topGoldPlayers,
			TopPlayersByKills: topKillsPlayers
		);
	}

	/// <summary>
	/// Constrói a query base de eventos aplicando filtros opcionais de data.
	/// </summary>
	private IQueryable<Event> BuildEventsQuery(DateTime? startDate, DateTime? endDate)
	{
		var query = _context.Events.AsNoTracking();

		if (startDate.HasValue)
			query = query.Where(e => e.OccurredAt >= DateTimeUtcHelper.NormalizeToUtc(startDate.Value));

		if (endDate.HasValue)
			query = query.Where(e => e.OccurredAt <= DateTimeUtcHelper.NormalizeToUtc(endDate.Value));

		return query;
	}

	private static class DateTimeUtcHelper
	{
		public static DateTime NormalizeToUtc(DateTime value) =>
			value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
	}

	/// <summary>
	/// Calcula o total de jogadores ativos (distinct PlayerId).
	/// </summary>
	private static Task<int> GetTotalActivePlayersAsync(IQueryable<Event> eventsQuery, CancellationToken ct) =>
		eventsQuery
			.Where(e => e.PlayerId != null)
			.Select(e => e.PlayerId)
			.Distinct()
			.CountAsync(ct);

	/// <summary>
	/// Calcula a pontuação total acumulada somando os Points dos eventos.
	/// </summary>
	private static async Task<long> GetTotalScoreAsync(IQueryable<Event> eventsQuery, CancellationToken ct) =>
		await eventsQuery
			.Where(e => e.Points != null)
			.SumAsync(e => (long?)e.Points, ct) ?? 0L;

	/// <summary>
	/// Obtém os top 10 itens mais coletados por quantidade total.
	/// </summary>
	private async Task<IReadOnlyList<ItemStatsDto>> GetTopCollectedItemsAsync(IQueryable<Event> eventsQuery, CancellationToken ct)
	{
		var topItemsData = await eventsQuery
			.Where(e => e.ItemId != null && e.Quantity != null)
			.GroupBy(e => e.ItemId)
			.Select(g => new
			{
				ItemId = g.Key!.Value,
				TotalQuantity = g.Sum(e => (long)e.Quantity!.Value),
				CollectionCount = g.Count()
			})
			.OrderByDescending(x => x.TotalQuantity)
			.Take(DefaultTopLimit)
			.ToListAsync(ct);

		var itemNames = await LoadItemNamesAsync(topItemsData.Select(x => x.ItemId), ct);

		return topItemsData
			.Select(x => new ItemStatsDto(
				itemNames.GetValueOrDefault(x.ItemId) ?? "Unknown",
				x.TotalQuantity,
				x.CollectionCount
			))
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Carrega nomes de itens do banco para enriquecimento.
	/// </summary>
	private Task<Dictionary<int, string>> LoadItemNamesAsync(IEnumerable<int> itemIds, CancellationToken ct)
	{
		var ids = itemIds.Distinct().ToList();
		if (ids.Count == 0)
			return Task.FromResult(new Dictionary<int, string>());

		return _context.Items
			.AsNoTracking()
			.Where(i => ids.Contains(i.ItemId))
			.ToDictionaryAsync(i => i.ItemId, i => i.Name, ct);
	}

	/// <summary>
	/// Obtém os top 10 jogadores com mais mortes (VictimPlayerId).
	/// </summary>
	private async Task<IReadOnlyList<PlayerDeathStatsDto>> GetTopPlayerDeathsAsync(IQueryable<Event> eventsQuery, CancellationToken ct)
	{
		var deathsData = await eventsQuery
			.Where(e => e.VictimPlayerId != null)
			.GroupBy(e => e.VictimPlayerId)
			.Select(g => new { PlayerId = g.Key!, DeathCount = g.Count() })
			.OrderByDescending(x => x.DeathCount)
			.Take(DefaultTopLimit)
			.ToListAsync(ct);

		var playerNames = await LoadPlayerNamesAsync(deathsData.Select(x => x.PlayerId), ct);

		return deathsData
			.Select(x => new PlayerDeathStatsDto(
				x.PlayerId,
				playerNames.GetValueOrDefault(x.PlayerId),
				x.DeathCount
			))
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Obtém estatísticas de chefes derrotados (ActionType = BOSS_DEFEAT).
	/// </summary>
	private async Task<IReadOnlyList<BossDefeatStatsDto>> GetBossesDefeatedAsync(IQueryable<Event> eventsQuery, CancellationToken ct)
	{
		var defeatData = await eventsQuery
			.Where(e => e.BossId != null && e.ActionType.Name == ActionBossDefeat)
			.GroupBy(e => e.BossId)
			.Select(g => new { BossId = g.Key!.Value, DefeatCount = g.Count() })
			.OrderByDescending(x => x.DefeatCount)
			.ToListAsync(ct);

		var bossNames = await LoadBossNamesAsync(defeatData.Select(x => x.BossId), ct);

		return defeatData
			.Select(x => new BossDefeatStatsDto(
				bossNames.GetValueOrDefault(x.BossId) ?? "Unknown",
				x.DefeatCount
			))
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Carrega nomes de chefes do banco para enriquecimento.
	/// </summary>
	private Task<Dictionary<int, string>> LoadBossNamesAsync(IEnumerable<int> bossIds, CancellationToken ct)
	{
		var ids = bossIds.Distinct().ToList();
		if (ids.Count == 0)
			return Task.FromResult(new Dictionary<int, string>());

		return _context.Bosses
			.AsNoTracking()
			.Where(b => ids.Contains(b.BossId))
			.ToDictionaryAsync(b => b.BossId, b => b.Name, ct);
	}

	/// <summary>
	/// Obtém os top 10 jogadores com mais XP acumulado.
	/// </summary>
	private async Task<IReadOnlyList<PlayerXpStatsDto>> GetTopPlayersByXpAsync(IQueryable<Event> eventsQuery, CancellationToken ct)
	{
		var xpData = await eventsQuery
			.Where(e => e.PlayerId != null && e.Xp != null)
			.GroupBy(e => e.PlayerId)
			.Select(g => new { PlayerId = g.Key!, TotalXp = g.Sum(e => (long)e.Xp!.Value) })
			.OrderByDescending(x => x.TotalXp)
			.Take(DefaultTopLimit)
			.ToListAsync(ct);

		var playerNames = await LoadPlayerNamesAsync(xpData.Select(x => x.PlayerId), ct);

		return xpData
			.Select(x => new PlayerXpStatsDto(
				x.PlayerId,
				playerNames.GetValueOrDefault(x.PlayerId),
				x.TotalXp
			))
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Obtém os top 10 jogadores com mais Gold acumulado.
	/// </summary>
	private async Task<IReadOnlyList<PlayerGoldStatsDto>> GetTopPlayersByGoldAsync(IQueryable<Event> eventsQuery, CancellationToken ct)
	{
		var goldData = await eventsQuery
			.Where(e => e.PlayerId != null && e.Gold != null)
			.GroupBy(e => e.PlayerId)
			.Select(g => new { PlayerId = g.Key!, TotalGold = g.Sum(e => (long)e.Gold!.Value) })
			.OrderByDescending(x => x.TotalGold)
			.Take(DefaultTopLimit)
			.ToListAsync(ct);

		var playerNames = await LoadPlayerNamesAsync(goldData.Select(x => x.PlayerId), ct);

		return goldData
			.Select(x => new PlayerGoldStatsDto(
				x.PlayerId,
				playerNames.GetValueOrDefault(x.PlayerId),
				x.TotalGold
			))
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Obtém os top 10 jogadores com mais abates/kills (KillerPlayerId).
	/// </summary>
	private async Task<IReadOnlyList<PlayerKillStatsDto>> GetTopPlayersByKillsAsync(IQueryable<Event> eventsQuery, CancellationToken ct)
	{
		var killsData = await eventsQuery
			.Where(e => e.KillerPlayerId != null)
			.GroupBy(e => e.KillerPlayerId)
			.Select(g => new { PlayerId = g.Key!, TotalKills = g.Count() })
			.OrderByDescending(x => x.TotalKills)
			.Take(DefaultTopLimit)
			.ToListAsync(ct);

		var playerNames = await LoadPlayerNamesAsync(killsData.Select(x => x.PlayerId), ct);

		return killsData
			.Select(x => new PlayerKillStatsDto(
				x.PlayerId,
				playerNames.GetValueOrDefault(x.PlayerId),
				x.TotalKills
			))
			.ToList()
			.AsReadOnly();
	}

	/// <summary>
	/// Carrega nomes de jogadores do banco para enriquecimento.
	/// </summary>
	private Task<Dictionary<string, string>> LoadPlayerNamesAsync(IEnumerable<string> playerIds, CancellationToken ct)
	{
		var ids = playerIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
		if (ids.Count == 0)
			return Task.FromResult(new Dictionary<string, string>());

		return _context.Players
			.AsNoTracking()
			.Where(p => ids.Contains(p.PlayerId))
			.ToDictionaryAsync(p => p.PlayerId, p => p.Name, ct);
	}
}
