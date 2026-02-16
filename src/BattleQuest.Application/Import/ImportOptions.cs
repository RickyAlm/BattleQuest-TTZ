namespace BattleQuest.Application.Import;

/// <summary>
/// Opções de configuração para o pipeline de importação de logs.
/// </summary>
public sealed class ImportOptions
{
	/// <summary>
	/// Tamanho do lote para inserção em batch no banco de dados.
	/// Valor padrão: 2000 eventos por batch.
	/// </summary>
	public int BatchSize { get; set; } = 2000;
}
