using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class BossConfiguration : IEntityTypeConfiguration<Boss>
	{
		public void Configure(EntityTypeBuilder<Boss> builder)
		{
			builder.ToTable("Boss");

			builder.HasKey(b => b.BossId);

			builder.Property(b => b.Name)
				.IsRequired()
				.HasColumnType("varchar(100)");

			builder.HasIndex(b => b.Name)
				.IsUnique();
		}
	}
}
