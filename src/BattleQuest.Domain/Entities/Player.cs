namespace BattleQuest.Domain.Entities
{
    public class Player
    {
        public required string PlayerId { get; set; }
		public int? LastKnownZoneId { get; set; }
		public string? Name { get; set; }
        public int? LastKnownLevel { get; set; }

		public Zone? LastKnownZone { get; set; }
	}
}
