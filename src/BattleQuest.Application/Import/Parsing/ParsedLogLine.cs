namespace BattleQuest.Application.Import.Parsing;

/// <summary>
/// Representa uma linha de log já parseada em seus componentes estruturados.
/// Resultado direto do GameLogLineParser antes do mapeamento para entidades de domínio.
/// </summary>
/// <param name="OccurredAt">Timestamp de quando o evento ocorreu no jogo</param>
/// <param name="Channel">Canal do evento (ex: SYSTEM, CHAT, COMBAT)</param>
/// <param name="ActionType">Tipo de ação executada (ex: PLAYER_JOIN, QUEST_START)</param>
/// <param name="Fields">Dicionário de pares key=value extraídos da linha</param>
/// <param name="Raw">Linha de log original sem modificações</param>
public sealed record ParsedLogLine(
	DateTimeOffset OccurredAt,
	string Channel,
	string ActionType,
	IReadOnlyDictionary<string, string> Fields,
	string Raw
);
