using BattleQuest.API.Controllers;
using BattleQuest.Application.Queries.Leaderboard;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BattleQuest.API.Tests.Controllers;

/// <summary>
/// Testes unitários para o LeaderboardController.
/// Valida o comportamento do endpoint de ranking de jogadores.
/// </summary>
public sealed class LeaderboardControllerTests
{
	private readonly Mock<ILeaderboardQueries> _mockQueries;
	private readonly LeaderboardController _controller;

	public LeaderboardControllerTests()
	{
		_mockQueries = new Mock<ILeaderboardQueries>();
		_controller = new LeaderboardController(_mockQueries.Object);
	}

	/// <summary>
	/// Valida que GetTopPlayers retorna o leaderboard corretamente usando parâmetros padrão.
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_WithDefaultParameters_ShouldReturnLeaderboard()
	{
		// Arrange
		var expectedLeaderboard = new List<LeaderboardEntryDto>
		{
			new(1, "p1", "Alice", 10000, 20),
			new(2, "p2", "Bob", 8500, 18),
			new(3, "p3", "Charlie", 7200, 15)
		}.AsReadOnly();

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(50, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedLeaderboard);

		// Act
		var result = await _controller.GetTopPlayers();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var leaderboard = okResult.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryDto>>().Subject;

		leaderboard.Should().HaveCount(3);
		leaderboard[0].Rank.Should().Be(1);
		leaderboard[0].PlayerId.Should().Be("p1");
		leaderboard[0].Name.Should().Be("Alice");
		leaderboard[0].TotalScore.Should().Be(10000);
		leaderboard[0].LastKnownLevel.Should().Be(20);
	}

	/// <summary>
	/// Valida que GetTopPlayers passa o parâmetro limit customizado corretamente para a query.
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_WithCustomLimit_ShouldPassParameterCorrectly()
	{
		// Arrange
		var expectedLeaderboard = new List<LeaderboardEntryDto>
		{
			new(1, "p1", "Alice", 10000, 20)
		}.AsReadOnly();

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedLeaderboard);

		// Act
		var result = await _controller.GetTopPlayers(limit: 10);

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var leaderboard = okResult.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryDto>>().Subject;

