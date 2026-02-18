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
	/// Extensões de arquivo permitidas para importação de logs.
	/// </summary>
	private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".txt",
		".log",
		".csv",
		".tsv",
	};

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
	/// <exception cref="ArgumentException">Caminho do arquivo está vazio ou nulo, ou extensão não permitida.</exception>
	/// <exception cref="FileNotFoundException">Arquivo não existe no caminho especificado.</exception>
	/// <exception cref="UnauthorizedAccessException">Caminho é um diretório ou não há permissão de leitura.</exception>
	/// <exception cref="InvalidDataException">Arquivo parece ser binário, não texto.</exception>
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

		// Valida extensão do arquivo
		var extension = Path.GetExtension(filePath);
		if (!AllowedExtensions.Contains(extension))
		{
			var allowedList = string.Join(", ", AllowedExtensions.Where(e => !string.IsNullOrEmpty(e)));
			throw new ArgumentException(
				$"Tipo de arquivo não suportado: '{extension}'. " +
				$"Apenas arquivos de texto são permitidos: {allowedList}",
				nameof(filePath));
		}

		// Verifica se o arquivo parece ser texto (não binário)
		ValidateTextFile(filePath);
	}

	/// <summary>
	/// Verifica se o arquivo parece ser um arquivo de texto válido.
	/// Lê os primeiros bytes para detectar conteúdo binário.
	/// </summary>
	/// <exception cref="InvalidDataException">Arquivo contém dados binários.</exception>
	private static void ValidateTextFile(string filePath)
	{
		const int sampleSize = 8192;
		const double maxBinaryThreshold = 0.3;

		try
		{
			using var fs = File.OpenRead(filePath);
			var buffer = new byte[Math.Min(sampleSize, fs.Length)];
			var bytesRead = fs.Read(buffer, 0, buffer.Length);

			if (bytesRead == 0)
				return;

			// Conta bytes que parecem não ser texto
			var nonTextBytes = 0;
			for (var i = 0; i < bytesRead; i++)
			{
				var b = buffer[i];
				// Considera não-texto: bytes de controle (exceto tab, CR, LF) e valores altos
				if ((b < 32 && b != 9 && b != 10 && b != 13) || b == 127)
					nonTextBytes++;
			}

			var binaryRatio = (double)nonTextBytes / bytesRead;
			if (binaryRatio > maxBinaryThreshold)
			{
				throw new InvalidDataException(
					$"O arquivo parece ser binário, não texto. " +
					$"Arquivos como imagens (.jpg, .png), documentos (.docx, .pdf) e planilhas (.xlsx) não são suportados.");
			}
		}
		catch (InvalidDataException)
		{
			throw;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[AVISO] Não foi possível validar o tipo do arquivo: {ex.Message}");
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
