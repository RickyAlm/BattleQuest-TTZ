namespace BattleQuest.Application.Import;

/// <summary>
/// Representa o resultado de uma operação de importação de logs.
/// Contém estatísticas sobre linhas processadas, eventos inseridos e duplicatas ignoradas.
/// </summary>
/// <param name="LinesRead">Total de linhas lidas do arquivo de log</param>
/// <param name="EventsInserted">Quantidade de eventos novos inseridos no banco de dados</param>
/// <param name="DuplicatesSkipped">Quantidade de eventos duplicados que foram ignorados (idempotência)</param>
/// <param name="Duration">Duração total da importação</param>
public sealed record ImportResult(
	long LinesRead,
	long EventsInserted,
	long DuplicatesSkipped,
	TimeSpan Duration = default
);