		leaderboard.Should().HaveCount(1);
		_mockQueries.Verify(q => q.GetTopPlayersAsync(10, It.IsAny<CancellationToken>()), Times.Once);
	}

	/// <summary>
	/// Valida que GetTopPlayers retorna lista vazia quando não há jogadores.
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_WithNoPlayers_ShouldReturnEmptyList()
	{
		// Arrange
		var emptyLeaderboard = Array.Empty<LeaderboardEntryDto>();

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(emptyLeaderboard);

		// Act
		var result = await _controller.GetTopPlayers();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var leaderboard = okResult.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryDto>>().Subject;

		leaderboard.Should().BeEmpty();
	}

	/// <summary>
	/// Valida que GetTopPlayers propaga o CancellationToken corretamente para a camada de queries.
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_ShouldPropagateCancellationToken()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var token = cts.Token;

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(It.IsAny<int>(), token))
			.ReturnsAsync(Array.Empty<LeaderboardEntryDto>());

		// Act
		await _controller.GetTopPlayers(ct: token);

		// Assert
		_mockQueries.Verify(q => q.GetTopPlayersAsync(It.IsAny<int>(), token), Times.Once);
	}

	/// <summary>
	/// Valida que GetTopPlayers aceita diferentes valores válidos para o parâmetro limit.
	/// </summary>
	/// <param name="limit">Limite a ser testado.</param>
	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(50)]
	[InlineData(100)]
	[InlineData(500)]
	public async Task GetTopPlayers_WithValidLimits_ShouldAcceptParameter(int limit)
	{
		// Arrange
		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(limit, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Array.Empty<LeaderboardEntryDto>());

		// Act
		var result = await _controller.GetTopPlayers(limit: limit);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();
		_mockQueries.Verify(q => q.GetTopPlayersAsync(limit, It.IsAny<CancellationToken>()), Times.Once);
	}

	/// <summary>
	/// Valida que GetTopPlayers retorna jogadores com ranking sequencial correto e scores em ordem decrescente.
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_ShouldReturnPlayersWithRankSequence()
	{
		// Arrange
		var expectedLeaderboard = new List<LeaderboardEntryDto>
		{
			new(1, "p1", "Alice", 10000, 20),
			new(2, "p2", "Bob", 9000, 19),
			new(3, "p3", "Charlie", 8000, 18),
			new(4, "p4", "Diana", 7000, 17),
			new(5, "p5", "Edward", 6000, 16)
		}.AsReadOnly();

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedLeaderboard);

		// Act
		var result = await _controller.GetTopPlayers();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var leaderboard = okResult.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryDto>>().Subject;

		leaderboard.Should().HaveCount(5);
		leaderboard.Select(e => e.Rank).Should().BeInAscendingOrder();
		leaderboard[0].Rank.Should().Be(1);
		leaderboard[4].Rank.Should().Be(5);

		// Verificar que scores estão em ordem decrescente
		leaderboard.Select(e => e.TotalScore).Should().BeInDescendingOrder();
	}

	/// <summary>
	/// Valida que GetTopPlayers lida corretamente com jogadores que possuem campos nulos (Name e LastKnownLevel).
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_WithPlayersHavingNullFields_ShouldHandleCorrectly()
	{
		// Arrange
		var expectedLeaderboard = new List<LeaderboardEntryDto>
		{
			new(1, "p1", null, 10000, null),
			new(2, "p2", "Bob", 8000, 15)
		}.AsReadOnly();

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedLeaderboard);

		// Act
		var result = await _controller.GetTopPlayers();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var leaderboard = okResult.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryDto>>().Subject;

		leaderboard.Should().HaveCount(2);
		leaderboard[0].Name.Should().BeNull();
		leaderboard[0].LastKnownLevel.Should().BeNull();
		leaderboard[1].Name.Should().Be("Bob");
		leaderboard[1].LastKnownLevel.Should().Be(15);
	}

	/// <summary>
	/// Valida que GetTopPlayers retorna o tipo de resultado correto (OkObjectResult com status code 200).
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_ShouldReturnOkResultType()
	{
		// Arrange
		var expectedLeaderboard = new List<LeaderboardEntryDto>
		{
			new(1, "p1", "Alice", 10000, 20)
		}.AsReadOnly();

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedLeaderboard);

		// Act
		var result = await _controller.GetTopPlayers();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		okResult.StatusCode.Should().Be(200);
	}

	/// <summary>
	/// Valida que GetTopPlayers respeita diferentes valores de limit em múltiplas chamadas.
	/// </summary>
	[Fact]
	public async Task GetTopPlayers_WithDifferentLimits_ShouldRespectEachLimit()
	{
		// Arrange
		var leaderboard3 = Enumerable.Range(1, 3)
			.Select(i => new LeaderboardEntryDto(i, $"p{i}", $"Player{i}", 1000 - i * 100, 10 + i))
			.ToList()
			.AsReadOnly();

		var leaderboard10 = Enumerable.Range(1, 10)
			.Select(i => new LeaderboardEntryDto(i, $"p{i}", $"Player{i}", 1000 - i * 100, 10 + i))
			.ToList()
			.AsReadOnly();

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(3, It.IsAny<CancellationToken>()))
			.ReturnsAsync(leaderboard3);

		_mockQueries
			.Setup(q => q.GetTopPlayersAsync(10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(leaderboard10);

		// Act
		var result3 = await _controller.GetTopPlayers(limit: 3);
		var result10 = await _controller.GetTopPlayers(limit: 10);

		// Assert
		var okResult3 = result3.Result.Should().BeOfType<OkObjectResult>().Subject;
		var leaderboard3Result = okResult3.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryDto>>().Subject;
		leaderboard3Result.Should().HaveCount(3);

		var okResult10 = result10.Result.Should().BeOfType<OkObjectResult>().Subject;
		var leaderboard10Result = okResult10.Value.Should().BeAssignableTo<IReadOnlyList<LeaderboardEntryDto>>().Subject;
		leaderboard10Result.Should().HaveCount(10);
	}
}
