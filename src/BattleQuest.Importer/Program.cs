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
using static BattleQuest.Importer.FileLineReader;

// Configuração e inicialização do host
var host = CreateHost(args);

// Validação de argumentos
if (!TryGetFilePath(args, out var filePath))
{
	DisplayUsage();
	return;
}

// Execução da importação
try
{
	var pipeline = host.Services.GetRequiredService<IGameLogImportPipeline>();
	var result = await ExecuteImportAsync(pipeline, filePath);

	// Exibição do resultado
	DisplayResult(result);
}
catch (FileNotFoundException ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine($"\n[ERRO] Arquivo não encontrado: {ex.FileName}");
	Console.WriteLine("\nVerifique se o caminho está correto e tente novamente.");
	Console.ResetColor();
	Environment.Exit(1);
}
catch (UnauthorizedAccessException ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine($"\n[ERRO] Acesso negado: {ex.Message}");
	Console.WriteLine("\nPossíveis causas:");
	Console.WriteLine("  - O caminho aponta para um diretório ao invés de um arquivo");
	Console.WriteLine("  - Você não tem permissão de leitura no arquivo");
	Console.WriteLine("  - O arquivo está sendo usado por outro processo");
	Console.ResetColor();
	Environment.Exit(1);
}
catch (IOException ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine($"\n[ERRO] Erro ao acessar o arquivo: {ex.Message}");
	Console.WriteLine("\nVerifique se o arquivo não está corrompido ou sendo usado por outro programa.");
	Console.ResetColor();
	Environment.Exit(1);
}
catch (Exception ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine($"\n[ERRO] Erro inesperado: {ex.Message}");
	Console.WriteLine($"\nTipo: {ex.GetType().Name}");
	#if DEBUG
	Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");
	#endif
	Console.ResetColor();
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
	var lines = ReadLinesAsync(filePath, CancellationToken.None);
	return await pipeline.ImportAsync(lines, CancellationToken.None);
}

/// <summary>
/// Exibe a mensagem de uso da aplicação.
/// </summary>
static void DisplayUsage()
{
	Console.WriteLine("=== BattleQuest Log Importer ===");
	Console.WriteLine();
	Console.WriteLine("Uso:");
	Console.WriteLine("  dotnet run --project src/BattleQuest.Importer -- <caminho-do-arquivo-log>");
	Console.WriteLine();
	Console.WriteLine("Exemplos:");
	Console.WriteLine("  dotnet run --project src/BattleQuest.Importer -- Data/game_log_large.txt");
	Console.WriteLine("  dotnet run --project src/BattleQuest.Importer -- \"C:\\logs\\game_log.txt\"");
	Console.WriteLine();
	Console.WriteLine("Observações:");
	Console.WriteLine("  - O caminho deve apontar para um ARQUIVO, não um diretório");
	Console.WriteLine("  - Use aspas se o caminho contiver espaços");
	Console.WriteLine("  - Certifique-se de ter permissão de leitura no arquivo");
}

/// <summary>
/// Exibe o resultado da importação no console.
/// </summary>
static void DisplayResult(ImportResult result)
{
	Console.WriteLine($"Import finalizado. Lidas: {result.LinesRead}, Inseridas: {result.EventsInserted}, Duplicadas: {result.DuplicatesSkipped}");
}
