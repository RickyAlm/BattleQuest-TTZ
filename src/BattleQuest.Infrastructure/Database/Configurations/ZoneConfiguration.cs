using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
	{
		public void Configure(EntityTypeBuilder<Zone> builder)
		{
			builder.ToTable("zones");

			// Keys
			builder.HasKey(z => z.ZoneId);

			builder.Property(z => z.ZoneId)
				.IsRequired()
				.HasColumnName("zone_id")
				.ValueGeneratedOnAdd();

			// Properties
			builder.Property(z => z.Name)
				.IsRequired()
				.HasColumnName("name")
				.HasColumnType("varchar(100)");

			// Indexes
			builder.HasIndex(z => z.Name)
				.IsUnique();
		}
	}
}
