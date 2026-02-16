using BattleQuest.Application.Import;
using BattleQuest.Application.Tests.Fakes;
using BattleQuest.Application.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BattleQuest.Application.Tests.Import;

/// <summary>
/// Testes de integração para o GameLogImportPipeline.
/// Valida o fluxo completo de importação usando fakes.
/// </summary>
public class GameLogImportPipelineTests
{
	/// <summary>
	/// Valida a normalização de canais/ações e idempotência por hash.
	/// Testa o pipeline completo incluindo deduplicação.
	/// </summary>
	[Fact]
	public async Task ImportAsync_ShouldNormalizeChannelAndAction_AndBeIdempotentByHash()
	{
		// Arrange
		var dims = new FakeDimensionResolver();
		var upserter = new FakePlayerQuestUpserter();
		var store = new FakeEventIngestionStore();

		var options = Options.Create(new ImportOptions { BatchSize = 2 });

		var pipeline = new GameLogImportPipeline(dims, upserter, store, options);

		var line1 = @"2025-08-04 14:02:14 [SYSTEM] SERVER_ANNOUNCEMENT text=""Double XP event started""";
		var line2 = @"2025-08-04 14:09:52 [GAME] ITEM_PICKUP player_id=p5 item=health_potion qty=9 location=(90,95)";
		var line2Dup = line2;

		var lines = new[] { line1, line2, line2Dup };

		// Act 1: primeira importação
		var r1 = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert 1
		r1.LinesRead.Should().Be(3);
		r1.EventsInserted.Should().Be(2);
		r1.DuplicatesSkipped.Should().Be(1);

		dims.ChannelRequests.Should().Contain("SYSTEM").And.Contain("GAME");
		dims.ActionRequests.Should().Contain("SERVER_ANNOUNCEMENT").And.Contain("ITEM_PICKUP");

		dims.ChannelRequests.Should().OnlyContain(x => x == x.ToUpperInvariant());
		dims.ActionRequests.Should().OnlyContain(x => x == x.ToUpperInvariant());

		// Act 2: importar tudo de novo -> nada novo entra
		var r2 = await pipeline.ImportAsync(AsyncEnumerableHelper.ToAsyncLines(lines), CancellationToken.None);

		// Assert 2
		r2.LinesRead.Should().Be(3);
		r2.EventsInserted.Should().Be(0);
		r2.DuplicatesSkipped.Should().Be(3);
	}
}
