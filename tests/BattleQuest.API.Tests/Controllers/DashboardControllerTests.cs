using BattleQuest.API.Controllers;
using BattleQuest.Application.Queries.Dashboard;
using BattleQuest.Application.Queries.Items;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BattleQuest.API.Tests.Controllers;

/// <summary>
/// Testes unitários para o DashboardController
/// Valida o comportamento do endpoint de métricas consolidadas do dashboard.
/// </summary>
public sealed class DashboardControllerTests
{
	private readonly Mock<IDashboardQueries> _mockQueries;
	private readonly DashboardController _controller;

	public DashboardControllerTests()
	{
		_mockQueries = new Mock<IDashboardQueries>();
		_controller = new DashboardController(_mockQueries.Object);
	}

	/// <summary>
	/// Valida que GetMetrics retorna o dashboard com métricas completas usando parâmetros padrão.
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithDefaultParameters_ShouldReturnDashboard()
	{
		// Arrange
		var expectedMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 100,
			TotalScoreAccumulated: 500000L,
			TopCollectedItems: new List<ItemStatsDto>
			{
				new("HEALTH_POTION", 1000, 50),
				new("MANA_POTION", 800, 40)
			}.AsReadOnly(),
			TopPlayerDeaths: new List<PlayerDeathStatsDto>
			{
				new("p1", "Alice", 25),
				new("p2", "Bob", 20)
			}.AsReadOnly(),
			BossesDefeated: new List<BossDefeatStatsDto>
			{
				new("DRAGON_LORD", 15),
				new("SHADOW_KING", 10)
			}.AsReadOnly(),
			TopPlayersByXp: new List<PlayerXpStatsDto>
			{
				new("p1", "Alice", 50000L),
				new("p2", "Bob", 40000L)
			}.AsReadOnly(),
			TopPlayersByGold: new List<PlayerGoldStatsDto>
			{
				new("p1", "Alice", 10000L),
				new("p2", "Bob", 8000L)
			}.AsReadOnly(),
			TopPlayersByKills: new List<PlayerKillStatsDto>
			{
				new("p1", "Alice", 75),
				new("p2", "Bob", 60)
			}.AsReadOnly()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedMetrics);

		// Act
		var result = await _controller.GetMetrics();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var metrics = okResult.Value.Should().BeAssignableTo<DashboardMetricsDto>().Subject;

