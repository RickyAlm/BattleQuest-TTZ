using BattleQuest.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BattleQuest.API.Controllers;

/// <summary>
/// Controller para testes e diagnóstico de autenticação.
/// </summary>
[ApiController]
[Route("api/diagnostics")]
public class AuthTestController : ControllerBase
{
	/// <summary>
	/// Verifica se a autenticação via token está funcionando corretamente.
	/// </summary>
	/// <returns>Informações sobre o status de autenticação.</returns>
	/// <response code="200">Autenticação válida e funcionando.</response>
	/// <response code="401">Token ausente, inválido ou expirado.</response>
	[HttpGet("auth")]
	[Authorize]
	[ProducesResponseType(typeof(AuthTestResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public IActionResult CheckAuth()
	{
		return Ok(new AuthTestResponse
		{
			Authenticated = true,
			AuthenticationScheme = User.Identity?.AuthenticationType ?? "none",
			TimestampUtc = DateTimeOffset.UtcNow
		});
	}
}
