namespace BattleQuest.Domain.Entities
{
	public class ActionType
	{
		public int ActionTypeId { get; set; }
		public required string Name { get; set; }

		public ICollection<Event> Events { get; set; } = new List<Event>();
	}
}
