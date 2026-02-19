using BattleQuest.Application.Import;
using BattleQuest.Application.Import.Ports;
using BattleQuest.Infrastructure.Database;
using BattleQuest.Infrastructure.Import.Lookup;
using BattleQuest.Infrastructure.Import.Persistence;
using BattleQuest.Infrastructure.Import.Upsert;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static BattleQuest.Importer.FileLineReader;

ConsoleOutput.WriteHeader();

var host = CreateHost(args);

if (!TryGetFilePath(args, out var filePath))
{
	ConsoleOutput.WriteUsage();
	return;
}

ConsoleOutput.WriteValidating();
if (!File.Exists(filePath))
{
	ConsoleOutput.WriteFileNotFound(filePath);
	return;
}
ConsoleOutput.WriteFileValidated();

try
{
	ConsoleOutput.WriteStartingImport(Path.GetFileName(filePath));

	var pipeline = host.Services.GetRequiredService<IGameLogImportPipeline>();
	var result = await ExecuteImportAsync(pipeline, filePath);

	ConsoleOutput.WriteImportSummary(result);
}
catch (ArgumentException ex) when (ex.Message.Contains("Tipo de arquivo não suportado"))
{
	ConsoleOutput.WriteUnsupportedFileTypeError(ex.Message);
	Environment.Exit(1);
}
catch (InvalidDataException ex)
{
	ConsoleOutput.WriteInvalidDataError(ex.Message);
	Environment.Exit(1);
}
catch (FileNotFoundException ex)
{
	ConsoleOutput.WriteFileNotFoundError(ex.FileName ?? string.Empty);
	Environment.Exit(1);
}
catch (UnauthorizedAccessException ex)
{
	ConsoleOutput.WriteUnauthorizedAccessError(ex.Message);
	Environment.Exit(1);
}
catch (IOException ex)
{
	ConsoleOutput.WriteIOError(ex.Message);
	Environment.Exit(1);
}
catch (Exception ex)
{
	ConsoleOutput.WriteUnexpectedError(ex);
	Environment.Exit(1);
}

return;

/// <summary>
/// Cria e configura o host da aplicação com todos os serviços necessários.
/// </summary>
static IHost CreateHost(string[] args)
{
	return Host.CreateDefaultBuilder(args)
		.ConfigureAppConfiguration(ConfigureAppSettings)
		.ConfigureServices(ConfigureServices)
		.Build();
}

/// <summary>
/// Configura as fontes de configuração da aplicação.
/// </summary>
static void ConfigureAppSettings(HostBuilderContext context, IConfigurationBuilder config)
{
	config.SetBasePath(AppContext.BaseDirectory);
	config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
	config.AddEnvironmentVariables();
}

/// <summary>
/// Registra todos os serviços necessários para o pipeline de importação.
/// </summary>
static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
{
	// Database
	services.AddDbContext<BattleQuestDbContext>(options =>
		options.UseNpgsql(context.Configuration.GetConnectionString("BattleQuestDb")));

	// Configuration
	services.Configure<ImportOptions>(context.Configuration.GetSection("Import"));

	// Pipeline (Application Layer)
	services.AddScoped<IGameLogImportPipeline, GameLogImportPipeline>();

	// Infrastructure Implementations
	services.AddScoped<IDimensionResolver, EfDimensionResolver>();
	services.AddScoped<IPlayerQuestUpserter, EfPlayerQuestUpserter>();
	services.AddScoped<IEventIngestionStore, EfEventIngestionStore>();
}

/// <summary>
/// Tenta obter o caminho do arquivo de log a partir dos argumentos.
/// </summary>
static bool TryGetFilePath(string[] args, out string filePath)
{
	filePath = string.Empty;

	if (args.Length == 0)
		return false;

	filePath = args[0];

	if (!Path.IsPathRooted(filePath))
		filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, filePath));

	return true;
}

/// <summary>
/// Executa o pipeline de importação para o arquivo especificado.
/// </summary>
static async Task<ImportResult> ExecuteImportAsync(IGameLogImportPipeline pipeline, string filePath)
{
	var stopwatch = Stopwatch.StartNew();
	var lineCount = 0;
	var lines = ReadLinesWithProgressAsync(filePath, CancellationToken.None);
	
	var result = await pipeline.ImportAsync(lines, CancellationToken.None);
	
	stopwatch.Stop();
	result = result with { Duration = stopwatch.Elapsed };
	
	return result;

	async IAsyncEnumerable<string> ReadLinesWithProgressAsync(string path, [EnumeratorCancellation] CancellationToken ct)
	{
		await foreach (var line in ReadLinesAsync(path, ct))
		{
			lineCount++;
			if (lineCount % 5000 == 0)
			{
				ConsoleOutput.WriteProgress(lineCount, stopwatch.Elapsed.TotalSeconds);
			}
			yield return line;
		}
	}
}

