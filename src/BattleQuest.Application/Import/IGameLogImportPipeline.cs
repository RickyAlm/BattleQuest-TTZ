namespace BattleQuest.Application.Import;

/// <summary>
/// Interface para o pipeline de importação de logs.
/// Define o contrato para processar linhas de log e persistir eventos no banco de dados.
/// </summary>
public interface IGameLogImportPipeline
{
	/// <summary>
	/// Importa linhas de log de forma assíncrona, processando em lotes.
	/// </summary>
	/// <param name="lines">Stream assíncrono de linhas de log para processar</param>
	/// <param name="ct">Token de cancelamento para interromper a operação</param>
	/// <returns>Resultado da importação com estatísticas de processamento</returns>
	Task<ImportResult> ImportAsync(IAsyncEnumerable<string> lines, CancellationToken ct);
}
