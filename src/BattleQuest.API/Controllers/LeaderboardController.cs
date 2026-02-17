using BattleQuest.Application.Queries.Leaderboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BattleQuest.API.Controllers;

/// <summary>
/// Controller para consulta do ranking de jogadores do BattleQuest.
/// Permite recuperar o leaderboard ordenado por pontuação.
/// </summary>
[ApiController]
[Route("api/leaderboard")]
[Authorize]
public sealed class LeaderboardController : ControllerBase
{
	private readonly ILeaderboardQueries _queries;

	public LeaderboardController(ILeaderboardQueries queries) => _queries = queries;

	/// <summary>
	/// Retorna o ranking de jogadores ordenado por pontuação total.
	/// Agrega pontos de todos os eventos de cada jogador e atribui ranking sequencial (1 = primeiro lugar).
	/// </summary>
	/// <param name="limit">Número máximo de entradas a retornar. Padrão: 50, Máximo: 500.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Lista de jogadores ordenados por pontuação total (decrescente).</returns>
	/// <response code="200">Leaderboard retornado com sucesso.</response>
	/// <response code="401">Token de autenticação ausente ou inválido.</response>
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<LeaderboardEntryDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<IReadOnlyList<LeaderboardEntryDto>>> GetTopPlayers(
		[FromQuery] [Range(1, 500)] int limit = 50,
		CancellationToken ct = default)
	{
		var leaderboard = await _queries.GetTopPlayersAsync(limit, ct);
		return Ok(leaderboard);
	}
}
