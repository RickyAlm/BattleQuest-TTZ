using BattleQuest.Application.Import.Ports;
using BattleQuest.Domain.Entities;

namespace BattleQuest.Application.Tests.Fakes;

/// <summary>
/// Fake implementation do IEventIngestionStore para testes.
/// Simula persistência em memória com detecção de duplicatas por hash.
/// </summary>
public sealed class FakeEventIngestionStore : IEventIngestionStore
{
	private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

	public List<Event> InsertedEvents { get; } = new();

	public Task<(int inserted, int skipped)> InsertBatchAsync(IReadOnlyList<Event> events, CancellationToken ct)
	{
		var inserted = 0;
		var skipped = 0;

		foreach (var e in events)
		{
			if (_seen.Add(e.EventHash))
			{
				inserted++;
				InsertedEvents.Add(e);
			}
			else
			{
				skipped++;
			}
		}

		return Task.FromResult((inserted, skipped));
	}
}
