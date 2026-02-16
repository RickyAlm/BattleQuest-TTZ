using BattleQuest.API.Controllers;
using BattleQuest.Application.Queries.Items;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BattleQuest.API.Tests.Controllers;

/// <summary>
/// Testes unitários para o ItemsController.
/// Valida comportamento do endpoint de consulta de itens mais coletados.
/// </summary>
public sealed class ItemsControllerTests
{
	private readonly Mock<IItemQueries> _queriesMock;
	private readonly ItemsController _controller;

	public ItemsControllerTests()
	{
		_queriesMock = new Mock<IItemQueries>();
		_controller = new ItemsController(_queriesMock.Object);
	}

	/// <summary>
	/// Valida que GetTopCollected retorna lista de itens ordenados por quantidade.
	/// </summary>
	[Fact]
	public async Task GetTopCollected_WithDefaultParameters_ShouldReturnTopItems()
	{
		// Arrange
		var expectedItems = new List<ItemStatsDto>
		{
			new("Health Potion", 5000, 350),
			new("Mana Potion", 3500, 280),
			new("Iron Sword", 1200, 45)
		};

		_queriesMock
			.Setup(x => x.GetTopCollectedAsync(50, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedItems);

		// Act
		var result = await _controller.GetTopCollected();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		okResult.Value.Should().BeEquivalentTo(expectedItems);

		_queriesMock.Verify(
			x => x.GetTopCollectedAsync(50, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetTopCollected com limite customizado passa o parâmetro corretamente.
	/// </summary>
	[Fact]
	public async Task GetTopCollected_WithCustomLimit_ShouldPassParameterCorrectly()
	{
		// Arrange
		const int customLimit = 10;
		var expectedItems = new List<ItemStatsDto>
		{
			new("Rare Gem", 800, 120)
		};

		_queriesMock
			.Setup(x => x.GetTopCollectedAsync(customLimit, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedItems);

		// Act
		var result = await _controller.GetTopCollected(limit: customLimit);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		_queriesMock.Verify(
			x => x.GetTopCollectedAsync(customLimit, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetTopCollected retorna lista vazia quando não há itens.
	/// </summary>
	[Fact]
	public async Task GetTopCollected_WithNoItems_ShouldReturnEmptyList()
	{
		// Arrange
		var emptyList = new List<ItemStatsDto>();

		_queriesMock
			.Setup(x => x.GetTopCollectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(emptyList);

		// Act
		var result = await _controller.GetTopCollected();

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		var okResult = (OkObjectResult)result.Result!;
		var resultList = okResult.Value as IReadOnlyList<ItemStatsDto>;
		resultList.Should().NotBeNull();
		resultList.Should().BeEmpty();
	}

	/// <summary>
	/// Valida que GetTopCollected propaga o CancellationToken corretamente.
	/// </summary>
	[Fact]
	public async Task GetTopCollected_ShouldPropagateCancellationToken()
	{
		// Arrange
		var cts = new CancellationTokenSource();
		var expectedItems = new List<ItemStatsDto>();

		_queriesMock
			.Setup(x => x.GetTopCollectedAsync(It.IsAny<int>(), cts.Token))
			.ReturnsAsync(expectedItems);

		// Act
		var result = await _controller.GetTopCollected(ct: cts.Token);

		// Assert
		result.Should().NotBeNull();
		_queriesMock.Verify(
			x => x.GetTopCollectedAsync(It.IsAny<int>(), cts.Token),
			Times.Once);
	}

	/// <summary>
	/// Valida que GetTopCollected aceita diferentes valores válidos de limite.
	/// </summary>
	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(50)]
	[InlineData(100)]
	[InlineData(500)]
	public async Task GetTopCollected_WithValidLimits_ShouldAcceptParameter(int limit)
	{
		// Arrange
		var expectedItems = new List<ItemStatsDto>
		{
			new("Test Item", 100, 10)
		};

		_queriesMock
			.Setup(x => x.GetTopCollectedAsync(limit, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedItems);

		// Act
		var result = await _controller.GetTopCollected(limit: limit);

		// Assert
		result.Should().NotBeNull();
		result.Result.Should().BeOfType<OkObjectResult>();

		_queriesMock.Verify(
			x => x.GetTopCollectedAsync(limit, It.IsAny<CancellationToken>()),
			Times.Once);
	}

	/// <summary>
	/// Valida que os itens retornados contêm todas as estatísticas esperadas.
	/// </summary>
	[Fact]
	public async Task GetTopCollected_ShouldReturnItemsWithAllStatistics()
	{
		// Arrange
		var expectedItems = new List<ItemStatsDto>
		{
			new ItemStatsDto(
				ItemName: "Dragon Scale",
				TotalCollected: 1500,
				CollectionCount: 75
			)
		};

		_queriesMock
			.Setup(x => x.GetTopCollectedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedItems);

		// Act
		var result = await _controller.GetTopCollected();

		// Assert
		var okResult = (OkObjectResult)result.Result!;
		var items = okResult.Value as IReadOnlyList<ItemStatsDto>;
		items.Should().NotBeNull();
		items![0].ItemName.Should().Be("Dragon Scale");
		items[0].TotalCollected.Should().Be(1500);
		items[0].CollectionCount.Should().Be(75);
	}
}