		metrics.TotalActivePlayers.Should().Be(100);
		metrics.TotalScoreAccumulated.Should().Be(500000L);
		metrics.TopCollectedItems.Should().HaveCount(2);
		metrics.TopPlayerDeaths.Should().HaveCount(2);
		metrics.BossesDefeated.Should().HaveCount(2);
		metrics.TopPlayersByXp.Should().HaveCount(2);
		metrics.TopPlayersByGold.Should().HaveCount(2);
		metrics.TopPlayersByKills.Should().HaveCount(2);
	}

	/// <summary>
	/// Valida que GetMetrics passa parâmetros de data corretamente para a query.
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithDateParameters_ShouldPassParametersCorrectly()
	{
		// Arrange
		var startDate = new DateTime(2024, 1, 1);
		var endDate = new DateTime(2024, 12, 31);
		var expectedMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 50,
			TotalScoreAccumulated: 250000L,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: Array.Empty<PlayerDeathStatsDto>(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(startDate, endDate, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedMetrics);

		// Act
		var result = await _controller.GetMetrics(startDate, endDate);

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var metrics = okResult.Value.Should().BeAssignableTo<DashboardMetricsDto>().Subject;

		metrics.TotalActivePlayers.Should().Be(50);
		_mockQueries.Verify(q => q.GetMetricsAsync(startDate, endDate, It.IsAny<CancellationToken>()), Times.Once);
	}

	/// <summary>
	/// Valida que GetMetrics retorna métricas zeradas quando não há dados.
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithNoData_ShouldReturnEmptyMetrics()
	{
		// Arrange
		var emptyMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 0,
			TotalScoreAccumulated: 0L,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: Array.Empty<PlayerDeathStatsDto>(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(emptyMetrics);

		// Act
		var result = await _controller.GetMetrics();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var metrics = okResult.Value.Should().BeAssignableTo<DashboardMetricsDto>().Subject;

		metrics.TotalActivePlayers.Should().Be(0);
		metrics.TotalScoreAccumulated.Should().Be(0L);
		metrics.TopCollectedItems.Should().BeEmpty();
		metrics.TopPlayerDeaths.Should().BeEmpty();
		metrics.BossesDefeated.Should().BeEmpty();
		metrics.TopPlayersByXp.Should().BeEmpty();
		metrics.TopPlayersByGold.Should().BeEmpty();
		metrics.TopPlayersByKills.Should().BeEmpty();
	}

	/// <summary>
	/// Valida que GetMetrics propaga o CancellationToken corretamente para a camada de queries.
	/// </summary>
	[Fact]
	public async Task GetMetrics_ShouldPropagateCancellationToken()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		var token = cts.Token;
		var emptyMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 0,
			TotalScoreAccumulated: 0L,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: Array.Empty<PlayerDeathStatsDto>(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, null, token))
			.ReturnsAsync(emptyMetrics);

		// Act
		await _controller.GetMetrics(ct: token);

		// Assert
		_mockQueries.Verify(q => q.GetMetricsAsync(null, null, token), Times.Once);
	}

	/// <summary>
	/// Valida que GetMetrics retorna OkObjectResult com status code 200.
	/// </summary>
	[Fact]
	public async Task GetMetrics_ShouldReturnOkResultType()
	{
		// Arrange
		var metrics = new DashboardMetricsDto(
			TotalActivePlayers: 10,
			TotalScoreAccumulated: 10000L,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: Array.Empty<PlayerDeathStatsDto>(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(metrics);

		// Act
		var result = await _controller.GetMetrics();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		okResult.StatusCode.Should().Be(200);
	}

	/// <summary>
	/// Valida que GetMetrics lida corretamente apenas com startDate definido.
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithOnlyStartDate_ShouldPassParameterCorrectly()
	{
		// Arrange
		var startDate = new DateTime(2024, 6, 1);
		var expectedMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 30,
			TotalScoreAccumulated: 150000L,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: Array.Empty<PlayerDeathStatsDto>(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(startDate, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedMetrics);

		// Act
		var result = await _controller.GetMetrics(startDate: startDate);

		// Assert
		result.Should().NotBeNull();
		_mockQueries.Verify(q => q.GetMetricsAsync(startDate, null, It.IsAny<CancellationToken>()), Times.Once);
	}

	/// <summary>
	/// Valida que GetMetrics lida corretamente apenas com endDate definido.
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithOnlyEndDate_ShouldPassParameterCorrectly()
	{
		// Arrange
		var endDate = new DateTime(2024, 12, 31);
		var expectedMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 40,
			TotalScoreAccumulated: 200000L,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: Array.Empty<PlayerDeathStatsDto>(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, endDate, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedMetrics);

		// Act
		var result = await _controller.GetMetrics(endDate: endDate);

		// Assert
		result.Should().NotBeNull();
		_mockQueries.Verify(q => q.GetMetricsAsync(null, endDate, It.IsAny<CancellationToken>()), Times.Once);
	}

	/// <summary>
	/// Valida que GetMetrics retorna métricas com todas as coleções populadas corretamente.
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithCompleteData_ShouldReturnAllMetrics()
	{
		// Arrange
		var expectedMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 150,
			TotalScoreAccumulated: 1000000L,
			TopCollectedItems: new List<ItemStatsDto>
			{
				new("HEALTH_POTION", 2000, 100),
				new("MANA_POTION", 1500, 80),
				new("DRAGON_SCALE", 1000, 50)
			}.AsReadOnly(),
			TopPlayerDeaths: new List<PlayerDeathStatsDto>
			{
				new("p1", "Alice", 50),
				new("p2", "Bob", 45),
				new("p3", "Charlie", 40)
			}.AsReadOnly(),
			BossesDefeated: new List<BossDefeatStatsDto>
			{
				new("DRAGON_LORD", 25),
				new("SHADOW_KING", 20),
				new("ICE_QUEEN", 15)
			}.AsReadOnly(),
			TopPlayersByXp: new List<PlayerXpStatsDto>
			{
				new("p1", "Alice", 150000L),
				new("p2", "Bob", 120000L),
				new("p3", "Charlie", 100000L)
			}.AsReadOnly(),
			TopPlayersByGold: new List<PlayerGoldStatsDto>
			{
				new("p1", "Alice", 30000L),
				new("p2", "Bob", 25000L),
				new("p3", "Charlie", 20000L)
			}.AsReadOnly(),
			TopPlayersByKills: new List<PlayerKillStatsDto>
			{
				new("p1", "Alice", 200),
				new("p2", "Bob", 175),
				new("p3", "Charlie", 150)
			}.AsReadOnly()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedMetrics);

		// Act
		var result = await _controller.GetMetrics();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var metrics = okResult.Value.Should().BeAssignableTo<DashboardMetricsDto>().Subject;

		metrics.TotalActivePlayers.Should().Be(150);
		metrics.TotalScoreAccumulated.Should().Be(1000000L);

		metrics.TopCollectedItems.Should().HaveCount(3);
		metrics.TopCollectedItems[0].ItemName.Should().Be("HEALTH_POTION");
		metrics.TopCollectedItems[0].TotalCollected.Should().Be(2000);

		metrics.TopPlayerDeaths.Should().HaveCount(3);
		metrics.TopPlayerDeaths[0].PlayerId.Should().Be("p1");
		metrics.TopPlayerDeaths[0].TotalDeaths.Should().Be(50);

		metrics.BossesDefeated.Should().HaveCount(3);
		metrics.BossesDefeated[0].BossName.Should().Be("DRAGON_LORD");
		metrics.BossesDefeated[0].DefeatCount.Should().Be(25);

		metrics.TopPlayersByXp.Should().HaveCount(3);
		metrics.TopPlayersByXp[0].PlayerId.Should().Be("p1");
		metrics.TopPlayersByXp[0].TotalXp.Should().Be(150000L);

		metrics.TopPlayersByGold.Should().HaveCount(3);
		metrics.TopPlayersByGold[0].PlayerId.Should().Be("p1");
		metrics.TopPlayersByGold[0].TotalGold.Should().Be(30000L);

		metrics.TopPlayersByKills.Should().HaveCount(3);
		metrics.TopPlayersByKills[0].PlayerId.Should().Be("p1");
		metrics.TopPlayersByKills[0].TotalKills.Should().Be(200);
	}

	/// <summary>
	/// Valida que GetMetrics lida corretamente com valores extremos de pontuação.
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithLargeScoreValues_ShouldHandleCorrectly()
	{
		// Arrange
		var expectedMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 10000,
			TotalScoreAccumulated: long.MaxValue - 1000,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: Array.Empty<PlayerDeathStatsDto>(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedMetrics);

		// Act
		var result = await _controller.GetMetrics();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var metrics = okResult.Value.Should().BeAssignableTo<DashboardMetricsDto>().Subject;

		metrics.TotalActivePlayers.Should().Be(10000);
		metrics.TotalScoreAccumulated.Should().Be(long.MaxValue - 1000);
	}

	/// <summary>
	/// Valida que GetMetrics funciona corretamente com jogadores sem nome (campo Name nulo).
	/// </summary>
	[Fact]
	public async Task GetMetrics_WithPlayersWithoutNames_ShouldHandleNulls()
	{
		// Arrange
		var expectedMetrics = new DashboardMetricsDto(
			TotalActivePlayers: 5,
			TotalScoreAccumulated: 10000L,
			TopCollectedItems: Array.Empty<ItemStatsDto>(),
			TopPlayerDeaths: new List<PlayerDeathStatsDto>
			{
				new("p1", null, 10),
				new("p2", "Bob", 8)
			}.AsReadOnly(),
			BossesDefeated: Array.Empty<BossDefeatStatsDto>(),
			TopPlayersByXp: Array.Empty<PlayerXpStatsDto>(),
			TopPlayersByGold: Array.Empty<PlayerGoldStatsDto>(),
			TopPlayersByKills: Array.Empty<PlayerKillStatsDto>()
		);

		_mockQueries
			.Setup(q => q.GetMetricsAsync(null, null, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedMetrics);

		// Act
		var result = await _controller.GetMetrics();

		// Assert
		result.Should().NotBeNull();
		var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
		var metrics = okResult.Value.Should().BeAssignableTo<DashboardMetricsDto>().Subject;

		metrics.TopPlayerDeaths.Should().HaveCount(2);
		metrics.TopPlayerDeaths[0].Name.Should().BeNull();
		metrics.TopPlayerDeaths[1].Name.Should().Be("Bob");
	}
}
