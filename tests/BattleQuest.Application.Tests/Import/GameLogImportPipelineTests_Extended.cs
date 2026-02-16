using BattleQuest.Application.Import;
using BattleQuest.Application.Tests.Fakes;
using BattleQuest.Application.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BattleQuest.Application.Tests.Import;

/// <summary>
/// Testes estendidos para GameLogImportPipeline cobrindo edge cases e comportamentos específicos.
/// </summary>
public class GameLogImportPipelineTests_Extended
{
	/// <summary>
	/// Valida que linhas vazias são ignoradas sem afetar contadores.
	/// </summary>
	[Fact]
	public async Task ImportAsync_EmptyLines_ShouldBeIgnored()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:02:14 [SYSTEM] SERVER_ANNOUNCEMENT text=""Welcome to BattleQuest!""",
			"",
			"   ",
			@"2025-08-04 14:04:00 [SYSTEM] SERVER_ANNOUNCEMENT text=""PvP tournament registration open"""
		};

		// Act
		var result = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		result.LinesRead.Should().Be(4);
		result.EventsInserted.Should().Be(2);
		result.DuplicatesSkipped.Should().Be(0);
	}

	/// <summary>
	/// Valida que linhas inválidas (não parseáveis) são ignoradas.
	/// </summary>
	[Fact]
	public async Task ImportAsync_InvalidLines_ShouldBeIgnored()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:02:14 [SYSTEM] SERVER_ANNOUNCEMENT text=""World boss will spawn soon""",
			@"INVALID LINE WITHOUT TIMESTAMP",
			@"2025-08-04 14:04:25 [CHAT] MESSAGE player_id=p1 message=""Going to the cave"""
		};

		// Act
		var result = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		result.LinesRead.Should().Be(3);
		result.EventsInserted.Should().Be(2);
		store.InsertedEvents.Should().HaveCount(2);
	}

	/// <summary>
	/// Valida que batch size é respeitado ao processar eventos.
	/// </summary>
	[Fact]
	public async Task ImportAsync_BatchSize_ShouldFlushWhenReached()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 2 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:00:01 [GAME] ZONE_ENTER player_id=p1 zone=DarkCave",
			@"2025-08-04 14:00:02 [GAME] ZONE_EXIT player_id=p2 zone=MysticLake",
			@"2025-08-04 14:00:03 [GAME] ZONE_ENTER player_id=p3 zone=AncientRuins"
		};

		// Act
		var result = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		result.EventsInserted.Should().Be(3);
		// Batch 1: ZONE_ENTER, ZONE_EXIT (flush ao atingir 2)
		// Batch 2: ZONE_ENTER (flush final)
		store.InsertedEvents.Should().HaveCount(3);
	}

	/// <summary>
	/// Valida que eventos com mesmo hash são considerados duplicados.
	/// </summary>
	[Fact]
	public async Task ImportAsync_SameEventHash_ShouldBeDeduplicated()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var sameLine = @"2025-08-04 14:02:14 [SYSTEM] SERVER_ANNOUNCEMENT text=""Double XP event started""";
		var lines = new[] { sameLine, sameLine, sameLine };

		// Act
		var result = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		result.LinesRead.Should().Be(3);
		result.EventsInserted.Should().Be(1);
		result.DuplicatesSkipped.Should().Be(2);
		store.InsertedEvents.Should().HaveCount(1);
	}

	/// <summary>
	/// Valida que players são garantidos antes de processar evento.
	/// </summary>
	[Fact]
	public async Task ImportAsync_ShouldEnsurePlayersExist()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:02:17 [COMBAT] DEATH victim_id=p3 killer_id=p1 method=sword player_id=p1"
		};

		// Act
		await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		upserter.EnsurePlayersCalls.Should().Contain("p1");
		upserter.EnsurePlayersCalls.Should().Contain("p3");
	}

	/// <summary>
	/// Valida que snapshot de player é atualizado quando disponível.
	/// </summary>
	[Fact]
	public async Task ImportAsync_PlayerWithNameAndLevel_ShouldUpdateSnapshot()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:19:43 [INFO] PLAYER_JOIN id=p1 name=""Alice"" level=5 zone=GreenFields"
		};

		// Act
		await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		upserter.PlayerSnapshots.Should().ContainSingle();
		var snapshot = upserter.PlayerSnapshots[0];
		snapshot.playerId.Should().Be("p1");
		snapshot.name.Should().Be("Alice");
		snapshot.level.Should().Be(5);
	}

	/// <summary>
	/// Valida que quest é garantida quando presente no evento.
	/// </summary>
	[Fact]
	public async Task ImportAsync_EventWithQuest_ShouldEnsureQuestExists()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:02:06 [GAME] QUEST_START player_id=p2 quest_id=q285 name=""Quest q285"""
		};

		// Act
		await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		upserter.EnsureQuestCalls.Should().Contain("q285");
		upserter.QuestNameUpserts.Should().ContainSingle();
		upserter.QuestNameUpserts[0].questId.Should().Be("q285");
		upserter.QuestNameUpserts[0].questName.Should().Be("Quest q285");
	}

	/// <summary>
	/// Valida que todas as dimensões são resolvidas corretamente.
	/// </summary>
	[Fact]
	public async Task ImportAsync_ShouldResolvAllDimensions()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:00:27 [COMBAT] BOSS_DEFEAT boss_name=GolemKing defeated_by=p2 xp=4752 gold=483 zone=DarkCave item=dragon_scale"
		};

		// Act
		await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		dims.ChannelRequests.Should().Contain("COMBAT");
		dims.ActionRequests.Should().Contain("BOSS_DEFEAT");
		dims.ZoneRequests.Should().Contain("DARKCAVE");
		dims.ItemRequests.Should().Contain("DRAGON_SCALE");
		dims.BossRequests.Should().Contain("GOLEMKING");
	}

	/// <summary>
	/// Valida que importação com stream vazio retorna zeros.
	/// </summary>
	[Fact]
	public async Task ImportAsync_EmptyStream_ShouldReturnZeros()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = Array.Empty<string>();

		// Act
		var result = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		result.LinesRead.Should().Be(0);
		result.EventsInserted.Should().Be(0);
		result.DuplicatesSkipped.Should().Be(0);
	}

	/// <summary>
	/// Valida que último batch (mesmo incompleto) é processado.
	/// </summary>
	[Fact]
	public async Task ImportAsync_IncompleteBatch_ShouldBeProcessedAtEnd()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();
		var options = Options.Create(new ImportOptions { BatchSize = 10 });
		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var lines = new[] 
		{ 
			@"2025-08-04 14:00:01 [GAME] SCORE player_id=p1 points=100 reason=defeated_monster",
			@"2025-08-04 14:00:02 [GAME] SCORE player_id=p2 points=150 reason=defeated_monster",
			@"2025-08-04 14:00:03 [GAME] SCORE player_id=p3 points=200 reason=defeated_monster"
		};

		// Act
		var result = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert
		result.EventsInserted.Should().Be(3);
		store.InsertedEvents.Should().HaveCount(3);
	}
}
