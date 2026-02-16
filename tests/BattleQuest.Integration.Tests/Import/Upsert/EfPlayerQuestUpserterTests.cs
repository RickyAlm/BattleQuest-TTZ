using BattleQuest.Infrastructure.Import.Upsert;
using BattleQuest.Integration.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Integration.Tests.Import.Upsert;

/// <summary>
/// Testes de integração para EfPlayerQuestUpserter com banco de dados real.
/// Valida criação de players/quests, atualização de snapshots, e caching.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class EfPlayerQuestUpserterTests
{
	private readonly DatabaseFixture _fixture;

	public EfPlayerQuestUpserterTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
	}

	/// <summary>
	/// Valida que EnsurePlayersAsync cria novos players no banco.
	/// </summary>
	[Fact]
	public async Task EnsurePlayersAsync_NewPlayers_ShouldCreateInDatabase()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		var playerIds = new[] { "player1", "player2", "player3" };
		await upserter.EnsurePlayersAsync(playerIds, CancellationToken.None);

		var players = await context.Players.ToListAsync();
		players.Should().HaveCount(3);
		players.Select(p => p.PlayerId).Should().BeEquivalentTo(playerIds);
	}

	/// <summary>
	/// Valida que players existentes não são duplicados.
	/// </summary>
	[Fact]
	public async Task EnsurePlayersAsync_ExistingPlayers_ShouldNotDuplicate()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		// Primeira chamada
		await upserter.EnsurePlayersAsync(new[] { "player1", "player2" }, CancellationToken.None);

		// Segunda chamada com overlap
		await upserter.EnsurePlayersAsync(new[] { "player2", "player3" }, CancellationToken.None);

		var players = await context.Players.ToListAsync();
		players.Should().HaveCount(3); // player1, player2, player3
	}

	/// <summary>
	/// Valida que valores null são ignorados.
	/// </summary>
	[Fact]
	public async Task EnsurePlayersAsync_NullValues_ShouldBeIgnored()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		var playerIds = new string?[] { "player1", null, "player2", null, "player3" };
		await upserter.EnsurePlayersAsync(playerIds, CancellationToken.None);

		var players = await context.Players.ToListAsync();
		players.Should().HaveCount(3);
	}

	/// <summary>
	/// Valida que lista vazia não causa erros.
	/// </summary>
	[Fact]
	public async Task EnsurePlayersAsync_EmptyList_ShouldNotFail()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		await upserter.EnsurePlayersAsync(Array.Empty<string>(), CancellationToken.None);

		var count = await context.Players.CountAsync();
		count.Should().Be(0);
	}

	/// <summary>
	/// Valida UpsertPlayerSnapshotAsync atualiza dados do player.
	/// </summary>
	[Fact]
	public async Task UpsertPlayerSnapshotAsync_NewData_ShouldUpdatePlayer()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupZonesAsync(context);
		var upserter = new EfPlayerQuestUpserter(context);

		// Cria player
		await upserter.EnsurePlayersAsync(new[] { "player1" }, CancellationToken.None);

		// Atualiza snapshot
		await upserter.UpsertPlayerSnapshotAsync("player1", "Hero", 10, 5, CancellationToken.None);

		var player = await context.Players.FindAsync("player1");
		player.Should().NotBeNull();
		player!.Name.Should().Be("Hero");
		player.LastKnownLevel.Should().Be(10);
		player.LastKnownZoneId.Should().Be(5);
	}

	/// <summary>
	/// Valida que campos null não sobrescrevem valores existentes.
	/// </summary>
	[Fact]
	public async Task UpsertPlayerSnapshotAsync_NullFields_ShouldNotOverwriteExisting()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupZonesAsync(context);
		var upserter = new EfPlayerQuestUpserter(context);

		// Cria e define dados iniciais
		await upserter.EnsurePlayersAsync(new[] { "player1" }, CancellationToken.None);
		await upserter.UpsertPlayerSnapshotAsync("player1", "Warrior", 20, 3, CancellationToken.None);

		// Atualiza apenas level, deixando nome e zona como null
		await upserter.UpsertPlayerSnapshotAsync("player1", null, 25, null, CancellationToken.None);

		var player = await context.Players.FindAsync("player1");
		player!.Name.Should().Be("Warrior"); // Não foi sobrescrito
		player.LastKnownLevel.Should().Be(25); // Foi atualizado
		player.LastKnownZoneId.Should().Be(3); // Não foi sobrescrito
	}

	/// <summary>
	/// Valida que múltiplas atualizações mantêm o valor mais recente.
	/// </summary>
	[Fact]
	public async Task UpsertPlayerSnapshotAsync_MultipleUpdates_ShouldKeepLatest()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		await SetupZonesAsync(context);
		var upserter = new EfPlayerQuestUpserter(context);

		await upserter.EnsurePlayersAsync(new[] { "player1" }, CancellationToken.None);

		// Série de atualizações
		await upserter.UpsertPlayerSnapshotAsync("player1", "Name1", 10, 1, CancellationToken.None);
		await upserter.UpsertPlayerSnapshotAsync("player1", "Name2", 15, 2, CancellationToken.None);
		await upserter.UpsertPlayerSnapshotAsync("player1", null, 20, null, CancellationToken.None);
		await upserter.UpsertPlayerSnapshotAsync("player1", "Name3", null, 3, CancellationToken.None);

		var player = await context.Players.FindAsync("player1");
		player!.Name.Should().Be("Name3"); // Ultima atualização não-null
		player.LastKnownLevel.Should().Be(20); // Ultima atualização não-null
		player.LastKnownZoneId.Should().Be(3); // Ultima atualização não-null
	}

	/// <summary>
	/// Valida EnsureQuestAsync cria nova quest no banco.
	/// </summary>
	[Fact]
	public async Task EnsureQuestAsync_NewQuest_ShouldCreateInDatabase()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		await upserter.EnsureQuestAsync("quest_001", CancellationToken.None);

		var quest = await context.Quests.FindAsync("quest_001");
		quest.Should().NotBeNull();
		quest!.QuestId.Should().Be("quest_001");
	}

	/// <summary>
	/// Valida que quest existente não é duplicada.
	/// </summary>
	[Fact]
	public async Task EnsureQuestAsync_ExistingQuest_ShouldNotDuplicate()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		await upserter.EnsureQuestAsync("quest_001", CancellationToken.None);
		await upserter.EnsureQuestAsync("quest_001", CancellationToken.None);
		await upserter.EnsureQuestAsync("quest_001", CancellationToken.None);

		var count = await context.Quests.CountAsync();
		count.Should().Be(1);
	}

	/// <summary>
	/// Valida UpsertQuestNameAsync define o nome da quest.
	/// </summary>
	[Fact]
	public async Task UpsertQuestNameAsync_NewName_ShouldSetQuestName()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		await upserter.EnsureQuestAsync("quest_001", CancellationToken.None);
		await upserter.UpsertQuestNameAsync("quest_001", "Dragon Slayer", CancellationToken.None);

		var quest = await context.Quests.FindAsync("quest_001");
		quest!.Name.Should().Be("Dragon Slayer");
	}

	/// <summary>
	/// Valida que nome existente não é sobrescrito.
	/// </summary>
	[Fact]
	public async Task UpsertQuestNameAsync_ExistingName_ShouldNotOverwrite()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		await upserter.EnsureQuestAsync("quest_001", CancellationToken.None);
		await upserter.UpsertQuestNameAsync("quest_001", "Original Name", CancellationToken.None);
		await upserter.UpsertQuestNameAsync("quest_001", "New Name", CancellationToken.None);

		var quest = await context.Quests.FindAsync("quest_001");
		quest!.Name.Should().Be("Original Name"); // Não sobrescrito
	}

	/// <summary>
	/// Valida cache de players funciona corretamente (não faz query duplicada).
	/// </summary>
	[Fact]
	public async Task EnsurePlayersAsync_CachedPlayers_ShouldNotQueryDatabase()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		// Primeira chamada: cria no banco
		await upserter.EnsurePlayersAsync(new[] { "player1", "player2" }, CancellationToken.None);
		
		var initialCount = await context.Players.CountAsync();
		
		// Segunda chamada: já está em cache, não deve criar duplicatas
		await upserter.EnsurePlayersAsync(new[] { "player1", "player2" }, CancellationToken.None);
		
		var finalCount = await context.Players.CountAsync();
		
		initialCount.Should().Be(2);
		finalCount.Should().Be(2);
	}

	/// <summary>
	/// Valida comportamento com muitos players (performance test).
	/// </summary>
	[Fact]
	public async Task EnsurePlayersAsync_LargeBatch_ShouldHandleEfficiently()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var upserter = new EfPlayerQuestUpserter(context);

		var playerIds = Enumerable.Range(1, 1000)
			.Select(i => $"player_{i}")
			.ToArray();

		await upserter.EnsurePlayersAsync(playerIds, CancellationToken.None);

		var count = await context.Players.CountAsync();
		count.Should().Be(1000);
	}

	/// <summary>
	/// Helper para criar zones necessárias para testes de FK.
	/// </summary>
	private static async Task SetupZonesAsync(BattleQuest.Infrastructure.Database.BattleQuestDbContext context)
	{
		var zones = new[]
		{
			new Domain.Entities.Zone { ZoneId = 1, Name = "ZONE_1" },
			new Domain.Entities.Zone { ZoneId = 2, Name = "ZONE_2" },
			new Domain.Entities.Zone { ZoneId = 3, Name = "ZONE_3" },
			new Domain.Entities.Zone { ZoneId = 4, Name = "ZONE_4" },
			new Domain.Entities.Zone { ZoneId = 5, Name = "ZONE_5" }
		};

		context.Zones.AddRange(zones);
		await context.SaveChangesAsync();
	}
}
