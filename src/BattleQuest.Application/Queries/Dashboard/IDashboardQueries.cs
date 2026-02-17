namespace BattleQuest.Application.Queries.Dashboard;

/// <summary>
/// Interface para consultas relacionadas às métricas do dashboard.
/// Fornece estatísticas consolidadas sobre o jogo BattleQuest.
/// </summary>
public interface IDashboardQueries
{
	/// <summary>
	/// Recupera métricas consolidadas do dashboard.
	/// Inclui total de jogadores ativos, pontuação, itens mais coletados,
	/// jogadores com mais mortes e chefes derrotados.
	/// </summary>
	/// <param name="startDate">Data inicial opcional para filtro de eventos.</param>
	/// <param name="endDate">Data final opcional para filtro de eventos.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>Métricas consolidadas do dashboard.</returns>
	Task<DashboardMetricsDto> GetMetricsAsync(
		DateTime? startDate = null,
		DateTime? endDate = null,
		CancellationToken ct = default);
}
