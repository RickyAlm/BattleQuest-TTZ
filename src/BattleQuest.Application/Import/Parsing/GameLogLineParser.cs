using System.Globalization;

namespace BattleQuest.Application.Import.Parsing;

/// <summary>
/// Parser estático para linhas de log.
/// Converte linhas de texto no formato "[timestamp] [CHANNEL] ACTION_TYPE key1=value1 key2="value""
/// em objetos ParsedLogLine estruturados.
/// </summary>
public static class GameLogLineParser
{
	private const int TimestampLength = 19;
	private const int MinimumLineLength = 25;
	private const char ChannelStartDelimiter = '[';
	private const char ChannelEndDelimiter = ']';
	private const char KeyValueSeparator = '=';
	private const char QuoteDelimiter = '"';
	private const char Space = ' ';

	private static readonly string[] TimestampFormats =
	[
		"yyyy-MM-dd HH:mm:ss",
		"yyyy-MM-dd HH:mm:ss.fff"
	];

	/// <summary>
	/// Tenta fazer o parsing de uma linha de log no formato:
	/// [timestamp] [CHANNEL] ACTION_TYPE key1=value1 key2="value with spaces"
	/// </summary>
	public static bool TryParse(string line, out ParsedLogLine parsed)
	{
		parsed = default!;
		
		if (string.IsNullOrWhiteSpace(line) || line.Length < MinimumLineLength)
			return false;

		var position = 0;

		if (!TryExtractTimestamp(line, ref position, out var occurredAt))
			return false;

		if (!TryExtractChannel(line, ref position, out var channel))
			return false;

		if (!TryExtractActionType(line, ref position, out var actionType))
			return false;

		var fields = ExtractKeyValueFields(line, position);

		parsed = new ParsedLogLine(occurredAt, channel, actionType, fields, line);
		return true;
	}

	/// <summary>
	/// Extrai e valida o timestamp do início da linha de log.
	/// </summary>
	private static bool TryExtractTimestamp(string line, ref int position, out DateTimeOffset occurredAt)
	{
		occurredAt = default;

		if (line.Length < TimestampLength)
			return false;

		var timestampText = line[..TimestampLength];
		if (!TryParseTimestamp(timestampText, out occurredAt))
			return false;

		position = TimestampLength;
		SkipSpaces(line, ref position);

		return true;
	}

	/// <summary>
	/// Extrai o nome do canal entre os delimitadores [CHANNEL].
	/// </summary>
	private static bool TryExtractChannel(string line, ref int position, out string channel)
	{
		channel = string.Empty;

		if (position >= line.Length || line[position] != ChannelStartDelimiter)
			return false;

		position++;

		var channelStart = position;
		var channelEnd = line.IndexOf(ChannelEndDelimiter, channelStart);

		if (channelEnd < 0)
			return false;

		channel = line.Substring(channelStart, channelEnd - channelStart);
		position = channelEnd + 1;
		SkipSpaces(line, ref position);

		return true;
	}

	/// <summary>
	/// Extrai o tipo de ação (token até o próximo espaço após o canal).
	/// </summary>
	private static bool TryExtractActionType(string line, ref int position, out string actionType)
	{
		actionType = string.Empty;

		if (position >= line.Length)
			return false;

		var actionStart = position;
		while (position < line.Length && line[position] != Space)
			position++;

		actionType = line.Substring(actionStart, position - actionStart);
		return !string.IsNullOrEmpty(actionType);
	}

	/// <summary>
	/// Extrai todos os pares key=value do restante da linha.
	/// Suporta valores com e sem aspas.
	/// </summary>
	private static Dictionary<string, string> ExtractKeyValueFields(string line, int position)
	{
		var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		while (position < line.Length)
		{
			SkipSpaces(line, ref position);

			if (position >= line.Length)
				break;

			if (!TryExtractKey(line, ref position, out var key))
				continue;

			var value = ExtractValue(line, ref position);
			fields[key] = value;
		}

		return fields;
	}

	/// <summary>
	/// Extrai a chave de um par key=value.
	/// Retorna false se não encontrar o separador '='.
	/// </summary>
	private static bool TryExtractKey(string line, ref int position, out string key)
	{
		key = string.Empty;
		var keyStart = position;

		while (position < line.Length && line[position] != KeyValueSeparator && line[position] != Space)
			position++;

		if (position >= line.Length || line[position] != KeyValueSeparator)
		{
			SkipToNextSpace(line, ref position);
			return false;
		}

		key = line.Substring(keyStart, position - keyStart);
		position++;
		return true;
	}

	/// <summary>
	/// Extrai o valor de um par key=value.
	/// Delega para ExtractQuotedValue ou ExtractUnquotedValue conforme necessário.
	/// </summary>
	private static string ExtractValue(string line, ref int position)
	{
		if (position >= line.Length)
			return string.Empty;

		if (line[position] == QuoteDelimiter)
			return ExtractQuotedValue(line, ref position);

		return ExtractUnquotedValue(line, ref position);
	}

	/// <summary>
	/// Extrai um valor delimitado por aspas duplas.
	/// </summary>
	private static string ExtractQuotedValue(string line, ref int position)
	{
		position++;
		var valueStart = position;

		while (position < line.Length && line[position] != QuoteDelimiter)
			position++;

		var value = line.Substring(valueStart, position - valueStart);

		if (position < line.Length && line[position] == QuoteDelimiter)
			position++;

		return value;
	}

	/// <summary>
	/// Extrai um valor sem aspas (até o próximo espaço).
	/// </summary>
	private static string ExtractUnquotedValue(string line, ref int position)
	{
		var valueStart = position;

		while (position < line.Length && line[position] != Space)
			position++;

		return line.Substring(valueStart, position - valueStart);
	}

	/// <summary>
	/// Avança a posição pulando todos os espaços consecutivos.
	/// </summary>
	private static void SkipSpaces(string line, ref int position)
	{
		while (position < line.Length && line[position] == Space)
			position++;
	}

	/// <summary>
	/// Avança a posição até o próximo espaço (usado para ignorar tokens inválidos).
	/// </summary>
	private static void SkipToNextSpace(string line, ref int position)
	{
		while (position < line.Length && line[position] != Space)
			position++;
	}

	/// <summary>
	/// Converte o texto do timestamp para DateTimeOffset usando os formatos suportados.
	/// </summary>
	private static bool TryParseTimestamp(string timestampText, out DateTimeOffset occurredAt)
	{
		occurredAt = default;

		if (!DateTime.TryParseExact(
			timestampText,
			TimestampFormats,
			CultureInfo.InvariantCulture,
			DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
			out var dateTime))
		{
			return false;
		}

		occurredAt = new DateTimeOffset(dateTime, TimeSpan.Zero);
		return true;
	}
}
