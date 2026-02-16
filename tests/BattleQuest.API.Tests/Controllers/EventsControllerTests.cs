using BattleQuest.API.Controllers;
using BattleQuest.Application.Queries.Events;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BattleQuest.API.Tests.Controllers;

/// <summary>
/// Testes unitários para o EventsController.
/// Valida comportamento dos endpoints de consulta de eventos do jogo.
/// </summary>
public sealed class EventsControllerTests
{
	private readonly Mock<IEventQueries> _queriesMock;
	private readonly EventsController _controller;

	public EventsControllerTests()
	{
		_queriesMock = new Mock<IEventQueries>();
		_controller = new EventsController(_queriesMock.Object);
	}

	/// <summary>
	/// Valida que GetLatest com parâmetros padrão retorna eventos corretamente.
	/// </summary>
	[Fact]
	public async Task GetLatest_WithDefaultParameters_ShouldReturnEvents()
	{
		// Arrange
		var expectedEvents = new List<EventDto>
		{
			new(
				EventId: 1,
				OccurredAt: new DateTimeOffset(2025, 8, 5, 23, 7, 0, TimeSpan.Zero),
				Channel: "GAME",
				ActionType: "QUEST_COMPLETE",
				PlayerId: "p6",
				VictimPlayerId: null,
				KillerPlayerId: null,
				QuestId: "q591",
				Zone: null,
				Item: null,
				Boss: null,
				Quantity: null,
				Xp: 713,
				Gold: 18,
				Hp: null,
				Damage: null,
				Method: null,
				PlayerLevel: null,
				Points: null,
				Reason: null,
				LocationX: null,
				LocationY: null,
				MessageText: null,
				Raw: null
			)
		};

		_queriesMock
			.Setup(x => x.GetLatestAsync(50, false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedEvents);

		// Act
		var result = await _controller.GetLatest();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();
		
		var okResult = (OkObjectResult)result.Result!;
		okResult.Value.Should().BeEquivalentTo(expectedEvents);

		_queriesMock.Verify(
			x => x.GetLatestAsync(50, false, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetLatest com limite customizado passa o parâmetro corretamente.
	/// </summary>
	[Fact]
	public async Task GetLatest_WithCustomLimit_ShouldPassParameterCorrectly()
	{
		// Arrange
		const int customLimit = 100;
		var expectedEvents = new List<EventDto>();

		_queriesMock
			.Setup(x => x.GetLatestAsync(customLimit, false, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedEvents);

		// Act
		var result = await _controller.GetLatest(limit: customLimit);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		_queriesMock.Verify(
			x => x.GetLatestAsync(customLimit, false, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetLatest com includeRaw=true retorna o campo Raw preenchido.
	/// </summary>
	[Fact]
	public async Task GetLatest_WithIncludeRawTrue_ShouldIncludeRawField()
	{
		// Arrange
		var expectedEvents = new List<EventDto>
		{
			new(
				EventId: 1,
				OccurredAt: new DateTimeOffset(2025, 8, 5, 23, 7, 0, TimeSpan.Zero),
				Channel: "GAME",
				ActionType: "QUEST_COMPLETE",
				PlayerId: "p6",
				VictimPlayerId: null,
				KillerPlayerId: null,
				QuestId: "q591",
				Zone: null,
				Item: null,
				Boss: null,
				Quantity: null,
				Xp: 713,
				Gold: 18,
				Hp: null,
				Damage: null,
				Method: null,
				PlayerLevel: null,
				Points: null,
				Reason: null,
				LocationX: null,
				LocationY: null,
				MessageText: null,
				Raw: "2025-08-05 23:07:00 [GAME] QUEST_COMPLETE player_id=p6 quest_id=q591 xp=713 gold=18"
			)
		};

		_queriesMock
			.Setup(x => x.GetLatestAsync(50, true, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedEvents);

		// Act
		var result = await _controller.GetLatest(includeRaw: true);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		var resultList = okResult.Value as IReadOnlyList<EventDto>;
		resultList.Should().NotBeNull();
		resultList![0].Raw.Should().NotBeNullOrEmpty();
		resultList[0].Raw.Should().Contain("QUEST_COMPLETE");

		_queriesMock.Verify(
			x => x.GetLatestAsync(50, true, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetLatest retorna lista vazia quando não há eventos.
	/// </summary>
	[Fact]
	public async Task GetLatest_WithEmptyResult_ShouldReturnOkWithEmptyList()
	{
		// Arrange
		var emptyList = new List<EventDto>();

		_queriesMock
			.Setup(x => x.GetLatestAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(emptyList);

		// Act
		var result = await _controller.GetLatest();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		var resultList = okResult.Value as IReadOnlyList<EventDto>;
		resultList.Should().NotBeNull();
		resultList.Should().BeEmpty();
	}

	/// <summary>
	/// Valida que GetLatest propaga o CancellationToken corretamente para a camada de queries.
	/// </summary>
	[Fact]
	public async Task GetLatest_ShouldPropagateCancellationToken()
	{
		// Arrange
		var cts = new CancellationTokenSource();
		var expectedEvents = new List<EventDto>();

		_queriesMock
			.Setup(x => x.GetLatestAsync(It.IsAny<int>(), It.IsAny<bool>(), cts.Token))
			.ReturnsAsync(expectedEvents);

		// Act
		var result = await _controller.GetLatest(ct: cts.Token);

		// Assert
		result.Should().NotBeNull();
		_queriesMock.Verify(
			x => x.GetLatestAsync(It.IsAny<int>(), It.IsAny<bool>(), cts.Token),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetLatest aceita diferentes valores válidos de limite.
	/// </summary>
	[Theory]
	[InlineData(1)]
	[InlineData(50)]
	[InlineData(100)]
	[InlineData(500)]
	public async Task GetLatest_WithValidLimits_ShouldAcceptParameter(int limit)
	{
		// Arrange
		var expectedEvents = new List<EventDto>();

		_queriesMock
			.Setup(x => x.GetLatestAsync(limit, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedEvents);

		// Act
		var result = await _controller.GetLatest(limit: limit);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		_queriesMock.Verify(
			x => x.GetLatestAsync(limit, It.IsAny<bool>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
