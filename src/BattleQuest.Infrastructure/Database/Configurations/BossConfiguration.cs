using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations;

public class BossConfiguration : IEntityTypeConfiguration<Boss>
{
	public void Configure(EntityTypeBuilder<Boss> builder)
	{
		builder.ToTable("bosses");

		// Keys
		builder.HasKey(b => b.BossId);

		builder.Property(b => b.BossId)
			.HasColumnName("boss_id")
			.ValueGeneratedOnAdd();

		// Properties
		builder.Property(b => b.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasColumnType("varchar(100)");

		// Indexes
		builder.HasIndex(b => b.Name)
			.IsUnique();
	}
}
