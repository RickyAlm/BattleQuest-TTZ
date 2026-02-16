using BattleQuest.Domain.Entities;

namespace BattleQuest.Application.Import.Ports;

public interface IEventIngestionStore
{
	Task<(int inserted, int skipped)> InsertBatchAsync(IReadOnlyList<Event> events, CancellationToken ct);
}
