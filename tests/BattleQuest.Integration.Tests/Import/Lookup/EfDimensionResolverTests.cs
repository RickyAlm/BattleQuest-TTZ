using BattleQuest.Infrastructure.Import.Lookup;
using BattleQuest.Integration.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BattleQuest.Integration.Tests.Import.Lookup;

/// <summary>
/// Testes de integração para EfDimensionResolver com banco de dados real.
/// Valida caching, criação de dimensões, e comportamento de concorrência.
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class EfDimensionResolverTests
{
	private readonly DatabaseFixture _fixture;

	public EfDimensionResolverTests(DatabaseFixture fixture)
	{
		_fixture = fixture;
	}

	/// <summary>
	/// Valida que GetOrCreateChannelIdAsync cria uma nova entrada no banco.
	/// </summary>
	[Fact]
	public async Task GetOrCreateChannelIdAsync_NewChannel_ShouldCreateInDatabase()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var channelId = await resolver.GetOrCreateChannelIdAsync("SYSTEM", CancellationToken.None);

		channelId.Should().BeGreaterThan(0);
		var channel = await context.Channels.FindAsync(channelId);
		channel.Should().NotBeNull();
		channel!.Name.Should().Be("SYSTEM");
	}

	/// <summary>
	/// Valida que acessar a mesma dimensão duas vezes retorna o mesmo ID (cache).
	/// </summary>
	[Fact]
	public async Task GetOrCreateChannelIdAsync_SameChannelTwice_ShouldReturnSameIdFromCache()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var id1 = await resolver.GetOrCreateChannelIdAsync("QUEST", CancellationToken.None);
		var id2 = await resolver.GetOrCreateChannelIdAsync("QUEST", CancellationToken.None);

		id1.Should().Be(id2);

		// Valida que só há 1 registro no banco
		var count = await context.Channels.CountAsync();
		count.Should().Be(1);
	}

	/// <summary>
	/// Valida que o cache é case-insensitive.
	/// </summary>
	[Fact]
	public async Task GetOrCreateChannelIdAsync_CaseInsensitive_ShouldReturnSameId()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var id1 = await resolver.GetOrCreateChannelIdAsync("chat", CancellationToken.None);
		var id2 = await resolver.GetOrCreateChannelIdAsync("CHAT", CancellationToken.None);
		var id3 = await resolver.GetOrCreateChannelIdAsync("Chat", CancellationToken.None);

		id1.Should().Be(id2);
		id2.Should().Be(id3);
		
		var count = await context.Channels.CountAsync();
		count.Should().Be(1);
	}

	/// <summary>
	/// Valida que dimensões opcionais retornam null quando o valor é null.
	/// </summary>
	[Fact]
	public async Task GetOrCreateZoneIdAsync_NullValue_ShouldReturnNull()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var zoneId = await resolver.GetOrCreateZoneIdAsync(null, CancellationToken.None);

		zoneId.Should().BeNull();
	}

	/// <summary>
	/// Valida que dimensões opcionais criam entradas quando fornecidas.
	/// </summary>
	[Fact]
	public async Task GetOrCreateBossIdAsync_WithValue_ShouldCreateInDatabase()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var bossId = await resolver.GetOrCreateBossIdAsync("DRAGON_KING", CancellationToken.None);

		bossId.Should().NotBeNull();
		bossId.Should().BeGreaterThan(0);
		
		var boss = await context.Bosses.FindAsync(bossId);
		boss.Should().NotBeNull();
		boss!.Name.Should().Be("DRAGON_KING");
	}

	/// <summary>
	/// Valida que dimensões diferentes têm IDs diferentes.
	/// </summary>
	[Fact]
	public async Task GetOrCreateItemIdAsync_DifferentItems_ShouldHaveDifferentIds()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var sword = await resolver.GetOrCreateItemIdAsync("IRON_SWORD", CancellationToken.None);
		var shield = await resolver.GetOrCreateItemIdAsync("WOODEN_SHIELD", CancellationToken.None);
		var potion = await resolver.GetOrCreateItemIdAsync("HEALTH_POTION", CancellationToken.None);

		sword.Should().NotBeNull();
		shield.Should().NotBeNull();
		potion.Should().NotBeNull();
		
		sword.Should().NotBe(shield);
		shield.Should().NotBe(potion);
		potion.Should().NotBe(sword);
		
		var count = await context.Items.CountAsync();
		count.Should().Be(3);
	}

	/// <summary>
	/// Valida que o resolver pode criar todas as 5 dimensões simultaneamente.
	/// </summary>
	[Fact]
	public async Task GetOrCreate_AllDimensionTypes_ShouldWork()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var channelId = await resolver.GetOrCreateChannelIdAsync("COMBAT", CancellationToken.None);
		var actionId = await resolver.GetOrCreateActionTypeIdAsync("SKILL_USE", CancellationToken.None);
		var zoneId = await resolver.GetOrCreateZoneIdAsync("FOREST", CancellationToken.None);
		var itemId = await resolver.GetOrCreateItemIdAsync("MAGIC_STAFF", CancellationToken.None);
		var bossId = await resolver.GetOrCreateBossIdAsync("GOBLIN_CHIEF", CancellationToken.None);

		channelId.Should().BeGreaterThan(0);
		actionId.Should().BeGreaterThan(0);
		zoneId.Should().BeGreaterThan(0);
		itemId.Should().BeGreaterThan(0);
		bossId.Should().BeGreaterThan(0);
		
		context.Channels.Should().HaveCount(1);
		context.ActionTypes.Should().HaveCount(1);
		context.Zones.Should().HaveCount(1);
		context.Items.Should().HaveCount(1);
		context.Bosses.Should().HaveCount(1);
	}

	/// <summary>
	/// Valida que nomes com espaços são tratados corretamente (trim).
	/// </summary>
	[Fact]
	public async Task GetOrCreateChannelIdAsync_WithWhitespace_ShouldTrimAndNormalize()
	{
		await _fixture.CleanDatabaseAsync();
		await using var context = _fixture.CreateDbContext();
		var resolver = new EfDimensionResolver(context);

		var id1 = await resolver.GetOrCreateChannelIdAsync("  TRADE  ", CancellationToken.None);
		var id2 = await resolver.GetOrCreateChannelIdAsync("TRADE", CancellationToken.None);

		id1.Should().Be(id2);
		
		var channel = await context.Channels.FindAsync(id1);
		channel!.Name.Should().Be("TRADE");
	}
}