/// <summary>
/// Centraliza toda a lógica de saída do console da aplicação.
/// Fornece métodos formatados para exibir cabeçalhos, mensagens de progresso, 
/// resumos e tratamento consistente de erros com cores apropriadas.
/// </summary>
static class ConsoleOutput
{
	private const string Separator = "====================================================";
	private const ConsoleColor ErrorColor = ConsoleColor.Red;
	private const ConsoleColor SuccessColor = ConsoleColor.Green;

	/// <summary>
	/// Exibe o cabeçalho da aplicação com o nome da ferramenta.
	/// </summary>
	public static void WriteHeader()
	{
		WriteLine(Separator);
		WriteLine("  Ferramenta de Importação de Logs BattleQuest");
		WriteLine(Separator);
		WriteLine();
	}

	/// <summary>
	/// Exibe mensagem informando que a validação do arquivo está em andamento.
	/// </summary>
	public static void WriteValidating()
	{
		WriteLine("Validando o caminho do arquivo...");
	}

	/// <summary>
	/// Exibe mensagem confirmando que o arquivo foi validado com sucesso.
	/// </summary>
	public static void WriteFileValidated()
	{
		WriteLine("Arquivo validado com sucesso.");
		WriteLine();
	}

	/// <summary>
	/// Exibe mensagem de erro quando o arquivo não é encontrado.
	/// </summary>
	/// <param name="filePath">Caminho completo do arquivo que não foi encontrado.</param>
	public static void WriteFileNotFound(string filePath)
	{
		WriteError($"Erro: Arquivo não encontrado - {filePath}");
	}

	/// <summary>
	/// Exibe mensagem informando o início da importação.
	/// </summary>
	/// <param name="fileName">Nome do arquivo que está sendo importado.</param>
	public static void WriteStartingImport(string fileName)
	{
		WriteLine($"Iniciando importação de: {fileName}");
		WriteLine();
	}

	/// <summary>
	/// Exibe o progresso da importação em intervalos regulares.
	/// </summary>
	/// <param name="lineCount">Número de linhas processadas até o momento.</param>
	/// <param name="elapsedSeconds">Tempo decorrido em segundos desde o início.</param>
	public static void WriteProgress(int lineCount, double elapsedSeconds)
	{
		WriteLine($"[Progresso] Linha {lineCount}: Processando... ({elapsedSeconds:F1}s decorridos)");
	}

	/// <summary>
	/// Exibe o resumo completo da importação com estatísticas e resultado final.
	/// </summary>
	/// <param name="result">Resultado da operação de importação contendo estatísticas.</param>
	public static void WriteImportSummary(ImportResult result)
	{
		WriteLine();
		WriteLine(Separator);
		WriteLine("  Resumo da Importação");
		WriteLine(Separator);
		WriteLine();
		WriteLine($"Total de Linhas Processadas:   {result.LinesRead}");
		WriteLine($"Eventos Importados:             {result.EventsInserted}");
		WriteLine($"Ignorados (Duplicatas):         {result.DuplicatesSkipped}");
		WriteLine($"Erros:                          0");
		WriteLine($"Duração:                        {result.Duration.TotalSeconds:F1} segundos");
		WriteLine();
		WriteSuccess("Importação concluída com sucesso!");
		WriteLine(Separator);
	}

	/// <summary>
	/// Exibe instruções de uso da aplicação com exemplos de comandos.
	/// </summary>
	public static void WriteUsage()
	{
		WriteLine("Uso:");
		WriteLine("  dotnet run --project src/BattleQuest.Importer -- <caminho-do-arquivo-log>");
		WriteLine();
		WriteLine("Exemplos:");
		WriteLine("  dotnet run --project src/BattleQuest.Importer -- Data/game_log_small.txt");
		WriteLine("  dotnet run --project src/BattleQuest.Importer -- Data/game_log_large.txt");
		WriteLine("  dotnet run --project src/BattleQuest.Importer -- \"C:\\logs\\game_log.txt\"");
		WriteLine();
	}

