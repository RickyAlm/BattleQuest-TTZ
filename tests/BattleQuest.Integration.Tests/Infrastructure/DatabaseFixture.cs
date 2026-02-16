using BattleQuest.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BattleQuest.Integration.Tests.Infrastructure;

/// <summary>
/// Fixture que gerencia um container PostgreSQL para testes de integração.
/// Implementa IAsyncLifetime para inicializar e destruir o container automaticamente.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
	private readonly PostgreSqlContainer _postgresContainer;
	
	public string ConnectionString => _postgresContainer.GetConnectionString();

	/// <summary>
	/// Inicializa um container PostgreSQL 16 para testes.
	/// </summary>
	public DatabaseFixture()
	{
		_postgresContainer = new PostgreSqlBuilder()
			.WithImage("postgres:16-alpine")
			.WithDatabase("battlequest_test")
			.WithUsername("test")
			.WithPassword("test")
			.WithCleanUp(true)
			.Build();
	}

	/// <summary>
	/// Inicia o container e aplica as migrations antes dos testes.
	/// </summary>
	public async Task InitializeAsync()
	{
		await _postgresContainer.StartAsync();

		// Aplica as migrations no banco de teste
		await using var context = CreateDbContext();
		await context.Database.MigrateAsync();
	}

	/// <summary>
	/// Para e remove o container após os testes.
	/// </summary>
	public async Task DisposeAsync()
	{
		await _postgresContainer.DisposeAsync();
	}

	/// <summary>
	/// Cria uma nova instância do DbContext conectada ao container de teste.
	/// </summary>
	public BattleQuestDbContext CreateDbContext()
	{
		var options = new DbContextOptionsBuilder<BattleQuestDbContext>()
			.UseNpgsql(ConnectionString)
			.UseSnakeCaseNamingConvention()
			.Options;

		return new BattleQuestDbContext(options);
	}

    /// <summary>
    /// Limpa todos os dados de todas as tabelas, mantendo a estrutura.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        await using var context = CreateDbContext();

        await context.Database.ExecuteSqlRawAsync(@"
        TRUNCATE TABLE
            events,
            players,
            quests,
            items,
            bosses,
            zones,
            action_types,
            channels
        RESTART IDENTITY CASCADE;
    ");
    }
}

/// <summary>
/// Collection fixture para compartilhar o DatabaseFixture entre múltiplos testes.
/// Evita criar/destruir o container para cada classe de teste.
/// </summary>
[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
