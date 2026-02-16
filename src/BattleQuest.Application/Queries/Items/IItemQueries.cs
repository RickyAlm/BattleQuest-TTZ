namespace BattleQuest.Application.Queries.Items;

/// <summary>
/// Interface para consultas relacionadas a itens.
/// Define operações de leitura para recuperar estatísticas de coleta de itens.
/// </summary>
public interface IItemQueries
{
	/// <summary>
	/// Recupera os itens mais coletados ordenados por quantidade total.
	/// Realiza agregação de eventos de coleta para calcular totais.
	/// </summary>
	/// <param name="limit">Número máximo de itens a retornar. Padrão: 50, Máximo: 500.</param>
	/// <param name="ct">Token de cancelamento.</param>
	/// <returns>
	/// Lista de itens ordenados por quantidade total coletada (descendente).
	/// Cada item inclui nome, total coletado e número de eventos de coleta.
	/// </returns>
	Task<IReadOnlyList<ItemStatsDto>> GetTopCollectedAsync(int limit = 50, CancellationToken ct = default);
}
