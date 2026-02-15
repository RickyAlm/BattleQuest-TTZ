using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class PlayerConfiguration : IEntityTypeConfiguration<Player>
	{
		public void Configure(EntityTypeBuilder<Player> builder)
		{
			builder.ToTable("players");

			// Keys
			builder.HasKey(p => p.PlayerId);

			builder.Property(p => p.PlayerId)
				.IsRequired()
				.HasColumnName("player_id")
				.HasColumnType("varchar(20)");

			builder.Property(p => p.LastKnownZoneId)
				.HasColumnName("last_known_zone_id");

			// Properties
			builder.Property(p => p.LastKnownLevel)
				.HasColumnName("last_known_level");

			builder.Property(p => p.Name)
				.HasColumnName("name")
				.HasColumnType("varchar(100)");


			// Relationships
			builder.HasOne(p => p.LastKnownZone)
				.WithMany(z => z.LastKnownPlayers)
				.HasForeignKey(p => p.LastKnownZoneId)
				.OnDelete(DeleteBehavior.SetNull);

			// Indexes
			builder.HasIndex(p => p.Name);
			builder.HasIndex(p => p.LastKnownZoneId);
		}
	}
}
