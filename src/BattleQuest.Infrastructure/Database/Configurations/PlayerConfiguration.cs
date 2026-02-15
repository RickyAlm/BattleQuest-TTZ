using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class PlayerConfiguration : IEntityTypeConfiguration<Player>
	{
		public void Configure(EntityTypeBuilder<Player> builder)
		{
			builder.ToTable("Player");

			builder.HasKey(p => p.PlayerId);

			builder.Property(p => p.PlayerId)
				.IsRequired()
				.HasColumnType("varchar(20)");

			builder.Property(p => p.Name)
				.HasColumnType("varchar(100)");

			builder.HasOne(p => p.LastKnownZone)
				.WithMany(z => z.LastKnownPlayers)
				.HasForeignKey(p => p.LastKnownZoneId)
				.OnDelete(DeleteBehavior.SetNull);

			builder.HasIndex(p => p.Name);
			builder.HasIndex(p => p.LastKnownZoneId);
		}
	}
}
