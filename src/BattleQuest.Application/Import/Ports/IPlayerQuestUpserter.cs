namespace BattleQuest.Application.Import.Ports;

public interface IPlayerQuestUpserter
{
	Task EnsurePlayersAsync(IEnumerable<string?> playerIds, CancellationToken ct);

	Task UpsertPlayerSnapshotAsync(
		string playerId,
		string? name,
		int? level,
		int? zoneId,
		CancellationToken ct);

	Task EnsureQuestAsync(string questId, CancellationToken ct);

	Task UpsertQuestNameAsync(string questId, string questName, CancellationToken ct);
}