	/// <summary>
	/// Exibe mensagem de erro detalhada quando um tipo de arquivo não suportado é fornecido.
	/// </summary>
	/// <param name="message">Mensagem de erro original da exceção.</param>
	public static void WriteUnsupportedFileTypeError(string message)
	{
		WriteErrorBlock(
			"[ERRO] " + message,
			"Apenas arquivos de texto com logs são suportados.",
			"",
			"Exemplos de arquivos NÃO suportados:",
			"  ✗ Imagens: .jpg, .png, .gif, .bmp",
			"  ✗ Documentos: .docx, .pdf, .doc",
			"  ✗ Planilhas: .xlsx, .xls",
			"  ✗ Executáveis: .exe, .dll"
		);
	}

	/// <summary>
	/// Exibe mensagem de erro quando os dados do arquivo são inválidos.
	/// </summary>
	/// <param name="message">Mensagem de erro original da exceção.</param>
	public static void WriteInvalidDataError(string message)
	{
		WriteErrorBlock(
			"[ERRO] " + message,
			"O arquivo fornecido não é um arquivo de texto válido."
		);
	}

	/// <summary>
	/// Exibe mensagem de erro quando o arquivo especificado não é encontrado.
	/// </summary>
	/// <param name="fileName">Nome do arquivo que não foi encontrado.</param>
	public static void WriteFileNotFoundError(string fileName)
	{
		WriteErrorBlock(
			$"[ERRO] Arquivo não encontrado: {fileName}",
			"Verifique se o caminho está correto e tente novamente."
		);
	}

	/// <summary>
	/// Exibe mensagem de erro quando há problemas de permissão ao acessar o arquivo.
	/// </summary>
	/// <param name="message">Mensagem de erro original da exceção.</param>
	public static void WriteUnauthorizedAccessError(string message)
	{
		WriteErrorBlock(
			$"[ERRO] Acesso negado: {message}",
			"Possíveis causas:",
			"  - O caminho aponta para um diretório ao invés de um arquivo",
			"  - Você não tem permissão de leitura no arquivo",
			"  - O arquivo está sendo usado por outro processo"
		);
	}

	/// <summary>
	/// Exibe mensagem de erro quando ocorre um problema de I/O ao acessar o arquivo.
	/// </summary>
	/// <param name="message">Mensagem de erro original da exceção.</param>
	public static void WriteIOError(string message)
	{
		WriteErrorBlock(
			$"[ERRO] Erro ao acessar o arquivo: {message}",
			"Verifique se o arquivo não está corrompido ou sendo usado por outro programa."
		);
	}

	/// <summary>
	/// Exibe mensagem de erro para exceções não tratadas especificamente.
	/// </summary>
	/// <param name="ex">Exceção capturada contendo detalhes do erro.</param>
	public static void WriteUnexpectedError(Exception ex)
	{
		var lines = new List<string>
		{
			$"[ERRO] Erro inesperado: {ex.Message}",
			$"Tipo: {ex.GetType().Name}"
		};

		#if DEBUG
		lines.Add("");
		lines.Add($"Stack Trace:\n{ex.StackTrace}");
		#endif

		WriteErrorBlock(lines.ToArray());
	}

	/// <summary>
	/// Escreve múltiplas linhas de erro formatadas em vermelho.
	/// </summary>
	/// <param name="lines">Array de linhas a serem exibidas como erro.</param>
	private static void WriteErrorBlock(params string[] lines)
	{
		Console.ForegroundColor = ErrorColor;
		WriteLine();
		foreach (var line in lines)
		{
			WriteLine(line);
		}
		Console.ResetColor();
	}

	/// <summary>
	/// Escreve uma mensagem de erro em vermelho.
	/// </summary>
	/// <param name="message">Mensagem de erro a ser exibida.</param>
	private static void WriteError(string message)
	{
		Console.ForegroundColor = ErrorColor;
		WriteLine(message);
		Console.ResetColor();
	}

	/// <summary>
	/// Escreve uma mensagem de sucesso em verde.
	/// </summary>
	/// <param name="message">Mensagem de sucesso a ser exibida.</param>
	private static void WriteSuccess(string message)
	{
		Console.ForegroundColor = SuccessColor;
		WriteLine(message);
		Console.ResetColor();
	}

	/// <summary>
	/// Wrapper para Console.WriteLine que facilita testes e manutenção futura.
	/// </summary>
	/// <param name="message">Mensagem a ser escrita no console.</param>
	private static void WriteLine(string message = "")
	{
		Console.WriteLine(message);
	}
}
