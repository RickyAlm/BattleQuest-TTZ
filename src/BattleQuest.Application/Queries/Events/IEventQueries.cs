namespace BattleQuest.Application.Queries.Events;

/// <summary>
/// Interface para consultas de eventos do jogo.
/// Abstração da camada de acesso a dados para queries read-only de eventos.
/// </summary>
public interface IEventQueries
{
	/// <summary>
	/// Retorna os últimos eventos do jogo ordenados por data de ocorrência (mais recente primeiro).
	/// </summary>
	/// <param name="limit">Número máximo de eventos a retornar (padrão: 50, máximo: 500).</param>
	/// <param name="includeRaw">Indica se deve incluir a linha original do log no resultado.</param>
	/// <param name="ct">Token de cancelamento para operações assíncronas.</param>
	/// <returns>Lista de eventos ordenados por OccurredAt desc, EventId desc.</returns>
	Task<IReadOnlyList<EventDto>> GetLatestAsync(int limit, bool includeRaw, CancellationToken ct);
}
