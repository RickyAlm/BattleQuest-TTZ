using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BattleQuest.API.Controllers;

/// <summary>
/// Controller para verificação de saúde da API.
/// Endpoint público (sem autenticação) para monitoramento de disponibilidade.
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
[Tags("Health")]
public class HealthController : ControllerBase
{
	/// <summary>
	/// Verifica se a API está ativa e respondendo.
	/// </summary>
	/// <returns>Status de saúde da API.</returns>
	/// <response code="200">API está funcionando normalmente.</response>
	[HttpGet]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public IActionResult Get() => Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
}
