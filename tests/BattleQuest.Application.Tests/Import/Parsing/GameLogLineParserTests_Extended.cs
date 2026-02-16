using BattleQuest.Application.Import.Parsing;
using FluentAssertions;
using Xunit;

namespace BattleQuest.Application.Tests.Import.Parsing;

/// <summary>
/// Testes estendidos para GameLogLineParser cobrindo edge cases e cenários negativos.
/// </summary>
public class GameLogLineParserTests_Extended
{
	/// <summary>
	/// Valida que linhas vazias ou apenas espaços retornam false.
	/// </summary>
	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData("\t")]
	[InlineData(null)]
	public void TryParse_EmptyOrWhitespace_ShouldReturnFalse(string? line)
	{
		var result = GameLogLineParser.TryParse(line!, out var parsed);

		result.Should().BeFalse();
	}

	/// <summary>
	/// Valida que linhas muito curtas (menos que o mínimo) retornam false.
	/// </summary>
	[Fact]
	public void TryParse_LineTooShort_ShouldReturnFalse()
	{
		var line = "2025-08-04 14:00:00";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeFalse();
	}

	/// <summary>
	/// Valida parsing de timestamp com milissegundos.
	/// </summary>
	[Fact]
	public void TryParse_TimestampWithMillisecondsShouldParse()
	{
		var line = @"2025-08-04 14:00:27.123 [COMBAT] BOSS_DEFEAT boss_name=GolemKing defeated_by=p2";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeTrue();
		parsed.OccurredAt.Millisecond.Should().Be(123);
	}

	/// <summary>
	/// Valida que timestamp inválido retorna false.
	/// </summary>
	[Fact]
	public void TryParse_InvalidTimestamp_ShouldReturnFalse()
	{
		var line = @"INVALID-TIMESTAMP [SYSTEM] SERVER_ANNOUNCEMENT text=""test""";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeFalse();
	}

	/// <summary>
	/// Valida que canal sem delimitador de fechamento retorna false.
	/// </summary>
	[Fact]
	public void TryParse_MissingChannelClosingBracket_ShouldReturnFalse()
	{
		var line = @"2025-08-04 14:00:00 [SYSTEM SERVER_ANNOUNCEMENT text=""test""";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeFalse();
	}

	/// <summary>
	/// Valida parsing de campo sem valor (key= sem nada depois).
	/// </summary>
	[Fact]
	public void TryParse_KeyWithoutValue_ShouldStoreEmptyString()
	{
		var line = @"2025-08-04 14:00:00 [GAME] ZONE_ENTER player_id=p1 zone=";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeTrue();
		parsed.Fields.Should().ContainKey("zone");
		parsed.Fields["zone"].Should().BeEmpty();
	}

	/// <summary>
	/// Valida que tokens inválidos (sem = ) são ignorados.
	/// </summary>
	[Fact]
	public void TryParse_InvalidTokenWithoutEquals_ShouldIgnoreToken()
	{
		var line = @"2025-08-04 14:00:00 [COMBAT] BOSS_DAMAGE player_id=p1 boss_name=LichQueen invalid_token damage=423";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeTrue();
		parsed.Fields.Should().ContainKey("player_id");
		parsed.Fields.Should().ContainKey("damage");
		parsed.Fields.Should().NotContainKey("invalid_token");
		parsed.Fields["player_id"].Should().Be("p1");
		parsed.Fields["damage"].Should().Be("423");
	}

	/// <summary>
	/// Valida parsing com múltiplos espaços entre elementos.
	/// </summary>
	[Fact]
	public void TryParse_MultipleSpacesBetweenElements_ShouldParse()
	{
		var line = @"2025-08-04 14:00:00    [SYSTEM]    SERVER_ANNOUNCEMENT    text=""Welcome to BattleQuest!""";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeTrue();
		parsed.Channel.Should().Be("SYSTEM");
		parsed.ActionType.Should().Be("SERVER_ANNOUNCEMENT");
		parsed.Fields["text"].Should().Be("Welcome to BattleQuest!");
	}

	/// <summary>
	/// Valida parsing de valor com aspas contendo espaços e caracteres especiais.
	/// </summary>
	[Fact]
	public void TryParse_QuotedValueWithSpecialCharacters_ShouldPreserveContent()
	{
		var line = @"2025-08-04 14:00:00 [CHAT] MESSAGE player_id=p1 message=""Hello! How are you? :)""";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeTrue();
		parsed.Fields["message"].Should().Be("Hello! How are you? :)");
	}

	/// <summary>
	/// Valida que campos com nomes case-insensitive são acessíveis.
	/// </summary>
	[Fact]
	public void TryParse_FieldNames_ShouldBeCaseInsensitive()
	{
		var line = @"2025-08-04 14:00:00 [GAME] ZONE_ENTER player_ID=p1 zone=DarkCave";

		var result = GameLogLineParser.TryParse(line, out var parsed);

		result.Should().BeTrue();
		parsed.Fields.Should().ContainKey("player_id");
		parsed.Fields.Should().ContainKey("PLAYER_ID");
		parsed.Fields.Should().ContainKey("Player_Id");
	}
}
