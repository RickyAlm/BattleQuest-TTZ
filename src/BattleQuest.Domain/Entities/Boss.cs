namespace BattleQuest.Domain.Entities
{
    public class Boss
    {
        public int BossId { get; set; }
        public string? Name { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
	}
}
 