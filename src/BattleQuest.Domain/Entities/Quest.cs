namespace BattleQuest.Domain.Entities;

public class Quest
{
	public required string QuestId { get; set; }
	public string? Name { get; set; }

	public ICollection<Event> Events { get; set; } = new List<Event>();
}
