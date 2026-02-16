using BattleQuest.Application.Import.Ports;
using BattleQuest.Domain.Entities;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BattleQuest.Infrastructure.Import.Lookup;

/// <summary>
/// Implementação EF Core do resolvedor de dimensões.
/// Resolve nomes de dimensões (Channel, ActionType, Zone, Item, Boss) para seus IDs correspondentes,
/// criando novas entradas quando necessário. Utiliza cache em memória para otimizar performance.
/// </summary>
public sealed class EfDimensionResolver : IDimensionResolver
{
	private readonly BattleQuestDbContext _db;

	private readonly Dictionary<string, int> _channels = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _actions = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _zones = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _items = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, int> _bosses = new(StringComparer.OrdinalIgnoreCase);

	public EfDimensionResolver(BattleQuestDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Obtém ou cria um canal pelo nome (obrigatório).
	/// </summary>
	public Task<int> GetOrCreateChannelIdAsync(string channelUpper, CancellationToken ct)
		=> GetOrCreateAsync(
			channelUpper,
			_channels,
			_db.Channels,
			x => x.Name,
			name => new Channel { Name = name },
			x => x.ChannelId,
			ct);

	/// <summary>
	/// Obtém ou cria um tipo de ação pelo nome (obrigatório).
	/// </summary>
	public Task<int> GetOrCreateActionTypeIdAsync(string actionTypeUpper, CancellationToken ct)
		=> GetOrCreateAsync(
			actionTypeUpper,
			_actions,
			_db.ActionTypes,
			x => x.Name,
			name => new ActionType { Name = name },
			x => x.ActionTypeId,
			ct);

	/// <summary>
	/// Obtém ou cria uma zona pelo nome (opcional).
	/// </summary>
	public Task<int?> GetOrCreateZoneIdAsync(string? zoneUpper, CancellationToken ct)
		=> GetOrCreateNullableAsync(
			zoneUpper,
			_zones,
			_db.Zones,
			x => x.Name,
			name => new Zone { Name = name },
			x => x.ZoneId,
			ct);

	/// <summary>
	/// Obtém ou cria um item pelo nome (opcional).
	/// </summary>
	public Task<int?> GetOrCreateItemIdAsync(string? itemUpper, CancellationToken ct)
		=> GetOrCreateNullableAsync(
			itemUpper,
			_items,
			_db.Items,
			x => x.Name,
			name => new Item { Name = name },
			x => x.ItemId,
			ct);

	/// <summary>
	/// Obtém ou cria um boss pelo nome (opcional).
	/// </summary>
	public Task<int?> GetOrCreateBossIdAsync(string? bossUpper, CancellationToken ct)
		=> GetOrCreateNullableAsync(
			bossUpper,
			_bosses,
			_db.Bosses,
			x => x.Name,
			name => new Boss { Name = name },
			x => x.BossId,
			ct);

	/// <summary>
	/// Normaliza o nome para MAIÚSCULO invariante.
	/// </summary>
	private static string NormalizeUpper(string value) 
		=> value.Trim().ToUpperInvariant();

	/// <summary>
	/// Obtém ou cria uma entidade de dimensão pelo nome.
	/// Usa cache em memória para evitar consultas repetidas ao banco.
	/// Limpa o ChangeTracker após cada inserção para otimizar imports grandes.
	/// </summary>
	private async Task<int> GetOrCreateAsync<TEntity>(
		string name,
		Dictionary<string, int> cache,
		DbSet<TEntity> set,
		Expression<Func<TEntity, string>> getNameExpr,
		Func<string, TEntity> factory,
		Func<TEntity, int> getId,
		CancellationToken ct)
		where TEntity : class
	{
		name = NormalizeUpper(name);

		if (cache.TryGetValue(name, out var cachedId))
			return cachedId;

		var predicate = BuildEqualityExpression(getNameExpr, name);
		var existing = await set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

		if (existing is not null)
		{
			var id = getId(existing);
			cache[name] = id;
			return id;
		}

		return await CreateAndCacheAsync(name, cache, set, factory, getId, ct);
	}

	/// <summary>
	/// Cria uma nova entidade de dimensão, persiste no banco e adiciona ao cache.
	/// </summary>
	private async Task<int> CreateAndCacheAsync<TEntity>(
		string name,
		Dictionary<string, int> cache,
		DbSet<TEntity> set,
		Func<string, TEntity> factory,
		Func<TEntity, int> getId,
		CancellationToken ct)
		where TEntity : class
	{
		var entity = factory(name);
		set.Add(entity);
		await _db.SaveChangesAsync(ct);

		_db.ChangeTracker.Clear();

		var id = getId(entity);
		cache[name] = id;
		return id;
	}

	/// <summary>
	/// Versão nullable do GetOrCreateAsync para dimensões opcionais.
	/// Retorna null se o nome for nulo ou vazio.
	/// </summary>
	private async Task<int?> GetOrCreateNullableAsync<TEntity>(
		string? name,
		Dictionary<string, int> cache,
		DbSet<TEntity> set,
		Expression<Func<TEntity, string>> getNameExpr,
		Func<string, TEntity> factory,
		Func<TEntity, int> getId,
		CancellationToken ct)
		where TEntity : class
	{
		if (string.IsNullOrWhiteSpace(name))
			return null;

		return await GetOrCreateAsync(name, cache, set, getNameExpr, factory, getId, ct);
	}

	/// <summary>
	/// Constrói uma expressão de igualdade traduzível pelo EF Core.
	/// Transforma (TEntity e) => e.Name em (TEntity e) => e.Name == value.
	/// </summary>
	private static Expression<Func<TEntity, bool>> BuildEqualityExpression<TEntity>(
		Expression<Func<TEntity, string>> selector,
		string value)
	{
		var parameter = selector.Parameters[0];
		var equalityBody = Expression.Equal(selector.Body, Expression.Constant(value));
		return Expression.Lambda<Func<TEntity, bool>>(equalityBody, parameter);
	}
}
