using BattleQuest.Domain.Entities;
using BattleQuest.Infrastructure.Database;
using BattleQuest.Infrastructure.Import.Persistence;
using BattleQuest.Integration.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Integration.Tests.Import.Persistence;

/// <summary>
/// Testes de integração para EfEventIngestionStore com banco de dados real.
/// Valida inserção, deduplicação, idempotência e performance.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class EfEventIngestionStoreTests
{
	private readonly DatabaseFixture _fixture;

	public EfEventIngestionStoreTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
	}

	/// <summary>
	/// Valida que eventos são inseridos corretamente no banco.
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_NewEvents_ShouldInsertInDatabase()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		var events = new[]
		{
			CreateEvent("hash1", "Player joined"),
			CreateEvent("hash2", "Player completed quest"),
			CreateEvent("hash3", "Player gained item")
		};

		var (inserted, skipped) = await store.InsertBatchAsync(events, CancellationToken.None);

		inserted.Should().Be(3);
		skipped.Should().Be(0);
		
		var count = await context.Events.CountAsync();
		count.Should().Be(3);
	}

	/// <summary>
	/// Valida que eventos duplicados no mesmo batch são deduplicados.
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_DuplicatesInBatch_ShouldDeduplicateBeforeInsert()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		var events = new[]
		{
			CreateEvent("hash1", "Event A"),
			CreateEvent("hash1", "Event A duplicate"),
			CreateEvent("hash2", "Event B"),
			CreateEvent("hash1", "Event A another duplicate")
		};

		var (inserted, skipped) = await store.InsertBatchAsync(events, CancellationToken.None);

		inserted.Should().Be(2);
		skipped.Should().Be(0);
		
		var count = await context.Events.CountAsync();
		count.Should().Be(2);
	}

	/// <summary>
	/// Valida que eventos já existentes no banco são ignorados (idempotência).
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_ExistingEvents_ShouldSkipDuplicates()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		// Primeira inserção
		var initialEvents = new[]
		{
			CreateEvent("hash1", "Event A"),
			CreateEvent("hash2", "Event B")
		};
		await store.InsertBatchAsync(initialEvents, CancellationToken.None);

		// Segunda inserção com duplicatas
		var newEvents = new[]
		{
			CreateEvent("hash1", "Event A duplicate"),
			CreateEvent("hash3", "Event C"),
			CreateEvent("hash2", "Event B duplicate")
		};

		var (inserted, skipped) = await store.InsertBatchAsync(newEvents, CancellationToken.None);

		inserted.Should().Be(1); // Apenas hash3
		skipped.Should().Be(2); // hash1 e hash2
		
		var count = await context.Events.CountAsync();
		count.Should().Be(3); // Total: hash1, hash2, hash3
	}

	/// <summary>
	/// Valida que inserir lista vazia retorna (0, 0).
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_EmptyList_ShouldReturnZeros()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		var (inserted, skipped) = await store.InsertBatchAsync(Array.Empty<Event>(), CancellationToken.None);

		inserted.Should().Be(0);
		skipped.Should().Be(0);
	}

	/// <summary>
	/// Valida que múltiplas chamadas com os mesmos eventos são idempotentes.
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_Idempotency_SameEventsMultipleTimes()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		var events = new[]
		{
			CreateEvent("hash1", "Event A"),
			CreateEvent("hash2", "Event B")
		};

		// Primeira inserção
		var (inserted1, skipped1) = await store.InsertBatchAsync(events, CancellationToken.None);

		// Segunda inserção (mesmos eventos)
		var (inserted2, skipped2) = await store.InsertBatchAsync(events, CancellationToken.None);

		// Terceira inserção (mesmos eventos)
		var (inserted3, skipped3) = await store.InsertBatchAsync(events, CancellationToken.None);

		inserted1.Should().Be(2);
		skipped1.Should().Be(0);
		
		inserted2.Should().Be(0);
		skipped2.Should().Be(2);
		
		inserted3.Should().Be(0);
		skipped3.Should().Be(2);
		
		var count = await context.Events.CountAsync();
		count.Should().Be(2);
	}

	/// <summary>
	/// Valida inserção de lote grande (1000 eventos).
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_LargeBatch_ShouldHandleEfficiently()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		var events = Enumerable.Range(1, 1000)
			.Select(i => CreateEvent($"hash{i}", $"Event {i}"))
			.ToList();

		var (inserted, skipped) = await store.InsertBatchAsync(events, CancellationToken.None);

		inserted.Should().Be(1000);
		skipped.Should().Be(0);
		
		var count = await context.Events.CountAsync();
		count.Should().Be(1000);
	}

	/// <summary>
	/// Valida que hashes são case-sensitive.
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_HashCaseSensitive_ShouldTreatAsDifferent()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		var events = new[]
		{
			CreateEvent("HashA", "Event with uppercase hash"),
			CreateEvent("hasha", "Event with lowercase hash"),
			CreateEvent("HASHA", "Event with all uppercase hash")
		};

		var (inserted, skipped) = await store.InsertBatchAsync(events, CancellationToken.None);

		inserted.Should().Be(3);
		skipped.Should().Be(0);

		var count = await context.Events.CountAsync();
		count.Should().Be(3);
	}

	/// <summary>
	/// Valida comportamento quando alguns eventos são novos e outros já existem.
	/// </summary>
	[Fact]
	public async Task InsertBatchAsync_MixedNewAndExisting_ShouldProcessCorrectly()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupDimensionsAsync(context);
		var store = new EfEventIngestionStore(context);

		// Insere eventos iniciais
		var initialEvents = new[]
		{
			CreateEvent("hash1", "Event A"),
			CreateEvent("hash2", "Event B")
		};
		await store.InsertBatchAsync(initialEvents, CancellationToken.None);

		// Lote misto: alguns novos, alguns existentes, alguns duplicados no batch
		var mixedEvents = new[]
		{
			CreateEvent("hash1", "Duplicate A"),
			CreateEvent("hash3", "New C"),
			CreateEvent("hash3", "Duplicate C in batch"),
			CreateEvent("hash4", "New D"),
			CreateEvent("hash2", "Duplicate B"),
			CreateEvent("hash5", "New E")
		};

		var (inserted, skipped) = await store.InsertBatchAsync(mixedEvents, CancellationToken.None);

		// Deduplicado no batch primeiro: hash1, hash3, hash4, hash2, hash5 (5 eventos)
		// Já existem: hash1, hash2 (2 eventos)
		// Novos: hash3, hash4, hash5 (3 eventos)
		inserted.Should().Be(3);
		skipped.Should().Be(2);
		
		var count = await context.Events.CountAsync();
		count.Should().Be(5); // Total: hash1, hash2, hash3, hash4, hash5
	}

	/// <summary>
	/// Helper para criar dimensões básicas necessárias para eventos.
	/// </summary>
	private static async Task SetupDimensionsAsync(BattleQuestDbContext context)
	{
		context.Channels.Add(new Channel { ChannelId = 1, Name = "TEST" });
		context.ActionTypes.Add(new ActionType { ActionTypeId = 1, Name = "TEST_ACTION" });
		await context.SaveChangesAsync();
	}

	/// <summary>
	/// Helper para criar um evento de teste.
	/// </summary>
	private static Event CreateEvent(string hash, string debugInfo)
	{
		return new Event
		{
			EventHash = hash,
			OccurredAt = DateTimeOffset.UtcNow,
			ChannelId = 1,
			ActionTypeId = 1,
			Raw = debugInfo
		};
	}
}
