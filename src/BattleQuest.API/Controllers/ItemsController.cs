using BattleQuest.Application.Queries.Items;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BattleQuest.API.Controllers;

/// <summary>
/// Controller para consulta de itens do BattleQuest.
/// Permite recuperar estatísticas de coleta de itens.
/// </summary>
[ApiController]
[Route("api/items")]
[Authorize]
public sealed class ItemsController : ControllerBase
{
	private readonly IItemQueries _queries;

	public ItemsController(IItemQueries queries) => _queries = queries;

	/// <summary>
	/// Retorna os itens mais coletados ordenados por quantidade total.
	/// Realiza agregação de eventos de coleta para calcular os itens mais populares.
	/// </summary>
	/// <param name="limit">Número máximo de itens a retornar. Padrão: 50, Máximo: 500.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Lista de itens ordenados por quantidade total coletada (decrescente).</returns>
	/// <response code="200">Itens retornados com sucesso.</response>
	/// <response code="401">Token de autenticação ausente ou inválido.</response>
	[HttpGet("top")]
	[ProducesResponseType(typeof(IReadOnlyList<ItemStatsDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<IReadOnlyList<ItemStatsDto>>> GetTopCollected(
		[FromQuery] [Range(1, 500)] int limit = 50,
		CancellationToken ct = default)
	{
		var items = await _queries.GetTopCollectedAsync(limit, ct);
		return Ok(items);
	}
}
