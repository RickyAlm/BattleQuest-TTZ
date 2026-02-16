using BattleQuest.Application.Import.Ports;
using BattleQuest.Domain.Entities;
using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Infrastructure.Import.Persistence;

/// <summary>
/// Implementação EF Core do armazenamento de eventos com suporte a idempotência.
/// Garante que eventos duplicados (mesmo hash) não sejam inseridos.
/// </summary>
public sealed class EfEventIngestionStore : IEventIngestionStore
{
	private readonly BattleQuestDbContext _db;

	public EfEventIngestionStore(BattleQuestDbContext db)
	{
		_db = db;
	}

	/// <summary>
	/// Insere um lote de eventos no banco de dados, ignorando duplicatas.
	/// Usa o EventHash para garantir idempotência na importação.
	/// </summary>
	/// <param name="events">Lista de eventos a serem inseridos</param>
	/// <param name="ct">Token de cancelamento</param>
	/// <returns>Tupla com quantidade de eventos inseridos e ignorados</returns>
	public async Task<(int inserted, int skipped)> InsertBatchAsync(IReadOnlyList<Event> events, CancellationToken ct)
	{
		if (events.Count == 0)
			return (0, 0);

		var uniqueEvents = DeduplicateEvents(events);
		var existingHashes = await GetExistingHashesAsync(uniqueEvents, ct);
		var eventsToInsert = FilterNewEvents(uniqueEvents, existingHashes);

		var skippedCount = uniqueEvents.Count - eventsToInsert.Count;

		await PersistEventsAsync(eventsToInsert, ct);

		return (eventsToInsert.Count, skippedCount);
	}

	/// <summary>
	/// Remove eventos duplicados dentro do batch usando o EventHash.
	/// </summary>
	private static List<Event> DeduplicateEvents(IReadOnlyList<Event> events)
	{
		return events
			.GroupBy(e => e.EventHash, StringComparer.Ordinal)
			.Select(g => g.First())
			.ToList();
	}

	/// <summary>
	/// Busca hashes de eventos que já existem no banco de dados.
	/// </summary>
	private async Task<HashSet<string>> GetExistingHashesAsync(List<Event> events, CancellationToken ct)
	{
		var hashes = events.Select(e => e.EventHash).ToList();

		var existingHashes = await _db.Events
			.Where(e => hashes.Contains(e.EventHash))
			.Select(e => e.EventHash)
			.ToListAsync(ct);

		return existingHashes.ToHashSet(StringComparer.Ordinal);
	}

	/// <summary>
	/// Filtra eventos que ainda não existem no banco de dados.
	/// </summary>
	private static List<Event> FilterNewEvents(List<Event> events, HashSet<string> existingHashes)
	{
		return events
			.Where(e => !existingHashes.Contains(e.EventHash))
			.ToList();
	}

	/// <summary>
	/// Persiste eventos no banco de dados e limpa o ChangeTracker.
	/// </summary>
	private async Task PersistEventsAsync(List<Event> events, CancellationToken ct)
	{
		if (events.Count == 0)
			return;

		_db.Events.AddRange(events);
		await _db.SaveChangesAsync(ct);
		_db.ChangeTracker.Clear();
	}
}
