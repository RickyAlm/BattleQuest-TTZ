using BattleQuest.Application.Import.Parsing;
using FluentAssertions;
using Xunit;

namespace BattleQuest.Application.Tests.Import.Parsing;

/// <summary>
/// Testes unitários para o GameLogLineParser.
/// Valida o parsing de diferentes formatos de linhas de log.
/// </summary>
public class GameLogLineParserTests
{
	/// <summary>
	/// Valida o parsing de anúncios do sistema com texto entre aspas.
	/// </summary>
	[Fact]
	public void TryParse_SystemAnnouncement_WithQuotedText_ShouldParseFields()
	{
		var line = @"2025-08-04 14:02:14 [SYSTEM] SERVER_ANNOUNCEMENT text=""Double XP event started""";

		var ok = GameLogLineParser.TryParse(line, out var parsed);

		ok.Should().BeTrue();
		parsed.Raw.Should().Be(line);

		parsed.Channel.Should().Be("SYSTEM");
		parsed.ActionType.Should().Be("SERVER_ANNOUNCEMENT");

		parsed.OccurredAt.Year.Should().Be(2025);
		parsed.OccurredAt.Month.Should().Be(8);
		parsed.OccurredAt.Day.Should().Be(4);
		parsed.OccurredAt.Hour.Should().Be(14);
		parsed.OccurredAt.Minute.Should().Be(2);
		parsed.OccurredAt.Second.Should().Be(14);

		parsed.Fields.Should().ContainKey("text");
		parsed.Fields["text"].Should().Be("Double XP event started");
	}

	/// <summary>
	/// Valida o parsing de coleta de itens com localização geográfica.
	/// </summary>
	[Fact]
	public void TryParse_ItemPickup_WithLocation_ShouldParseFields()
	{
		var line = @"2025-08-04 14:09:52 [GAME] ITEM_PICKUP player_id=p5 item=health_potion qty=9 location=(90,95)";

		var ok = GameLogLineParser.TryParse(line, out var parsed);

		ok.Should().BeTrue();
		parsed.Channel.Should().Be("GAME");
		parsed.ActionType.Should().Be("ITEM_PICKUP");

		parsed.Fields["player_id"].Should().Be("p5");
		parsed.Fields["item"].Should().Be("health_potion");
		parsed.Fields["qty"].Should().Be("9");

		parsed.Fields.Should().ContainKey("location");
		parsed.Fields["location"].Should().Be("(90,95)");
	}

	/// <summary>
	/// Valida o parsing de mensagens de chat sem aspas.
	/// </summary>
	[Fact]
	public void TryParse_ChatMessage_Unquoted_ShouldParseMessage()
	{
		var line = @"2025-08-04 14:01:14 [CHAT] MESSAGE player_id=p1 message=GG!";

		var ok = GameLogLineParser.TryParse(line, out var parsed);

		ok.Should().BeTrue();
		parsed.Channel.Should().Be("CHAT");
		parsed.ActionType.Should().Be("MESSAGE");

		parsed.Fields["player_id"].Should().Be("p1");
		parsed.Fields["message"].Should().Be("GG!");
	}
}
