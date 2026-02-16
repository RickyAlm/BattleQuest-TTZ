namespace BattleQuest.Application.Queries.Items;

/// <summary>
/// DTO representando estatísticas de coleta de um item.
/// </summary>
/// <param name="ItemName">Nome do item.</param>
/// <param name="TotalCollected">Quantidade total de unidades coletadas do item.</param>
/// <param name="CollectionCount">Número de eventos de coleta do item.</param>
public sealed record ItemStatsDto(
	string ItemName,
	long TotalCollected,
	int CollectionCount
);
