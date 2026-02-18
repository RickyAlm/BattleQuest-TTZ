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
	/// <exception cref="FileNotFoundException">Arquivo não encontrado no caminho especificado.</exception>
	/// <exception cref="UnauthorizedAccessException">Permissão negada ou caminho é um diretório.</exception>
	/// <exception cref="IOException">Erro de I/O ao acessar o arquivo.</exception>
	public static async IAsyncEnumerable<string> ReadLinesAsync(
		string filePath,
		[EnumeratorCancellation] CancellationToken ct)
	{
		ValidateFilePath(filePath);
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
	/// Valida se o caminho do arquivo é válido e acessível.
	/// </summary>
	/// <exception cref="ArgumentException">Caminho do arquivo está vazio ou nulo.</exception>
	/// <exception cref="FileNotFoundException">Arquivo não existe no caminho especificado.</exception>
	/// <exception cref="UnauthorizedAccessException">Caminho é um diretório ou não há permissão de leitura.</exception>
	private static void ValidateFilePath(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			throw new ArgumentException("O caminho do arquivo não pode ser vazio.", nameof(filePath));

		if (!File.Exists(filePath))
		{
			// Verifica se é um diretório
			if (Directory.Exists(filePath))
				throw new UnauthorizedAccessException(
					$"O caminho especificado é um diretório, não um arquivo: '{filePath}'");

			throw new FileNotFoundException(
				$"Arquivo não encontrado: '{filePath}'", filePath);
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
