namespace BattleQuest.Domain.Entities
{
    public class Item
    {
        public int ItemId { get; set; }
        public string? Name { get; set; }

		public ICollection<Event> Events { get; set; } = new List<Event>();
	}
}
