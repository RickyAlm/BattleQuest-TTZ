namespace BattleQuest.Domain.Entities;

public class Channel
{
	public int ChannelId { get; set; }
	public required string Name { get; set; }

	public ICollection<Event> Events { get; set; } = new List<Event>();
}
