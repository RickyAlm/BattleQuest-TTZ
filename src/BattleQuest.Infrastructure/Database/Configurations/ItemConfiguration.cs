using BattleQuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BattleQuest.Infrastructure.Database.Configurations
{
	public class ItemConfiguration : IEntityTypeConfiguration<Item>
	{
		public void Configure(EntityTypeBuilder<Item> builder)
		{
			builder.ToTable("Item");

			builder.HasKey(i => i.ItemId);

			builder.Property(i => i.Name)
				.IsRequired()
				.HasColumnType("varchar(100)");

			builder.HasIndex(i => i.Name)
				.IsUnique();
		}
	}
}
