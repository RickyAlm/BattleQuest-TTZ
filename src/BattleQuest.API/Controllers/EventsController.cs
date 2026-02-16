using BattleQuest.Application.Queries.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BattleQuest.API.Controllers;

/// <summary>
/// Controller para consulta de eventos do jogo BattleQuest.
/// Permite recuperar histórico de eventos processados do log.
/// </summary>
[ApiController]
[Route("api/events")]
[Authorize]
public sealed class EventsController : ControllerBase
{
	private readonly IEventQueries _queries;

	public EventsController(IEventQueries queries) => _queries = queries;

	/// <summary>
	/// Retorna os últimos eventos do jogo ordenados por data de ocorrência (mais recentes primeiro).
	/// </summary>
	/// <param name="limit">Número máximo de eventos a retornar. Padrão: 50, Máximo: 500.</param>
	/// <param name="includeRaw">Indica se deve incluir a linha original do log no resultado. Padrão: false.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Lista de eventos com informações detalhadas.</returns>
	/// <response code="200">Eventos retornados com sucesso.</response>
	/// <response code="401">Token de autenticação ausente ou inválido.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<IReadOnlyList<EventDto>>> GetLatest(
		[FromQuery] [Range(1, 500)] int limit = 50,
		[FromQuery] bool includeRaw = false,
		CancellationToken ct = default)
	{
		var result = await _queries.GetLatestAsync(limit, includeRaw, ct);
		return Ok(result);
	}
}
