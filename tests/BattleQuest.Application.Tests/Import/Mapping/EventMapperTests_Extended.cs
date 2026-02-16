using BattleQuest.Application.Import.Mapping;
using BattleQuest.Application.Import.Parsing;
using FluentAssertions;
using Xunit;

namespace BattleQuest.Application.Tests.Import.Mapping;

/// <summary>
/// Testes estendidos para EventMapper cobrindo edge cases e heurísticas complexas.
/// </summary>
public class EventMapperTests_Extended
{
	/// <summary>
	/// Valida que dimensões opcionais ausentes retornam null.
	/// </summary>
	[Fact]
	public void Map_EventWithoutOptionalDimensions_ShouldHaveNullValues()
	{
		var line = @"2025-08-04 14:02:14 [SYSTEM] SERVER_ANNOUNCEMENT text=""Double XP event started""";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.ZoneName.Should().BeNull();
		mapped.ItemName.Should().BeNull();
		mapped.BossName.Should().BeNull();
		mapped.QuestName.Should().BeNull();
		mapped.PlayerName.Should().BeNull();
		mapped.PlayerLevel.Should().BeNull();

		mapped.Event.ZoneId.Should().BeNull();
		mapped.Event.ItemId.Should().BeNull();
		mapped.Event.BossId.Should().BeNull();
	}

	/// <summary>
	/// Valida identificação do player principal usando campo alternativo "id" (PLAYER_JOIN).
	/// </summary>
	[Fact]
	public void Map_PlayerJoin_ShouldUseIdAsPlayerId()
	{
		var line = @"2025-08-04 14:05:59 [INFO] PLAYER_JOIN id=p6 name=""Frank"" level=39 zone=MysticLake";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.PlayerId.Should().Be("p6");
		mapped.PlayerName.Should().Be("Frank");
		mapped.PlayerLevel.Should().Be(39);
	}

	/// <summary>
	/// Valida identificação do player usando campo "defeated_by" (BOSS_DEFEAT).
	/// </summary>
	[Fact]
	public void Map_BossDefeat_ShouldUseDefeatedByAsPlayerId()
	{
		var line = @"2025-08-04 14:00:27 [COMBAT] BOSS_DEFEAT boss_name=GolemKing defeated_by=p2 xp=4752 gold=483";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.PlayerId.Should().Be("p2");
		mapped.BossName.Should().Be("GOLEMKING");
	}

	/// <summary>
	/// Valida heurística de nome: quando há quest_id, "name" refere-se à quest.
	/// </summary>
	[Fact]
	public void Map_QuestStartWithName_ShouldMapNameToQuest()
	{
		var line = @"2025-08-04 14:07:28 [GAME] QUEST_START player_id=p1 quest_id=q593 name=""Quest q593""";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.QuestId.Should().Be("q593");
		mapped.QuestName.Should().Be("Quest q593");
		mapped.PlayerName.Should().BeNull();
	}

	/// <summary>
	/// Valida heurística de nome: quando há player_id + level, "name" refere-se ao player.
	/// </summary>
	[Fact]
	public void Map_PlayerJoinWithNameAndLevel_ShouldMapNameToPlayer()
	{
		var line = @"2025-08-04 14:19:43 [INFO] PLAYER_JOIN id=p1 name=""Alice"" level=5 zone=GreenFields";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.PlayerName.Should().Be("Alice");
		mapped.PlayerLevel.Should().Be(5);
		mapped.QuestName.Should().BeNull();
	}

	/// <summary>
	/// Valida parsing de localização no formato "(x,y)".
	/// </summary>
	[Fact]
	public void Map_EventWithLocation_ShouldParseCoordinates()
	{
		var line = @"2025-08-04 14:06:46 [GAME] PLAYER_RESPAWN player_id=p6 location=(101,67) hp=100";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.LocationX.Should().Be(101);
		mapped.Event.LocationY.Should().Be(67);
	}

	/// <summary>
	/// Valida parsing de localização sem parênteses "x,y".
	/// </summary>
	[Fact]
	public void Map_EventWithLocationNoParentheses_ShouldParseCoordinates()
	{
		var line = @"2025-08-04 14:10:57 [GAME] PLAYER_RESPAWN player_id=p2 location=66,135 hp=100";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.LocationX.Should().Be(66);
		mapped.Event.LocationY.Should().Be(135);
	}

