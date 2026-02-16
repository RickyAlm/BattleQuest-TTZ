using BattleQuest.Application.Import.Parsing;
using BattleQuest.Domain.Entities;

namespace BattleQuest.Application.Import.Mapping;

/// <summary>
/// Mapeia uma linha de log parseada para um evento de domínio com todas as suas dimensões.
/// </summary>
public static class EventMapper
{
	/// <summary>
	/// Mapeia uma linha de log parseada para um EventMap contendo o evento e metadados de dimensões.
	/// </summary>
	/// <param name="parsed">Linha de log já parseada</param>
	/// <param name="insertedAtUtc">Timestamp de inserção no banco</param>
	/// <returns>EventMap com evento e nomes das dimensões para resolução</returns>
	public static EventMap Map(ParsedLogLine parsed, DateTimeOffset insertedAtUtc)
	{
		var channelName = NormalizeToUpper(parsed.Channel);
		var actionName = NormalizeToUpper(parsed.ActionType);

		var playerIds = ExtractPlayerIds(parsed.Fields);
		var dimensions = ExtractDimensionNames(parsed.Fields);
		var (questName, playerName, playerLevel) = ResolvePlayerAndQuestNames(parsed.Fields, playerIds.PlayerId, dimensions.QuestId);
		var messageText = ExtractMessageText(parsed.Fields);
		var (locationX, locationY) = ExtractLocation(parsed.Fields);

		var ev = BuildEvent(parsed, insertedAtUtc, playerIds, dimensions, playerLevel, messageText, locationX, locationY);

		return new EventMap(
			ev,
			channelName,
			actionName,
			dimensions.ZoneName,
			dimensions.ItemName,
			dimensions.BossName,
			questName,
			playerName,
			playerLevel,
			parsed.Fields
		);
	}

	/// <summary>
	/// Extrai os IDs dos players envolvidos no evento.
	/// Player principal pode vir de player_id, id (PLAYER_JOIN) ou defeated_by (BOSS_DEFEAT).
	/// </summary>
	private static PlayerIds ExtractPlayerIds(IReadOnlyDictionary<string, string> fields)
	{
		var playerId = GetField(fields, "player_id")
					?? GetField(fields, "id")
					?? GetField(fields, "defeated_by");

		var victimId = GetField(fields, "victim_id");
		var killerId = GetField(fields, "killer_id");

		return new PlayerIds(playerId, victimId, killerId);
	}

	/// <summary>
	/// Extrai os nomes das dimensões (Zone, Item, Boss, Quest) normalizadas em MAIÚSCULO.
	/// </summary>
	private static DimensionNames ExtractDimensionNames(IReadOnlyDictionary<string, string> fields)
	{
		var zoneName = NormalizeToUpperOrNull(GetField(fields, "zone"));
		var itemName = NormalizeToUpperOrNull(GetField(fields, "item"));
		var bossName = NormalizeToUpperOrNull(GetField(fields, "boss_name"));
		var questId = GetField(fields, "quest_id");

		return new DimensionNames(zoneName, itemName, bossName, questId);
	}

	/// <summary>
	/// Resolve ambiguidade do campo "name" que pode ser nome de quest ou player.
	/// Usa heurística baseada em quest_id e level para determinar o tipo.
	/// </summary>
	private static (string? QuestName, string? PlayerName, int? PlayerLevel) ResolvePlayerAndQuestNames(
		IReadOnlyDictionary<string, string> fields,
		string? playerId,
		string? questId)
	{
		string? questName = null;
		string? playerName = null;
		int? playerLevel = null;

		var nameField = GetField(fields, "name");

		if (!string.IsNullOrWhiteSpace(questId) && !string.IsNullOrWhiteSpace(nameField))
		{
			questName = nameField;
		}
		else if (!string.IsNullOrWhiteSpace(playerId))
		{
			playerLevel = TryGetInt(fields, "level");
			if (playerLevel.HasValue && !string.IsNullOrWhiteSpace(nameField))
				playerName = nameField;
		}

		return (questName, playerName, playerLevel);
	}

	/// <summary>
	/// Extrai o texto de mensagem do evento.
	/// Pode vir do campo "message" (CHAT) ou "text" (SYSTEM).
	/// </summary>
	private static string? ExtractMessageText(IReadOnlyDictionary<string, string> fields)
		=> GetField(fields, "message") ?? GetField(fields, "text");

