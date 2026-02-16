using BattleQuest.Application.Import;
using BattleQuest.Infrastructure.Database;
using BattleQuest.Infrastructure.Import.Lookup;
using BattleQuest.Infrastructure.Import.Persistence;
using BattleQuest.Infrastructure.Import.Upsert;
using BattleQuest.Integration.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BattleQuest.Integration.Tests.Import;

/// <summary>
/// Testes de integração end-to-end do GameLogImportPipeline.
/// Valida comportamento completo do pipeline com banco de dados real.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class GameLogImportPipelineIntegrationTests
{
	private readonly DatabaseFixture _fixture;

	public GameLogImportPipelineIntegrationTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
	}

	/// <summary>
	/// Valida importação completa de arquivo com múltiplas linhas.
	/// </summary>
	[Fact]
	public async Task ImportAsync_ValidLogLines_ShouldImportSuccessfully()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline();

		var logLines = new[]
		{
			"2025-08-04 14:00:00 [GAME] QUEST_START player_id=p1 quest_id=q285 name=\"Quest q285\"",
			"2025-08-04 14:05:00 [INFO] PLAYER_JOIN id=p1 name=Alice level=10 zone=GreenFields",
			"2025-08-04 14:10:00 [GAME] QUEST_COMPLETE player_id=p1 quest_id=q285 xp=450 gold=120",
			"2025-08-04 14:15:00 [GAME] ITEM_PICKUP player_id=p1 item=health_potion qty=5 location=(120,85)"
		};

		var result = await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);

		result.LinesRead.Should().Be(4);
		result.EventsInserted.Should().Be(4);
		result.DuplicatesSkipped.Should().Be(0);

		await using var context = _fixture.CreateDbContext();
		
		// Valida dimensões criadas
		context.Channels.Count().Should().BeGreaterThanOrEqualTo(2); // GAME, INFO
		context.ActionTypes.Count().Should().BeGreaterThanOrEqualTo(4); // QUEST_START, PLAYER_JOIN, QUEST_COMPLETE, ITEM_PICKUP
		context.Zones.Count().Should().BeGreaterThanOrEqualTo(1); // GreenFields
		context.Items.Count().Should().BeGreaterThanOrEqualTo(1); // health_potion
		
		// Valida eventos inseridos
		context.Events.Should().HaveCount(4);
		
		// Valida players criados
		context.Players.Should().HaveCount(1);
		var player = await context.Players.FindAsync("p1");
		player.Should().NotBeNull();
		player!.Name.Should().Be("Alice");
		player.LastKnownLevel.Should().Be(10);
		
		// Valida quests criadas
		context.Quests.Should().HaveCount(1);
		var quest = await context.Quests.FindAsync("q285");
		quest.Should().NotBeNull();
		quest!.Name.Should().Be("Quest q285");
	}

	/// <summary>
	/// Valida que linhas inválidas são contabilizadas mas não impedem o processamento.
	/// </summary>
	[Fact]
	public async Task ImportAsync_MixedValidAndInvalidLines_ShouldProcessOnlyValid()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline();

		var logLines = new[]
		{
			"2025-08-04 14:00:00 [GAME] QUEST_START player_id=p1 quest_id=q117 name=\"Quest q117\"",
			"INVALID LINE WITHOUT TIMESTAMP",
			"2025-08-04 14:05:00 [COMBAT] BOSS_DAMAGE player_id=p2 boss_name=GolemKing damage=520",
			"",
			"2025-08-04 14:10:00 [GAME] ITEM_PICKUP player_id=p3 item=mana_potion qty=3 location=(95,140)"
		};

		var result = await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);

		result.LinesRead.Should().Be(5);
		result.EventsInserted.Should().Be(3); // Apenas válidas
		result.DuplicatesSkipped.Should().Be(0); // Linhas inválidas não são contadas

		await using var context = _fixture.CreateDbContext();
		context.Events.Should().HaveCount(3);
		context.Players.Should().HaveCount(3);
	}

	/// <summary>
	/// Valida idempotência: importar o mesmo arquivo duas vezes não duplica eventos.
	/// </summary>
	[Fact]
	public async Task ImportAsync_SameLogTwice_ShouldBeIdempotent()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline();

		var logLines = new[]
		{
			"2025-08-04 14:00:00 [GAME] QUEST_START player_id=p1 quest_id=q593 name=\"Quest q593\"",
			"2025-08-04 14:05:00 [GAME] ZONE_ENTER player_id=p1 zone=DarkCave",
			"2025-08-04 14:10:00 [GAME] QUEST_COMPLETE player_id=p1 quest_id=q593 xp=350 gold=85"
		};

		// Primeira importação
		var result1 = await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);
		
		// Segunda importação (mesmas linhas)
		var result2 = await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);

		result1.EventsInserted.Should().Be(3);
		result2.EventsInserted.Should().Be(0); // Todos duplicados
		result2.DuplicatesSkipped.Should().Be(3); // Hash detecta duplicatas

		await using var context = _fixture.CreateDbContext();
		
		// Eventos não devem ser duplicados (hash garante idempotência)
		var eventCount = await context.Events.CountAsync();
		eventCount.Should().Be(3); // Não 6!
		
		// Players/Quests também não devem duplicar
		context.Players.Should().HaveCount(1);
		context.Quests.Should().HaveCount(1);
	}

	/// <summary>
	/// Valida processamento em batches quando há muitas linhas.
	/// </summary>
	[Fact]
	public async Task ImportAsync_LargeLog_ShouldProcessInBatches()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline(batchSize: 50);

		// Gera 150 linhas únicas (3 batches de 50)
		// Garante timestamps únicos para evitar hashes duplicados
		var logLines = Enumerable.Range(1, 150)
			.Select(i => $"2025-08-04 {14 + i / 3600:D2}:{(i / 60) % 60:D2}:{i % 60:D2} [GAME] SCORE player_id=p{i % 10} points={100 + i} reason=defeated_monster")
			.ToArray();

		var result = await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);

		result.LinesRead.Should().Be(150);
		result.EventsInserted.Should().Be(150);

		await using var context = _fixture.CreateDbContext();
		context.Events.Should().HaveCount(150);
		context.Players.Should().HaveCount(10);
	}

	/// <summary>
	/// Valida que arquivo vazio não causa erros.
	/// </summary>
	[Fact]
	public async Task ImportAsync_EmptyFile_ShouldReturnZeros()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline();

		var result = await pipeline.ImportAsync(ToAsyncEnumerable(Array.Empty<string>()), CancellationToken.None);

		result.LinesRead.Should().Be(0);
		result.EventsInserted.Should().Be(0);
		result.DuplicatesSkipped.Should().Be(0);
	}

	/// <summary>
	/// Valida que todas as linhas inválidas são contabilizadas.
	/// </summary>
	[Fact]
	public async Task ImportAsync_OnlyInvalidLines_ShouldSkipAll()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline();

		var logLines = new[]
		{
			"INVALID",
			"ALSO INVALID",
			"",
			"   "
		};

		var result = await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);

		result.LinesRead.Should().Be(4);
		result.EventsInserted.Should().Be(0);
		result.DuplicatesSkipped.Should().Be(0);

		await using var context = _fixture.CreateDbContext();
		context.Events.Should().HaveCount(0);
	}

	/// <summary>
	/// Valida snapshot de player: nome e nível são atualizados.
	/// </summary>
	[Fact]
	public async Task ImportAsync_PlayerSnapshot_ShouldUpdateWithLatestInfo()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline();

		var logLines = new[]
		{
			"2025-08-04 14:00:00 [INFO] PLAYER_JOIN id=hero name=Alice level=1 zone=GreenFields",
			"2025-08-04 14:05:00 [COMBAT] BOSS_DAMAGE player_id=hero boss_name=LichQueen damage=380 level=5 zone=DarkCave",
			"2025-08-04 14:10:00 [COMBAT] BOSS_DAMAGE player_id=hero boss_name=ShadowDragon damage=450 level=6 zone=AncientRuins"
		};

		await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);

		await using var context = _fixture.CreateDbContext();
		var player = await context.Players.FindAsync("hero");
		
		player.Should().NotBeNull();
		player!.Name.Should().Be("Alice");
		player.LastKnownLevel.Should().Be(6); // Último nível
	}

	/// <summary>
	/// Valida que dimensões são normalizadas (case-insensitive, trim).
	/// </summary>
	[Fact]
	public async Task ImportAsync_DimensionNormalization_ShouldNotDuplicate()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline();

		var logLines = new[]
		{
			"2025-08-04 14:00:00 [game] QUEST_START player_id=p1 quest_id=q001 name=\"Quest q001\"",
			"2025-08-04 14:05:00 [GAME] QUEST_COMPLETE player_id=p1 quest_id=q001 xp=200 gold=50",
			"2025-08-04 14:10:00 [Game] ZONE_ENTER player_id=p1 zone=MysticLake"
		};

		await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);

		await using var context = _fixture.CreateDbContext();
		var gameChannels = await context.Channels
			.Where(c => c.Name.ToUpper() == "GAME")
			.ToListAsync();
		
		gameChannels.Should().HaveCount(1);
	}

	/// <summary>
	/// Valida performance com arquivo médio (5000 linhas).
	/// </summary>
	[Fact]
	public async Task ImportAsync_MediumFile_ShouldCompleteReasonablyFast()
	{
		await _fixture.CleanDatabaseAsync();
		var pipeline = CreatePipeline(batchSize: 1000);

		var logLines = Enumerable.Range(1, 5000)
			.Select(i => $"2025-08-04 {14 + i / 3600:D2}:{(i / 60) % 60:D2}:{i % 60:D2} [CHAT] MESSAGE player_id=p{i % 100} message=\"Test message {i}\"")
			.ToArray();

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var result = await pipeline.ImportAsync(ToAsyncEnumerable(logLines), CancellationToken.None);
		stopwatch.Stop();

		result.EventsInserted.Should().Be(5000);
		stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));

		await using var context = _fixture.CreateDbContext();
		context.Events.Should().HaveCount(5000);
	}

	/// <summary>
	/// Helper: cria uma instância do pipeline com todas as dependências reais.
	/// </summary>
	private GameLogImportPipeline CreatePipeline(int batchSize = 2000)
	{
		var context = _fixture.CreateDbContext();
		
		var dimensionResolver = new EfDimensionResolver(context);
		var eventStore = new EfEventIngestionStore(context);
		var playerQuestUpserter = new EfPlayerQuestUpserter(context);
		
		var options = Options.Create(new ImportOptions { BatchSize = batchSize });
		
		return new GameLogImportPipeline(
			dimensionResolver,
			playerQuestUpserter,
			eventStore,
			options);
	}

	/// <summary>
	/// Helper: converte array em IAsyncEnumerable.
	/// </summary>
	private static async IAsyncEnumerable<string> ToAsyncEnumerable(string[] lines)
	{
		foreach (var line in lines)
		{
			await Task.Yield();
			yield return line;
		}
	}
}
