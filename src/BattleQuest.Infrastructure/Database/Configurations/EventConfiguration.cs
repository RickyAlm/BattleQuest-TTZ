using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
	public void Configure(EntityTypeBuilder<Event> builder)
	{
		builder.ToTable("events");

		// Keys
		builder.HasKey(e => e.EventId);

		builder.Property(e => e.EventId)
			.HasColumnName("event_id")
			.ValueGeneratedOnAdd();

		// FKs
		builder.Property(e => e.ChannelId).HasColumnName("channel_id");
		builder.Property(e => e.ActionTypeId).HasColumnName("action_type_id");

		builder.Property(e => e.PlayerId)
			.HasColumnName("player_id")
			.HasColumnType("varchar(20)");

		builder.Property(e => e.VictimPlayerId)
			.HasColumnName("victim_player_id")
			.HasColumnType("varchar(20)");

		builder.Property(e => e.KillerPlayerId)
			.HasColumnName("killer_player_id")
			.HasColumnType("varchar(20)");

		builder.Property(e => e.QuestId)
			.HasColumnName("quest_id")
			.HasColumnType("varchar(20)");

		builder.Property(e => e.ZoneId).HasColumnName("zone_id");
		builder.Property(e => e.ItemId).HasColumnName("item_id");
		builder.Property(e => e.BossId).HasColumnName("boss_id");

		// Properties
		builder.Property(e => e.OccurredAt)
			.IsRequired()
			.HasColumnName("occurred_at")
			.HasColumnType("timestamptz");

		builder.Property(e => e.Quantity).HasColumnName("quantity");
		builder.Property(e => e.Xp).HasColumnName("xp");
		builder.Property(e => e.Gold).HasColumnName("gold");
		builder.Property(e => e.Hp).HasColumnName("hp");
		builder.Property(e => e.Damage).HasColumnName("damage");

		builder.Property(e => e.Method)
			.HasColumnName("method")
			.HasColumnType("varchar(100)");

		builder.Property(e => e.PlayerLevel).HasColumnName("player_level");
		builder.Property(e => e.Points).HasColumnName("points");

		builder.Property(e => e.Reason)
			.HasColumnName("reason")
			.HasColumnType("varchar(255)");

		builder.Property(e => e.LocationX).HasColumnName("location_x");
		builder.Property(e => e.LocationY).HasColumnName("location_y");

		builder.Property(e => e.MessageText)
			.HasColumnName("message_text")
			.HasColumnType("text");

		builder.Property(e => e.InsertedAt)
			.IsRequired()
			.HasColumnName("inserted_at")
			.HasColumnType("timestamptz");

		builder.Property(e => e.Raw)
			.IsRequired()
			.HasColumnName("raw")
			.HasColumnType("text");

		builder.Property(e => e.EventHash)
			.IsRequired()
			.HasColumnName("event_hash")
			.HasColumnType("varchar(64)");

		// Relationships
		builder.HasOne(e => e.Channel)
			.WithMany(c => c.Events)
			.HasForeignKey(e => e.ChannelId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.ActionType)
			.WithMany(a => a.Events)
			.HasForeignKey(e => e.ActionTypeId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.Zone)
			.WithMany(z => z.Events)
			.HasForeignKey(e => e.ZoneId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.Boss)
			.WithMany(b => b.Events)
			.HasForeignKey(e => e.BossId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.Item)
			.WithMany(i => i.Events)
			.HasForeignKey(e => e.ItemId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.Quest)
			.WithMany(q => q.Events)
			.HasForeignKey(e => e.QuestId)
			.OnDelete(DeleteBehavior.Restrict);

		// Player relationships
		builder.HasOne(e => e.Player)
			.WithMany()
			.HasForeignKey(e => e.PlayerId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.VictimPlayer)
			.WithMany()
			.HasForeignKey(e => e.VictimPlayerId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(e => e.KillerPlayer)
			.WithMany()
			.HasForeignKey(e => e.KillerPlayerId)
			.OnDelete(DeleteBehavior.Restrict);

		// Indexes
		builder.HasIndex(e => e.OccurredAt);
		builder.HasIndex(e => e.EventHash).IsUnique();
		builder.HasIndex(e => new { e.PlayerId, e.ActionTypeId });
		builder.HasIndex(e => new { e.VictimPlayerId, e.ActionTypeId });
		builder.HasIndex(e => new { e.KillerPlayerId, e.ActionTypeId });
		builder.HasIndex(e => new { e.ActionTypeId, e.ItemId });
	}
}
