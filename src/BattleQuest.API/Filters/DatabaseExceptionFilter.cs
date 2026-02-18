using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;
using System.Net.Sockets;

namespace BattleQuest.API.Filters;

/// <summary>
/// Filtro global para tratamento de exceções de banco de dados.
/// Transforma exceções em respostas HTTP amigáveis ao usuário.
/// </summary>
public sealed class DatabaseExceptionFilter : IExceptionFilter
{
	private readonly ILogger<DatabaseExceptionFilter> _logger;
	private readonly IHostEnvironment _environment;

	public DatabaseExceptionFilter(
		ILogger<DatabaseExceptionFilter> logger,
		IHostEnvironment environment)
	{
		_logger = logger;
		_environment = environment;
	}

	public void OnException(ExceptionContext context)
	{
		var (isDatabaseError, problemDetails) = AnalyzeException(context.Exception);

		if (!isDatabaseError)
			return;

		_logger.LogError(
			context.Exception,
			"Erro de banco de dados no endpoint {Endpoint}: {Message}",
			context.ActionDescriptor.DisplayName,
			context.Exception.Message);

		context.Result = new ObjectResult(problemDetails)
		{
			StatusCode = problemDetails.Status
		};

		context.ExceptionHandled = true;
	}

	/// <summary>
	/// Analisa a exceção e retorna ProblemDetails apropriado se for erro de banco.
	/// </summary>
	private (bool IsDatabaseError, ProblemDetails Details) AnalyzeException(Exception exception)
	{
		var innerException = exception.InnerException ?? exception;

		if (innerException is NpgsqlException npgsqlEx)
		{
			return (true, CreateDatabaseProblemDetails(npgsqlEx));
		}

		if (innerException is SocketException socketEx)
		{
			return (true, CreateConnectionProblemDetails(socketEx));
		}

		if (exception.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase) &&
        exception.Message.Contains("connect", StringComparison.OrdinalIgnoreCase))
		{
			return (true, CreateConnectionProblemDetails(exception));
		}

		return (false, null!);
	}

	/// <summary>
	/// Cria ProblemDetails para erros de conexão com o banco de dados.
	/// </summary>
	private ProblemDetails CreateConnectionProblemDetails(Exception exception)
	{
		var problemDetails = new ProblemDetails
		{
			Type = "https://tools.ietf.org/html/rfc9110#section-15.6.4",
			Title = "Serviço de Banco de Dados Indisponível",
			Status = StatusCodes.Status503ServiceUnavailable,
			Detail = "Não foi possível conectar ao banco de dados. " +
              "O serviço pode estar temporariamente indisponível."
		};

		problemDetails.Extensions["possibleCauses"] = new[]
		{
			"O container Docker do PostgreSQL não está em execução",
			"O banco de dados está reiniciando",
			"Problemas de rede ou firewall",
			"Configuração de conexão incorreta"
		};

		problemDetails.Extensions["suggestion"] = _environment.IsDevelopment()
			? "Verifique se o Docker está em execução: docker ps | findstr postgres"
			: "Entre em contato com o administrador do sistema.";

		if (_environment.IsDevelopment())
		{
			problemDetails.Extensions["technicalDetails"] = new
			{
				exceptionType = exception.GetType().Name,
				message = exception.Message
			};
		}

		return problemDetails;
	}

	/// <summary>
	/// Cria ProblemDetails para erros específicos do PostgreSQL.
	/// </summary>
	private ProblemDetails CreateDatabaseProblemDetails(NpgsqlException exception)
	{
		var isConnectionError = exception.Message.Contains("connect", StringComparison.OrdinalIgnoreCase) ||
                            exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase);

		if (isConnectionError)
		{
			return CreateConnectionProblemDetails(exception);
		}

		var problemDetails = new ProblemDetails
		{
			Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
			Title = "Erro no Banco de Dados",
			Status = StatusCodes.Status500InternalServerError,
			Detail = "Ocorreu um erro ao processar a requisição no banco de dados."
		};

		if (_environment.IsDevelopment())
		{
			problemDetails.Extensions["technicalDetails"] = new
			{
				errorCode = exception.ErrorCode,
				sqlState = exception.SqlState,
				message = exception.Message
			};
		}

		return problemDetails;
	}
}
