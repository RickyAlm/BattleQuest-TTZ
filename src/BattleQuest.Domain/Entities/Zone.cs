namespace BattleQuest.Domain.Entities
{
    public class Zone
    {
        public int ZoneId { get; set; }
        public string? Name { get; set; }

		public ICollection<Player> LastKnownPlayers { get; set; } = new List<Player>();
		public ICollection<Event> Events { get; set; } = new List<Event>();
	}
}
