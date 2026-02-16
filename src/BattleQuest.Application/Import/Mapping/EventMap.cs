using BattleQuest.Domain.Entities;

namespace BattleQuest.Application.Import.Mapping;

/// <summary>
/// Contém um evento mapeado junto com os nomes normalizados das dimensões para resolução de IDs.
/// Usado como intermediário entre o parsing da linha de log e a persistência no banco de dados.
/// </summary>
/// <param name="Event">Entidade Event com dados extraídos (IDs de dimensões ainda não resolvidos)</param>
/// <param name="ChannelName">Nome normalizado do canal em MAIÚSCULO</param>
/// <param name="ActionTypeName">Nome normalizado do tipo de ação em MAIÚSCULO</param>
/// <param name="ZoneName">Nome normalizado da zona em MAIÚSCULO (opcional)</param>
/// <param name="ItemName">Nome normalizado do item em MAIÚSCULO (opcional)</param>
/// <param name="BossName">Nome normalizado do boss em MAIÚSCULO (opcional)</param>
/// <param name="QuestName">Nome da quest extraído (quando disponível)</param>
/// <param name="PlayerName">Nome do player extraído (quando disponível)</param>
/// <param name="PlayerLevel">Nível do player extraído (quando disponível)</param>
/// <param name="Fields">Dicionário original de campos parseados para referência</param>
public sealed record EventMap(
	Event Event,
	string ChannelName,
	string ActionTypeName,
	string? ZoneName,
	string? ItemName,
	string? BossName,
	string? QuestName,
	string? PlayerName,
	int? PlayerLevel,
	IReadOnlyDictionary<string, string> Fields
);
