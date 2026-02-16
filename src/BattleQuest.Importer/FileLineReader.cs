using System.Runtime.CompilerServices;

namespace BattleQuest.Importer;

/// <summary>
/// Fornece métodos para leitura assíncrona de arquivos linha por linha de forma eficiente.
/// Otimizado para arquivos grandes com buffer de leitura configurável.
/// </summary>
public static class FileLineReader
{
	private const int DefaultBufferSize = 64 * 1024; // 64 KB

	/// <summary>
	/// Lê linhas de um arquivo de texto de forma assíncrona e eficiente.
	/// Utiliza buffer otimizado e leitura sequencial para melhor performance em arquivos grandes.
	/// </summary>
	/// <param name="filePath">Caminho completo do arquivo a ser lido</param>
	/// <param name="ct">Token de cancelamento para interromper a leitura</param>
	/// <returns>Stream assíncrono de linhas do arquivo</returns>
	public static async IAsyncEnumerable<string> ReadLinesAsync(
		string filePath,
		[EnumeratorCancellation] CancellationToken ct)
	{
		await using var fileStream = OpenFileForReading(filePath);
		using var reader = new StreamReader(fileStream);

		while (!reader.EndOfStream)
		{
			ct.ThrowIfCancellationRequested();
			var line = await reader.ReadLineAsync();
			if (line is null)
				yield break;
			
			yield return line;
		}
	}

	/// <summary>
	/// Abre um arquivo para leitura com configurações otimizadas para leitura sequencial assíncrona.
	/// </summary>
	private static FileStream OpenFileForReading(string filePath)
	{
		return new FileStream(
			filePath,
			FileMode.Open,
			FileAccess.Read,
			FileShare.Read,
			bufferSize: DefaultBufferSize,
			options: FileOptions.Asynchronous | FileOptions.SequentialScan);
	}
}
