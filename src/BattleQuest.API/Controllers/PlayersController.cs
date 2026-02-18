using BattleQuest.Application.Queries.Players;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BattleQuest.API.Controllers;

/// <summary>
/// Controller para consulta de jogadores do BattleQuest.
/// Permite recuperar lista de jogadores e estatísticas detalhadas.
/// </summary>
[ApiController]
[Route("api/players")]
[Authorize]
[Produces("application/json")]
[Tags("Players")]
public sealed class PlayersController : ControllerBase
{
	private readonly IPlayerQueries _queries;

	public PlayersController(IPlayerQueries queries) => _queries = queries;

	/// <summary>
	/// Retorna a lista de todos os jogadores com informações básicas.
	/// </summary>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Lista de jogadores ordenados por identificador.</returns>
	/// <response code="200">Jogadores retornados com sucesso.</response>
	/// <response code="401">Token de autenticação ausente ou inválido.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<PlayerDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<IReadOnlyList<PlayerDto>>> GetAll(CancellationToken ct = default)
	{
		var players = await _queries.GetAllAsync(ct);
		return Ok(players);
	}

	/// <summary>
	/// Retorna estatísticas detalhadas de um jogador específico.
	/// Inclui pontuação total, mortes, eliminações, itens coletados, quests concluídas, XP e ouro acumulados.
	/// </summary>
	/// <param name="id">Identificador único do jogador (ex: p1, p2, p3).</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Estatísticas do jogador.</returns>
	/// <response code="200">Estatísticas retornadas com sucesso.</response>
	/// <response code="401">Token de autenticação ausente ou inválido.</response>
	/// <response code="404">Jogador não encontrado.</response>
	[HttpGet("{id}/stats")]
	[ProducesResponseType(typeof(PlayerStatsDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<PlayerStatsDto>> GetStatsById(
		[FromRoute] string id,
		CancellationToken ct = default)
	{
		var stats = await _queries.GetStatsByIdAsync(id, ct);

		if (stats == null)
			return NotFound(new { message = $"Player '{id}' not found." });

		return Ok(stats);
	}
}