	/// <summary>
	/// Valida que localização inválida retorna null nas coordenadas.
	/// </summary>
	[Theory]
	[InlineData("location=invalid")]
	[InlineData("location=123")]
	[InlineData("location=(,)")]
	public void Map_EventWithInvalidLocation_ShouldReturnNullCoordinates(string locationField)
	{
		var line = $"2025-08-04 14:00:00 [GAME] ZONE_ENTER player_id=p1 {locationField}";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.LocationX.Should().BeNull();
		mapped.Event.LocationY.Should().BeNull();
	}

	/// <summary>
	/// Valida que quantidade pode vir de "qty" ou "quantity".
	/// </summary>
	[Theory]
	[InlineData("qty=5", 5)]
	[InlineData("quantity=10", 10)]
	public void Map_EventWithQuantity_ShouldMapFromQtyOrQuantity(string field, int expectedQty)
	{
		var line = $"2025-08-04 14:09:52 [GAME] ITEM_PICKUP player_id=p5 item=health_potion {field}";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.Quantity.Should().Be(expectedQty);
	}

	/// <summary>
	/// Valida que mensagem de chat usa campo "message" (CHAT MESSAGE).
	/// </summary>
	[Fact]
	public void Map_ChatMessage_ShouldMapFromMessageField()
	{
		var line = @"2025-08-04 14:01:14 [CHAT] MESSAGE player_id=p1 message=""Hello""";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = new DateTimeOffset(2026, 02, 16, 12, 0, 0, TimeSpan.Zero);
		var mapped = EventMapper.Map(parsed, now);

		mapped.ChannelName.Should().Be("CHAT");
		mapped.ActionTypeName.Should().Be("MESSAGE");

		mapped.Event.PlayerId.Should().Be("p1");
		mapped.Event.MessageText.Should().Be("Hello");
	}

	/// <summary>
	/// Valida que anúncio do sistema usa campo "text" (SYSTEM SERVER_ANNOUNCEMENT).
	/// </summary>
	[Fact]
	public void Map_SystemAnnouncement_ShouldMapFromTextField()
	{
		var line = @"2025-08-04 14:02:14 [SYSTEM] SERVER_ANNOUNCEMENT text=""System alert""";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = new DateTimeOffset(2026, 02, 16, 12, 0, 0, TimeSpan.Zero);
		var mapped = EventMapper.Map(parsed, now);

		mapped.ChannelName.Should().Be("SYSTEM");
		mapped.ActionTypeName.Should().Be("SERVER_ANNOUNCEMENT");

		mapped.Event.MessageText.Should().Be("System alert");
	}

	/// <summary>
	/// Valida que level pode vir de "level" ou "player_level".
	/// </summary>
	[Theory]
	[InlineData("level=25", 25)]
	[InlineData("player_level=30", 30)]
	public void Map_EventWithPlayerLevel_ShouldMapFromLevelOrPlayerLevel(string field, int expectedLevel)
	{
		var line = $"2025-08-04 14:19:43 [INFO] PLAYER_JOIN id=p1 name=\"Alice\" {field} zone=GreenFields";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.PlayerLevel.Should().Be(expectedLevel);
	}

	/// <summary>
	/// Valida normalização de nomes de dimensões para MAIÚSCULO.
	/// </summary>
	[Fact]
	public void Map_DimensionNames_ShouldBeNormalizedToUppercase()
	{
		var line = @"2025-08-04 14:00:00 [game] item_pickup player_id=p1 item=health_Potion zone=Dark_Cave";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.ChannelName.Should().Be("GAME");
		mapped.ActionTypeName.Should().Be("ITEM_PICKUP");
		mapped.ItemName.Should().Be("HEALTH_POTION");
		mapped.ZoneName.Should().Be("DARK_CAVE");
	}

	/// <summary>
	/// Valida que EventHash é inicialmente vazio (preenchido pelo pipeline).
	/// </summary>
	[Fact]
	public void Map_Event_ShouldHaveEmptyEventHash()
	{
		var line = @"2025-08-04 14:02:19 [GAME] ZONE_ENTER player_id=p2 zone=AncientRuins";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.EventHash.Should().BeEmpty();
	}

	/// <summary>
	/// Valida que linha original é preservada no campo Raw.
	/// </summary>
	[Fact]
	public void Map_Event_ShouldPreserveRawLine()
	{
		var line = @"2025-08-04 14:24:19 [GAME] SCORE player_id=p3 points=681 reason=defeated_monster";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = DateTimeOffset.UtcNow;
		var mapped = EventMapper.Map(parsed, now);

		mapped.Event.Raw.Should().Be(line);
	}
}
