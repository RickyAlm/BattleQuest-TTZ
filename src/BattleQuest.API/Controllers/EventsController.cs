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
[Produces("application/json")]
[Tags("Events")]
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
	/// <response code="400">Parâmetro 'limit' inválido (deve estar entre 1 e 500).</response>
	/// <response code="401">Token de autenticação ausente ou inválido.</response>
	/// <response code="503">Banco de dados indisponível ou o container do docker está inativo.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public async Task<ActionResult<IReadOnlyList<EventDto>>> GetLatest(
		[FromQuery] [Range(1, 500)] int limit = 50,
		[FromQuery] bool includeRaw = false,
		CancellationToken ct = default)
	{
		var result = await _queries.GetLatestAsync(limit, includeRaw, ct);
		return Ok(result);
	}
}
