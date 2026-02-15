using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
	{
		public void Configure(EntityTypeBuilder<Zone> builder)
		{
			builder.ToTable("Zone");

			builder.HasKey(z => z.ZoneId);

			builder.Property(z => z.Name)
				.IsRequired()
				.HasColumnType("varchar(100)");

			builder.HasIndex(z => z.Name)
				.IsUnique();
		}
	}
}
