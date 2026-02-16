using BattleQuest.Application.Import.Mapping;
using BattleQuest.Application.Import.Parsing;
using FluentAssertions;
using Xunit;

namespace BattleQuest.Application.Tests.Import.Mapping;

/// <summary>
/// Testes unitários para o EventMapper.
/// Valida o mapeamento de linhas parseadas para eventos de domínio.
/// </summary>
public class EventMapperTests
{
	/// <summary>
	/// Valida o mapeamento de evento de coleta de item e normalização para MAIÚSCULO.
	/// </summary>
	[Fact]
	public void Map_ItemPickup_ShouldFillEventMapAndNormalizeNamesToUpper()
	{
		var line = @"2025-08-04 14:09:52 [GAME] ITEM_PICKUP player_id=p5 item=health_potion qty=9 location=(90,95)";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = new DateTimeOffset(2026, 02, 16, 12, 0, 0, TimeSpan.Zero);
		var mapped = EventMapper.Map(parsed, now);

		mapped.ChannelName.Should().Be("GAME");
		mapped.ActionTypeName.Should().Be("ITEM_PICKUP");

		mapped.ItemName.Should().Be("HEALTH_POTION");
		mapped.ZoneName.Should().BeNull();
		mapped.BossName.Should().BeNull();

		mapped.Event.Raw.Should().Be(line);
		mapped.Event.OccurredAt.Should().Be(parsed.OccurredAt);
		mapped.Event.InsertedAt.Should().Be(now);

		mapped.Event.PlayerId.Should().Be("p5");
		mapped.Event.Quantity.Should().Be(9);

		mapped.Event.LocationX.Should().Be(90);
		mapped.Event.LocationY.Should().Be(95);
	}

	/// <summary>
	/// Valida o mapeamento de início de quest com nome entre aspas.
	/// </summary>
	[Fact]
	public void Map_QuestStart_WithQuotedName_ShouldFillQuestIdAndQuestName()
	{
		var line = @"2025-08-04 14:02:06 [GAME] QUEST_START player_id=p2 quest_id=q285 name=""Quest q285""";
		GameLogLineParser.TryParse(line, out var parsed).Should().BeTrue();

		var now = new DateTimeOffset(2026, 02, 16, 12, 0, 0, TimeSpan.Zero);
		var mapped = EventMapper.Map(parsed, now);

		mapped.ChannelName.Should().Be("GAME");
		mapped.ActionTypeName.Should().Be("QUEST_START");

		mapped.Event.PlayerId.Should().Be("p2");
		mapped.Event.QuestId.Should().Be("q285");
		mapped.QuestName.Should().Be("Quest q285");
	}
}
