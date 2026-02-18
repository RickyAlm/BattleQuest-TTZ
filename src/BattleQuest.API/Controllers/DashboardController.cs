using BattleQuest.Application.Queries.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BattleQuest.API.Controllers;

/// <summary>
/// Controller para consulta de métricas consolidadas do dashboard do BattleQuest.
/// Fornece estatísticas agregadas sobre jogadores, pontuação, itens, mortes e chefes derrotados.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
[Produces("application/json")]
[Tags("Dashboard")]
public sealed class DashboardController : ControllerBase
{
	private readonly IDashboardQueries _queries;

	public DashboardController(IDashboardQueries queries) => _queries = queries;

	/// <summary>
	/// Retorna métricas consolidadas do dashboard.
	/// Inclui total de jogadores ativos, pontuação acumulada, top itens coletados,
	/// jogadores com mais mortes, lista de chefes derrotados, jogadores com mais XP, ouro e abates.
	/// </summary>
	/// <param name="startDate">Data inicial opcional para filtro de eventos (formato: yyyy-MM-dd ou yyyy-MM-dd HH:mm:ss).</param>
	/// <param name="endDate">Data final opcional para filtro de eventos (formato: yyyy-MM-dd ou yyyy-MM-dd HH:mm:ss).</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Métricas consolidadas do dashboard.</returns>
	/// <response code="200">Dashboard retornado com sucesso.</response>
	/// <response code="400">Parâmetros de data inválidos ou formato incorreto.</response>
	/// <response code="401">Token de autenticação ausente ou inválido.</response>
	[HttpGet]
	[ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<DashboardMetricsDto>> GetMetrics(
		[FromQuery] DateTime? startDate = null,
		[FromQuery] DateTime? endDate = null,
		CancellationToken ct = default)
	{
		var metrics = await _queries.GetMetricsAsync(startDate, endDate, ct);
		return Ok(metrics);
	}
}
