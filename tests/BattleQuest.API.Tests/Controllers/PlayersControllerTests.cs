using BattleQuest.API.Controllers;
using BattleQuest.Application.Queries.Players;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BattleQuest.API.Tests.Controllers;

/// <summary>
/// Testes unitários para o PlayersController.
/// Valida comportamento dos endpoints de consulta de jogadores e estatísticas.
/// </summary>
public sealed class PlayersControllerTests
{
	private readonly Mock<IPlayerQueries> _queriesMock;
	private readonly PlayersController _controller;

	public PlayersControllerTests()
	{
		_queriesMock = new Mock<IPlayerQueries>();
		_controller = new PlayersController(_queriesMock.Object);
	}

	/// <summary>
	/// Valida que GetAll retorna lista de jogadores corretamente.
	/// </summary>
	[Fact]
	public async Task GetAll_WithPlayers_ShouldReturnPlayerList()
	{
		// Arrange
		var expectedPlayers = new List<PlayerDto>
		{
			new("p1", "Warrior", 45, "Dragon's Lair"),
			new("p2", "Mage", 38, "Mystic Forest"),
			new("p3", null, 12, null)
		};

		_queriesMock
			.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedPlayers);

		// Act
		var result = await _controller.GetAll();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		okResult.Value.Should().BeEquivalentTo(expectedPlayers);

		_queriesMock.Verify(
			x => x.GetAllAsync(It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetAll retorna lista vazia quando não há jogadores.
	/// </summary>
	[Fact]
	public async Task GetAll_WithNoPlayers_ShouldReturnEmptyList()
	{
		// Arrange
		var emptyList = new List<PlayerDto>();

		_queriesMock
			.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(emptyList);

		// Act
		var result = await _controller.GetAll();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		var resultList = okResult.Value as IReadOnlyList<PlayerDto>;
		resultList.Should().NotBeNull();
		resultList.Should().BeEmpty();
	}

	/// <summary>
	/// Valida que GetAll propaga o CancellationToken corretamente.
	/// </summary>
	[Fact]
	public async Task GetAll_ShouldPropagateCancellationToken()
	{
		// Arrange
		var cts = new CancellationTokenSource();
		var expectedPlayers = new List<PlayerDto>();

		_queriesMock
			.Setup(x => x.GetAllAsync(cts.Token))
			.ReturnsAsync(expectedPlayers);

		// Act
		var result = await _controller.GetAll(cts.Token);

		// Assert
		result.Should().NotBeNull();
		_queriesMock.Verify(
			x => x.GetAllAsync(cts.Token),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetStatsById retorna estatísticas do jogador quando encontrado.
	/// </summary>
	[Fact]
	public async Task GetStatsById_ExistingPlayer_ShouldReturnStats()
	{
		// Arrange
		const string playerId = "p1";
		var expectedStats = new PlayerStatsDto(
			PlayerId: playerId,
			Name: "Warrior",
			LastKnownLevel: 45,
			TotalScore: 15000,
			TotalDeaths: 5,
			TotalKills: 120,
			ItemsCollected: 350,
			QuestsCompleted: 25,
			TotalXpEarned: 80000,
			TotalGoldEarned: 5000
		);

		_queriesMock
			.Setup(x => x.GetStatsByIdAsync(playerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedStats);

		// Act
		var result = await _controller.GetStatsById(playerId);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		okResult.Value.Should().BeEquivalentTo(expectedStats);

		_queriesMock.Verify(
			x => x.GetStatsByIdAsync(playerId, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetStatsById retorna 404 quando jogador não existe.
	/// </summary>
	[Fact]
	public async Task GetStatsById_NonExistingPlayer_ShouldReturn404()
	{
		// Arrange
		const string playerId = "p999";

		_queriesMock
			.Setup(x => x.GetStatsByIdAsync(playerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((PlayerStatsDto?)null);

		// Act
		var result = await _controller.GetStatsById(playerId);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<NotFoundObjectResult>();

		var notFoundResult = (NotFoundObjectResult)result.Result!;
		notFoundResult.StatusCode.Should().Be(404);

		_queriesMock.Verify(
			x => x.GetStatsByIdAsync(playerId, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetStatsById propaga o CancellationToken corretamente.
	/// </summary>
	[Fact]
	public async Task GetStatsById_ShouldPropagateCancellationToken()
	{
		// Arrange
		const string playerId = "p1";
		var cts = new CancellationTokenSource();
		var expectedStats = new PlayerStatsDto(
			playerId, null, null, 0, 0, 0, 0, 0, 0, 0
		);

		_queriesMock
			.Setup(x => x.GetStatsByIdAsync(playerId, cts.Token))
			.ReturnsAsync(expectedStats);

		// Act
		var result = await _controller.GetStatsById(playerId, cts.Token);

		// Assert
		result.Should().NotBeNull();
		_queriesMock.Verify(
			x => x.GetStatsByIdAsync(playerId, cts.Token),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetStatsById aceita diferentes formatos de player IDs.
	/// </summary>
	[Theory]
	[InlineData("p1")]
	[InlineData("p123")]
	[InlineData("player_001")]
	[InlineData("ADMIN")]
	public async Task GetStatsById_WithDifferentPlayerIds_ShouldAcceptParameter(string playerId)
	{
		// Arrange
		var expectedStats = new PlayerStatsDto(
			playerId, null, null, 0, 0, 0, 0, 0, 0, 0
		);

		_queriesMock
			.Setup(x => x.GetStatsByIdAsync(playerId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedStats);

		// Act
		var result = await _controller.GetStatsById(playerId);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		_queriesMock.Verify(
			x => x.GetStatsByIdAsync(playerId, It.IsAny<CancellationToken>()),
			Times.Once);
	}
}
