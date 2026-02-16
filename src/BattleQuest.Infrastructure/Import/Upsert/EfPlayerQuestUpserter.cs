using BattleQuest.Application.Import.Ports;
using BattleQuest.Domain.Entities;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Import.Upsert;

/// <summary>
/// Implementação EF Core para gerenciamento de players e quests durante a importação.
/// Garante existência de entidades e atualiza snapshots sem sobrescrever dados existentes com null.
/// Utiliza cache em memória para otimizar verificações de existência.
/// </summary>
public sealed class EfPlayerQuestUpserter : IPlayerQuestUpserter
{
	private readonly BattleQuestDbContext _db;

	private readonly HashSet<string> _knownPlayers = new(StringComparer.Ordinal);
	private readonly HashSet<string> _knownQuests = new(StringComparer.Ordinal);

	public EfPlayerQuestUpserter(BattleQuestDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Garante que todos os players informados existem no banco de dados.
	/// Cria novos registros para players que ainda não existem.
	/// Usa cache em memória para evitar consultas repetidas.
	/// </summary>
	/// <param name="playerIds">Lista de IDs de players (pode conter nulls)</param>
	/// <param name="ct">Token de cancelamento</param>
	public async Task EnsurePlayersAsync(IEnumerable<string?> playerIds, CancellationToken ct)
	{
		var validIds = FilterValidPlayerIds(playerIds);

		if (validIds.Count == 0)
			return;

		var existingIds = await GetExistingPlayerIdsAsync(validIds, ct);
		CachePlayerIds(existingIds);

		var newIds = GetNewPlayerIds(validIds, existingIds);
		
		if (newIds.Count > 0)
		{
			await CreatePlayersAsync(newIds, ct);
			CachePlayerIds(newIds);
		}
	}

	/// <summary>
	/// Atualiza o snapshot do player com informações mais recentes.
	/// Não sobrescreve campos existentes com valores nulos (usa coalesce no SQL).
	/// </summary>
	/// <param name="playerId">ID do player</param>
	/// <param name="name">Nome do player (opcional)</param>
	/// <param name="level">Nível do player (opcional)</param>
	/// <param name="zoneId">ID da zona onde o player foi visto (opcional)</param>
	/// <param name="ct">Token de cancelamento</param>
	public async Task UpsertPlayerSnapshotAsync(
		string playerId, 
		string? name, 
		int? level, 
		int? zoneId, 
		CancellationToken ct)
	{
		await _db.Players
			.Where(p => p.PlayerId == playerId)
			.ExecuteUpdateAsync(s => s
				.SetProperty(p => p.Name, p => name ?? p.Name)
				.SetProperty(p => p.LastKnownLevel, p => level ?? p.LastKnownLevel)
				.SetProperty(p => p.LastKnownZoneId, p => zoneId ?? p.LastKnownZoneId),
				ct);
	}

	/// <summary>
	/// Garante que a quest existe no banco de dados.
	/// Cria um novo registro se ainda não existir.
	/// Usa cache em memória para evitar consultas repetidas.
	/// </summary>
	/// <param name="questId">ID da quest</param>
	/// <param name="ct">Token de cancelamento</param>
	public async Task EnsureQuestAsync(string questId, CancellationToken ct)
	{
		if (_knownQuests.Contains(questId))
			return;

		var exists = await _db.Quests
			.AsNoTracking()
			.AnyAsync(q => q.QuestId == questId, ct);

		if (!exists)
		{
			_db.Quests.Add(new Quest { QuestId = questId });
			await _db.SaveChangesAsync(ct);
			_db.ChangeTracker.Clear();
		}

		_knownQuests.Add(questId);
	}

	/// <summary>
	/// Atualiza o nome da quest se ainda não estiver definido.
	/// Não sobrescreve nomes existentes (usa coalesce no SQL).
	/// </summary>
	/// <param name="questId">ID da quest</param>
	/// <param name="questName">Nome da quest</param>
	/// <param name="ct">Token de cancelamento</param>
	public async Task UpsertQuestNameAsync(string questId, string questName, CancellationToken ct)
	{
		await _db.Quests
			.Where(q => q.QuestId == questId)
			.ExecuteUpdateAsync(s => s
				.SetProperty(q => q.Name, q => q.Name ?? questName),
				ct);
	}

	/// <summary>
	/// Filtra e normaliza os IDs de players válidos (não nulos, únicos, não conhecidos).
	/// </summary>
	private List<string> FilterValidPlayerIds(IEnumerable<string?> playerIds)
	{
		return playerIds
			.Where(id => !string.IsNullOrWhiteSpace(id))
			.Select(id => id!)
			.Distinct(StringComparer.Ordinal)
			.Where(id => !_knownPlayers.Contains(id))
			.ToList();
	}

	/// <summary>
	/// Busca IDs de players que já existem no banco de dados.
	/// </summary>
	private async Task<List<string>> GetExistingPlayerIdsAsync(List<string> playerIds, CancellationToken ct)
	{
		return await _db.Players
			.AsNoTracking()
			.Where(p => playerIds.Contains(p.PlayerId))
			.Select(p => p.PlayerId)
			.ToListAsync(ct);
	}

	/// <summary>
	/// Adiciona IDs ao cache de players conhecidos.
	/// </summary>
	private void CachePlayerIds(IEnumerable<string> playerIds)
	{
		foreach (var id in playerIds)
			_knownPlayers.Add(id);
	}

	/// <summary>
	/// Retorna IDs de players que ainda não existem no banco.
	/// </summary>
	private static List<string> GetNewPlayerIds(List<string> allIds, List<string> existingIds)
	{
		return allIds
			.Except(existingIds, StringComparer.Ordinal)
			.ToList();
	}

	/// <summary>
	/// Cria novos registros de players no banco de dados.
	/// </summary>
	private async Task CreatePlayersAsync(List<string> playerIds, CancellationToken ct)
	{
		var players = playerIds.Select(id => new Player { PlayerId = id });
		
		_db.Players.AddRange(players);
		await _db.SaveChangesAsync(ct);
		_db.ChangeTracker.Clear();
	}
}
