using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class EventConfiguration : IEntityTypeConfiguration<Event>
	{
		public void Configure(EntityTypeBuilder<Event> builder)
		{
			builder.ToTable("Event");
			builder.HasKey(e => e.EventId);

			// Strings
			builder.Property(e => e.PlayerId).HasColumnType("varchar(20)");
			builder.Property(e => e.VictimPlayerId).HasColumnType("varchar(20)");
			builder.Property(e => e.KillerPlayerId).HasColumnType("varchar(20)");
			builder.Property(e => e.QuestId).HasColumnType("varchar(20)");

			builder.Property(e => e.Raw)
				.IsRequired()
				.HasColumnType("varchar(max)");

			builder.Property(e => e.EventHash)
				.IsRequired()
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
			builder.HasIndex(e => e.PlayerId);
			builder.HasIndex(e => e.VictimPlayerId);
			builder.HasIndex(e => e.KillerPlayerId);
			builder.HasIndex(e => e.ChannelId);
			builder.HasIndex(e => e.ActionTypeId);
			builder.HasIndex(e => e.OccurredAt);
			builder.HasIndex(e => e.ZoneId);
			builder.HasIndex(e => e.EventHash).IsUnique();
		}
	}
}
