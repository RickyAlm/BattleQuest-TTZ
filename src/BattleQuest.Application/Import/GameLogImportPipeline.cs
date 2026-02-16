using BattleQuest.Application.Import.Mapping;
using BattleQuest.Application.Import.Parsing;
using BattleQuest.Application.Import.Ports;
using BattleQuest.Domain.Entities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BattleQuest.Application.Import;

/// <summary>
/// Pipeline principal para importação de logs em lote.
/// Processa linhas de log assincronamente, resolve dimensões, e persiste eventos no banco de dados.
/// </summary>
public sealed class GameLogImportPipeline : IGameLogImportPipeline
{
	private readonly IDimensionResolver _dimensions;
	private readonly IPlayerQuestUpserter _upserter;
	private readonly IEventIngestionStore _store;
	private readonly ImportOptions _options;

	public GameLogImportPipeline(
		IDimensionResolver dimensions,
		IPlayerQuestUpserter upserter,
		IEventIngestionStore store,
		IOptions<ImportOptions> options)
	{
		_dimensions = dimensions;
		_upserter = upserter;
		_store = store;
		_options = options.Value;
	}

	/// <summary>
	/// Importa logs de forma assíncrona e em lote.
	/// </summary>
	/// <param name="lines">Stream assíncrono de linhas de log para processar</param>
	/// <param name="ct">Token de cancelamento</param>
	/// <returns>Resultado da importação contendo estatísticas de linhas processadas</returns>
	public async Task<ImportResult> ImportAsync(IAsyncEnumerable<string> lines, CancellationToken ct)
	{
		var context = new ImportContext(_options.BatchSize);

		await foreach (var line in lines.WithCancellation(ct))
		{
			context.LinesRead++;

			if (string.IsNullOrWhiteSpace(line))
				continue;

			if (await TryProcessLineAsync(line, context.Batch, ct))
			{
				if (context.Batch.Count >= _options.BatchSize)
					await FlushBatchAsync(context, ct);
			}
		}

		if (context.Batch.Count > 0)
			await FlushBatchAsync(context, ct);

		return new ImportResult(context.LinesRead, context.EventsInserted, context.DuplicatesSkipped);
	}

	/// <summary>
	/// Tenta processar uma única linha de log e adiciona ao batch se válida.
	/// </summary>
	private async Task<bool> TryProcessLineAsync(string line, List<Event> batch, CancellationToken ct)
	{
		if (!GameLogLineParser.TryParse(line, out var parsed))
			return false;

		var map = EventMapper.Map(parsed, DateTimeOffset.UtcNow);
		var ev = map.Event;

		ev.EventHash = ComputeEventHash(ev.Raw);

		await ResolveDimensionsAsync(ev, map, ct);
		await EnsurePlayerDataAsync(ev, map, ct);
		await EnsureQuestDataAsync(ev, map, ct);

		batch.Add(ev);
		return true;
	}

	/// <summary>
	/// Resolve todas as dimensões do evento (Channel, ActionType, Zone, Item, Boss).
	/// </summary>
	private async Task ResolveDimensionsAsync(Event ev, EventMap map, CancellationToken ct)
	{
		ev.ChannelId = await _dimensions.GetOrCreateChannelIdAsync(map.ChannelName, ct);
		ev.ActionTypeId = await _dimensions.GetOrCreateActionTypeIdAsync(map.ActionTypeName, ct);
		ev.ZoneId = await _dimensions.GetOrCreateZoneIdAsync(map.ZoneName, ct);
		ev.ItemId = await _dimensions.GetOrCreateItemIdAsync(map.ItemName, ct);
		ev.BossId = await _dimensions.GetOrCreateBossIdAsync(map.BossName, ct);
	}

	/// <summary>
	/// Garante a existência dos players envolvidos e atualiza snapshot do player principal.
	/// </summary>
	private async Task EnsurePlayerDataAsync(Event ev, EventMap map, CancellationToken ct)
	{
		await _upserter.EnsurePlayersAsync(
			new[] { ev.PlayerId, ev.VictimPlayerId, ev.KillerPlayerId },
			ct);

		if (!string.IsNullOrWhiteSpace(ev.PlayerId))
		{
			await _upserter.UpsertPlayerSnapshotAsync(
				ev.PlayerId!,
				map.PlayerName,
				map.PlayerLevel ?? ev.PlayerLevel,
				ev.ZoneId,
				ct);
		}
	}

	/// <summary>
	/// Garante a existência da quest e atualiza seu nome quando disponível.
	/// </summary>
	private async Task EnsureQuestDataAsync(Event ev, EventMap map, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(ev.QuestId))
			return;

		await _upserter.EnsureQuestAsync(ev.QuestId!, ct);

		if (!string.IsNullOrWhiteSpace(map.QuestName))
			await _upserter.UpsertQuestNameAsync(ev.QuestId!, map.QuestName!, ct);
	}

	/// <summary>
	/// Persiste o batch atual no banco e atualiza os contadores.
	/// </summary>
	private async Task FlushBatchAsync(ImportContext context, CancellationToken ct)
	{
		var (inserted, skipped) = await _store.InsertBatchAsync(context.Batch, ct);
		
		context.EventsInserted += inserted;
		context.DuplicatesSkipped += skipped;
		context.Batch.Clear();
	}

	/// <summary>
	/// Calcula o hash SHA-256 do evento para garantir idempotência na importação.
	/// </summary>
	private static string ComputeEventHash(string rawEvent)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawEvent.Trim()));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}

	/// <summary>
	/// Contexto mutável para rastrear o progresso da importação.
	/// </summary>
	private sealed class ImportContext
	{
		public long LinesRead { get; set; }
		public long EventsInserted { get; set; }
		public long DuplicatesSkipped { get; set; }
		public List<Event> Batch { get; }

		public ImportContext(int batchSize)
		{
			Batch = new List<Event>(batchSize);
		}
	}
}
