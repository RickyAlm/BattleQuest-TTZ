namespace BattleQuest.Application.Import.Ports;

public interface IDimensionResolver
{
	Task<int> GetOrCreateChannelIdAsync(string channelUpper, CancellationToken ct);
	Task<int> GetOrCreateActionTypeIdAsync(string actionTypeUpper, CancellationToken ct);

	Task<int?> GetOrCreateZoneIdAsync(string? zoneUpper, CancellationToken ct);
	Task<int?> GetOrCreateItemIdAsync(string? itemUpper, CancellationToken ct);
	Task<int?> GetOrCreateBossIdAsync(string? bossUpper, CancellationToken ct);
}
