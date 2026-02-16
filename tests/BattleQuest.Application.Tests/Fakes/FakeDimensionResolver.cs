using BattleQuest.Application.Import.Ports;

namespace BattleQuest.Application.Tests.Fakes;

/// <summary>
/// Fake implementation do IDimensionResolver para testes.
/// Armazena requisições para validação e simula geração de IDs.
/// </summary>
public sealed class FakeDimensionResolver : IDimensionResolver
{
	private int _nextId = 1;

	public List<string> ChannelRequests { get; } = new();
	public List<string> ActionRequests { get; } = new();
	public List<string> ZoneRequests { get; } = new();
	public List<string> ItemRequests { get; } = new();
	public List<string> BossRequests { get; } = new();

	private readonly Dictionary<string, int> _channels = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> _actions = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> _zones = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> _items = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> _bosses = new(StringComparer.Ordinal);

	public Task<int> GetOrCreateChannelIdAsync(string channelUpper, CancellationToken ct)
	{
		ChannelRequests.Add(channelUpper);
		return Task.FromResult(GetOrAdd(_channels, channelUpper));
	}

	public Task<int> GetOrCreateActionTypeIdAsync(string actionTypeUpper, CancellationToken ct)
	{
		ActionRequests.Add(actionTypeUpper);
		return Task.FromResult(GetOrAdd(_actions, actionTypeUpper));
	}

	public Task<int?> GetOrCreateZoneIdAsync(string? zoneUpper, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(zoneUpper)) return Task.FromResult<int?>(null);
		ZoneRequests.Add(zoneUpper);
		return Task.FromResult<int?>(GetOrAdd(_zones, zoneUpper));
	}

	public Task<int?> GetOrCreateItemIdAsync(string? itemUpper, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(itemUpper)) return Task.FromResult<int?>(null);
		ItemRequests.Add(itemUpper);
		return Task.FromResult<int?>(GetOrAdd(_items, itemUpper));
	}

	public Task<int?> GetOrCreateBossIdAsync(string? bossUpper, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(bossUpper)) return Task.FromResult<int?>(null);
		BossRequests.Add(bossUpper);
		return Task.FromResult<int?>(GetOrAdd(_bosses, bossUpper));
	}

	private int GetOrAdd(Dictionary<string, int> dict, string key)
	{
		if (dict.TryGetValue(key, out var id)) return id;
		id = _nextId++;
		dict[key] = id;
		return id;
	}
}
