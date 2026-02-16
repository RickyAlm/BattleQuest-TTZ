using BattleQuest.Application.Import.Ports;

namespace BattleQuest.Application.Tests.Fakes;

/// <summary>
/// Fake implementation do IPlayerQuestUpserter para testes.
/// Registra todas as chamadas para validação em testes.
/// </summary>
public sealed class FakePlayerQuestUpserter : IPlayerQuestUpserter
{
	public List<string?> EnsurePlayersCalls { get; } = new();
	public List<(string playerId, string? name, int? level, int? zoneId)> PlayerSnapshots { get; } = new();
	public List<string> EnsureQuestCalls { get; } = new();
	public List<(string questId, string questName)> QuestNameUpserts { get; } = new();

	public Task EnsurePlayersAsync(IEnumerable<string?> playerIds, CancellationToken ct)
	{
		EnsurePlayersCalls.AddRange(playerIds);
		return Task.CompletedTask;
	}

	public Task UpsertPlayerSnapshotAsync(string playerId, string? name, int? level, int? zoneId, CancellationToken ct)
	{
		PlayerSnapshots.Add((playerId, name, level, zoneId));
		return Task.CompletedTask;
	}

	public Task EnsureQuestAsync(string questId, CancellationToken ct)
	{
		EnsureQuestCalls.Add(questId);
		return Task.CompletedTask;
	}

	public Task UpsertQuestNameAsync(string questId, string questName, CancellationToken ct)
	{
		QuestNameUpserts.Add((questId, questName));
		return Task.CompletedTask;
	}
}