	/// <summary>
	/// Extrai as coordenadas de localização do evento.
	/// </summary>
	private static (int? X, int? Y) ExtractLocation(IReadOnlyDictionary<string, string> fields)
	{
		var location = GetField(fields, "location");
		
		if (!string.IsNullOrWhiteSpace(location) && TryParseLocation(location!, out var x, out var y))
			return (x, y);

		return (null, null);
	}

	/// <summary>
	/// Constrói a entidade Event com todos os dados extraídos.
	/// </summary>
	private static Event BuildEvent(
		ParsedLogLine parsed,
		DateTimeOffset insertedAtUtc,
		PlayerIds playerIds,
		DimensionNames dimensions,
		int? playerLevel,
		string? messageText,
		int? locationX,
		int? locationY)
	{
		return new Event
		{
			ChannelId = 0,
			ActionTypeId = 0,

			PlayerId = playerIds.PlayerId,
			VictimPlayerId = playerIds.VictimId,
			KillerPlayerId = playerIds.KillerId,

			QuestId = dimensions.QuestId,

			ZoneId = null,
			ItemId = null,
			BossId = null,

			OccurredAt = parsed.OccurredAt,
			InsertedAt = insertedAtUtc,

			Quantity = TryGetInt(parsed.Fields, "qty") ?? TryGetInt(parsed.Fields, "quantity"),
			Xp = TryGetInt(parsed.Fields, "xp"),
			Gold = TryGetInt(parsed.Fields, "gold"),
			Hp = TryGetInt(parsed.Fields, "hp"),
			Damage = TryGetInt(parsed.Fields, "damage"),
			Points = TryGetInt(parsed.Fields, "points"),

			Method = GetField(parsed.Fields, "method"),
			Reason = GetField(parsed.Fields, "reason"),
			PlayerLevel = TryGetInt(parsed.Fields, "level") ?? TryGetInt(parsed.Fields, "player_level"),

			LocationX = locationX,
			LocationY = locationY,

			MessageText = messageText,

			Raw = parsed.Raw,
			EventHash = string.Empty
		};
	}

	/// <summary>
	/// Normaliza uma string para MAIÚSCULO invariante.
	/// </summary>
	private static string NormalizeToUpper(string value) 
		=> value.Trim().ToUpperInvariant();

	/// <summary>
	/// Normaliza uma string para MAIÚSCULO ou retorna null se vazia.
	/// </summary>
	private static string? NormalizeToUpperOrNull(string? value)
		=> string.IsNullOrWhiteSpace(value) ? null : NormalizeToUpper(value!);

	/// <summary>
	/// Obtém um valor do dicionário de campos ou retorna null.
	/// </summary>
	private static string? GetField(IReadOnlyDictionary<string, string> fields, string key)
		=> fields.TryGetValue(key, out var value) ? value : null;

	/// <summary>
	/// Tenta obter um valor inteiro do dicionário de campos.
	/// </summary>
	private static int? TryGetInt(IReadOnlyDictionary<string, string> fields, string key) 
		=> fields.TryGetValue(key, out var value) && int.TryParse(value, out var number) ? number : null;

	/// <summary>
	/// Tenta fazer o parsing de uma localização no formato "(x,y)" ou "x,y".
	/// </summary>
	private static bool TryParseLocation(string value, out int x, out int y)
	{
		x = y = 0;

		value = value.Trim();
		if (value.Length < 5)
			return false;

		if (value[0] == '(' && value[^1] == ')')
			value = value[1..^1];

		var parts = value.Split(',', 2, StringSplitOptions.TrimEntries);
		if (parts.Length != 2)
			return false;

		return int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
	}

	/// <summary>
	/// Agrupa os IDs dos players envolvidos em um evento.
	/// </summary>
	private readonly record struct PlayerIds(string? PlayerId, string? VictimId, string? KillerId);

	/// <summary>
	/// Agrupa os nomes das dimensões extraídas dos campos.
	/// </summary>
	private readonly record struct DimensionNames(string? ZoneName, string? ItemName, string? BossName, string? QuestId);
}
