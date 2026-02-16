namespace BattleQuest.Domain.Entities;

public class Event
{
	public int EventId { get; set; }
	public int ChannelId { get; set; }
	public int ActionTypeId { get; set; }
	public string? PlayerId { get; set; }
	public string? VictimPlayerId { get; set; }
	public string? KillerPlayerId { get; set; }
	public string? QuestId { get; set; }
	public int? ZoneId { get; set; }
	public int? ItemId { get; set; }
	public int? BossId { get; set; }
	public DateTimeOffset OccurredAt { get; set; }
	public int? Quantity { get; set; }
	public int? Xp { get; set; }
	public int? Gold { get; set; }
	public int? Hp { get; set; }
	public int? Damage { get; set; }
	public string? Method { get; set; }
	public int? PlayerLevel { get; set; }
	public int? Points { get; set; }
	public string? Reason { get; set; }
	public int? LocationX { get; set; }
	public int? LocationY { get; set; }
	public string? MessageText { get; set; }
	public DateTimeOffset InsertedAt { get; set; }
	public required string Raw { get; set; }
	public required string EventHash { get; set; }

	public Channel Channel { get; set; } = default!;
	public ActionType ActionType { get; set; } = default!;
	public Boss? Boss { get; set; }
	public Item? Item { get; set; }
	public Player? Player { get; set; }
	public Player? VictimPlayer { get; set; }
	public Player? KillerPlayer { get; set; }
	public Quest? Quest { get; set; }
	public Zone? Zone { get; set; }
}
